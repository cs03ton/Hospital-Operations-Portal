namespace Hop.Api.DTOs;

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int UsersCount = 0,
    int PermissionsCount = 0
);

public record CreateRoleRequest(string Name, string? Description, bool IsActive);

public record UpdateRoleRequest(string Name, string? Description, bool IsActive);

public record PermissionResponse(
    Guid Id,
    string Code,
    string Name,
    string Group,
    string Action,
    bool IsActive,
    int RolesCount = 0,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);

public record UpdateRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);

public record DashboardSummaryResponse(
    int TotalUsers,
    int TotalDepartments,
    int PendingApprovals,
    int TotalPendingLeaveRequests,
    int OpenRepairRequests,
    int ActiveBorrowRequests,
    int InventoryItems,
    int StaffOnLeaveToday,
    int StaffOnLeaveThisWeek,
    int StaffOnLeaveThisMonth,
    decimal MyRemainingLeaveDays,
    int MyLeaveRequestsTotal,
    int MyLeaveRequestsDraft,
    int MyLeaveRequestsPending,
    int MyLeaveRequestsReturnedForRevision,
    int MyLeaveRequestsApproved,
    int MyLeaveRequestsRejected,
    int MyLeaveRequestsCancelled,
    int MyLeaveCancellationRequestsPending,
    int TotalLeaveTypes,
    int TotalApprovalRules,
    int TotalHolidaysThisYear,
    int TotalAuditLogsToday,
    int LoginEventsToday,
    int FailedLoginEventsToday,
    int PermissionDeniedEventsToday,
    int UnreadNotifications,
    int LineQueued,
    int LineFailed,
    string ApiHealth,
    string DatabaseStatus,
    string ApplicationVersion,
    IReadOnlyList<DashboardLeaveBalanceResponse>? MyCoreLeaveBalances = null,
    DashboardLeaveRequestGroupResponse? MyPendingRequests = null,
    DashboardLeaveRequestGroupResponse? DepartmentRequests = null,
    DashboardLeaveRequestGroupResponse? MyRecentLeaveRequests = null,
    DashboardLeaveCancellationSummaryResponse? LeaveCancellationSummary = null
);

public record DashboardLeaveBalanceResponse(
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal EntitledDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays
);

public record DashboardLeaveRequestGroupResponse(
    int Count,
    IReadOnlyList<DashboardLeaveRequestItemResponse> Items
);

public record DashboardLeaveRequestItemResponse(
    Guid Id,
    string? RequestNumber,
    string RequesterName,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Status,
    string? CurrentApproverName,
    DateTime CreatedAt,
    string SourceType = "LeaveRequest",
    string? DetailPath = null
);

public record DashboardLeaveCancellationSummaryResponse(
    int Total,
    int Pending,
    int Approved,
    int Rejected,
    int Cancelled,
    int ReturnedForRevision,
    int Draft,
    int PendingApprovalsForMe,
    int ApprovedToday,
    int RejectedToday,
    decimal RestoredDaysThisYear,
    decimal RestoredDaysTotal,
    decimal? AverageApprovalHours,
    decimal ApprovalRate,
    decimal RejectionRate,
    IReadOnlyList<DashboardLeaveCancellationTrendResponse> MonthlyTrend,
    IReadOnlyList<DashboardLeaveCancellationBreakdownResponse> ByLeaveType,
    IReadOnlyList<DashboardLeaveCancellationBreakdownResponse> ByDepartment,
    DashboardLeaveRequestGroupResponse RecentRequests
);

public record DashboardLeaveCancellationTrendResponse(
    string Month,
    int RequestCount,
    decimal RestoredDays
);

public record DashboardLeaveCancellationBreakdownResponse(
    string Name,
    int RequestCount,
    decimal RestoredDays
);

