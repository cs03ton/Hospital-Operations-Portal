using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-attachments")]
[Authorize]
public class LeaveAttachmentsController(
    AppDbContext db,
    IAuditLogService auditLogService,
    ILeaveAttachmentStorageService attachmentStorage,
    ILeaveRequestAccessService leaveRequestAccessService) : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    [HttpGet("/api/leave-requests/{leaveRequestId:guid}/attachments/{id:guid}/download")]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ApproveCurrentStep, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.SupportViewAll)]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid? leaveRequestId = null)
    {
        var attachment = await db.LeaveAttachments
            .AsNoTracking()
            .Include(item => item.LeaveRequest)
                .ThenInclude(item => item!.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (attachment is null)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment not found."));
        }

        if (leaveRequestId is not null && attachment.LeaveRequestId != leaveRequestId)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment not found for this leave request."));
        }

        var userId = GetCurrentUserId();
        if (!await leaveRequestAccessService.CanAccessLeaveRequestAsync(attachment.LeaveRequest!, userId))
        {
            return Forbid();
        }

        FileInfo fileInfo;
        try
        {
            fileInfo = attachmentStorage.GetFileInfo(attachment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }

        if (!fileInfo.Exists)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment file not found."));
        }

        await auditLogService.WriteAsync(userId, "LeaveAttachment.Downloaded", "LeaveAttachment", attachment.Id.ToString(), $"Downloaded attachment {attachment.FileName}.", "Success", HttpContext);
        return PhysicalFile(fileInfo.FullName, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
    }

    [HttpGet("/api/leave-requests/{leaveRequestId:guid}/attachments/{id:guid}/preview")]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ApproveCurrentStep, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.SupportViewAll)]
    public async Task<IActionResult> PreviewAttachment(Guid leaveRequestId, Guid id)
    {
        var attachment = await db.LeaveAttachments
            .AsNoTracking()
            .Include(item => item.LeaveRequest)
                .ThenInclude(item => item!.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id && item.LeaveRequestId == leaveRequestId);

        if (attachment is null)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment not found for this leave request."));
        }

        var userId = GetCurrentUserId();
        if (!await leaveRequestAccessService.CanAccessLeaveRequestAsync(attachment.LeaveRequest!, userId))
        {
            return Forbid();
        }

        if (!IsPreviewSupported(attachment))
        {
            return BadRequest(ApiResponse<string>.Fail("ไม่รองรับการแสดงตัวอย่างไฟล์ประเภทนี้"));
        }

        FileInfo fileInfo;
        try
        {
            fileInfo = attachmentStorage.GetFileInfo(attachment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }

        if (!fileInfo.Exists)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment file not found."));
        }

        Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(attachment.FileName)}";
        await auditLogService.WriteAsync(userId, "LeaveAttachment.Previewed", "LeaveAttachment", attachment.Id.ToString(), $"Previewed attachment {attachment.FileName}.", "Success", HttpContext);
        return PhysicalFile(fileInfo.FullName, attachment.ContentType ?? ResolveContentTypeFromFileName(attachment.FileName), enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}")]
    [HttpDelete("/api/leave-requests/{leaveRequestId:guid}/attachments/{id:guid}")]
    [RequirePermission(LeavePermissions.EditOwn)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid? leaveRequestId = null)
    {
        var attachment = await db.LeaveAttachments
            .Include(item => item.LeaveRequest)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (attachment is null)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment not found."));
        }

        if (leaveRequestId is not null && attachment.LeaveRequestId != leaveRequestId)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment not found for this leave request."));
        }

        var userId = GetCurrentUserId();
        var leaveRequest = attachment.LeaveRequest;
        if (leaveRequest is null || leaveRequest.UserId != userId || !await HasPermission(userId, LeavePermissions.EditOwn))
        {
            return Forbid();
        }

        if (leaveRequest.Status is not ("Draft" or "ReturnedForRevision"))
        {
            return BadRequest(ApiResponse<string>.Fail("ลบไฟล์แนบได้เฉพาะคำขอแบบร่างหรือคำขอที่ถูกตีกลับรอแก้ไขเท่านั้น"));
        }

        await attachmentStorage.DeleteAsync(attachment);
        db.LeaveAttachments.Remove(attachment);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(userId, "LeaveAttachment.Deleted", "LeaveAttachment", attachment.Id.ToString(), $"Deleted attachment {attachment.FileName}.", "Success", HttpContext);

        return NoContent();
    }

    private async Task<bool> HasPermission(Guid? userId, string permissionCode)
    {
        return await HasAnyPermission(userId, permissionCode);
    }

    private async Task<bool> HasAnyPermission(Guid? userId, params string[] permissionCodes)
    {
        if (userId is null)
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive &&
                permissionCodes.Contains(item.Permission.Code));
    }

    private static bool IsPreviewSupported(LeaveAttachment attachment)
    {
        var contentType = attachment.ContentType?.Trim().ToLowerInvariant();
        if (contentType is "application/pdf" or "image/jpeg" or "image/jpg" or "image/png" or "image/webp")
        {
            return true;
        }

        var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
        return extension is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".webp";
    }

    private static string ResolveContentTypeFromFileName(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
