namespace Hop.Api.Models;

public class FleetVehicle
{
    public Guid Id { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? RegistrationProvince { get; set; }
    public Guid VehicleTypeId { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? ManufactureYear { get; set; }
    public int SeatCapacityTotal { get; set; }
    public int PassengerCapacity { get; set; }
    public string? FuelType { get; set; }
    public decimal CurrentMileage { get; set; }
    public Guid? OwningDepartmentId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public string Status { get; set; } = FleetVehicleStatuses.Available;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public string? ImagePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public FleetVehicleType? VehicleType { get; set; }
    public Department? OwningDepartment { get; set; }
    public User? ResponsibleUser { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public ICollection<FleetVehicleUnavailability> UnavailabilityPeriods { get; set; } = [];
}
