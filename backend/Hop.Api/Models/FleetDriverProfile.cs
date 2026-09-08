namespace Hop.Api.Models;

public class FleetDriverProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateOnly? LicenseIssueDate { get; set; }
    public DateOnly? LicenseExpiryDate { get; set; }
    public bool CanDriveSedan { get; set; }
    public bool CanDrivePickup { get; set; }
    public bool CanDriveVan { get; set; }
    public bool CanDriveAmbulance { get; set; }
    public bool CanDriveOther { get; set; }
    public string DriverStatus { get; set; } = FleetDriverStatuses.Available;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public User? User { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}
