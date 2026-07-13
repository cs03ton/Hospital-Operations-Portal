using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/admin/backups")]
[Authorize]
public sealed class AdminBackupsController(IBackupCenterService backupCenterService) : ControllerBase
{
    [HttpGet("overview")]
    [RequirePermission(BackupPermissions.View)]
    public async Task<ActionResult<ApiResponse<BackupOverviewResponse>>> GetOverview(CancellationToken cancellationToken)
    {
        return ApiResponse<BackupOverviewResponse>.Ok(await backupCenterService.GetOverviewAsync(cancellationToken));
    }

    [HttpGet]
    [RequirePermission(BackupPermissions.View)]
    public async Task<ActionResult<ApiResponse<PagedResponse<BackupRunResponse>>>> GetBackups([FromQuery] BackupQuery query, CancellationToken cancellationToken)
    {
        return ApiResponse<PagedResponse<BackupRunResponse>>.Ok(await backupCenterService.GetBackupsAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(BackupPermissions.View)]
    public async Task<ActionResult<ApiResponse<BackupRunDetailResponse>>> GetBackup(Guid id, CancellationToken cancellationToken)
    {
        var result = await backupCenterService.GetBackupAsync(id, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<BackupRunDetailResponse>.Fail("ไม่พบข้อมูล backup"))
            : ApiResponse<BackupRunDetailResponse>.Ok(result);
    }

    [HttpPost("{id:guid}/verify")]
    [RequirePermission(BackupPermissions.Run)]
    public async Task<ActionResult<ApiResponse<BackupVerificationResponse>>> VerifyBackup(Guid id, CancellationToken cancellationToken)
    {
        var result = await backupCenterService.VerifyBackupAsync(id, CurrentUserId(), cancellationToken);
        return result is null
            ? NotFound(ApiResponse<BackupVerificationResponse>.Fail("ไม่พบข้อมูล backup"))
            : ApiResponse<BackupVerificationResponse>.Ok(result, "ตรวจสอบไฟล์ Backup เรียบร้อยแล้ว");
    }

    [HttpPost("{id:guid}/restore-preview")]
    [RequirePermission(BackupPermissions.Restore)]
    public async Task<ActionResult<ApiResponse<RestorePreviewResponse>>> RestorePreview(Guid id, CancellationToken cancellationToken)
    {
        var result = await backupCenterService.PreviewRestoreAsync(id, CurrentUserId(), cancellationToken);
        return result is null
            ? NotFound(ApiResponse<RestorePreviewResponse>.Fail("ไม่พบข้อมูล backup"))
            : ApiResponse<RestorePreviewResponse>.Ok(result);
    }

    [HttpPost("{id:guid}/restore")]
    [RequirePermission(BackupPermissions.Restore)]
    public async Task<ActionResult<ApiResponse<RestoreRunResponse>>> Restore(Guid id, [FromBody] RestoreRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await backupCenterService.RestoreAsync(id, request, CurrentUserId(), cancellationToken);
            return result is null
                ? NotFound(ApiResponse<RestoreRunResponse>.Fail("ไม่พบข้อมูล backup"))
                : ApiResponse<RestoreRunResponse>.Ok(result, "บันทึกคำขอ restore เรียบร้อยแล้ว");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RestoreRunResponse>.Fail(ex.Message));
        }
    }

    [HttpGet("restore-runs")]
    [RequirePermission(BackupPermissions.Restore)]
    public async Task<ActionResult<ApiResponse<PagedResponse<RestoreRunResponse>>>> GetRestoreRuns([FromQuery] BackupQuery query, CancellationToken cancellationToken)
    {
        return ApiResponse<PagedResponse<RestoreRunResponse>>.Ok(await backupCenterService.GetRestoreRunsAsync(query, cancellationToken));
    }

    [HttpPost("retention/preview")]
    [RequirePermission(BackupPermissions.ManageRetention)]
    public async Task<ActionResult<ApiResponse<RetentionPreviewResponse>>> PreviewRetention(CancellationToken cancellationToken)
    {
        return ApiResponse<RetentionPreviewResponse>.Ok(await backupCenterService.PreviewRetentionAsync(CurrentUserId(), cancellationToken));
    }

    [HttpPost("retention/apply")]
    [RequirePermission(BackupPermissions.ManageRetention)]
    public async Task<ActionResult<ApiResponse<ApplyRetentionResponse>>> ApplyRetention([FromBody] ApplyRetentionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return ApiResponse<ApplyRetentionResponse>.Ok(
                await backupCenterService.ApplyRetentionAsync(request, CurrentUserId(), cancellationToken),
                "ดำเนินการ retention policy เรียบร้อยแล้ว");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ApplyRetentionResponse>.Fail(ex.Message));
        }
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

public static class BackupPermissions
{
    public const string View = "System.Backup.View";
    public const string Run = "System.Backup.Run";
    public const string Restore = "System.Backup.Restore";
    public const string ManageRetention = "System.Backup.ManageRetention";
}
