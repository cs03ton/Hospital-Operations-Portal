using System.Security.Claims;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/admin/diagnostics")]
[Authorize]
public sealed class AdminDiagnosticsController(
    AppDbContext db,
    IDiagnosticsService diagnosticsService,
    IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DiagnosticsSummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.View", cancellationToken))
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();
        await auditLogService.WriteAsync(userId, "Diagnostics.Viewed", "Diagnostics", null, "Diagnostics summary viewed", "Success", HttpContext);
        return ApiResponse<DiagnosticsSummaryResponse>.Ok(await diagnosticsService.GetSummaryAsync(cancellationToken));
    }

    [HttpPost("test/{diagnosticType}")]
    public async Task<ActionResult<ApiResponse<DiagnosticTestResultResponse>>> RunTest(string diagnosticType, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.Run", cancellationToken))
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();
        var referenceId = HttpContext.TraceIdentifier;
        await auditLogService.WriteAsync(userId, "Diagnostics.TestStarted", "Diagnostics", diagnosticType, $"Started {diagnosticType}", "Success", HttpContext);
        var result = await diagnosticsService.RunTestAsync(diagnosticType, userId, referenceId, cancellationToken);
        await auditLogService.WriteAsync(
            userId,
            result.Status is "Failed" or "Unhealthy" ? "Diagnostics.TestFailed" : "Diagnostics.TestCompleted",
            "Diagnostics",
            result.RunId.ToString(),
            $"{diagnosticType}: {result.Status}",
            result.Status is "Failed" or "Unhealthy" ? "Failed" : "Success",
            HttpContext);

        return ApiResponse<DiagnosticTestResultResponse>.Ok(result);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<ApiResponse<DiagnosticsLogResponse>>> GetLogs([FromQuery] DiagnosticsLogQuery query, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.View", cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<DiagnosticsLogResponse>.Ok(await diagnosticsService.GetLogsAsync(query, cancellationToken));
    }

    [HttpGet("recent-errors")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecentErrorResponse>>>> GetRecentErrors(CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.View", cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<IReadOnlyList<RecentErrorResponse>>.Ok(await diagnosticsService.GetRecentErrorsAsync(cancellationToken));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiagnosticRunResponse>>>> GetHistory(CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.View", cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<IReadOnlyList<DiagnosticRunResponse>>.Ok(await diagnosticsService.GetHistoryAsync(cancellationToken));
    }

    [HttpPost("support-bundle")]
    public async Task<ActionResult<ApiResponse<SupportBundleResponse>>> CreateSupportBundle(SupportBundleRequest request, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.Export", cancellationToken))
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();
        var bundle = await diagnosticsService.CreateSupportBundleAsync(request, userId, cancellationToken);
        await auditLogService.WriteAsync(userId, "Diagnostics.SupportBundleGenerated", "SupportBundle", bundle.Id.ToString(), $"Generated support bundle {bundle.FileName}", "Success", HttpContext);
        return ApiResponse<SupportBundleResponse>.Ok(bundle);
    }

    [HttpGet("support-bundles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupportBundleHistoryResponse>>>> GetSupportBundles(CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.View", cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<IReadOnlyList<SupportBundleHistoryResponse>>.Ok(await diagnosticsService.GetSupportBundlesAsync(cancellationToken));
    }

    [HttpGet("support-bundle/{id:guid}/download")]
    public async Task<IActionResult> DownloadSupportBundle(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("System.Diagnostics.Export", cancellationToken))
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();
        var file = await diagnosticsService.GetSupportBundleFileAsync(id, userId, cancellationToken);
        if (file is null)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบไฟล์ Support Bundle หรือไฟล์หมดอายุแล้ว"));
        }

        await auditLogService.WriteAsync(userId, "Diagnostics.SupportBundleDownloaded", "SupportBundle", id.ToString(), $"Downloaded support bundle {file.Value.FileName}", "Success", HttpContext);
        return PhysicalFile(file.Value.FilePath, file.Value.ContentType, file.Value.FileName);
    }

    private async Task<bool> CanAccessAsync(string permissionCode, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId && userRole.Role != null && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .AnyAsync(rolePermission => rolePermission.Permission != null &&
                rolePermission.Permission.IsActive &&
                rolePermission.Permission.Code == permissionCode, cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
