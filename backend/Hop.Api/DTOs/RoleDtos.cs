namespace Hop.Api.DTOs;

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateRoleRequest(string Name, string? Description, bool IsActive);

public record UpdateRoleRequest(string Name, string? Description, bool IsActive);

public record PermissionResponse(
    Guid Id,
    string Code,
    string Name,
    string Group,
    string Action,
    bool IsActive
);

public record UpdateRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);

public record DashboardSummaryResponse(
    int TotalUsers,
    int TotalDepartments,
    int PendingApprovals,
    int OpenRepairRequests,
    int ActiveBorrowRequests,
    int InventoryItems,
    int StaffOnLeaveToday,
    int StaffOnLeaveThisWeek,
    int StaffOnLeaveThisMonth,
    decimal MyRemainingLeaveDays
);
