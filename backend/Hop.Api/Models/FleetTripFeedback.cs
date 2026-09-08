namespace Hop.Api.Models;

public class FleetTripFeedback
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid VehicleAssignmentId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DriverUserId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public int PunctualityRating { get; set; }
    public int SafetyRating { get; set; }
    public int ServiceRating { get; set; }
    public int OverallRating { get; set; }
    public int VehicleConditionRating { get; set; }
    public int VehicleCleanlinessRating { get; set; }
    public bool HasIncident { get; set; }
    public string? IncidentCategory { get; set; }
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public FleetTripRecord? Trip { get; set; }
    public FleetRequest? FleetRequest { get; set; }
    public FleetAssignment? VehicleAssignment { get; set; }
    public FleetVehicle? Vehicle { get; set; }
    public User? DriverUser { get; set; }
    public User? SubmittedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}

public static class FleetFeedbackIncidentCategories
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "DRIVING", "PUNCTUALITY", "SERVICE", "VEHICLE_CONDITION", "CLEANLINESS", "OTHER"
    };
}