public record ExecutiveDashboardResponse(
    ExecutiveKpiResponse Kpis,
    ExecutiveTodaySummaryResponse TodaySummary,
    IReadOnlyList<ExecutiveMonthlyTrendResponse> MonthlyTrend,
    IReadOnlyList<ExecutiveDepartmentLeaveResponse> LeaveByDepartment,
    IReadOnlyList<ExecutiveLeaveTypeResponse> LeaveByType,
    IReadOnlyList<ExecutiveYearlySummaryResponse> YearlySummary,
    ExecutiveSystemHealthResponse SystemHealth
);

public record ExecutiveKpiResponse(
    int TotalActiveUsers,
    int PresentToday,
    int OnLeaveToday,
    int PendingApprovals,
    int DirectorPendingApprovals,
    int ApprovedToday,
    int RejectedToday,
    decimal LeaveRate,
    decimal? ApprovalSlaHours
);

public record ExecutiveTodaySummaryResponse(
    int TotalLeaveToday,
    int SickLeaveToday,
    int PersonalLeaveToday,
    int VacationLeaveToday,
    int PendingApprovals,
    int ApprovedToday,
    int RejectedToday,
    string? TopDepartmentToday
);

public record ExecutiveMonthlyTrendResponse(
    string Month,
    decimal SickLeaveDays,
    decimal PersonalLeaveDays,
    decimal VacationLeaveDays,
    decimal TotalDays
);

public record ExecutiveDepartmentLeaveResponse(
    string DepartmentName,
    int UserCount,
    decimal TotalDays
);

public record ExecutiveLeaveTypeResponse(
    string LeaveTypeCode,
    string LeaveTypeName,
    int RequestCount,
    decimal TotalDays
);

public record ExecutiveYearlySummaryResponse(
    int FiscalYear,
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal UsedDays
);

public record ExecutiveSystemHealthResponse(
    HealthComponentResponse Api,
    HealthComponentResponse Database,
    StorageHealthResponse Storage,
    LineHealthResponse Line,
    DiskHealthResponse Disk,
    BackupHealthResponse Backup,
    string Version,
    string Environment
);

public record AdminDashboardResponse(
    AdminDashboardUserSummaryResponse Users,
    AdminDashboardDepartmentSummaryResponse Departments,
    AdminDashboardRolePermissionSummaryResponse Roles,
    AdminDashboardLineSummaryResponse Line,
    AdminDashboardLeaveSummaryResponse Leave,
    AdminDashboardHealthSummaryResponse Health,
    AdminDashboardAuditSummaryResponse Audit
);

public record AdminDashboardUserSummaryResponse(
    int Total,
    int Active,
    int Inactive,
    int MissingLineBinding,
    int MissingEmploymentType,
    int MissingApprovalRule
);

public record AdminDashboardDepartmentSummaryResponse(
    int Total,
    int WithoutHead,
    int WithoutUsers
);

public record AdminDashboardRolePermissionSummaryResponse(
    int Total,
    int Permissions,
    int UnusedRoles,
    int ImportantPermissionsUnassigned
);

public record AdminDashboardLineSummaryResponse(
    bool Enabled,
    int BoundUsers,
    int UnboundUsers,
    DateTime? LastFailedDeliveryAt
);

public record AdminDashboardLeaveSummaryResponse(
    int PendingApprovals,
    int TodayRequests,
    int MissingBalances,
    int MissingApprovalRules
);

public record AdminDashboardHealthSummaryResponse(
    string OverallStatus,
    HealthComponentResponse Api,
    HealthComponentResponse Database,
    StorageHealthResponse Storage,
    LineHealthResponse Line,
    DiskHealthResponse Disk,
    BackupHealthResponse Backup
);

public record AdminDashboardAuditSummaryResponse(
    int RecentFailedLogins,
    int RecentPermissionDenied,
    IReadOnlyList<AdminDashboardAuditActionResponse> RecentAdminActions
);

public record AdminDashboardAuditActionResponse(
    DateTime CreatedAt,
    string Action,
    string EntityName,
    string Result,
    string? ActorName
);
