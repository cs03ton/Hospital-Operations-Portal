namespace Hop.Api.Models;

public class FleetTripParticipant
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsRequester { get; set; }
    public string ParticipantType { get; set; } = FleetPassengerTypes.Employee;
    public bool IsActualParticipant { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    public FleetTripRecord? Trip { get; set; }
    public User? User { get; set; }
    public User? CreatedByUser { get; set; }
}
