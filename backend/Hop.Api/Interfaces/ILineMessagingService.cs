using Hop.Api.DTOs;
using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface ILineMessagingService
{
    Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default);
    Task NotifyUserAsync(Guid userId, string eventName, string message, Guid? leaveRequestId = null, CancellationToken cancellationToken = default);
    Task<LineDeliveryLog> NotifyUserPayloadAsync(Guid userId, string eventName, string payload, Guid? leaveRequestId = null, CancellationToken cancellationToken = default);
    Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, CancellationToken cancellationToken = default);
    Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, string eventName, CancellationToken cancellationToken = default);
    Task<LineTestSendResponse> SendRawPayloadToLineUserAsync(string toUserId, string payload, string eventName, Guid? leaveRequestId = null, CancellationToken cancellationToken = default);
    Task<LineConnectionValidationResponse> ValidateConnectionAsync(IReadOnlyList<LineChecklistItemResponse> checklist, CancellationToken cancellationToken = default);
    Task<int> RetryPendingDeliveriesAsync(CancellationToken cancellationToken = default);
}
