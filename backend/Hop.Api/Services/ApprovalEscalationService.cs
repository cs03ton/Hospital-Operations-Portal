using Hop.Api.Data;
using Hop.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class ApprovalEscalationService(AppDbContext db, IAuditLogService auditLogService) : IApprovalEscalationService
{
    public async Task<int> EscalateOverdueApprovalsAsync(CancellationToken cancellationToken = default)
    {
        var rules = await db.ApprovalEscalationRules
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.DepartmentId != null)
            .ThenByDescending(item => item.LeaveTypeId != null)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return 0;
        }

        var approvals = await db.LeaveApprovals
            .Include(item => item.LeaveRequest)
                .ThenInclude(request => request!.User)
            .Where(item => item.Status == "Pending")
            .Where(item => item.LeaveRequest != null && item.LeaveRequest.Status == "Pending")
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var approval in approvals)
        {
            var leaveRequest = approval.LeaveRequest!;
            var departmentId = leaveRequest.User?.DepartmentId;
            var rule = rules.FirstOrDefault(item =>
                (item.DepartmentId == null || item.DepartmentId == departmentId) &&
                (item.LeaveTypeId == null || item.LeaveTypeId == leaveRequest.LeaveTypeId));

            if (rule is null || approval.CreatedAt.AddHours(rule.EscalateAfterHours) > DateTime.UtcNow)
            {
                continue;
            }

            var newApproverId = rule.EscalateToUserId ?? await ResolveUserFromRoleAsync(rule.EscalateToRoleId, departmentId, cancellationToken);
            if (newApproverId is null || newApproverId == approval.ApproverId)
            {
                continue;
            }

            var previousApproverId = approval.ApproverId;
            approval.ApproverId = newApproverId.Value;
            approval.Remark = $"Escalated from {previousApproverId}.";
            leaveRequest.CurrentApproverId = newApproverId.Value;
            leaveRequest.UpdatedAt = DateTime.UtcNow;
            processed += 1;

            await auditLogService.WriteAsync(null, "Approval.Escalated", "LeaveApproval", approval.Id.ToString(), $"Escalated leave request {leaveRequest.Id} from {previousApproverId} to {newApproverId}.", "Success");
        }

        await db.SaveChangesAsync(cancellationToken);
        return processed;
    }

    private async Task<Guid?> ResolveUserFromRoleAsync(Guid? roleId, Guid? departmentId, CancellationToken cancellationToken)
    {
        if (roleId is null)
        {
            return null;
        }

        var query = db.UserRoles
            .AsNoTracking()
            .Include(item => item.User)
            .Where(item => item.RoleId == roleId && item.User != null && item.User.IsActive);

        var sameDepartment = departmentId is null
            ? null
            : await query.Where(item => item.User!.DepartmentId == departmentId).Select(item => (Guid?)item.UserId).FirstOrDefaultAsync(cancellationToken);

        return sameDepartment ?? await query.Select(item => (Guid?)item.UserId).FirstOrDefaultAsync(cancellationToken);
    }
}
