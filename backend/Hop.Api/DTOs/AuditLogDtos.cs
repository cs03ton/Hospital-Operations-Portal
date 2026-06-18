namespace Hop.Api.DTOs;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

public record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string? Username,
    string? Fullname,
    string Action,
    string Resource,
    string? ResourceId,
    string? Detail,
    string? IpAddress,
    string Result,
    DateTime Timestamp
);
