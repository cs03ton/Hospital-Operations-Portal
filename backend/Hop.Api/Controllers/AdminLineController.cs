using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/admin/line")]
[Authorize]
[RequireAnyPermission("System.Line.TestSend", "SystemSettings.View")]
public class AdminLineController(
    AppDbContext db,
    LineConfigurationResolver lineConfiguration,
    ILineMessagingService lineMessagingService,
    IUserAvatarUrlResolver avatarUrlResolver,
    IAuditLogService auditLogService,
    IHostEnvironment environment,
    ILogger<AdminLineController> logger) : ControllerBase
{
    [HttpGet("settings")]
    public ActionResult<ApiResponse<LineSettingsResponse>> GetSettings()
    {
        var response = new LineSettingsResponse(
            lineConfiguration.Enabled,
            lineConfiguration.ChannelId,
            lineConfiguration.HasChannelSecret,
            lineConfiguration.HasAccessToken,
            lineConfiguration.TestUserId,
            lineConfiguration.Endpoint,
            lineConfiguration.HasChannelSecret,
            lineConfiguration.HasAccessToken
        );

        logger.LogInformation(
            "LINE settings requested. Enabled={Enabled}, HasChannelSecret={HasChannelSecret}, HasAccessToken={HasAccessToken}, TestUserIdConfigured={TestUserIdConfigured}",
            response.Enabled,
            response.HasChannelSecret,
            response.HasAccessToken,
            !string.IsNullOrWhiteSpace(response.TestUserId));

        return ApiResponse<LineSettingsResponse>.Ok(response);
    }

    [HttpGet("operations-status")]
    public async Task<ActionResult<ApiResponse<LineOperationsStatusResponse>>> GetOperationsStatus(CancellationToken cancellationToken)
    {
        var lastSuccess = await db.LineDeliveryLogs
            .AsNoTracking()
            .Where(item => item.Status == "Sent")
            .OrderByDescending(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
            .Select(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var lastFailure = await db.LineDeliveryLogs
            .AsNoTracking()
            .Where(item => item.Status == "Failed")
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Select(item => item.UpdatedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var queueLength = await db.LineDeliveryLogs.CountAsync(item => item.Status == "Queued", cancellationToken);
        var pendingRetry = await db.LineDeliveryLogs.CountAsync(item => item.Status == "Failed" && item.NextRetryAt != null, cancellationToken);
        var recentDurations = await db.LineDeliveryLogs
            .AsNoTracking()
            .Where(item => item.UpdatedAt != null || item.SentAt != null)
            .OrderByDescending(item => item.UpdatedAt ?? item.SentAt ?? item.CreatedAt)
            .Take(50)
            .Select(item => new { item.CreatedAt, item.UpdatedAt, item.SentAt })
            .ToListAsync(cancellationToken);
        var averageResponse = recentDurations.Count == 0
            ? null
            : (double?)recentDurations.Average(item => Math.Max(0, ((item.SentAt ?? item.UpdatedAt)!.Value - item.CreatedAt).TotalMilliseconds));
        var response = new LineOperationsStatusResponse(
            lineConfiguration.Enabled,
            lineConfiguration.Enabled && lineConfiguration.HasAccessToken ? "Connected" : "Disconnected",
            MaskChannelId(lineConfiguration.ChannelId),
            lineConfiguration.HasChannelSecret,
            lineConfiguration.HasAccessToken,
            !string.IsNullOrWhiteSpace(lineConfiguration.TestUserId),
            MaskLineUserId(lineConfiguration.TestUserId),
            lineConfiguration.Endpoint,
            environment.EnvironmentName,
            IsHttps(lineConfiguration.WebhookUrl),
            null,
            lastSuccess == default ? null : lastSuccess,
            lastFailure == default ? null : lastFailure,
            queueLength,
            pendingRetry,
            averageResponse,
            BuildChecklist());

        logger.LogInformation(
            "LINE operations status requested. Enabled={Enabled}, HasChannelSecret={HasChannelSecret}, HasAccessToken={HasAccessToken}, TestUserIdConfigured={TestUserIdConfigured}",
            response.Enabled,
            response.HasChannelSecret,
            response.HasAccessToken,
            response.HasTestUserId);

        return ApiResponse<LineOperationsStatusResponse>.Ok(response);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<LineConnectionValidationResponse>>> ValidateConnection(CancellationToken cancellationToken)
    {
        var result = await lineMessagingService.ValidateConnectionAsync(BuildChecklist(), cancellationToken);
        await auditLogService.WriteAsync(
            GetCurrentUserId(),
            result.Success ? "Line.ConnectionValidated" : "Line.ConnectionValidationFailed",
            "Line",
            null,
            result.Message,
            result.Success ? "Success" : "Failed",
            HttpContext);
        return ApiResponse<LineConnectionValidationResponse>.Ok(result);
    }

    [HttpGet("test-history")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LineDeliveryLogResponse>>>> GetTestHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return await GetDeliveryLogsInternal(["Line.TestSend", "NotificationSimulator."], null, page, pageSize, search, cancellationToken);
    }

    [HttpGet("delivery-logs")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LineDeliveryLogResponse>>>> GetDeliveryLogs(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return await GetDeliveryLogsInternal([], status, page, pageSize, search, cancellationToken);
    }

    [HttpGet("delivery-logs/{id:guid}")]
    public async Task<ActionResult<ApiResponse<LineDeliveryLogDetailResponse>>> GetDeliveryLog(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.LineDeliveryLogs
            .AsNoTracking()
            .Include(log => log.RecipientUser)
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<LineDeliveryLogDetailResponse>.Fail("ไม่พบรายการส่ง LINE"));
        }

        return ApiResponse<LineDeliveryLogDetailResponse>.Ok(new LineDeliveryLogDetailResponse(
            item.Id,
            item.CreatedAt,
            item.RecipientUser?.FullName ?? TryReadRecipient(item.Payload) ?? "-",
            item.EventName,
            item.Status,
            item.AttemptCount,
            item.ResponseDetail,
            item.NextRetryAt,
            item.SentAt));
    }

    [HttpPost("simulate")]
    public async Task<ActionResult<ApiResponse<LineTestSendResponse>>> Simulate(LineNotificationSimulatorRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<LineTestSendResponse>.Fail("ไม่พบผู้ใช้งานที่เลือก"));
        }

        if (string.IsNullOrWhiteSpace(user.LineUserId))
        {
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail("ผู้ใช้งานนี้ยังไม่ได้ตั้งค่า LINE User ID"));
        }

        var eventType = NormalizeSimulatorEvent(request.EventType);
        var message = string.IsNullOrWhiteSpace(request.Message)
            ? BuildSimulatorMessage(eventType, user.FullName)
            : request.Message.Trim();
        var result = await lineMessagingService.SendTestMessageAsync(user.LineUserId, message, $"NotificationSimulator.{eventType}", cancellationToken);

        await auditLogService.WriteAsync(
            GetCurrentUserId(),
            result.Success ? "Line.NotificationSimulated" : "Line.NotificationSimulationFailed",
            "Line",
            user.Id.ToString(),
            $"{eventType}: {result.Message}",
            result.Success ? "Success" : "Failed",
            HttpContext);

        return result.Success
            ? ApiResponse<LineTestSendResponse>.Ok(result)
            : BadRequest(ApiResponse<LineTestSendResponse>.Fail(result.Message));
    }

    [HttpPost("test-send")]
    public async Task<ActionResult<ApiResponse<LineTestSendResponse>>> TestSend(LineTestSendRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!lineConfiguration.Enabled)
        {
            var disabledResponse = new LineTestSendResponse(false, "ยังไม่ได้เปิดใช้งาน LINE Messaging (LINE_ENABLED=true)", "Disabled", null);
            await auditLogService.WriteAsync(userId, "Line.TestMessageFailed", "Line", null, disabledResponse.Message, "Failed", HttpContext);
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(disabledResponse.Message));
        }

        if (!lineConfiguration.HasAccessToken)
        {
            const string message = "ยังไม่ได้ตั้งค่า LINE Channel Access Token";
            await auditLogService.WriteAsync(userId, "Line.TestMessageFailed", "Line", null, message, "Failed", HttpContext);
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(message));
        }

        var toUserId = string.IsNullOrWhiteSpace(request.ToUserId)
            ? lineConfiguration.TestUserId
            : request.ToUserId;
        if (string.IsNullOrWhiteSpace(toUserId))
        {
            const string message = "กรุณาระบุ LINE_TEST_USER_ID หรือ toUserId สำหรับทดสอบ";
            await auditLogService.WriteAsync(userId, "Line.TestMessageFailed", "Line", null, message, "Failed", HttpContext);
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(message));
        }

        var result = await lineMessagingService.SendTestMessageAsync(
            toUserId,
            string.IsNullOrWhiteSpace(request.Message) ? "ทดสอบการแจ้งเตือนจาก HOP" : request.Message,
            cancellationToken);

        await auditLogService.WriteAsync(
            userId,
            result.Success ? "Line.TestMessageSent" : "Line.TestMessageFailed",
            "LineDeliveryLog",
            result.DeliveryLogId?.ToString(),
            result.Message,
            result.Success ? "Success" : "Failed",
            HttpContext);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(result.Message));
        }

        return ApiResponse<LineTestSendResponse>.Ok(result);
    }

    [HttpGet("flex-preview")]
    public async Task<ActionResult<ApiResponse<LineFlexPreviewResponse>>> PreviewFlex(
        [FromQuery] Guid? leaveRequestId,
        [FromQuery] string? variant,
        [FromQuery] string? avatarMode,
        CancellationToken cancellationToken)
    {
        var leaveRequest = leaveRequestId is null
            ? await GetLatestLeaveRequestAsync(cancellationToken)
            : await GetLeaveRequestAsync(leaveRequestId.Value, cancellationToken);
        var request = ApplyAvatarMode(leaveRequest ?? BuildSampleLeaveRequest(), avatarMode);
        var payload = BuildFlexPayloadByVariant(request, variant);
        var validation = ValidateFlexPayloadInternal(payload).Checks;

        return ApiResponse<LineFlexPreviewResponse>.Ok(new LineFlexPreviewResponse(payload, validation));
    }

    [HttpPost("validate-flex")]
    public ActionResult<ApiResponse<LineFlexValidateResponse>> ValidateFlex(LineFlexValidateRequest request)
    {
        var result = ValidateFlexPayloadInternal(request.Payload);
        return ApiResponse<LineFlexValidateResponse>.Ok(result);
    }

    [HttpPost("test-flex")]
    public async Task<ActionResult<ApiResponse<LineTestSendResponse>>> TestFlex(LineFlexTestSendRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var toUserId = string.IsNullOrWhiteSpace(request.ToUserId)
            ? lineConfiguration.TestUserId
            : request.ToUserId.Trim();
        if (string.IsNullOrWhiteSpace(toUserId))
        {
            const string message = "กรุณาระบุ LINE_TEST_USER_ID หรือ LINE User ID สำหรับทดสอบ Flex";
            await auditLogService.WriteAsync(userId, "Line.TestFlexFailed", "Line", null, message, "Failed", HttpContext);
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(message));
        }

        var payload = request.Payload;
        if (string.IsNullOrWhiteSpace(payload))
        {
            var leaveRequest = request.LeaveRequestId is null
                ? await GetLatestLeaveRequestAsync(cancellationToken)
                : await GetLeaveRequestAsync(request.LeaveRequestId.Value, cancellationToken);
            payload = BuildFlexPayloadByVariant(ApplyAvatarMode(leaveRequest ?? BuildSampleLeaveRequest(), request.AvatarMode), request.Variant);
        }

        var validation = ValidateFlexPayloadInternal(payload);
        if (!validation.IsValid)
        {
            await auditLogService.WriteAsync(userId, "Line.TestFlexFailed", "Line", null, validation.Message, "Failed", HttpContext);
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(validation.Message));
        }

        var result = await lineMessagingService.SendRawPayloadToLineUserAsync(
            toUserId,
            payload,
            "Line.TestFlex",
            request.LeaveRequestId,
            cancellationToken);

        await auditLogService.WriteAsync(
            userId,
            result.Success ? "Line.TestFlexSent" : "Line.TestFlexFailed",
            "LineDeliveryLog",
            result.DeliveryLogId?.ToString(),
            result.Error ?? result.Message,
            result.Success ? "Success" : "Failed",
            HttpContext);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<LineTestSendResponse>.Fail(result.Error ?? result.Message));
        }

        return ApiResponse<LineTestSendResponse>.Ok(result);
    }

    [HttpGet("line-users")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LineUserBindingResponse>>>> GetLineUsers(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.LineUserBindings
            .AsNoTracking()
            .Include(item => item.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(item =>
                item.LineUserId.ToLower().Contains(keyword) ||
                (item.DisplayName != null && item.DisplayName.ToLower().Contains(keyword)) ||
                (item.User != null && (
                    item.User.FullName.ToLower().Contains(keyword) ||
                    item.User.Username.ToLower().Contains(keyword))));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.LastEventAt ?? item.UpdatedAt ?? item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new LineUserBindingResponse(
                item.Id,
                LineUserBindingService.MaskLineUserId(item.LineUserId),
                item.DisplayName,
                item.PictureUrl,
                item.UserId,
                item.User != null ? item.User.FullName : null,
                item.User != null ? item.User.Username : null,
                item.Status,
                item.LastEventType,
                item.LastEventAt,
                item.BoundAt,
                item.UnboundAt,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<LineUserBindingResponse>>.Ok(new PagedResponse<LineUserBindingResponse>(
            items,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    [HttpGet("line-users/stats")]
    public async Task<ActionResult<ApiResponse<LineUserBindingStatsResponse>>> GetLineUserStats(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = await db.LineConnectTokens.CountAsync(item => item.Status == "Pending" && item.ExpiresAt > now, cancellationToken);
        var expired = await db.LineConnectTokens.CountAsync(item => item.Status == "Pending" && item.ExpiresAt <= now || item.Status == "Expired", cancellationToken);
        var recentlyBound = await db.LineUserBindings.CountAsync(item => item.Status == "Bound" && item.BoundAt != null && item.BoundAt >= now.AddDays(-7), cancellationToken);
        return ApiResponse<LineUserBindingStatsResponse>.Ok(new LineUserBindingStatsResponse(pending, expired, recentlyBound));
    }

    [HttpPost("line-users/{id:guid}/test-send")]
    public async Task<ActionResult<ApiResponse<LineTestSendResponse>>> SendLineUserTest(Guid id, LineTestSendRequest request, CancellationToken cancellationToken)
    {
        var binding = await db.LineUserBindings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (binding is null)
        {
            return NotFound(ApiResponse<LineTestSendResponse>.Fail("ไม่พบผู้ใช้ LINE"));
        }

        var result = await lineMessagingService.SendTestMessageAsync(
            binding.LineUserId,
            string.IsNullOrWhiteSpace(request.Message) ? "ทดสอบการแจ้งเตือนจาก HOP" : request.Message,
            "Line.AdminUserTest",
            cancellationToken);

        await auditLogService.WriteAsync(
            GetCurrentUserId(),
            result.Success ? "Line.AdminUserTestSent" : "Line.AdminUserTestFailed",
            "LineUserBinding",
            id.ToString(),
            result.Message,
            result.Success ? "Success" : "Failed",
            HttpContext);

        return result.Success
            ? ApiResponse<LineTestSendResponse>.Ok(result)
            : BadRequest(ApiResponse<LineTestSendResponse>.Fail(result.Message));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private async Task<ActionResult<ApiResponse<PagedResponse<LineDeliveryLogResponse>>>> GetDeliveryLogsInternal(
        IReadOnlyList<string> eventPrefixes,
        string? status,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.LineDeliveryLogs
            .AsNoTracking()
            .Include(item => item.RecipientUser)
            .AsQueryable();

        if (eventPrefixes.Count > 0)
        {
            query = query.Where(item =>
                item.EventName.StartsWith("Line.TestSend") ||
                item.EventName.StartsWith("NotificationSimulator."));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(item =>
                item.EventName.ToLower().Contains(keyword) ||
                item.Status.ToLower().Contains(keyword) ||
                (item.RecipientUser != null && item.RecipientUser.FullName.ToLower().Contains(keyword)) ||
                item.Payload.ToLower().Contains(keyword));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var lineUserIds = logs
            .Select(item => TryReadRecipient(item.Payload))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var usersByLineId = lineUserIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : await db.Users
                .AsNoTracking()
                .Where(user => user.LineUserId != null && lineUserIds.Contains(user.LineUserId))
                .Select(user => new { user.LineUserId, user.FullName })
                .GroupBy(item => item.LineUserId!)
                .Select(group => new
                {
                    LineUserId = group.Key,
                    FullName = group
                        .OrderBy(item => item.FullName)
                        .Select(item => item.FullName)
                        .First()
                })
                .ToDictionaryAsync(item => item.LineUserId, item => item.FullName, StringComparer.Ordinal, cancellationToken);
        var bindingsByLineId = lineUserIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : await db.LineUserBindings
                .AsNoTracking()
                .Include(item => item.User)
                .Where(item => lineUserIds.Contains(item.LineUserId))
                .Select(item => new
                {
                    item.LineUserId,
                    Name = item.User != null ? item.User.FullName : item.DisplayName
                })
                .GroupBy(item => item.LineUserId)
                .Select(group => new
                {
                    LineUserId = group.Key,
                    Name = group
                        .OrderByDescending(item => item.Name != null)
                        .Select(item => item.Name)
                        .FirstOrDefault() ?? "-"
                })
                .ToDictionaryAsync(item => item.LineUserId, item => item.Name, StringComparer.Ordinal, cancellationToken);
        var items = logs.Select(item =>
        {
            var lineUserId = TryReadRecipient(item.Payload);
            var recipientName = item.RecipientUser?.FullName ??
                (!string.IsNullOrWhiteSpace(lineUserId) && usersByLineId.TryGetValue(lineUserId, out var userName) ? userName : null) ??
                (!string.IsNullOrWhiteSpace(lineUserId) && bindingsByLineId.TryGetValue(lineUserId, out var bindingName) ? bindingName : null) ??
                MaskLineUserId(lineUserId) ??
                "-";

            return new LineDeliveryLogResponse(
                item.Id,
                item.CreatedAt,
                recipientName,
                item.EventName.StartsWith("LeaveRequest.") ? "Leave" : item.EventName.StartsWith("NotificationSimulator.") ? "Notification" : "Line",
                item.EventName,
                item.Status,
                item.AttemptCount,
                item.UpdatedAt == null && item.SentAt == null ? null : Math.Max(0, (long)((item.SentAt ?? item.UpdatedAt)!.Value - item.CreatedAt).TotalMilliseconds),
                item.Status == "Sent" ? null : item.ResponseDetail,
                TryReadRequestType(item.Payload),
                MaskLineUserId(lineUserId),
                BuildPayloadPreview(item.Payload),
                ExtractHttpStatusCode(item.ResponseDetail),
                ExtractResponseBody(item.ResponseDetail));
        }).ToList();

        return ApiResponse<PagedResponse<LineDeliveryLogResponse>>.Ok(new PagedResponse<LineDeliveryLogResponse>(
            items,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    private IReadOnlyList<LineChecklistItemResponse> BuildChecklist()
    {
        return
        [
            new("LINE Enabled", lineConfiguration.Enabled, "ตั้งค่า Line__Enabled=true"),
            new("Channel ID", !string.IsNullOrWhiteSpace(lineConfiguration.ChannelId), "ตั้งค่า Line__ChannelId"),
            new("Access Token Loaded", lineConfiguration.HasAccessToken, "ตั้งค่า Line__AccessToken ผ่าน Environment Variables หรือ Secret Manager"),
            new("Channel Secret Loaded", lineConfiguration.HasChannelSecret, "ตั้งค่า Line__ChannelSecret ผ่าน Environment Variables หรือ Secret Manager"),
            new("Test User Configured", !string.IsNullOrWhiteSpace(lineConfiguration.TestUserId), "ตั้งค่า Line__TestUserId สำหรับส่งข้อความทดสอบ"),
            new("Delivery Log", true, "ระบบพร้อมบันทึก line_delivery_logs"),
            new("Test Send API", true, "POST /api/admin/line/test-send พร้อมใช้งาน"),
            new("HTTPS Endpoint", IsHttps(lineConfiguration.Endpoint), "Production ควรใช้ HTTPS endpoint"),
            new("Push API Reachable", lineConfiguration.Enabled && lineConfiguration.HasAccessToken, "กดตรวจสอบการเชื่อมต่อเพื่อยืนยันกับ LINE API"),
            new("Webhook Verified", IsHttps(lineConfiguration.WebhookUrl), "ตรวจสอบ webhook URL ใน LINE Developers Console")
        ];
    }

    private static string? MaskChannelId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= 6 ? "******" : $"{value[..4]}******{value[^2..]}";
    }

    private static string? MaskLineUserId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= 10 ? "U********" : $"{value[..5]}...{value[^4..]}";
    }

    private static bool IsHttps(string? endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    private static string NormalizeSimulatorEvent(string eventType)
    {
        return eventType switch
        {
            "LeaveSubmitted" or "LeaveApproved" or "LeaveRejected" or "PendingApproval" or "Cancelled" or "Reminder" => eventType,
            _ => "Reminder"
        };
    }

    private static string BuildSimulatorMessage(string eventType, string fullname)
    {
        var message = eventType switch
        {
            "LeaveSubmitted" => "ระบบได้รับคำขอลาเรียบร้อยแล้ว",
            "LeaveApproved" => "คำขอลาของคุณได้รับการอนุมัติแล้ว",
            "LeaveRejected" => "คำขอลาของคุณไม่ได้รับการอนุมัติ",
            "PendingApproval" => "มีคำขอลารออนุมัติจากคุณ",
            "Cancelled" => "คำขอลาถูกยกเลิกเรียบร้อยแล้ว",
            _ => "แจ้งเตือนทดสอบจาก Hospital Operations Portal"
        };

        return $"ทดสอบแจ้งเตือน: {message}\nผู้รับ: {fullname}";
    }

    private static string? TryReadRecipient(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("to", out var to) ? to.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadRequestType(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array ||
                messages.GetArrayLength() == 0)
            {
                return null;
            }

            return messages[0].TryGetProperty("type", out var type) ? type.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? BuildPayloadPreview(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(payload);
            if (node is JsonObject root && root["to"] is not null)
            {
                root["to"] = MaskLineUserId(root["to"]?.GetValue<string>());
            }

            var preview = node?.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? payload;
            return preview.Length <= 500 ? preview : $"{preview[..500]}...";
        }
        catch (JsonException)
        {
            return payload.Length <= 500 ? payload : $"{payload[..500]}...";
        }
    }

    private static int? ExtractHttpStatusCode(string? responseDetail)
    {
        if (string.IsNullOrWhiteSpace(responseDetail))
        {
            return null;
        }

        var prefix = responseDetail.StartsWith("HTTP ", StringComparison.OrdinalIgnoreCase)
            ? "HTTP "
            : responseDetail.StartsWith("Recipient validation HTTP ", StringComparison.OrdinalIgnoreCase)
                ? "Recipient validation HTTP "
                : null;
        if (prefix is null)
        {
            return null;
        }

        var statusText = responseDetail[prefix.Length..].Split([' ', ':', '('], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(statusText, out var statusCode) ? statusCode : null;
    }

    private static string? ExtractResponseBody(string? responseDetail)
    {
        if (string.IsNullOrWhiteSpace(responseDetail))
        {
            return null;
        }

        var markerIndex = responseDetail.IndexOf("): ", StringComparison.Ordinal);
        if (markerIndex >= 0 && markerIndex + 3 < responseDetail.Length)
        {
            return responseDetail[(markerIndex + 3)..];
        }

        var validationMarker = responseDetail.IndexOf(": ", StringComparison.Ordinal);
        return validationMarker >= 0 && responseDetail.StartsWith("Recipient validation HTTP ", StringComparison.OrdinalIgnoreCase)
            ? responseDetail[(validationMarker + 2)..]
            : responseDetail;
    }

    private async Task<LeaveRequest?> GetLeaveRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.Approvals)
                .ThenInclude(item => item.Approver)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    private async Task<LeaveRequest?> GetLatestLeaveRequestAsync(CancellationToken cancellationToken)
    {
        return await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.Approvals)
                .ThenInclude(item => item.Approver)
            .OrderByDescending(item => item.SubmittedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static LeaveRequest BuildSampleLeaveRequest()
    {
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        return new LeaveRequest
        {
            Id = requestId,
            RequestNumber = "LV-202606-001",
            UserId = userId,
            LeaveTypeId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            TotalDays = 2,
            DurationType = "FULL_DAY",
            Reason = "ทดสอบ Flex Message",
            Status = "Pending",
            User = new User
            {
                Id = userId,
                FullName = "เจ้าหน้าที่ทดสอบ ระบบลา",
                Username = "line.preview",
                Department = new Department { Id = Guid.NewGuid(), Name = "Information Technology" }
            },
            LeaveType = new LeaveType
            {
                Id = Guid.NewGuid(),
                Code = "annual",
                Name = "ลาพักผ่อน"
            },
            Approvals =
            [
                new LeaveApproval
                {
                    Id = Guid.NewGuid(),
                    LeaveRequestId = requestId,
                    ApproverId = Guid.NewGuid(),
                    StepOrder = 1,
                    StepName = "หัวหน้าหน่วยงาน",
                    Status = "Pending"
                }
            ]
        };
    }

    private string BuildFlexPayloadByVariant(LeaveRequest request, string? variant)
    {
        var avatar = avatarUrlResolver.ResolveForLine(request.User);
        return NormalizeFlexVariant(variant) switch
        {
            "approved" => LeaveLineFlexMessageTemplates.BuildApprovedCard(request, lineConfiguration.PublicAppUrl, avatar),
            "rejected" => LeaveLineFlexMessageTemplates.BuildRejectedCard(request, lineConfiguration.PublicAppUrl, avatar),
            "cancelled" => LeaveLineFlexMessageTemplates.BuildCancelledCard(request, lineConfiguration.PublicAppUrl, avatar),
            _ => LeaveLineFlexMessageTemplates.BuildPendingApprovalCard(request, lineConfiguration.PublicAppUrl, avatar)
        };
    }

    private static LeaveRequest ApplyAvatarMode(LeaveRequest request, string? avatarMode)
    {
        if (request.User is null)
        {
            return request;
        }

        if (string.Equals(avatarMode, "without-image", StringComparison.OrdinalIgnoreCase))
        {
            request.User.ProfileImagePath = null;
            request.User.ProfileImageUpdatedAt = null;
            return request;
        }

        if (string.Equals(avatarMode, "with-image", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.User.ProfileImagePath))
        {
            request.User.ProfileImagePath = $"storage/profile-images/{request.User.Id:N}/avatar.webp";
            request.User.ProfileImageUpdatedAt = DateTime.UtcNow;
        }

        return request;
    }

    private static string NormalizeFlexVariant(string? variant)
    {
        return variant?.Trim().ToLowerInvariant() switch
        {
            "approved" => "approved",
            "rejected" => "rejected",
            "cancelled" => "cancelled",
            _ => "pending"
        };
    }

    private LineFlexValidateResponse ValidateFlexPayloadInternal(string? payload)
    {
        var checks = new List<LineChecklistItemResponse>();
        if (string.IsNullOrWhiteSpace(payload))
        {
            checks.Add(new("Payload JSON", false, "กรุณาระบุ Flex JSON"));
            return new LineFlexValidateResponse(false, "Flex JSON ว่าง", checks);
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(payload)?.AsObject();
            checks.Add(new("Payload JSON", root is not null, root is null ? "Payload ต้องเป็น JSON object" : "JSON ถูกต้อง"));
        }
        catch (JsonException ex)
        {
            checks.Add(new("Payload JSON", false, ex.Message));
            return new LineFlexValidateResponse(false, $"Flex JSON ไม่ถูกต้อง: {ex.Message}", checks);
        }

        if (root is null)
        {
            return new LineFlexValidateResponse(false, "Payload ต้องเป็น JSON object", checks);
        }

        var messages = root["messages"] as JsonArray;
        checks.Add(new("messages", messages is { Count: > 0 }, "ต้องมี messages อย่างน้อย 1 รายการ"));
        var message = messages?.FirstOrDefault() as JsonObject;
        var type = message?["type"]?.GetValue<string>();
        checks.Add(new("type:flex", string.Equals(type, "flex", StringComparison.Ordinal), "message.type ต้องเป็น flex"));

        var altText = message?["altText"]?.GetValue<string>();
        checks.Add(new("altText", !string.IsNullOrWhiteSpace(altText) && altText.Length <= 400, "altText ต้องไม่ว่างและไม่เกิน 400 ตัวอักษร"));

        var contents = message?["contents"] as JsonObject;
        var bubbleType = contents?["type"]?.GetValue<string>();
        checks.Add(new("bubble structure", string.Equals(bubbleType, "bubble", StringComparison.Ordinal), "contents.type ต้องเป็น bubble"));

        var footerContents = contents?["footer"]?["contents"] as JsonArray;
        checks.Add(new("footer action", footerContents is { Count: > 0 }, "footer ต้องมีปุ่ม action อย่างน้อย 1 ปุ่ม"));

        var uriActions = new List<string>();
        if (footerContents is not null)
        {
            foreach (var footerItem in footerContents.OfType<JsonObject>())
            {
                var action = footerItem["action"] as JsonObject;
                if (string.Equals(action?["type"]?.GetValue<string>(), "uri", StringComparison.Ordinal))
                {
                    var uri = action?["uri"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        uriActions.Add(uri);
                    }
                }
            }
        }

        var urlsValid = uriActions.Count > 0 && uriActions.All(item =>
            Uri.TryCreate(item, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || IsLocalhost(uri)));
        checks.Add(new("URI action", urlsValid, "URI action ต้องเป็น absolute URL และ production ควรใช้ HTTPS"));

        var hasLocalhost = uriActions.Any(item => Uri.TryCreate(item, UriKind.Absolute, out var uri) && IsLocalhost(uri));
        checks.Add(new("Action URL", !hasLocalhost, "ห้ามใช้ localhost/127.0.0.1 เป็น PublicAppUrl เมื่อส่งให้ LINE จริง"));

        var imageUrls = FindImageUrls(contents).ToList();
        var imagesValid = imageUrls.Count == 0 || imageUrls.All(item =>
            Uri.TryCreate(item, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            !IsPrivateOrLocalNetwork(uri));
        checks.Add(new("image URL", imagesValid, "image component ต้องใช้ HTTPS public URL หรือไม่ส่ง image component"));

        var fatalCheckLabels = new HashSet<string>(StringComparer.Ordinal)
        {
            "Payload JSON",
            "messages",
            "type:flex",
            "altText",
            "bubble structure",
            "footer action",
            "URI action",
            "image URL"
        };
        var isValid = checks.Where(item => fatalCheckLabels.Contains(item.Label)).All(item => item.Passed);
        return new LineFlexValidateResponse(
            isValid,
            isValid ? "Flex payload ผ่านการตรวจสอบเบื้องต้น" : "Flex payload ยังไม่พร้อมส่ง โปรดตรวจรายการที่ไม่ผ่าน",
            checks);
    }

    private static bool IsLocalhost(Uri uri)
    {
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivateOrLocalNetwork(Uri uri)
    {
        if (IsLocalhost(uri))
        {
            return true;
        }

        var host = uri.Host;
        return host.StartsWith("10.", StringComparison.Ordinal) ||
            host.StartsWith("192.168.", StringComparison.Ordinal) ||
            host.StartsWith("172.16.", StringComparison.Ordinal) ||
            host.StartsWith("172.17.", StringComparison.Ordinal) ||
            host.StartsWith("172.18.", StringComparison.Ordinal) ||
            host.StartsWith("172.19.", StringComparison.Ordinal) ||
            host.StartsWith("172.20.", StringComparison.Ordinal) ||
            host.StartsWith("172.21.", StringComparison.Ordinal) ||
            host.StartsWith("172.22.", StringComparison.Ordinal) ||
            host.StartsWith("172.23.", StringComparison.Ordinal) ||
            host.StartsWith("172.24.", StringComparison.Ordinal) ||
            host.StartsWith("172.25.", StringComparison.Ordinal) ||
            host.StartsWith("172.26.", StringComparison.Ordinal) ||
            host.StartsWith("172.27.", StringComparison.Ordinal) ||
            host.StartsWith("172.28.", StringComparison.Ordinal) ||
            host.StartsWith("172.29.", StringComparison.Ordinal) ||
            host.StartsWith("172.30.", StringComparison.Ordinal) ||
            host.StartsWith("172.31.", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindImageUrls(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (string.Equals(obj["type"]?.GetValue<string>(), "image", StringComparison.Ordinal) &&
                obj["url"]?.GetValue<string>() is string url &&
                !string.IsNullOrWhiteSpace(url))
            {
                yield return url;
            }

            foreach (var child in obj.Select(item => item.Value))
            {
                foreach (var nestedUrl in FindImageUrls(child))
                {
                    yield return nestedUrl;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                foreach (var nestedUrl in FindImageUrls(child))
                {
                    yield return nestedUrl;
                }
            }
        }
    }
}
