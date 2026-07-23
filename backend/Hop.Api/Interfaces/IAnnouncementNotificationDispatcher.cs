using Hop.Api.DTOs;
using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface IAnnouncementNotificationDispatcher
{
    Task<AnnouncementNotificationPreviewResponse> PreviewAsync(
        Announcement announcement,
        bool? notifyInApp = null,
        bool? notifyViaLine = null,
        CancellationToken cancellationToken = default);

    Task DispatchAsync(
        Announcement announcement,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);
}
