using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public sealed record LeaveBalanceValidationResult(
    bool IsValid,
    string? Message,
    decimal EntitledDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays,
    bool RequiresBalance
);

public interface ILeaveBalanceValidationService
{
    Task<LeaveBalanceValidationResult> ValidateAvailableBalanceAsync(
        LeaveRequest leaveRequest,
        LeaveType leaveType,
        decimal requestedDays);
}
