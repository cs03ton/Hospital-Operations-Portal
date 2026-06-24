namespace Hop.Api.DTOs;

public record UserResponse(
    Guid Id,
    string? EmployeeCode,
    string Fullname,
    string Username,
    string? Position,
    string? Email,
    string? PhoneNumber,
    string? LeaveContactAddress,
    string? ProfileImageUrl,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> Roles,
    Guid? DepartmentId,
    string? Department,
    Guid? LeaveApprovalRuleId,
    string? LeaveApprovalRuleName,
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
    Guid? LeaveApprovalRuleId,
    string? LineUserId,
    bool IsActive
);

public record UpdateUserRequest(
    string? EmployeeCode,
    string Fullname,
    IReadOnlyList<Guid> RoleIds,
    Guid? DepartmentId,
    Guid? LeaveApprovalRuleId,
    string? LineUserId,
    bool IsActive,
    string? Password
);

public record UserProfileResponse(
    Guid Id,
    string? EmployeeCode,
    string Fullname,
    string Username,
    string? Position,
    string? Email,
    string? PhoneNumber,
    string? LeaveContactAddress,
    string? ProfileImageUrl,
    IReadOnlyList<string> Roles,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? LeaveApprovalRuleId,
    string? LeaveApprovalRuleName,
    string? LineUserId,
    bool IsActive,
    IReadOnlyList<string> Permissions
);

public record UpdateUserProfileRequest(
    string Fullname,
    string? Position,
    string? Email,
    string? PhoneNumber,
    string? LeaveContactAddress,
    string? ProfileImageUrl
);
