using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface ILeaveBalanceReconciliationService
{
    Task<LeaveBalanceUsage> GetUsageAsync(Guid userId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default);
    Task<LeaveBalanceReconciliationResponse> PreviewAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default);
    Task<LeaveBalanceReconciliationResponse> ConfirmAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default);
}

public record LeaveBalanceUsage(decimal UsedDays, decimal PendingDays);
