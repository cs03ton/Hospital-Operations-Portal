namespace Hop.Api.Interfaces;

public interface IApprovalEscalationService
{
    Task<int> EscalateOverdueApprovalsAsync(CancellationToken cancellationToken = default);
}
