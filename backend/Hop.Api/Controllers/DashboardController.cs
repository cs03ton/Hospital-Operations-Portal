using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(
    AppDbContext db,
    IConfiguration? configuration = null,
    IWebHostEnvironment? environment = null,
    LineConfigurationResolver? lineConfiguration = null) : ControllerBase
{
    private static readonly string[] CoreLeaveTypeCodes = ["VACATION_LEAVE", "PERSONAL_LEAVE", "SICK_LEAVE"];
    private const string ExecutivePermission = "Dashboard.Executive.View";
    private const string ExecutiveSummaryPermission = "LeaveDashboard.ViewExecutiveSummary";
    private readonly IConfiguration configuration = configuration ?? new ConfigurationBuilder().Build();
    private readonly IWebHostEnvironment? environment = environment;
    private readonly LineConfigurationResolver lineConfiguration = lineConfiguration ?? new LineConfigurationResolver(
        Options.Create(new LineOptions()),
        configuration ?? new ConfigurationBuilder().Build());

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
        var myLeaveRequestsDraft = await myLeaveQuery.CountAsync(item => item.Status == "Draft");
        var myLeaveRequestsPending = await myLeaveQuery.CountAsync(item => item.Status == "Pending");
        var myLeaveRequestsReturnedForRevision = await myLeaveQuery.CountAsync(item => item.Status == "ReturnedForRevision");
        var myLeaveRequestsApproved = await myLeaveQuery.CountAsync(item => item.Status == "Approved");
        var myLeaveRequestsRejected = await myLeaveQuery.CountAsync(item => item.Status == "Rejected");
        var myLeaveRequestsCancelled = await myLeaveQuery.CountAsync(item => item.Status == "Cancelled");
        var myPendingRequests = userId is null
            ? EmptyLeaveRequestGroup()
            : await LoadMyPendingLeaveRequests(userId.Value);
        var departmentRequests = userId is null
            ? EmptyLeaveRequestGroup()
            : await LoadDepartmentLeaveRequests(userId.Value, canViewTeamDashboard);
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
            myLeaveRequestsDraft,
            myLeaveRequestsPending,
            myLeaveRequestsReturnedForRevision,
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
            myCoreLeaveBalances,
            myPendingRequests,
            departmentRequests
        ));
    }

    [HttpGet("executive")]
    public async Task<ActionResult<ApiResponse<ExecutiveDashboardResponse>>> GetExecutiveDashboard(
        [FromQuery] int? trendMonth,
        [FromQuery] int? trendYear,
        [FromQuery] int? fiscalYear,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanAccessExecutiveDashboard(currentUserId.Value))
        {
            return Forbid();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = DateTime.UtcNow.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var selectedYear = Math.Clamp(NormalizeCalendarYear(trendYear ?? today.Year), 2000, 2200);
        var requestedTrendMonth = trendMonth.GetValueOrDefault(0);
        var selectedMonth = requestedTrendMonth is >= 1 and <= 12 ? requestedTrendMonth : (int?)null;
        var trendStart = selectedMonth.HasValue
            ? new DateOnly(selectedYear, selectedMonth.Value, 1)
            : new DateOnly(selectedYear, 1, 1);
        var trendEnd = selectedMonth.HasValue
            ? trendStart.AddMonths(1).AddDays(-1)
            : new DateOnly(selectedYear, 12, 31);
        var selectedFiscalYear = Math.Clamp(NormalizeCalendarYear(fiscalYear ?? FiscalYearHelper.GetFiscalYear(today)), 2000, 2600);
        var fiscalYearStart = new DateOnly(selectedFiscalYear - 1, FiscalYearHelper.StartMonth, FiscalYearHelper.StartDay);
        var fiscalYearEnd = fiscalYearStart.AddYears(1).AddDays(-1);

        var totalActiveUsers = await db.Users.CountAsync(user => user.IsActive, cancellationToken);
        var approvedTodayQuery = db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Where(item => item.Status == "Approved")
            .Where(item => item.StartDate <= today && item.EndDate >= today);
        var approvedTodayLeaves = await approvedTodayQuery.ToListAsync(cancellationToken);
        var onLeaveToday = approvedTodayLeaves.Select(item => item.UserId).Distinct().Count();
        var pendingApprovals = await db.LeaveRequests.CountAsync(item => item.Status == "Pending", cancellationToken);
        var directorPendingApprovals = currentUserId is null
            ? 0
            : await db.LeaveApprovals.CountAsync(item =>
                item.ApproverId == currentUserId &&
                item.Status == "Pending" &&
                item.LeaveRequest != null &&
                item.LeaveRequest.Status == "Pending" &&
                item.LeaveRequest.CurrentApproverId == currentUserId,
                cancellationToken);
        var approvedToday = await db.LeaveRequests.CountAsync(item =>
            item.Status == "Approved" &&
            item.UpdatedAt >= todayStart &&
            item.UpdatedAt < tomorrowStart,
            cancellationToken);
        var rejectedToday = await db.LeaveRequests.CountAsync(item =>
            item.Status == "Rejected" &&
            item.UpdatedAt >= todayStart &&
            item.UpdatedAt < tomorrowStart,
            cancellationToken);
        var approvalSlaHours = await CalculateApprovalSlaHours(todayStart.AddDays(-90), cancellationToken);
        var leaveRate = totalActiveUsers == 0
            ? 0
            : Math.Round(onLeaveToday * 100m / totalActiveUsers, 2);

        var topDepartmentToday = approvedTodayLeaves
            .Where(item => item.User?.Department != null)
            .GroupBy(item => item.User!.Department!.Name)
            .Select(group => new
            {
                DepartmentName = group.Key,
                UserCount = group.Select(item => item.UserId).Distinct().Count()
            })
            .OrderByDescending(item => item.UserCount)
            .ThenBy(item => item.DepartmentName)
            .FirstOrDefault()?.DepartmentName;

        var todaySummary = new ExecutiveTodaySummaryResponse(
            onLeaveToday,
            CountDistinctTodayByLeaveCode(approvedTodayLeaves, "SICK_LEAVE"),
            CountDistinctTodayByLeaveCode(approvedTodayLeaves, "PERSONAL_LEAVE"),
            CountDistinctTodayByLeaveCode(approvedTodayLeaves, "VACATION_LEAVE"),
            pendingApprovals,
            approvedToday,
            rejectedToday,
            topDepartmentToday);

        var monthlyTrend = await BuildMonthlyTrend(trendStart, trendEnd, cancellationToken);
        var leaveByDepartment = await BuildLeaveByDepartment(trendStart, trendEnd, cancellationToken);
        var leaveByType = await BuildLeaveByType(trendStart, trendEnd, cancellationToken);
        var yearlySummary = await BuildYearlySummary(selectedFiscalYear, fiscalYearStart, fiscalYearEnd, cancellationToken);
        var systemHealth = await BuildExecutiveSystemHealth(cancellationToken);

        return ApiResponse<ExecutiveDashboardResponse>.Ok(new ExecutiveDashboardResponse(
            new ExecutiveKpiResponse(
                totalActiveUsers,
                Math.Max(totalActiveUsers - onLeaveToday, 0),
                onLeaveToday,
                pendingApprovals,
                directorPendingApprovals,
                approvedToday,
                rejectedToday,
                leaveRate,
                approvalSlaHours),
            todaySummary,
            monthlyTrend,
            leaveByDepartment,
            leaveByType,
            yearlySummary,
            systemHealth));
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

    private async Task<DashboardLeaveRequestGroupResponse> LoadMyPendingLeaveRequests(Guid userId)
    {
        var query = LoadDashboardLeaveRequests()
            .Where(item => item.UserId == userId && item.Status == "Pending")
            .OrderByDescending(item => item.CreatedAt);

        return await BuildLeaveRequestGroup(query);
    }

    private async Task<DashboardLeaveRequestGroupResponse> LoadDepartmentLeaveRequests(Guid userId, bool canViewTeamDashboard)
    {
        if (!canViewTeamDashboard)
        {
            return EmptyLeaveRequestGroup();
        }

        var departmentId = await db.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => item.DepartmentId)
            .FirstOrDefaultAsync();

        if (departmentId is null)
        {
            return EmptyLeaveRequestGroup();
        }

        var departmentStatuses = new[] { "Pending", "Approved", "ReturnedForRevision", "Rejected", "Cancelled" };
        var query = LoadDashboardLeaveRequests()
            .Where(item =>
                item.UserId != userId &&
                item.User != null &&
                item.User.DepartmentId == departmentId &&
                departmentStatuses.Contains(item.Status))
            .OrderByDescending(item => item.CreatedAt);

        return await BuildLeaveRequestGroup(query);
    }

    private async Task<DashboardLeaveRequestGroupResponse> BuildLeaveRequestGroup(IQueryable<LeaveRequest> query)
    {
        var count = await query.CountAsync();
        var items = await query
            .Take(5)
            .Select(item => new DashboardLeaveRequestItemResponse(
                item.Id,
                item.RequestNumber,
                item.User != null ? item.User.FullName : "-",
                item.LeaveType != null ? item.LeaveType.Name : null,
                item.StartDate,
                item.EndDate,
                item.TotalDays,
                item.Status,
                item.CurrentApprover != null ? item.CurrentApprover.FullName : null,
                item.CreatedAt))
            .ToListAsync();

        return new DashboardLeaveRequestGroupResponse(count, items);
    }

    private IQueryable<LeaveRequest> LoadDashboardLeaveRequests()
    {
        return db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover);
    }

    private static DashboardLeaveRequestGroupResponse EmptyLeaveRequestGroup()
    {
        return new DashboardLeaveRequestGroupResponse(0, Array.Empty<DashboardLeaveRequestItemResponse>());
    }

    private async Task<bool> CanAccessExecutiveDashboard(Guid userId)
    {
        var roleNames = await GetRoleNamesAsync(userId);
        if (roleNames.Contains("Director") || roleNames.Contains("Admin") || roleNames.Contains("SuperAdmin"))
        {
            return true;
        }

        var permissionCodes = await GetPermissionCodesAsync(userId);
        return permissionCodes.Contains(ExecutivePermission) ||
            permissionCodes.Contains(ExecutiveSummaryPermission);
    }

    private async Task<decimal?> CalculateApprovalSlaHours(DateTime from, CancellationToken cancellationToken)
    {
        var completed = await db.LeaveRequests
            .AsNoTracking()
            .Where(item => (item.Status == "Approved" || item.Status == "Rejected") &&
                item.SubmittedAt != null &&
                item.UpdatedAt != null &&
                item.UpdatedAt >= from)
            .Select(item => new { item.SubmittedAt, item.UpdatedAt })
            .ToListAsync(cancellationToken);

        if (completed.Count == 0)
        {
            return null;
        }

        var averageHours = completed
            .Select(item => (item.UpdatedAt!.Value - item.SubmittedAt!.Value).TotalHours)
            .Where(value => value >= 0)
            .DefaultIfEmpty()
            .Average();

        return Math.Round((decimal)averageHours, 1);
    }

    private static int CountDistinctTodayByLeaveCode(IEnumerable<LeaveRequest> requests, string code)
    {
        return requests
            .Where(item => string.Equals(item.LeaveType?.Code, code, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.UserId)
            .Distinct()
            .Count();
    }

    private static int NormalizeCalendarYear(int year)
    {
        return year >= 2400 ? year - 543 : year;
    }

    private async Task<IReadOnlyList<ExecutiveMonthlyTrendResponse>> BuildMonthlyTrend(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
    {
        var rows = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Where(item => item.Status == "Approved" &&
                item.StartDate <= periodEnd &&
                item.EndDate >= periodStart &&
                item.LeaveType != null &&
                CoreLeaveTypeCodes.Contains(item.LeaveType.Code))
            .Select(item => new
            {
                item.StartDate,
                item.EndDate,
                item.TotalDays,
                LeaveTypeCode = item.LeaveType!.Code
            })
            .ToListAsync(cancellationToken);

        var monthCount = ((periodEnd.Year - periodStart.Year) * 12) + periodEnd.Month - periodStart.Month + 1;
        return Enumerable.Range(0, monthCount)
            .Select(index => new DateOnly(periodStart.Year, periodStart.Month, 1).AddMonths(index))
            .Select(month =>
            {
                var monthEnd = month.AddMonths(1).AddDays(-1);
                var monthRows = rows
                    .Where(item => item.StartDate <= monthEnd && item.EndDate >= month)
                    .ToList();
                var sick = monthRows.Where(item => item.LeaveTypeCode == "SICK_LEAVE").Sum(item => item.TotalDays);
                var personal = monthRows.Where(item => item.LeaveTypeCode == "PERSONAL_LEAVE").Sum(item => item.TotalDays);
                var vacation = monthRows.Where(item => item.LeaveTypeCode == "VACATION_LEAVE").Sum(item => item.TotalDays);

                return new ExecutiveMonthlyTrendResponse(
                    month.ToString("yyyy-MM"),
                    sick,
                    personal,
                    vacation,
                    sick + personal + vacation);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<ExecutiveDepartmentLeaveResponse>> BuildLeaveByDepartment(DateOnly monthStart, DateOnly monthEnd, CancellationToken cancellationToken)
    {
        var rows = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Where(item => item.Status == "Approved" &&
                item.StartDate <= monthEnd &&
                item.EndDate >= monthStart)
            .Select(item => new
            {
                DepartmentName = item.User != null && item.User.Department != null ? item.User.Department.Name : "ไม่ระบุหน่วยงาน",
                item.UserId,
                item.TotalDays
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(item => item.DepartmentName)
            .Select(group => new ExecutiveDepartmentLeaveResponse(
                group.Key,
                group.Select(item => item.UserId).Distinct().Count(),
                group.Sum(item => item.TotalDays)))
            .OrderByDescending(item => item.UserCount)
            .ThenByDescending(item => item.TotalDays)
            .Take(10)
            .ToList();
    }

    private async Task<IReadOnlyList<ExecutiveLeaveTypeResponse>> BuildLeaveByType(DateOnly monthStart, DateOnly monthEnd, CancellationToken cancellationToken)
    {
        var rows = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Where(item => item.Status == "Approved" &&
                item.StartDate <= monthEnd &&
                item.EndDate >= monthStart &&
                item.LeaveType != null &&
                CoreLeaveTypeCodes.Contains(item.LeaveType.Code))
            .Select(item => new
            {
                LeaveTypeCode = item.LeaveType!.Code,
                LeaveTypeName = item.LeaveType.Name,
                item.TotalDays
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(item => new { item.LeaveTypeCode, item.LeaveTypeName })
            .Select(group => new ExecutiveLeaveTypeResponse(
                group.Key.LeaveTypeCode,
                group.Key.LeaveTypeName,
                group.Count(),
                group.Sum(item => item.TotalDays)))
            .OrderByDescending(item => item.TotalDays)
            .ToList();
    }

    private async Task<IReadOnlyList<ExecutiveYearlySummaryResponse>> BuildYearlySummary(int fiscalYear, DateOnly fiscalYearStart, DateOnly fiscalYearEnd, CancellationToken cancellationToken)
    {
        var rows = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Where(item => item.Status == "Approved" &&
                item.StartDate <= fiscalYearEnd &&
                item.EndDate >= fiscalYearStart &&
                item.LeaveType != null)
            .Select(item => new
            {
                LeaveTypeCode = item.LeaveType!.Code,
                LeaveTypeName = item.LeaveType.Name,
                item.TotalDays
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(item => new { item.LeaveTypeCode, item.LeaveTypeName })
            .Select(group => new ExecutiveYearlySummaryResponse(
                fiscalYear,
                group.Key.LeaveTypeCode,
                group.Key.LeaveTypeName,
                group.Sum(item => item.TotalDays)))
            .OrderByDescending(item => item.UsedDays)
            .ToList();
    }

    private async Task<ExecutiveSystemHealthResponse> BuildExecutiveSystemHealth(CancellationToken cancellationToken)
    {
        var database = await CheckDatabase(cancellationToken);
        return new ExecutiveSystemHealthResponse(
            new HealthComponentResponse("Healthy"),
            database,
            CheckStorage(),
            await CheckLine(cancellationToken),
            CheckDisk(),
            CheckBackup(),
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            environment?.EnvironmentName ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown");
    }

    private async Task<HealthComponentResponse> CheckDatabase(CancellationToken cancellationToken)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? new HealthComponentResponse("Healthy")
                : new HealthComponentResponse("Unhealthy", "ไม่สามารถเชื่อมต่อฐานข้อมูลได้");
        }
        catch (Exception)
        {
            return new HealthComponentResponse("Unhealthy", "ไม่สามารถตรวจสอบฐานข้อมูลได้");
        }
    }

    private StorageHealthResponse CheckStorage()
    {
        var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
        try
        {
            Directory.CreateDirectory(rootPath);
            return new StorageHealthResponse("Healthy", true);
        }
        catch (Exception)
        {
            return new StorageHealthResponse("Unhealthy", false, "ไม่สามารถเข้าถึง storage ได้");
        }
    }

    private async Task<LineHealthResponse> CheckLine(CancellationToken cancellationToken)
    {
        var lastSuccess = await db.LineDeliveryLogs
            .AsNoTracking()
            .Where(item => item.Status == "Sent" || item.Status == "Success")
            .OrderByDescending(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
            .Select(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var lastFailure = await db.LineDeliveryLogs
            .AsNoTracking()
            .Where(item => item.Status == "Failed")
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Select(item => item.UpdatedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var status = !lineConfiguration.Enabled
            ? "Disabled"
            : lineConfiguration.HasAccessToken && lineConfiguration.HasChannelSecret
                ? "Healthy"
                : "Warning";

        return new LineHealthResponse(
            status,
            lineConfiguration.Enabled,
            lastSuccess == default ? null : lastSuccess,
            lastFailure == default ? null : lastFailure);
    }

    private DiskHealthResponse CheckDisk()
    {
        try
        {
            var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? AppContext.BaseDirectory;
            var root = Path.GetPathRoot(Path.GetFullPath(rootPath));
            if (string.IsNullOrWhiteSpace(root))
            {
                return new DiskHealthResponse("Unknown", null);
            }

            var drive = new DriveInfo(root);
            var usedPercent = drive.TotalSize <= 0
                ? null
                : (double?)Math.Round((1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100, 2);
            var status = usedPercent is null ? "Unknown" : usedPercent >= 90 ? "Unhealthy" : usedPercent >= 80 ? "Warning" : "Healthy";
            return new DiskHealthResponse(status, usedPercent);
        }
        catch (Exception)
        {
            return new DiskHealthResponse("Unknown", null);
        }
    }

    private BackupHealthResponse CheckBackup()
    {
        var backupRoot = configuration["Backup:RootPath"] ?? configuration["BACKUP_ROOT"] ?? "backups";
        try
        {
            if (!Directory.Exists(backupRoot))
            {
                return new BackupHealthResponse("Warning", null, "ยังไม่พบโฟลเดอร์ backup");
            }

            var lastBackup = Directory
                .EnumerateFiles(backupRoot, "*", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => (DateTime?)file.LastWriteTimeUtc)
                .FirstOrDefault();
            return lastBackup is null
                ? new BackupHealthResponse("Warning", null, "ยังไม่พบไฟล์ backup")
                : new BackupHealthResponse("Healthy", lastBackup);
        }
        catch (Exception)
        {
            return new BackupHealthResponse("Warning", null, "ไม่สามารถตรวจสอบ backup ได้");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
