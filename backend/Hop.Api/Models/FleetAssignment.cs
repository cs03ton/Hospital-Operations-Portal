namespace Hop.Api.Models;

public class FleetAssignment
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DriverUserId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignmentStatus { get; set; } = FleetAssignmentStatuses.Assigned;
    public string? AssignmentReason { get; set; }
    public Guid? ReplacedAssignmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public FleetRequest? FleetRequest { get; set; }
    public FleetVehicle? Vehicle { get; set; }
    public User? DriverUser { get; set; }
    public User? AssignedByUser { get; set; }
    public FleetAssignment? ReplacedAssignment { get; set; }

    public Guid? ReplacementOfAssignmentId { get => ReplacedAssignmentId; set => ReplacedAssignmentId = value; }
}
