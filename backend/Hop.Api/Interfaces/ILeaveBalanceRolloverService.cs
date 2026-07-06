using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface ILeaveBalanceRolloverService
{
    Task<LeaveBalanceRolloverBatchResponse> PreviewAsync(LeaveBalanceRolloverFilterRequest request, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<LeaveBalanceRolloverBatchResponse> ConfirmAsync(LeaveBalanceRolloverConfirmBatchRequest request, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportPreviewCsvAsync(LeaveBalanceRolloverFilterRequest request, Guid? actorUserId, CancellationToken cancellationToken = default);
}
