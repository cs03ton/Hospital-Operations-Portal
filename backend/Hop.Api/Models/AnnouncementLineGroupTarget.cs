namespace Hop.Api.Models;

public sealed class AnnouncementLineGroupTarget
{
    public Guid AnnouncementId { get; set; }
    public Guid DestinationId { get; set; }
    public Announcement? Announcement { get; set; }
    public LineGroupDestination? Destination { get; set; }
}
