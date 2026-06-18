using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-approvals")]
[Authorize]
public class LeaveApprovalsController(AppDbContext db) : ControllerBase
{
    [HttpGet("request/{leaveRequestId:guid}")]
    [RequirePermission("LeaveManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveApprovalResponse>>>> GetApprovals(Guid leaveRequestId)
    {
        var leaveRequest = await db.LeaveRequests.AsNoTracking().FirstOrDefaultAsync(item => item.Id == leaveRequestId);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<LeaveApprovalResponse>>.Fail("Leave request not found."));
        }

        if (leaveRequest.UserId != GetCurrentUserId() && !await HasPermissionAsync("LeaveManagement.Approve"))
        {
            return Forbid();
        }

        var approvals = await db.LeaveApprovals
            .AsNoTracking()
            .Include(item => item.Approver)
            .Where(item => item.LeaveRequestId == leaveRequestId)
            .OrderBy(item => item.StepOrder)
            .Select(item => new LeaveApprovalResponse(
                item.Id,
                item.LeaveRequestId,
                item.ApproverId,
                item.Approver != null ? item.Approver.FullName : null,
                item.ApprovalChainId,
                item.ApprovalChainStepId,
                item.StepOrder,
                item.StepName,
                item.Status,
                item.RequiredPermissionCode,
                item.Remark,
                item.CreatedAt,
                item.ActionAt
            ))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveApprovalResponse>>.Ok(approvals);
    }

    private async Task<bool> HasPermissionAsync(string permissionCode)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
