namespace Hop.Api.Models;

public class FleetRequestStatusHistory
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ReturnTarget { get; set; }
    public string? Reason { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }

    public FleetRequest? FleetRequest { get; set; }
    public User? ActorUser { get; set; }
}
