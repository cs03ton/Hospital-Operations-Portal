namespace Hop.Api.Interfaces;

public sealed record LeaveEntitlementInitializationResult(
    Guid UserId,
    int FiscalYear,
    int Created,
    int Skipped,
    int Blocked,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors
);

public interface ILeaveEntitlementService
{
    Task<LeaveEntitlementInitializationResult> InitializeAsync(
        Guid userId,
        int fiscalYear,
        DateOnly effectiveDate,
        Guid? initiatedByUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
