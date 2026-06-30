using Hop.Api.Data;
using Hop.Api.Authorization;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveRequestAccessService(AppDbContext db) : ILeaveRequestAccessService
{
    public async Task<LeaveRequestVisibility> GetVisibilityAsync(Guid? userId)
    {
        if (userId is null)
        {
            return new LeaveRequestVisibility(false, false, false, false, null);
        }

        var permissions = await GetPermissionCodesAsync(userId.Value);
        var roles = await GetRoleNamesAsync(userId.Value);
        var departmentId = await db.Users
            .AsNoTracking()
            .Where(item => item.Id == userId.Value)
            .Select(item => item.DepartmentId)
            .FirstOrDefaultAsync();

        var canViewAll = permissions.Contains(LeavePermissions.ViewAll) ||
            permissions.Contains(LeavePermissions.SupportViewAll);
        var isDepartmentHead = roles.Contains("DepartmentHead");

        return new LeaveRequestVisibility(
            permissions.Contains(LeavePermissions.ViewOwn) || isDepartmentHead || canViewAll,
            permissions.Contains(LeavePermissions.ViewPendingApproval),
            permissions.Contains(LeavePermissions.ViewDepartment) || isDepartmentHead,
            canViewAll,
            departmentId,
            isDepartmentHead && !permissions.Contains(LeavePermissions.ViewDepartment));
    }

    public IQueryable<LeaveRequest> ApplyVisibility(IQueryable<LeaveRequest> query, Guid? userId, LeaveRequestVisibility visibility)
    {
        if (visibility.ViewAll)
        {
            return query;
        }

        if (userId is null)
        {
            return query.Where(_ => false);
        }

        return query.Where(item =>
            (visibility.ViewOwn && item.UserId == userId) ||
            (visibility.ViewPendingApproval && item.CurrentApproverId == userId) ||
            (visibility.ViewDepartment &&
                visibility.DepartmentId != null &&
                item.User != null &&
                item.User.DepartmentId == visibility.DepartmentId &&
                (!visibility.DepartmentStaffOnly ||
                    item.User.UserRoles.Any(userRole => userRole.Role != null && userRole.Role.Name == "Staff"))));
    }

    public async Task<bool> CanAccessLeaveRequestAsync(LeaveRequest leaveRequest, Guid? userId)
    {
        if (userId is null)
        {
            return false;
        }

        var visibility = await GetVisibilityAsync(userId);
        if (visibility.ViewAll)
        {
            return true;
        }

        if (visibility.ViewOwn && leaveRequest.UserId == userId)
        {
            return true;
        }

        if (visibility.ViewPendingApproval && leaveRequest.CurrentApproverId == userId)
        {
            return true;
        }

        if (visibility.ViewDepartment)
        {
            var requestDepartmentId = leaveRequest.User?.DepartmentId ?? await db.Users
                .AsNoTracking()
                .Where(item => item.Id == leaveRequest.UserId)
                .Select(item => item.DepartmentId)
                .FirstOrDefaultAsync();
            if (requestDepartmentId is null || requestDepartmentId != visibility.DepartmentId)
            {
                return false;
            }

            if (!visibility.DepartmentStaffOnly)
            {
                return true;
            }

            return await db.UserRoles
                .AsNoTracking()
                .AnyAsync(item =>
                    item.UserId == leaveRequest.UserId &&
                    item.Role != null &&
                    item.Role.IsActive &&
                    item.Role.Name == "Staff");
        }

        return false;
    }

    private async Task<HashSet<string>> GetPermissionCodesAsync(Guid userId)
    {
        return (await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .Where(item => item.Permission != null && item.Permission.IsActive)
            .Select(item => item.Permission!.Code)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> GetRoleNamesAsync(Guid userId)
    {
        return (await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .Select(item => item.Role!.Name)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
