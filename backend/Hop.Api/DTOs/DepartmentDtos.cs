namespace Hop.Api.DTOs;

public record DepartmentDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateDepartmentRequest(string Name, string? Description, bool IsActive);

public record UpdateDepartmentRequest(string Name, string? Description, bool IsActive);
