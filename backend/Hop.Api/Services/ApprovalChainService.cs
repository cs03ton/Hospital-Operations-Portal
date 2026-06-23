using Hop.Api.Data;
using Hop.Api.Authorization;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class ApprovalChainService(
    AppDbContext db,
    IConfiguration configuration,
    IAuditLogService auditLogService) : IApprovalChainService
{
    public async Task<IReadOnlyList<ApprovalStepPlan>> BuildApprovalPlanAsync(LeaveRequest leaveRequest)
    {
        var requestUser = leaveRequest.User ?? await db.Users.AsNoTracking().FirstAsync(item => item.Id == leaveRequest.UserId);
        var departmentId = requestUser.DepartmentId;

        if (requestUser.LeaveApprovalRuleId is null)
        {
            return [];
        }

        var chain = await db.ApprovalChains
            .AsNoTracking()
            .Include(item => item.Steps.Where(step => step.IsActive))
            .Where(item => item.Id == requestUser.LeaveApprovalRuleId && item.IsActive)
            .FirstOrDefaultAsync();

        if (chain is null || chain.Steps.Count == 0)
        {
            return [];
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
            approver = await ResolveSecureApproverAsync(approver, leaveRequest, step.RequiredPermissionCode, step.Name, chain.Id);
            if (approver is null)
            {
                return [];
            }

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
            ? await ApplyDelegationAsync(approver, delegation.DelegateUser, leaveRequest, delegation.Id)
            : approver;
    }

    private async Task<User> ApplyDelegationAsync(User originalApprover, User delegateUser, LeaveRequest leaveRequest, Guid delegationId)
    {
        await auditLogService.WriteAsync(
            leaveRequest.UserId,
            "LeaveApproval.DelegationApplied",
            "ApprovalDelegation",
            delegationId.ToString(),
            $"Applied delegation for leave request {leaveRequest.Id} from {originalApprover.Id} to {delegateUser.Id}.",
            "Success");
        return delegateUser;
    }

    private async Task<User?> ResolveSecureApproverAsync(
        User approver,
        LeaveRequest leaveRequest,
        string permissionCode,
        string stepName,
        Guid? approvalChainId)
    {
        if (approver.Id != leaveRequest.UserId)
        {
            return approver;
        }

        if (await UserHasRoleAsync(leaveRequest.UserId, "Director"))
        {
            var fallback = await ResolveDirectorFallbackApproverAsync(permissionCode);
            if (fallback is not null && fallback.Id != leaveRequest.UserId)
            {
                await auditLogService.WriteAsync(
                    leaveRequest.UserId,
                    "DirectorLeaveFallbackApplied",
                    "LeaveRequest",
                    leaveRequest.Id.ToString(),
                    $"Applied director leave fallback approver {fallback.Id} for step {stepName} in chain {approvalChainId?.ToString() ?? "default"}.",
                    "Success");
                return fallback;
            }
        }

        await auditLogService.WriteAsync(
            leaveRequest.UserId,
            "SelfApprovalBlocked",
            "LeaveRequest",
            leaveRequest.Id.ToString(),
            $"Blocked self-approval while building approval step {stepName} in chain {approvalChainId?.ToString() ?? "default"}.",
            "Denied");
        return null;
    }

    private async Task<User?> ResolveDirectorFallbackApproverAsync(string permissionCode)
    {
        var fallbackUserIdValue =
            configuration["LeaveApproval:DirectorFallbackApproverId"] ??
            configuration["LEAVE_DIRECTOR_FALLBACK_APPROVER_ID"];

        User? fallbackUser = null;
        if (Guid.TryParse(fallbackUserIdValue, out var fallbackUserId))
        {
            fallbackUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == fallbackUserId && item.IsActive);
        }

        if (fallbackUser is null)
        {
            var fallbackUsername =
                configuration["LeaveApproval:DirectorFallbackApproverUsername"] ??
                configuration["LEAVE_DIRECTOR_FALLBACK_APPROVER_USERNAME"];

            if (!string.IsNullOrWhiteSpace(fallbackUsername))
            {
                fallbackUser = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Username == fallbackUsername.Trim() && item.IsActive);
            }
        }

        if (fallbackUser is null)
        {
            return null;
        }

        return await UserHasPermissionAsync(fallbackUser.Id, permissionCode)
            ? fallbackUser
            : null;
    }

    private Task<bool> UserHasRoleAsync(Guid userId, string roleName)
    {
        return db.UserRoles
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == userId &&
                item.Role != null &&
                item.Role.IsActive &&
                item.Role.Name == roleName);
    }
}
