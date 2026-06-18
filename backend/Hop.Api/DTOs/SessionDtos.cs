namespace Hop.Api.DTOs;

public record SessionResponse(
    Guid Id,
    Guid UserId,
    string? Username,
    string? Fullname,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string? RevokedReason,
    string? CreatedByIp,
    string? UserAgent,
    DateTime? LastUsedAt,
    bool IsActive
);
