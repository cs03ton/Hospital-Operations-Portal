using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    private static readonly string[] CoreLeaveTypeCodes = ["VACATION_LEAVE", "PERSONAL_LEAVE", "SICK_LEAVE"];

    [HttpGet("summary")]
    [RequirePermission("Dashboard.View")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary()
    {
        var userId = GetCurrentUserId();
        var permissionCodes = userId is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : await GetPermissionCodesAsync(userId.Value);
        var roleNames = userId is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : await GetRoleNamesAsync(userId.Value);
        var isAdmin = roleNames.Contains("Admin") || roleNames.Contains("SuperAdmin");
        var isSuperAdmin = roleNames.Contains("SuperAdmin");
        var canViewTeamDashboard = permissionCodes.Contains(LeavePermissions.ViewDepartment) ||
            permissionCodes.Contains(LeavePermissions.ViewAll);
        var canViewAdminDashboard = isAdmin ||
            permissionCodes.Contains("UserManagement.View") ||
            permissionCodes.Contains("DepartmentManagement.View") ||
            permissionCodes.Contains(LeavePermissions.ManageTypes) ||
            permissionCodes.Contains(LeavePermissions.ManageHolidays) ||
            permissionCodes.Contains(LeavePermissions.ManageApprovalChains);
        var canViewAuditDashboard = isAdmin || isSuperAdmin || permissionCodes.Contains("SystemSettings.View");
        var canViewSecurityDashboard = isSuperAdmin || permissionCodes.Contains("SystemSettings.View");
        var canViewPendingApprovals = permissionCodes.Contains(LeavePermissions.ViewPendingApproval) ||
            permissionCodes.Contains(LeavePermissions.ApproveCurrentStep);

        var totalUsers = canViewAdminDashboard ? await db.Users.CountAsync(user => user.IsActive) : 0;
        var totalDepartments = canViewAdminDashboard ? await db.Departments.CountAsync(department => department.IsActive) : 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-1 * (int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(6);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var todayStart = DateTime.UtcNow.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var pendingApprovals = userId is null || !canViewPendingApprovals
            ? 0
            : await db.LeaveApprovals.CountAsync(item =>
                item.ApproverId == userId &&
                item.Status == "Pending" &&
                item.LeaveRequest != null &&
                item.LeaveRequest.Status == "Pending" &&
                item.LeaveRequest.CurrentApproverId == userId);

        var canViewLeaveOverview = canViewTeamDashboard || canViewAdminDashboard || roleNames.Contains("Director");
        var totalPendingLeaveRequests = canViewAdminDashboard || permissionCodes.Contains(LeavePermissions.SupportViewAll)
            ? await db.LeaveRequests.CountAsync(item => item.Status == "Pending")
            : 0;
        var staffOnLeaveToday = canViewLeaveOverview ? await CountDistinctApprovedLeaveUsers(today, today) : 0;
        var staffOnLeaveThisWeek = canViewLeaveOverview ? await CountDistinctApprovedLeaveUsers(weekStart, weekEnd) : 0;
        var staffOnLeaveThisMonth = canViewLeaveOverview ? await CountDistinctApprovedLeaveUsers(monthStart, monthEnd) : 0;

        var myCoreLeaveBalances = userId is null
            ? Array.Empty<DashboardLeaveBalanceResponse>()
            : await LoadMyCoreLeaveBalances(userId.Value, today);

        var myLeaveQuery = userId is null
            ? db.LeaveRequests.Where(item => false)
            : db.LeaveRequests.Where(item => item.UserId == userId);
        var myLeaveRequestsTotal = await myLeaveQuery.CountAsync();
        var myLeaveRequestsPending = await myLeaveQuery.CountAsync(item => item.Status == "Pending");
        var myLeaveRequestsApproved = await myLeaveQuery.CountAsync(item => item.Status == "Approved");
        var myLeaveRequestsRejected = await myLeaveQuery.CountAsync(item => item.Status == "Rejected");
        var myLeaveRequestsCancelled = await myLeaveQuery.CountAsync(item => item.Status == "Cancelled");
        var totalLeaveTypes = canViewAdminDashboard ? await db.LeaveTypes.CountAsync(item => item.IsActive) : 0;
        var totalApprovalRules = canViewAdminDashboard ? await db.ApprovalChains.CountAsync(item => item.IsActive) : 0;
        var totalHolidaysThisYear = canViewAdminDashboard ? await db.LeaveHolidays.CountAsync(item => item.IsActive && item.HolidayDate.Year == today.Year) : 0;
        var totalAuditLogsToday = canViewAuditDashboard ? await db.AuditLogs.CountAsync(item => item.CreatedAt >= todayStart && item.CreatedAt < tomorrowStart) : 0;
        var loginEventsToday = canViewAuditDashboard ? await db.AuditLogs.CountAsync(item => item.CreatedAt >= todayStart && item.CreatedAt < tomorrowStart && item.Action.Contains("Login")) : 0;
        var failedLoginEventsToday = canViewSecurityDashboard ? await db.AuditLogs.CountAsync(item => item.CreatedAt >= todayStart && item.CreatedAt < tomorrowStart && item.Action.Contains("Login") && item.Result != "Success") : 0;
        var permissionDeniedEventsToday = canViewSecurityDashboard ? await db.AuditLogs.CountAsync(item => item.CreatedAt >= todayStart && item.CreatedAt < tomorrowStart && item.Action.Contains("PermissionDenied")) : 0;
        var unreadNotifications = userId is null
            ? 0
            : await db.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead);
        var lineQueued = canViewSecurityDashboard ? await db.LineDeliveryLogs.CountAsync(item => item.Status == "Queued") : 0;
        var lineFailed = canViewSecurityDashboard ? await db.LineDeliveryLogs.CountAsync(item => item.Status == "Failed") : 0;
        var databaseStatus = canViewSecurityDashboard ? (await db.Database.CanConnectAsync() ? "Healthy" : "Unavailable") : "Restricted";
        var applicationVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        return ApiResponse<DashboardSummaryResponse>.Ok(new DashboardSummaryResponse(
            totalUsers,
            totalDepartments,
            pendingApprovals,
            totalPendingLeaveRequests,
            OpenRepairRequests: 0,
            ActiveBorrowRequests: 0,
            InventoryItems: 0,
            staffOnLeaveToday,
            staffOnLeaveThisWeek,
            staffOnLeaveThisMonth,
            MyRemainingLeaveDays: 0,
            myLeaveRequestsTotal,
            myLeaveRequestsPending,
            myLeaveRequestsApproved,
            myLeaveRequestsRejected,
            myLeaveRequestsCancelled,
            totalLeaveTypes,
            totalApprovalRules,
            totalHolidaysThisYear,
            totalAuditLogsToday,
            loginEventsToday,
            failedLoginEventsToday,
            permissionDeniedEventsToday,
            unreadNotifications,
            lineQueued,
            lineFailed,
            "Healthy",
            databaseStatus,
            applicationVersion,
            myCoreLeaveBalances
        ));
    }

    private async Task<HashSet<string>> GetPermissionCodesAsync(Guid userId)
    {
        return (await db.UserRoles
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
                .SelectMany(item => item.Role!.RolePermissions)
                .Where(item => item.Permission != null && item.Permission.IsActive)
                .Select(item => item.Permission!.Code)
                .Distinct()
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> GetRoleNamesAsync(Guid userId)
    {
        return (await db.UserRoles
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
                .Select(item => item.Role!.Name)
                .Distinct()
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private Task<int> CountDistinctApprovedLeaveUsers(DateOnly startDate, DateOnly endDate)
    {
        return db.LeaveRequests
            .Where(item => item.Status == "Approved")
            .Where(item => item.StartDate <= endDate && item.EndDate >= startDate)
            .Select(item => item.UserId)
            .Distinct()
            .CountAsync();
    }

    private async Task<IReadOnlyList<DashboardLeaveBalanceResponse>> LoadMyCoreLeaveBalances(Guid userId, DateOnly today)
    {
        var leaveTypes = await db.LeaveTypes
            .AsNoTracking()
            .Where(item => item.IsActive && CoreLeaveTypeCodes.Contains(item.Code))
            .ToListAsync();

        var leaveTypeIds = leaveTypes.Select(item => item.Id).ToHashSet();
        var targetYearsByLeaveTypeId = leaveTypes.ToDictionary(
            item => item.Id,
            item => FiscalYearHelper.ResolveBalanceYear(today, item));

        var balances = await db.LeaveBalances
            .AsNoTracking()
            .Where(item => item.UserId == userId && leaveTypeIds.Contains(item.LeaveTypeId))
            .ToListAsync();

        return leaveTypes
            .OrderBy(item => Array.IndexOf(CoreLeaveTypeCodes, item.Code))
            .Select(leaveType =>
            {
                var targetYear = targetYearsByLeaveTypeId[leaveType.Id];
                var balance = balances.FirstOrDefault(item => item.LeaveTypeId == leaveType.Id && item.Year == targetYear)
                    ?? balances
                        .Where(item => item.LeaveTypeId == leaveType.Id)
                        .OrderByDescending(item => item.Year)
                        .FirstOrDefault();
                var entitled = balance?.EntitledDays ?? leaveType.DefaultDaysPerYear;
                var carriedOver = balance?.CarriedOverDays ?? 0;
                var adjusted = balance?.AdjustedDays ?? 0;
                var used = balance?.UsedDays ?? 0;
                var pending = balance?.PendingDays ?? 0;

                return new DashboardLeaveBalanceResponse(
                    leaveType.Code,
                    leaveType.Name,
                    entitled + carriedOver + adjusted,
                    used,
                    pending,
                    FiscalYearHelper.CalculateAvailableDays(entitled, carriedOver, used, pending, adjusted));
            })
            .ToList();
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
