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
    int MyLeaveRequestsPending,
    int MyLeaveRequestsApproved,
    int MyLeaveRequestsRejected,
    int MyLeaveRequestsCancelled,
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
    IReadOnlyList<DashboardLeaveBalanceResponse>? MyCoreLeaveBalances = null
);

public record DashboardLeaveBalanceResponse(
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal EntitledDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays
);
