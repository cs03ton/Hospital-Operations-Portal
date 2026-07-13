using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-approvals")]
[Authorize]
public class LeaveApprovalsController(AppDbContext db, ILeaveRequestAccessService leaveRequestAccessService) : ControllerBase
{
    [HttpGet("request/{leaveRequestId:guid}")]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.SupportViewAll)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveApprovalResponse>>>> GetApprovals(Guid leaveRequestId)
    {
        var leaveRequest = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == leaveRequestId);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<LeaveApprovalResponse>>.Fail("Leave request not found."));
        }

        if (!await leaveRequestAccessService.CanAccessLeaveRequestAsync(leaveRequest, GetCurrentUserId()))
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
                item.ActionAt,
                item.ReturnedAt,
                item.ReturnReason
            ))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveApprovalResponse>>.Ok(approvals);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
