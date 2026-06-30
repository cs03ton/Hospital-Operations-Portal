using Hop.Api.Configuration;
using Hop.Api.DTOs;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;

namespace Hop.Api.Services;

public sealed class LineMessagingService(
    AppDbContext db,
    IConfiguration configuration,
    LineConfigurationResolver lineConfiguration,
    HttpClient httpClient,
    ILogger<LineMessagingService> logger) : ILineMessagingService
{
    public async Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default)
    {
        var enabled = IsLineEnabled();
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

    public async Task NotifyUserAsync(Guid userId, string eventName, string message, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            to = string.Empty,
            messages = new[]
            {
                new
                {
                    type = "text",
                    text = message
                }
            }
        });
        await NotifyUserPayloadAsync(userId, eventName, payload, leaveRequestId, cancellationToken);
    }

    public async Task NotifyUserPayloadAsync(Guid userId, string eventName, string payload, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
    {
        var enabled = IsLineEnabled();
        var deliveryLog = new LineDeliveryLog
        {
            LeaveRequestId = leaveRequestId,
            RecipientUserId = userId,
            EventName = eventName,
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
            logger.LogInformation("LINE notification disabled: event {EventName} leave request {LeaveRequestId}.", eventName, leaveRequestId);
            return;
        }

        await SendAsync(deliveryLog, userId, payload, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, CancellationToken cancellationToken = default)
    {
        return await SendTestMessageAsync(toUserId, message, "Line.TestSend", cancellationToken);
    }

    public async Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, string eventName, CancellationToken cancellationToken = default)
    {
        if (!IsLineEnabled())
        {
            return new LineTestSendResponse(false, "ยังไม่ได้เปิดใช้งาน LINE Messaging (LINE_ENABLED=true)", "Disabled", null);
        }

        if (string.IsNullOrWhiteSpace(GetAccessToken()))
        {
            return new LineTestSendResponse(false, "ยังไม่ได้ตั้งค่า LINE Channel Access Token", "Failed", null);
        }

        if (string.IsNullOrWhiteSpace(toUserId))
        {
            return new LineTestSendResponse(false, "กรุณาระบุ LINE User ID สำหรับทดสอบ", "Failed", null);
        }

        var payload = JsonSerializer.Serialize(new
        {
            to = toUserId.Trim(),
            messages = new[]
            {
                new
                {
                    type = "text",
                    text = string.IsNullOrWhiteSpace(message) ? "ทดสอบการแจ้งเตือนจาก HOP" : message.Trim()
                }
            }
        });

        var deliveryLog = new LineDeliveryLog
        {
            EventName = string.IsNullOrWhiteSpace(eventName) ? "Line.TestSend" : eventName.Trim(),
            Status = "Queued",
            Payload = payload,
            ResponseDetail = "Queued for LINE test delivery.",
            AttemptCount = 0,
            NextRetryAt = DateTime.UtcNow
        };

        db.LineDeliveryLogs.Add(deliveryLog);
        await db.SaveChangesAsync(cancellationToken);

        await SendRawAsync(deliveryLog, payload, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var success = deliveryLog.Status == "Sent";
        var responseTimeMs = GetDurationMs(deliveryLog);
        return new LineTestSendResponse(
            success,
            success ? "ส่งข้อความทดสอบสำเร็จ" : "ส่งข้อความทดสอบไม่สำเร็จ",
            deliveryLog.Status,
            deliveryLog.Id,
            ExtractHttpStatusCode(deliveryLog.ResponseDetail),
            responseTimeMs,
            success ? null : deliveryLog.ResponseDetail);
    }

    public async Task<LineTestSendResponse> SendRawPayloadToLineUserAsync(
        string toUserId,
        string payload,
        string eventName,
        Guid? leaveRequestId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsLineEnabled())
        {
            return new LineTestSendResponse(false, "ยังไม่ได้เปิดใช้งาน LINE Messaging (LINE_ENABLED=true)", "Disabled", null);
        }

        if (string.IsNullOrWhiteSpace(GetAccessToken()))
        {
            return new LineTestSendResponse(false, "ยังไม่ได้ตั้งค่า LINE Channel Access Token", "Failed", null);
        }

        if (string.IsNullOrWhiteSpace(toUserId))
        {
            return new LineTestSendResponse(false, "กรุณาระบุ LINE User ID สำหรับทดสอบ", "Failed", null);
        }

        var requestPayload = TrySetRecipient(payload, toUserId.Trim(), out var normalizedPayload, out var error)
            ? normalizedPayload
            : null;
        if (requestPayload is null)
        {
            return new LineTestSendResponse(false, error ?? "Flex JSON ไม่ถูกต้อง", "Failed", null);
        }

        var deliveryLog = new LineDeliveryLog
        {
            LeaveRequestId = leaveRequestId,
            EventName = string.IsNullOrWhiteSpace(eventName) ? "Line.TestFlex" : eventName.Trim(),
            Status = "Queued",
            Payload = requestPayload,
            ResponseDetail = "Queued for LINE Flex test delivery.",
            AttemptCount = 0,
            NextRetryAt = DateTime.UtcNow
        };

        db.LineDeliveryLogs.Add(deliveryLog);
        await db.SaveChangesAsync(cancellationToken);

        await SendRawAsync(deliveryLog, requestPayload, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var success = deliveryLog.Status == "Sent";
        return new LineTestSendResponse(
            success,
            success ? "ส่ง Flex Message ทดสอบสำเร็จ" : "ส่ง Flex Message ทดสอบไม่สำเร็จ",
            deliveryLog.Status,
            deliveryLog.Id,
            ExtractHttpStatusCode(deliveryLog.ResponseDetail),
            GetDurationMs(deliveryLog),
            success ? null : deliveryLog.ResponseDetail);
    }

    public async Task<LineConnectionValidationResponse> ValidateConnectionAsync(IReadOnlyList<LineChecklistItemResponse> checklist, CancellationToken cancellationToken = default)
    {
        if (!IsLineEnabled())
        {
            return new LineConnectionValidationResponse(false, "ยังไม่ได้เปิดใช้งาน LINE Messaging API", null, 0, null, checklist);
        }

        var accessToken = GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new LineConnectionValidationResponse(false, "ยังไม่ได้ตั้งค่า LINE Channel Access Token", null, 0, null, checklist);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.line.me/v2/bot/info");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();
            var botName = TryReadBotName(responseText);

            return new LineConnectionValidationResponse(
                response.IsSuccessStatusCode,
                response.IsSuccessStatusCode ? "ตรวจสอบการเชื่อมต่อ LINE สำเร็จ" : $"ตรวจสอบไม่สำเร็จ: HTTP {(int)response.StatusCode}",
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                botName,
                checklist);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "LINE connection validation failed.");
            return new LineConnectionValidationResponse(false, ex.Message, null, stopwatch.ElapsedMilliseconds, null, checklist);
        }
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
        if (string.IsNullOrWhiteSpace(GetAccessToken()))
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

            await SendRawAsync(deliveryLog, requestPayload, cancellationToken);
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

    private async Task SendRawAsync(LineDeliveryLog deliveryLog, string requestPayload, CancellationToken cancellationToken)
    {
        var accessToken = GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            MarkFailed(deliveryLog, "LINE access token is not configured.");
            logger.LogWarning("LINE delivery failed: access token missing for delivery {DeliveryLogId}.", deliveryLog.Id);
            return;
        }

        var endpoint = lineConfiguration.Endpoint;

        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();
            deliveryLog.AttemptCount += 1;
            deliveryLog.ResponseDetail = $"HTTP {(int)response.StatusCode} ({stopwatch.ElapsedMilliseconds} ms): {responseText}";
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
            logger.LogWarning(ex, "LINE delivery failed for delivery {DeliveryLogId}.", deliveryLog.Id);
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

    private bool IsLineEnabled()
    {
        return lineConfiguration.Enabled;
    }

    private string? GetAccessToken()
    {
        return lineConfiguration.AccessToken;
    }

    private static long? GetDurationMs(LineDeliveryLog deliveryLog)
    {
        var completedAt = deliveryLog.SentAt ?? deliveryLog.UpdatedAt;
        if (completedAt is null)
        {
            return null;
        }

        return Math.Max(0, (long)(completedAt.Value - deliveryLog.CreatedAt).TotalMilliseconds);
    }

    private static int? ExtractHttpStatusCode(string? responseDetail)
    {
        if (string.IsNullOrWhiteSpace(responseDetail) || !responseDetail.StartsWith("HTTP ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var statusText = responseDetail[5..].Split([' ', ':', '('], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(statusText, out var statusCode) ? statusCode : null;
    }

    private static string? TryReadBotName(string responseText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            return document.RootElement.TryGetProperty("displayName", out var displayName)
                ? displayName.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TrySetRecipient(string payload, string toUserId, out string normalizedPayload, out string? error)
    {
        normalizedPayload = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "กรุณาระบุ Flex JSON";
            return false;
        }

        try
        {
            var root = JsonNode.Parse(payload)?.AsObject();
            if (root is null)
            {
                error = "Flex JSON ต้องเป็น object";
                return false;
            }

            if (root["messages"] is not JsonArray messages || messages.Count == 0)
            {
                error = "Flex JSON ต้องมี messages อย่างน้อย 1 รายการ";
                return false;
            }

            root["to"] = toUserId;
            normalizedPayload = root.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Flex JSON ไม่ถูกต้อง: {ex.Message}";
            return false;
        }
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
