using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public class LeaveNotificationEventPublisher(ILogger<LeaveNotificationEventPublisher> logger) : ILeaveNotificationEventPublisher
{
    public Task PublishAsync(string eventName, Guid leaveRequestId, Guid? recipientUserId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Leave notification event prepared. Event={EventName}, LeaveRequestId={LeaveRequestId}, RecipientUserId={RecipientUserId}",
            eventName,
            leaveRequestId,
            recipientUserId);

        return Task.CompletedTask;
    }
}
