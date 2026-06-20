namespace Hop.Api.Interfaces;

public interface ILeaveNotificationEventPublisher
{
    Task PublishAsync(string eventName, Guid leaveRequestId, Guid? recipientUserId, CancellationToken cancellationToken = default);
}
