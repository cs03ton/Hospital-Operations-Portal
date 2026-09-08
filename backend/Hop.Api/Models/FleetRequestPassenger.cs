namespace Hop.Api.Models;

public class FleetRequestPassenger
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PositionOrOrganization { get; set; }
    public string? Phone { get; set; }
    public string PassengerType { get; set; } = FleetPassengerTypes.Employee;
    public bool IsRequester { get; set; }
    public int SortOrder { get; set; }

    public FleetRequest? FleetRequest { get; set; }
    public User? User { get; set; }
}
