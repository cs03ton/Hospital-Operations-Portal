using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface IAnnouncementAudienceResolver
{
    Task<AnnouncementAudienceResult> ResolveAsync(Announcement announcement, CancellationToken cancellationToken = default);
}

public sealed record AnnouncementAudienceResult(
    IReadOnlyList<User> Users,
    int TotalMatchedUsers,
    int ActiveUsers,
    int InactiveUsers,
    int LineBoundUsers,
    int LineUnboundUsers
);
