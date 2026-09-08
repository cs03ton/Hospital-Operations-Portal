namespace Hop.Api.Models;

public class FleetVehicleUnavailability
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Type { get; set; } = FleetVehicleUnavailabilityTypes.Other;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public FleetVehicle? Vehicle { get; set; }
    public User? CreatedByUser { get; set; }
}
