namespace Hop.Api.Models;

public class FleetCancellationRequest
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public FleetRequest? FleetRequest { get; set; }
    public User? RequestedByUser { get; set; }
    public User? ReviewedByUser { get; set; }
}
