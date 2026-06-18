using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface ILineMessagingService
{
    Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default);
    Task<int> RetryPendingDeliveriesAsync(CancellationToken cancellationToken = default);
}
