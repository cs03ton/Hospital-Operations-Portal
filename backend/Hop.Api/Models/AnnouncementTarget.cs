namespace Hop.Api.Models;

public class AnnouncementTarget
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public string TargetType { get; set; } = AnnouncementTargetTypes.Everyone;
    public string? TargetValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Announcement? Announcement { get; set; }
}
