namespace Hop.Api.DTOs;

public record UserResponse(
    Guid Id,
    string? EmployeeCode,
    string Fullname,
    string Username,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> Roles,
    Guid? DepartmentId,
    string? Department,
    string? LineUserId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateUserRequest(
    string? EmployeeCode,
    string Fullname,
    string Username,
    string Password,
    IReadOnlyList<Guid> RoleIds,
    Guid? DepartmentId,
    string? LineUserId,
    bool IsActive
);

public record UpdateUserRequest(
    string? EmployeeCode,
    string Fullname,
    IReadOnlyList<Guid> RoleIds,
    Guid? DepartmentId,
    string? LineUserId,
    bool IsActive,
    string? Password
);
