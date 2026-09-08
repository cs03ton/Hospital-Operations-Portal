using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hop.Api.Configuration;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

namespace Hop.Api.Controllers;

[ApiController, Route("api/fleet/line-groups"), Authorize]
public sealed partial class FleetLineGroupsController(AppDbContext db, ILineGroupPushClient groupLine, IOptions<LineGroupNotificationsOptions> groupOptions, IDataProtectionProvider? dataProtectionProvider = null, IWebHostEnvironment? environment = null) : ControllerBase
{
    private readonly IDataProtector credentialProtector = (dataProtectionProvider ?? new EphemeralDataProtectionProvider()).CreateProtector("HOP.LineGroupDestinationCredentials.v1");
    [HttpGet, RequireAnyPermission(FleetPermissions.LineGroupView, FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> List([FromQuery] string? status, [FromQuery] string? search, CancellationToken ct)
    {
        var query = db.LineGroupDestinations.AsNoTracking().Include(x => x.EventSubscriptions).Where(x => x.Module == "FLEET");
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => EF.Functions.ILike(x.DisplayName, $"%{search.Trim()}%"));
        var rows = await query.OrderBy(x => x.Status).ThenBy(x => x.DisplayName).Select(x => new
        {
            x.Id,
            x.DisplayName,
            GroupIdMasked = LineGroupRegistrationService.Mask(x.LineGroupId),
            x.Status,
            x.Module,
            x.DeliveryProvider,
            x.EndpointUrl,
            x.ClientId,
            HasClientSecret = x.ClientSecretProtected != null,
            x.AttentionRequired,
            x.AttentionReason,
            x.FirstDetectedAt,
            x.LastDetectedAt,
            x.ConfirmedAt,
            x.DisabledAt,
            x.ConcurrencyToken,
            Events = x.EventSubscriptions.OrderBy(s => s.EventType).Select(s => new { s.EventType, s.IsEnabled })
        }).ToListAsync(ct);
        return ApiResponse<object>.Ok(rows);
    }

    [HttpPost, RequirePermission(FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> Create(LineGroupConfigurationRequest body, CancellationToken ct)
    {
        var validation = ValidateConfiguration(body, requireSecret: true, requireGroupId: true);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));
        if (await db.LineGroupDestinations.AnyAsync(x => x.LineGroupId == body.GroupId.Trim(), ct)) return Conflict(ApiResponse<object>.Fail("Group ID นี้มีอยู่ในระบบแล้ว"));
        var now = DateTime.UtcNow;
        var item = new LineGroupDestination
        {
            DisplayName = body.DisplayName.Trim(), LineGroupId = body.GroupId.Trim(), Module = "FLEET", Status = LineGroupDestinationStatuses.Pending,
            DeliveryProvider = "CUSTOM_ENDPOINT", EndpointUrl = body.EndpointUrl.Trim(), ClientId = body.ClientId.Trim(),
            ClientSecretProtected = credentialProtector.Protect(body.ClientSecret!.Trim()), FirstDetectedAt = now, LastDetectedAt = now
        };
        foreach (var definition in FleetLineGroupEvents.Defaults) item.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = definition.Key, IsEnabled = definition.Value });
        db.LineGroupDestinations.Add(item); Audit("Fleet.LineGroupConfigurationCreated", item, Actor(), null); await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.Status, item.ConcurrencyToken });
    }

    [HttpPut("{id:guid}/configuration"), RequirePermission(FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateConfiguration(Guid id, LineGroupConfigurationRequest body, CancellationToken ct)
    {
        var validation = ValidateConfiguration(body, requireSecret: false, requireGroupId: false);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));
        var item = await db.LineGroupDestinations.SingleOrDefaultAsync(x => x.Id == id && x.Module == "FLEET", ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("ไม่พบปลายทาง LINE Group"));
        if (item.ConcurrencyToken != body.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("ข้อมูลถูกแก้ไขโดยผู้ใช้อื่น กรุณาโหลดใหม่"));
        if (!string.IsNullOrWhiteSpace(body.GroupId) && await db.LineGroupDestinations.AnyAsync(x => x.Id != id && x.LineGroupId == body.GroupId.Trim(), ct)) return Conflict(ApiResponse<object>.Fail("Group ID นี้มีอยู่ในระบบแล้ว"));
        item.DisplayName = body.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(body.GroupId)) item.LineGroupId = body.GroupId.Trim();
        item.DeliveryProvider = "CUSTOM_ENDPOINT";
        item.EndpointUrl = body.EndpointUrl.Trim(); item.ClientId = body.ClientId.Trim();
        if (!string.IsNullOrWhiteSpace(body.ClientSecret)) item.ClientSecretProtected = credentialProtector.Protect(body.ClientSecret.Trim());
        item.ConcurrencyToken = Guid.NewGuid(); item.AttentionRequired = false; item.AttentionReason = null;
        Audit("Fleet.LineGroupConfigurationUpdated", item, Actor(), null); await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.Status, item.ConcurrencyToken });
    }

    [HttpPost("{id:guid}/confirm"), RequirePermission(FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> Confirm(Guid id, LineGroupStateRequest body, CancellationToken ct)
    {
        var item = await db.LineGroupDestinations.SingleOrDefaultAsync(x => x.Id == id && x.Module == "FLEET", ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("LINE group destination not found."));
        if (item.ConcurrencyToken != body.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        var actor = Actor();
        item.Status = LineGroupDestinationStatuses.Active;
        item.ConfirmedAt = DateTime.UtcNow;
        item.ConfirmedByUserId = actor;
        item.DisabledAt = null;
        item.DisabledByUserId = null;
        item.AttentionRequired = false;
        item.AttentionReason = null;
        item.ConcurrencyToken = Guid.NewGuid();
        Audit("Fleet.LineGroupConfirmed", item, actor, body.Reason);
        await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.Status, item.ConcurrencyToken });
    }

    [HttpPost("{id:guid}/disable"), RequirePermission(FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> Disable(Guid id, LineGroupStateRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Reason)) return BadRequest(ApiResponse<object>.Fail("Reason is required."));
        var item = await db.LineGroupDestinations.SingleOrDefaultAsync(x => x.Id == id && x.Module == "FLEET", ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("LINE group destination not found."));
        if (item.ConcurrencyToken != body.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        var actor = Actor();
        item.Status = LineGroupDestinationStatuses.Disabled;
        item.DisabledAt = DateTime.UtcNow;
        item.DisabledByUserId = actor;
        item.AttentionRequired = false;
        item.AttentionReason = null;
        item.ConcurrencyToken = Guid.NewGuid();
        Audit("Fleet.LineGroupDisabled", item, actor, body.Reason);
        await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.Status, item.ConcurrencyToken });
    }

    [HttpPut("{id:guid}/subscriptions"), RequirePermission(FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> Subscriptions(Guid id, LineGroupSubscriptionsRequest body, CancellationToken ct)
    {
        var item = await db.LineGroupDestinations.Include(x => x.EventSubscriptions).SingleOrDefaultAsync(x => x.Id == id && x.Module == "FLEET", ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("LINE group destination not found."));
        if (item.ConcurrencyToken != body.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        if (body.Events.Keys.Any(x => !FleetLineGroupEvents.Defaults.ContainsKey(x))) return BadRequest(ApiResponse<object>.Fail("Unsupported Fleet event."));
        foreach (var definition in FleetLineGroupEvents.Defaults)
        {
            var subscription = item.EventSubscriptions.Single(x => x.EventType == definition.Key);
            subscription.IsEnabled = body.Events.GetValueOrDefault(definition.Key, false);
            subscription.UpdatedAt = DateTime.UtcNow;
        }
        item.ConcurrencyToken = Guid.NewGuid();
        Audit("Fleet.LineGroupSubscriptionsUpdated", item, Actor(), null);
        await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.ConcurrencyToken });
    }

    [HttpPost("{id:guid}/test"), RequirePermission(FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> Test(Guid id, LineGroupTestRequest body, CancellationToken ct)
    {
        if (!groupOptions.Value.Enabled) return Conflict(ApiResponse<object>.Fail("LINE group notifications are disabled."));
        var requestedMessage = body.Message?.Trim();
        if (requestedMessage?.Length > 1000) return BadRequest(ApiResponse<object>.Fail("Test message is too long."));
        if (!string.IsNullOrWhiteSpace(requestedMessage) && SensitiveTestMessage().IsMatch(requestedMessage))
            return BadRequest(ApiResponse<object>.Fail("Test message may contain sensitive information."));
        var item = await db.LineGroupDestinations.SingleOrDefaultAsync(x => x.Id == id && x.Module == "FLEET", ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("LINE group destination not found."));
        if (item.Status == LineGroupDestinationStatuses.Disabled) return Conflict(ApiResponse<object>.Fail("Destination is disabled."));
        var eventId = Guid.NewGuid();
        var log = new LineGroupDeliveryLog
        {
            EventId = eventId, DestinationId = item.Id, CanonicalEventType = "Fleet.LineGroupTest", SourceEventType = "Fleet.LineGroupTest",
            RequestId = Guid.Empty, DeduplicationKey = $"{eventId}:{item.Id}:Fleet.LineGroupTest", CorrelationId = HttpContext.TraceIdentifier,
            MessageText = string.IsNullOrWhiteSpace(requestedMessage) ? "ทดสอบการแจ้งเตือนกลุ่มงานยานพาหนะจาก HOP" : requestedMessage, AttemptCount = 1
        };
        var result = await groupLine.PushTextAsync(item, log.MessageText, ct);
        if (result.Success) { log.Status = "Sent"; log.SentAt = DateTime.UtcNow; }
        else { log.Status = result.IsTransient ? "Retry" : "Failed"; log.FailedAt = result.IsTransient ? null : DateTime.UtcNow; log.ErrorCode = result.ErrorCode; log.ErrorMessage = result.ErrorMessage; }
        db.LineGroupDeliveryLogs.Add(log);
        Audit(result.Success ? "Fleet.LineGroupTestSent" : "Fleet.LineGroupTestFailed", item, Actor(), result.ErrorCode);
        await db.SaveChangesAsync(ct);
        var response = new { log.Id, log.Status, log.AttemptCount, log.SentAt, log.FailedAt, log.ErrorCode };
        if (!result.Success)
        {
            var message = result.IsTransient
                ? "ไม่สามารถเชื่อมต่อ LINE endpoint ได้ ระบบบันทึกไว้เพื่อส่งใหม่"
                : "LINE endpoint ปฏิเสธข้อความทดสอบ กรุณาตรวจสอบการตั้งค่า";
            return StatusCode(StatusCodes.Status502BadGateway, new ApiResponse<object> { Success = false, Message = message, Data = response });
        }
        return ApiResponse<object>.Ok(response, "ส่งข้อความทดสอบสำเร็จ");
    }

    [HttpGet("{id:guid}/deliveries"), RequireAnyPermission(FleetPermissions.LineGroupView, FleetPermissions.LineGroupManage)]
    public async Task<ActionResult<ApiResponse<object>>> Deliveries(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        if (!await db.LineGroupDestinations.AnyAsync(x => x.Id == id && x.Module == "FLEET", ct)) return NotFound(ApiResponse<object>.Fail("LINE group destination not found."));
        var query = db.LineGroupDeliveryLogs.AsNoTracking().Where(x => x.DestinationId == id);
        var totalItems = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            x.Id, EventType = x.CanonicalEventType, DestinationType = "GROUP", DestinationIdMasked = "***",
            x.RequestId, x.Status, x.AttemptCount, x.SentAt, x.FailedAt, x.ErrorCode, x.ErrorMessage, x.CorrelationId, x.CreatedAt
        }).ToListAsync(ct);
        return ApiResponse<object>.Ok(new { items = rows, page, pageSize, totalItems, totalPages = (int)Math.Ceiling(totalItems / (double)pageSize) });
    }

    private void Audit(string action, LineGroupDestination item, Guid? actor, string? reason) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actor, Action = action, EntityName = nameof(LineGroupDestination), EntityId = item.Id.ToString(),
        NewValue = item.Status, Reason = reason?.Trim(), CorrelationId = HttpContext.TraceIdentifier,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString()
    });
    private Guid? Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string? ValidateConfiguration(LineGroupConfigurationRequest body, bool requireSecret, bool requireGroupId)
    {
        if (string.IsNullOrWhiteSpace(body.DisplayName)) return "กรุณาระบุชื่อกลุ่ม";
        if (requireGroupId && string.IsNullOrWhiteSpace(body.GroupId)) return "กรุณาระบุ Group ID";
        if (string.IsNullOrWhiteSpace(body.ClientId)) return "กรุณาระบุ Client ID";
        if (requireSecret && string.IsNullOrWhiteSpace(body.ClientSecret)) return "กรุณาระบุ Client Secret";
        if (!Uri.TryCreate(body.EndpointUrl, UriKind.Absolute, out var uri)) return "Endpoint URL ไม่ถูกต้อง";
        if (uri.Scheme != Uri.UriSchemeHttps && !(environment?.IsDevelopment() == true && uri.Scheme == Uri.UriSchemeHttp)) return "Endpoint URL ต้องใช้ HTTPS";
        if (environment?.IsDevelopment() != true && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))) return "ไม่อนุญาต Endpoint ภายในเครื่องใน Production";
        return null;
    }
    [GeneratedRegex(@"ผู้ป่วย|เลขใบขับขี่|ข้อมูลสุขภาพ|เหตุผลการลา|\bHN\s*[:#]?\s*\d+|\b\d{9,13}\b", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveTestMessage();
}

public sealed record LineGroupStateRequest(Guid ConcurrencyToken, string? Reason);
public sealed record LineGroupSubscriptionsRequest(Guid ConcurrencyToken, Dictionary<string, bool> Events);
public sealed record LineGroupTestRequest(string? Message);
public sealed record LineGroupConfigurationRequest(string DisplayName, string GroupId, string EndpointUrl, string ClientId, string? ClientSecret, Guid? ConcurrencyToken);
