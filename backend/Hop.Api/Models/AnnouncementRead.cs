namespace Hop.Api.Models;

public class AnnouncementRead
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }

    public Announcement? Announcement { get; set; }
    public User? User { get; set; }
}
