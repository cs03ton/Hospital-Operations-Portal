using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface IPendingApprovalNotificationService
{
    Task<IReadOnlyList<PendingApprovalNotificationResponse>> GetMyPendingApprovalsAsync(Guid userId, CancellationToken cancellationToken = default);
}
