using Hop.Api.DTOs;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace Hop.Api.Services;

public sealed class LineMessagingService(
    AppDbContext db,
    IConfiguration configuration,
    HttpClient httpClient,
    ILogger<LineMessagingService> logger) : ILineMessagingService
{
    public async Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue("Line:Enabled", false);
        var payload = BuildPayload(message);
        var deliveryLog = new LineDeliveryLog
        {
            LeaveRequestId = message.LeaveRequestId,
            RecipientUserId = message.UserId,
            EventName = $"LeaveRequest.{message.Status}",
            Status = enabled ? "Queued" : "Disabled",
            Payload = payload,
            ResponseDetail = enabled ? "Queued for LINE Messaging delivery." : "LINE delivery is disabled.",
            AttemptCount = 0,
            NextRetryAt = enabled ? DateTime.UtcNow : null
        };

        db.LineDeliveryLogs.Add(deliveryLog);

        await db.SaveChangesAsync(cancellationToken);

        if (!enabled)
        {
            logger.LogInformation("LINE notification disabled: leave request {LeaveRequestId}.", message.LeaveRequestId);
            return;
        }

        await SendAsync(deliveryLog, message.UserId, payload, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "LINE notification delivery recorded: leave request {LeaveRequestId} status {Status}, delivery {DeliveryStatus}.",
            message.LeaveRequestId,
            message.Status,
            deliveryLog.Status);
    }

    public async Task<int> RetryPendingDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var maxAttempts = Math.Max(1, configuration.GetValue("LineRetry:MaxAttempts", 3));
        var now = DateTime.UtcNow;
        var logs = await db.LineDeliveryLogs
            .Where(item => (item.Status == "Failed" || item.Status == "Queued") &&
                item.AttemptCount < maxAttempts &&
                (item.NextRetryAt == null || item.NextRetryAt <= now))
            .OrderBy(item => item.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            if (log.RecipientUserId is null)
            {
                MarkFailed(log, "Recipient user id is not configured.");
                continue;
            }

            await SendAsync(log, log.RecipientUserId.Value, log.Payload, cancellationToken);
            if (log.AttemptCount >= maxAttempts && log.Status != "Sent")
            {
                log.Status = "Failed";
                log.NextRetryAt = null;
                log.ResponseDetail = $"{log.ResponseDetail} Max retry attempts reached.";
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return logs.Count;
    }

    private async Task SendAsync(LineDeliveryLog deliveryLog, Guid recipientUserId, string payload, CancellationToken cancellationToken)
    {
        var accessToken = configuration["Line:ChannelAccessToken"] ?? configuration["LINE_ACCESS_TOKEN"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            MarkFailed(deliveryLog, "LINE access token is not configured.");
            logger.LogWarning("LINE delivery failed: access token missing for delivery {DeliveryLogId} leave request {LeaveRequestId}.", deliveryLog.Id, deliveryLog.LeaveRequestId);
            return;
        }

        var recipient = await db.Users
            .AsNoTracking()
            .Where(user => user.Id == recipientUserId)
            .Select(user => user.LineUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(recipient))
        {
            MarkFailed(deliveryLog, "Recipient LINE user id is not configured.");
            logger.LogWarning("LINE delivery failed: recipient LINE user id missing for delivery {DeliveryLogId} recipient user {RecipientUserId}.", deliveryLog.Id, recipientUserId);
            return;
        }

        try
        {
            var requestPayload = payload.Contains("\"to\":\"\"", StringComparison.Ordinal)
                ? payload.Replace("\"to\":\"\"", $"\"to\":\"{recipient}\"", StringComparison.Ordinal)
                : payload;
            deliveryLog.Payload = requestPayload;

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            deliveryLog.AttemptCount += 1;
            deliveryLog.ResponseDetail = $"HTTP {(int)response.StatusCode}: {responseText}";
            deliveryLog.UpdatedAt = DateTime.UtcNow;

            if (response.IsSuccessStatusCode)
            {
                deliveryLog.Status = "Sent";
                deliveryLog.SentAt = DateTime.UtcNow;
                deliveryLog.NextRetryAt = null;
                return;
            }

            deliveryLog.Status = "Failed";
            deliveryLog.NextRetryAt = DateTime.UtcNow.AddMinutes(GetRetryDelayMinutes(deliveryLog.AttemptCount));
            logger.LogWarning(
                "LINE delivery failed for delivery {DeliveryLogId} leave request {LeaveRequestId}. StatusCode={StatusCode}",
                deliveryLog.Id,
                deliveryLog.LeaveRequestId,
                (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            deliveryLog.AttemptCount += 1;
            deliveryLog.Status = "Failed";
            deliveryLog.ResponseDetail = ex.Message;
            deliveryLog.NextRetryAt = DateTime.UtcNow.AddMinutes(GetRetryDelayMinutes(deliveryLog.AttemptCount));
            deliveryLog.UpdatedAt = DateTime.UtcNow;
            logger.LogWarning(ex, "LINE delivery failed for leave request {LeaveRequestId}.", deliveryLog.LeaveRequestId);
        }
    }

    private static void MarkFailed(LineDeliveryLog deliveryLog, string reason)
    {
        deliveryLog.Status = "Failed";
        deliveryLog.ResponseDetail = reason;
        deliveryLog.AttemptCount += 1;
        deliveryLog.NextRetryAt = DateTime.UtcNow.AddMinutes(GetRetryDelayMinutes(deliveryLog.AttemptCount));
        deliveryLog.UpdatedAt = DateTime.UtcNow;
    }

    private static int GetRetryDelayMinutes(int attemptCount)
    {
        return Math.Min(60, (int)Math.Pow(2, Math.Max(0, attemptCount - 1)) * 5);
    }

    private static string BuildPayload(LeaveNotificationMessage message)
    {
        var lines = new List<string>
        {
            "แจ้งเตือนคำขอลา",
            $"ผู้ขอ: {message.Fullname}",
            $"ประเภท: {message.LeaveTypeName}",
            $"วันที่: {FormatDate(message.StartDate)} - {FormatDate(message.EndDate)}",
            $"สถานะ: {TranslateStatus(message.Status)}"
        };

        if (!string.IsNullOrWhiteSpace(message.Remark))
        {
            lines.Add($"หมายเหตุ: {message.Remark}");
        }

        var text = string.Join('\n', lines);

        return JsonSerializer.Serialize(new
        {
            to = string.Empty,
            messages = new[]
            {
                new
                {
                    type = "text",
                    text
                }
            }
        });
    }

    private static string FormatDate(DateOnly date)
    {
        return $"{date.Day:00}/{date.Month:00}/{date.Year + 543}";
    }

    private static string TranslateStatus(string status)
    {
        return status switch
        {
            "Pending" => "รออนุมัติ",
            "Approved" => "อนุมัติแล้ว",
            "Rejected" => "ไม่อนุมัติ",
            "Cancelled" => "ยกเลิก",
            _ => status
        };
    }
}
