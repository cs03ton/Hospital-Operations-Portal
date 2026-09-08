namespace Hop.Api.Models;

public class FleetDriverUnavailability
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public User? CreatedByUser { get; set; }
}
