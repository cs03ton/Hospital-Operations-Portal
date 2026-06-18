using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
[Authorize]
public class MonitoringController(AppDbContext db) : ControllerBase
{
    [HttpGet("security-summary")]
    [RequirePermission("SystemSettings.View")]
    public async Task<ActionResult<ApiResponse<object>>> GetSecuritySummary([FromQuery] int hours = 24)
    {
        var windowHours = Math.Clamp(hours, 1, 168);
        var since = DateTime.UtcNow.AddHours(-windowHours);

        var auditCounts = await db.AuditLogs
            .AsNoTracking()
            .Where(item => item.CreatedAt >= since)
            .GroupBy(item => item.Action)
            .Select(group => new { Action = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Action, item => item.Count);

        var failedLineDeliveries = await db.LineDeliveryLogs
            .AsNoTracking()
            .CountAsync(item => item.CreatedAt >= since && item.Status == "Failed");

        return ApiResponse<object>.Ok(new
        {
            windowHours,
            since,
            scanFailures = GetCount(auditCounts, "LeaveAttachment.ScanFailed"),
            failedUploads = GetCount(auditCounts, "LeaveAttachment.UploadFailed"),
            loginLockouts = GetCount(auditCounts, "Auth.LoginLocked"),
            permissionDenied = GetCount(auditCounts, "Authorization.Denied"),
            refreshTokenReuse = GetCount(auditCounts, "Auth.RefreshTokenReuseDetected"),
            auditExports = GetCount(auditCounts, "AuditLog.Export"),
            csrfFailures = GetCount(auditCounts, "Security.CsrfValidationFailed"),
            failedLineDeliveries
        });
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string action)
    {
        return counts.TryGetValue(action, out var count) ? count : 0;
    }
}
