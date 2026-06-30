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
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.SupportViewAll)]
    public async Task<IActionResult> DownloadAttachment(Guid id)
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

        await auditLogService.WriteAsync(userId, "LeaveAttachment.Download", "LeaveAttachment", attachment.Id.ToString(), $"Downloaded attachment {attachment.FileName}.", "Success", HttpContext);
        return PhysicalFile(fileInfo.FullName, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(LeavePermissions.EditOwn)]
    public async Task<IActionResult> DeleteAttachment(Guid id)
    {
        var attachment = await db.LeaveAttachments
            .Include(item => item.LeaveRequest)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (attachment is null)
        {
            return NotFound(ApiResponse<string>.Fail("Attachment not found."));
        }

        var userId = GetCurrentUserId();
        if (attachment.LeaveRequest?.UserId != userId || !await HasPermission(userId, LeavePermissions.EditOwn))
        {
            return Forbid();
        }

        await attachmentStorage.DeleteAsync(attachment);
        db.LeaveAttachments.Remove(attachment);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(userId, "LeaveAttachment.Delete", "LeaveAttachment", attachment.Id.ToString(), $"Deleted attachment {attachment.FileName}.", "Success", HttpContext);

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

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
