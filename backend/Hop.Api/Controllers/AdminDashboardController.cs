using System.Security.Claims;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class AdminDashboardController(
    AppDbContext db,
    IHealthCenterService healthCenterService,
    LineConfigurationResolver lineConfiguration) : ControllerBase
{
    private static readonly string[] AdminRoles = ["Admin", "SuperAdmin"];
    private static readonly string[] ImportantPermissions =
    [
        "AdminDashboard.View",
        "System.Health.View",
        "System.Line.TestSend",
        "UserManagement.View",
        "DepartmentManagement.View",
        "RoleManagement.View",
        "LeaveAdmin.ManageBalances",
        "LeaveAdmin.ManageApprovalChains",
        "LeaveApproval.ApproveCurrentStep"
    ];

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminDashboardResponse>>> Get(CancellationToken cancellationToken)
    {
        if (!await CanAccessAdminDashboard(cancellationToken))
        {
            return Forbid();
        }

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var since = now.AddDays(-7);
        var activeUsersQuery = db.Users.AsNoTracking().Where(user => user.IsActive);
        var activeUserIds = activeUsersQuery.Select(user => user.Id);
        var adminUserIds = db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.Role != null && AdminRoles.Contains(userRole.Role.Name))
            .Select(userRole => userRole.UserId);

        var boundLineUserIds = db.LineUserBindings
            .AsNoTracking()
            .Where(binding => binding.Status == "Bound" && binding.UserId != null)
            .Select(binding => binding.UserId!.Value);
        var usersWithLineIds = activeUsersQuery
            .Where(user => !string.IsNullOrWhiteSpace(user.LineUserId))
            .Select(user => user.Id);
        var boundUserIds = boundLineUserIds
            .Union(usersWithLineIds);
        var activeUsersWithoutBalance = activeUsersQuery
            .Where(user => !adminUserIds.Contains(user.Id))
            .Where(user => !db.LeaveBalances.Any(balance => balance.UserId == user.Id))
            .Select(user => user.Id);

        var users = new AdminDashboardUserSummaryResponse(
            await db.Users.CountAsync(cancellationToken),
            await activeUsersQuery.CountAsync(cancellationToken),
            await db.Users.CountAsync(user => !user.IsActive, cancellationToken),
            await activeUsersQuery.CountAsync(user => !boundUserIds.Contains(user.Id), cancellationToken),
            await activeUsersQuery.CountAsync(user => string.IsNullOrWhiteSpace(user.EmploymentType), cancellationToken),
            await activeUsersQuery
                .Where(user => !adminUserIds.Contains(user.Id))
                .CountAsync(user => user.LeaveApprovalRuleId == null, cancellationToken));

        var departmentIdsWithUsers = activeUsersQuery
            .Where(user => user.DepartmentId != null)
            .Select(user => user.DepartmentId!.Value)
            .Distinct();
        var departmentIdsWithHeads = db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.Role != null &&
                userRole.Role.IsActive &&
                userRole.Role.Name == "DepartmentHead" &&
                userRole.User != null &&
                userRole.User.IsActive &&
                userRole.User.DepartmentId != null)
            .Select(userRole => userRole.User!.DepartmentId!.Value)
            .Distinct();
        var activeDepartments = db.Departments.AsNoTracking().Where(department => department.IsActive);
        var departments = new AdminDashboardDepartmentSummaryResponse(
            await activeDepartments.CountAsync(cancellationToken),
            await activeDepartments.CountAsync(department => !departmentIdsWithHeads.Contains(department.Id), cancellationToken),
            await activeDepartments.CountAsync(department => !departmentIdsWithUsers.Contains(department.Id), cancellationToken));

        var importantPermissionsUnassigned = 0;
        foreach (var permissionCode in ImportantPermissions)
        {
            var assigned = await db.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.Permission != null &&
                    rolePermission.Role != null &&
                    rolePermission.Permission.Code == permissionCode &&
                    rolePermission.Permission.IsActive &&
                    rolePermission.Role.IsActive,
                    cancellationToken);
            if (!assigned)
            {
                importantPermissionsUnassigned++;
            }
        }

        var roles = new AdminDashboardRolePermissionSummaryResponse(
            await db.Roles.CountAsync(role => role.IsActive, cancellationToken),
            await db.Permissions.CountAsync(permission => permission.IsActive, cancellationToken),
            await db.Roles.CountAsync(role => role.IsActive && !role.UserRoles.Any(), cancellationToken),
            importantPermissionsUnassigned);

        var boundUsers = await activeUsersQuery.CountAsync(user => boundUserIds.Contains(user.Id), cancellationToken);
        var line = new AdminDashboardLineSummaryResponse(
            lineConfiguration.Enabled,
            boundUsers,
            Math.Max(users.Active - boundUsers, 0),
            await db.LineDeliveryLogs
                .AsNoTracking()
                .Where(log => log.Status == "Failed")
                .OrderByDescending(log => log.UpdatedAt ?? log.CreatedAt)
                .Select(log => (DateTime?)(log.UpdatedAt ?? log.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken));

        var pendingApprovals = await db.LeaveApprovals.CountAsync(item =>
            item.Status == "Pending" &&
            item.LeaveRequest != null &&
            item.LeaveRequest.Status == "Pending",
            cancellationToken);
        var leave = new AdminDashboardLeaveSummaryResponse(
            pendingApprovals,
            await db.LeaveRequests.CountAsync(item => item.CreatedAt >= todayStart && item.CreatedAt < tomorrowStart, cancellationToken),
            await activeUsersWithoutBalance.CountAsync(cancellationToken),
            users.MissingApprovalRule);

        var healthCenter = await healthCenterService.GetHealthAsync(cancellationToken);
        var health = new AdminDashboardHealthSummaryResponse(
            healthCenter.OverallStatus,
            healthCenter.Api,
            healthCenter.Database,
            healthCenter.Storage,
            healthCenter.Line,
            healthCenter.Disk,
            healthCenter.Backup);

        var recentAdminActions = await db.AuditLogs
            .AsNoTracking()
            .Include(log => log.User)
            .Where(log => log.CreatedAt >= since)
            .Where(log => log.Action.StartsWith("User.") ||
                log.Action.StartsWith("Department.") ||
                log.Action.StartsWith("Role.") ||
                log.Action.StartsWith("Permission") ||
                log.Action.StartsWith("System.") ||
                log.Action.StartsWith("Line."))
            .OrderByDescending(log => log.CreatedAt)
            .Take(5)
            .Select(log => new AdminDashboardAuditActionResponse(
                log.CreatedAt,
                log.Action,
                log.EntityName,
                log.Result,
                log.User == null ? null : log.User.FullName))
            .ToListAsync(cancellationToken);

        var audit = new AdminDashboardAuditSummaryResponse(
            await db.AuditLogs.CountAsync(log =>
                log.CreatedAt >= since &&
                log.Action.Contains("Login") &&
                log.Result != "Success",
                cancellationToken),
            await db.AuditLogs.CountAsync(log =>
                log.CreatedAt >= since &&
                log.Action.Contains("PermissionDenied"),
                cancellationToken),
            recentAdminActions);

        return ApiResponse<AdminDashboardResponse>.Ok(new AdminDashboardResponse(
            users,
            departments,
            roles,
            line,
            leave,
            health,
            audit));
    }

    private async Task<bool> CanAccessAdminDashboard(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .AnyAsync(rolePermission => rolePermission.Permission!.Code == "AdminDashboard.View", cancellationToken);
    }
}
