using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public sealed record LeavePaymentSegmentResult(
    decimal Days,
    string PaymentSource,
    string PaymentStatus,
    string Label,
    string? Notes
);

public sealed record LeavePolicyPreviewResult(
    string? EmploymentType,
    string EmploymentTypeName,
    int FiscalYear,
    decimal EntitlementDays,
    decimal CarriedOverDays,
    decimal AdjustedDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays,
    decimal RequestedDays,
    bool RequiresBalance,
    bool CanSubmit,
    string? LimitStatus,
    decimal? EmployerPaidLimitDays,
    decimal? MaximumLeaveDays,
    decimal? MaximumTotalAvailableDays,
    bool RequiresSpecialApproval,
    IReadOnlyList<LeavePaymentSegmentResult> PaymentSegments,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> PolicyNotes
);

public sealed record LeaveCarryOverPolicyResult(
    bool AllowCarryOver,
    decimal CarryOverCap,
    decimal CarryOverDays,
    decimal ForfeitedDays,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors
);

public interface ILeavePolicyService
{
    Task<LeavePolicyRule?> GetPolicyAsync(Guid userId, Guid leaveTypeId, int fiscalYear, CancellationToken cancellationToken = default);
    Task<decimal> CalculateEntitlementAsync(Guid userId, Guid leaveTypeId, int fiscalYear, CancellationToken cancellationToken = default);
    Task<LeavePolicyPreviewResult> ValidateLeaveRequestAsync(
        Guid userId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string? durationType,
        decimal requestedDays,
        CancellationToken cancellationToken = default);
    Task<LeavePolicyPreviewResult> CalculateAvailableDaysAsync(Guid userId, Guid leaveTypeId, int fiscalYear, decimal requestedDays = 0, DateOnly? referenceDate = null, CancellationToken cancellationToken = default);
    Task<LeaveCarryOverPolicyResult> CalculateCarryOverAsync(Guid userId, Guid leaveTypeId, int fromFiscalYear, decimal endYearRemaining, CancellationToken cancellationToken = default);
    string? ValidateMinimumService(User user, LeavePolicyRule policy, DateOnly asOfDate);
    string? ValidateGenderRequirement(User user, LeaveType leaveType);
    Task<IReadOnlyList<string>> ValidateCarryOverAsync(Guid userId, LeaveType leaveType, int fiscalYear, CancellationToken cancellationToken = default);
}
