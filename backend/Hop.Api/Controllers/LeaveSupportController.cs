using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-support")]
[Authorize]
[RequirePermission(LeavePermissions.SupportViewAll)]
public class LeaveSupportController(AppDbContext db) : ControllerBase
{
    [HttpGet("requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveSupportRequestResponse>>>> GetRequests(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] Guid? currentApproverId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var query = BaseLeaveQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(item =>
                item.Id.ToString().ToLower().Contains(keyword) ||
                (item.User != null && item.User.FullName.ToLower().Contains(keyword)));
        }

        if (departmentId is not null)
        {
            query = query.Where(item => item.User != null && item.User.DepartmentId == departmentId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status.Trim());
        }

        if (currentApproverId is not null)
        {
            query = query.Where(item => item.CurrentApproverId == currentApproverId);
        }

        if (fromDate is not null)
        {
            query = query.Where(item => item.EndDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(item => item.StartDate <= toDate);
        }

        var rules = await db.ApprovalEscalationRules.AsNoTracking().Where(item => item.IsActive).ToListAsync();
        var items = (await query.OrderByDescending(item => item.CreatedAt).Take(300).ToListAsync())
            .Select(item => ToSupportResponse(item, rules))
            .ToList();

        return ApiResponse<IReadOnlyList<LeaveSupportRequestResponse>>.Ok(items);
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeaveSupportDetailResponse>>> GetRequest(Guid id)
    {
        var leaveRequest = await BaseLeaveQuery()
            .Include(item => item.Approvals)
                .ThenInclude(approval => approval.Approver)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveSupportDetailResponse>.Fail("Leave request not found."));
        }

        var rules = await db.ApprovalEscalationRules.AsNoTracking().Where(item => item.IsActive).ToListAsync();
        var approvals = leaveRequest.Approvals
            .OrderBy(item => item.StepOrder)
            .Select(item => new LeaveApprovalResponse(
                item.Id,
                item.LeaveRequestId,
                item.ApproverId,
                item.Approver?.FullName,
                item.ApprovalChainId,
                item.ApprovalChainStepId,
                item.StepOrder,
                item.StepName,
                item.Status,
                item.RequiredPermissionCode,
                item.Remark,
                item.CreatedAt,
                item.ActionAt))
            .ToList();
        var overrideLogs = await db.ApprovalOverrideLogs
            .AsNoTracking()
            .Include(item => item.OriginalApprover)
            .Include(item => item.OverrideByUser)
            .Where(item => item.LeaveRequestId == id)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new ApprovalOverrideLogResponse(
                item.Id,
                item.LeaveRequestId,
                item.OriginalApproverId,
                item.OriginalApprover != null ? item.OriginalApprover.FullName : null,
                item.OverrideByUserId,
                item.OverrideByUser != null ? item.OverrideByUser.FullName : null,
                item.Action,
                item.Reason,
                item.IpAddress,
                item.UserAgent,
                item.CreatedAt))
            .ToListAsync();
        var auditLogs = await db.AuditLogs
            .AsNoTracking()
            .Include(item => item.User)
            .Where(item => item.EntityName == "LeaveRequest" && item.EntityId == id.ToString())
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new AuditLogItemResponse(
                item.Id,
                item.UserId,
                item.User != null ? item.User.Username : null,
                item.User != null ? item.User.FullName : null,
                item.Action,
                item.EntityName,
                item.EntityId,
                item.Detail,
                item.IpAddress,
                item.Result,
                item.CreatedAt))
            .ToListAsync();

        return ApiResponse<LeaveSupportDetailResponse>.Ok(new LeaveSupportDetailResponse(
            ToSupportResponse(leaveRequest, rules),
            approvals,
            overrideLogs,
            auditLogs));
    }

    private IQueryable<Models.LeaveRequest> BaseLeaveQuery()
    {
        return db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover);
    }

    private static LeaveSupportRequestResponse ToSupportResponse(Models.LeaveRequest item, IReadOnlyList<Models.ApprovalEscalationRule> rules)
    {
        var pendingSince = item.SubmittedAt ?? item.UpdatedAt ?? item.CreatedAt;
        var rule = rules.FirstOrDefault(rule =>
            (rule.DepartmentId == null || rule.DepartmentId == item.User?.DepartmentId) &&
            (rule.LeaveTypeId == null || rule.LeaveTypeId == item.LeaveTypeId));
        var isOverdue = item.Status == "Pending" && rule is not null && pendingSince.AddHours(rule.EscalateAfterHours) <= DateTime.UtcNow;
        var blockingReason = isOverdue
            ? $"ค้างอนุมัติเกิน {rule!.EscalateAfterHours} ชั่วโมง"
            : item.Status == "Pending" && item.CurrentApproverId is null
                ? "ไม่มีผู้อนุมัติปัจจุบัน"
                : null;

        return new LeaveSupportRequestResponse(
            item.Id,
            item.RequestNumber ?? "-",
            item.UserId,
            item.User?.FullName,
            item.User?.Department?.Name,
            item.LeaveType?.Name,
            item.StartDate,
            item.EndDate,
            item.DurationType,
            item.TotalDays,
            item.Status,
            item.CurrentApproverId,
            item.CurrentApprover?.FullName,
            item.CreatedAt,
            item.SubmittedAt,
            item.UpdatedAt,
            isOverdue,
            blockingReason);
    }
}
