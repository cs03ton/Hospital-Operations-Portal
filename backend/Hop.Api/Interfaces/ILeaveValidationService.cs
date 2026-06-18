using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public sealed record LeaveValidationResult(bool IsValid, string? Message, decimal CalculatedDays);

public interface ILeaveValidationService
{
    Task<LeaveValidationResult> ValidateDraftAsync(LeaveRequest leaveRequest, Guid? excludeLeaveRequestId = null);
    Task<LeaveValidationResult> ValidateSubmitAsync(LeaveRequest leaveRequest);
}
