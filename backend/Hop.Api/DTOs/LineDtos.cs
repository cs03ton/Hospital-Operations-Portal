namespace Hop.Api.DTOs;

public record LeaveNotificationMessage(
    Guid LeaveRequestId,
    Guid UserId,
    string Fullname,
    string LeaveTypeName,
    string Status,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Remark
);
