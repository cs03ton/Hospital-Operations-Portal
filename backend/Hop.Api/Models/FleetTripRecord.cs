namespace Hop.Api.Models;

public class FleetTripRecord
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid DriverUserId { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public decimal? StartMileage { get; set; }
    public decimal? EndMileage { get; set; }
    public decimal? FuelAmount { get; set; }
    public decimal? FuelCost { get; set; }
    public string? TripNotes { get; set; }
    public string? CompletionNotes { get; set; }
    public string? StartIdempotencyKey { get; set; }
    public string? CompletionIdempotencyKey { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public string? OverrideReason { get; set; }
    public bool IsAborted { get; set; }
    public DateTime? AbortedAt { get; set; }
    public Guid? AbortedByUserId { get; set; }
    public string? AbortReason { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public FleetRequest? FleetRequest { get; set; }
    public FleetAssignment? Assignment { get; set; }
    public User? DriverUser { get; set; }
    public User? CompletedByUser { get; set; }
    public User? AbortedByUser { get; set; }
    public ICollection<FleetTripParticipant> Participants { get; set; } = [];
    public ICollection<FleetTripFeedback> Feedbacks { get; set; } = [];
}
