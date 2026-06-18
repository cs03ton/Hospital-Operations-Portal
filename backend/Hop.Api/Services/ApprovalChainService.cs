using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class ApprovalChainService(AppDbContext db) : IApprovalChainService
{
    public async Task<IReadOnlyList<ApprovalStepPlan>> BuildApprovalPlanAsync(LeaveRequest leaveRequest)
    {
        var requestUser = leaveRequest.User ?? await db.Users.AsNoTracking().FirstAsync(item => item.Id == leaveRequest.UserId);
        var departmentId = requestUser.DepartmentId;

        var chain = await db.ApprovalChains
            .AsNoTracking()
            .Include(item => item.Steps.Where(step => step.IsActive))
            .Where(item => item.IsActive)
            .Where(item => item.MinimumDays <= leaveRequest.TotalDays)
            .Where(item => item.DepartmentId == null || item.DepartmentId == departmentId)
            .Where(item => item.LeaveTypeId == null || item.LeaveTypeId == leaveRequest.LeaveTypeId)
            .OrderByDescending(item => item.DepartmentId != null)
            .ThenByDescending(item => item.LeaveTypeId != null)
            .ThenByDescending(item => item.MinimumDays)
            .FirstOrDefaultAsync();

        if (chain is null || chain.Steps.Count == 0)
        {
            var defaultApprover = await FindDefaultApproverAsync(departmentId);
            return defaultApprover is null
                ? []
                : [new ApprovalStepPlan(null, null, 1, "ผู้อนุมัติเริ่มต้น", defaultApprover.Id, "LeaveManagement.Approve")];
        }

        var plans = new List<ApprovalStepPlan>();
        foreach (var step in chain.Steps.OrderBy(item => item.StepOrder))
        {
            var approver = await ResolveApproverAsync(step, departmentId);
            if (approver is null)
            {
                continue;
            }

            approver = await ResolveDelegatedApproverAsync(approver, leaveRequest, step.RequiredPermissionCode);

            var hasPermission = await UserHasPermissionAsync(approver.Id, step.RequiredPermissionCode);
            if (!hasPermission)
            {
                continue;
            }

            plans.Add(new ApprovalStepPlan(
                chain.Id,
                step.Id,
                step.StepOrder,
                step.Name,
                approver.Id,
                step.RequiredPermissionCode));
        }

        return plans;
    }

    private async Task<User?> ResolveApproverAsync(ApprovalChainStep step, Guid? departmentId)
    {
        if (step.ApproverUserId is not null)
        {
            return await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == step.ApproverUserId && item.IsActive);
        }

        if (step.ApproverRoleId is not null)
        {
            var query = db.UserRoles
                .AsNoTracking()
                .Include(item => item.User)
                .Where(item => item.RoleId == step.ApproverRoleId && item.User != null && item.User.IsActive);

            var sameDepartment = departmentId is null
                ? null
                : await query.Where(item => item.User!.DepartmentId == departmentId).Select(item => item.User!).FirstOrDefaultAsync();

            return sameDepartment ?? await query.Select(item => item.User!).FirstOrDefaultAsync();
        }

        return await FindDefaultApproverAsync(departmentId);
    }

    private async Task<User?> FindDefaultApproverAsync(Guid? departmentId)
    {
        var departmentHead = await db.UserRoles
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Role)
            .Where(item => item.Role != null && item.Role.Name == "DepartmentHead" && item.User != null && item.User.IsActive)
            .Where(item => departmentId == null || item.User!.DepartmentId == departmentId)
            .Select(item => item.User!)
            .FirstOrDefaultAsync();

        if (departmentHead is not null)
        {
            return departmentHead;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Role)
            .Where(item => item.Role != null && (item.Role.Name == "Admin" || item.Role.Name == "SuperAdmin") && item.User != null && item.User.IsActive)
            .Select(item => item.User!)
            .FirstOrDefaultAsync();
    }

    private Task<bool> UserHasPermissionAsync(Guid userId, string permissionCode)
    {
        return db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode);
    }

    private async Task<User> ResolveDelegatedApproverAsync(User approver, LeaveRequest leaveRequest, string permissionCode)
    {
        var date = leaveRequest.StartDate;
        var delegation = await db.ApprovalDelegations
            .AsNoTracking()
            .Include(item => item.DelegateUser)
            .Where(item => item.IsActive)
            .Where(item => item.ApproverUserId == approver.Id)
            .Where(item => item.StartDate <= date && item.EndDate >= date)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();

        if (delegation?.DelegateUser is null || !delegation.DelegateUser.IsActive)
        {
            return approver;
        }

        return await UserHasPermissionAsync(delegation.DelegateUserId, permissionCode)
            ? delegation.DelegateUser
            : approver;
    }
}
