using System.ComponentModel.DataAnnotations;

namespace Hop.Api.DTOs;

public sealed record FleetVehicleTypeResponse(Guid Id, string Code, string Name, string? Description, int SortOrder, bool IsActive);

public sealed class SaveFleetVehicleTypeRequest
{
    [Required, StringLength(50)] public string Code { get; init; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(1000)] public string? Description { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record FleetVehicleResponse(
    Guid Id, string VehicleCode, string RegistrationNumber, string? RegistrationProvince,
    Guid VehicleTypeId, string VehicleTypeName, string? Brand, string? Model, int? ManufactureYear,
    int SeatCapacityTotal, int PassengerCapacity, string? FuelType, decimal CurrentMileage,
    Guid? OwningDepartmentId, Guid? ResponsibleUserId, string Status, bool IsActive, string? Note, string? ImagePath);

public sealed class SaveFleetVehicleRequest : IValidatableObject
{
    [Required, StringLength(50)] public string VehicleCode { get; init; } = string.Empty;
    [Required, StringLength(50)] public string RegistrationNumber { get; init; } = string.Empty;
    [StringLength(100)] public string? RegistrationProvince { get; init; }
    public Guid VehicleTypeId { get; init; }
    [StringLength(100)] public string? Brand { get; init; }
    [StringLength(100)] public string? Model { get; init; }
    [Range(1900, 3000)] public int? ManufactureYear { get; init; }
    [Range(1, 200)] public int SeatCapacityTotal { get; init; }
    [Range(1, 200)] public int PassengerCapacity { get; init; }
    [StringLength(50)] public string? FuelType { get; init; }
    [Range(typeof(decimal), "0", "9999999999")] public decimal CurrentMileage { get; init; }
    public Guid? OwningDepartmentId { get; init; }
    public Guid? ResponsibleUserId { get; init; }
    [Required, StringLength(40)] public string Status { get; init; } = "AVAILABLE";
    public bool IsActive { get; init; } = true;
    [StringLength(2000)] public string? Note { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PassengerCapacity > SeatCapacityTotal)
            yield return new ValidationResult("Passenger capacity cannot exceed total seat capacity.", [nameof(PassengerCapacity)]);
    }
}

public sealed record FleetDriverProfileResponse(
    Guid Id, Guid UserId, string EmployeeCode, string FullName, string LicenseNumber, string LicenseType,
    DateOnly? LicenseIssueDate, DateOnly? LicenseExpiryDate, bool CanDriveSedan, bool CanDrivePickup,
    bool CanDriveVan, bool CanDriveAmbulance, bool CanDriveOther, string DriverStatus, bool IsActive, string? Note);

public sealed class SaveFleetDriverProfileRequest : IValidatableObject
{
    public Guid UserId { get; init; }
    [Required, StringLength(100)] public string LicenseNumber { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LicenseType { get; init; } = string.Empty;
    public DateOnly? LicenseIssueDate { get; init; }
    public DateOnly? LicenseExpiryDate { get; init; }
    public bool CanDriveSedan { get; init; }
    public bool CanDrivePickup { get; init; }
    public bool CanDriveVan { get; init; }
    public bool CanDriveAmbulance { get; init; }
    public bool CanDriveOther { get; init; }
    [Required, StringLength(40)] public string DriverStatus { get; init; } = "AVAILABLE";
    public bool IsActive { get; init; } = true;
    [StringLength(2000)] public string? Note { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LicenseIssueDate is not null && LicenseExpiryDate is not null && LicenseExpiryDate < LicenseIssueDate)
            yield return new ValidationResult("License expiry date cannot be before issue date.", [nameof(LicenseExpiryDate)]);
    }
}
