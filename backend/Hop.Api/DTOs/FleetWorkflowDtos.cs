using System.ComponentModel.DataAnnotations;

namespace Hop.Api.DTOs;

public sealed class SaveFleetRequestDto : IValidatableObject
{
    [Required, StringLength(2000)] public string Purpose { get; init; } = string.Empty;
    [Required, StringLength(100)] public string MissionType { get; init; } = string.Empty;
    public Guid? RequestedVehicleTypeId { get; init; }
    [Required, StringLength(1000)] public string Destination { get; init; } = string.Empty;
    [Required, StringLength(200)] public string ContactPersonName { get; init; } = string.Empty;
    [Required, StringLength(50)] public string ContactPhone { get; init; } = string.Empty;
    public DateTime DepartureAt { get; init; }
    public DateTime ExpectedReturnAt { get; init; }
    [Range(1, 200)] public int PassengerCount { get; init; }
    [StringLength(2000)] public string? SpecialRequirement { get; init; }
    public bool IsUrgent { get; init; }
    [StringLength(1000)] public string? UrgentReason { get; init; }
    public Guid? ConcurrencyToken { get; init; }
    public IReadOnlyList<SaveFleetPassengerDto> Passengers { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DepartureAt.Kind != DateTimeKind.Utc || ExpectedReturnAt.Kind != DateTimeKind.Utc)
            yield return new ValidationResult("Departure and return times must be UTC.");
        if (ExpectedReturnAt <= DepartureAt)
            yield return new ValidationResult(
                "วันและเวลากลับต้องอยู่หลังวันและเวลาออกเดินทาง กรุณาตรวจสอบวันที่และเวลาอีกครั้ง",
                [nameof(ExpectedReturnAt)]);
        if (IsUrgent && string.IsNullOrWhiteSpace(UrgentReason))
            yield return new ValidationResult("Urgent reason is required.", [nameof(UrgentReason)]);
        if (Passengers.Count != PassengerCount)
            yield return new ValidationResult("Passenger count must match the passenger list.", [nameof(PassengerCount)]);
        if (Passengers.Count(x => x.IsRequester) > 1)
            yield return new ValidationResult("Only one passenger can be marked as requester.", [nameof(Passengers)]);
    }
}

public sealed class SaveFleetPassengerDto
{
    public Guid? UserId { get; init; }
    [Required, StringLength(200)] public string FullName { get; init; } = string.Empty;
    [StringLength(300)] public string? PositionOrOrganization { get; init; }
    [StringLength(50)] public string? Phone { get; init; }
    [Required] public string PassengerType { get; init; } = "EMPLOYEE";
    public bool IsRequester { get; init; }
    public int SortOrder { get; init; }
}

public sealed record FleetPassengerDto(Guid Id, Guid? UserId, string FullName, string? PositionOrOrganization, string? Phone, string PassengerType, bool IsRequester, int SortOrder);
public sealed record FleetPersonnelOptionDto(Guid Id, string FullName, string? EmployeeCode, Guid? DepartmentId, string? DepartmentName);
public sealed record FleetStatusHistoryDto(Guid Id, string? FromStatus, string ToStatus, string Action, string? ReturnTarget, string? Reason, Guid ActorUserId, string? ActorName, DateTime CreatedAt);
public sealed record FleetAssignmentDto(Guid Id, Guid VehicleId, string VehicleCode, string RegistrationNumber, Guid DriverUserId, string DriverName, DateTime AssignedAt, string AssignmentStatus, bool IsActive, Guid ConcurrencyToken, Guid? ReplacementOfAssignmentId);
public sealed record FleetRequestDto(Guid Id, string RequestNo, Guid RequesterUserId, string RequesterName, Guid? RequesterDepartmentId, string? RequesterDepartmentName, DateOnly RequestDate, string Purpose, string MissionType, Guid? RequestedVehicleTypeId, string? RequestedVehicleTypeName, string Destination, string ContactPersonName, string ContactPhone, DateTime DepartureAt, DateTime ExpectedReturnAt, int PassengerCount, string? SpecialRequirement, bool IsUrgent, string? UrgentReason, string Status, string? ReturnTarget, DateTime? SubmittedAt, DateTime? CancelledAt, string? CancellationReason, DateTime CreatedAt, DateTime? UpdatedAt, Guid ConcurrencyToken, IReadOnlyList<FleetPassengerDto> Passengers, IReadOnlyList<FleetStatusHistoryDto> StatusHistories, FleetAssignmentDto? ActiveAssignment);

public sealed record FleetTransitionRequest(Guid ConcurrencyToken, string? Reason);
public sealed record FleetAssignRequest(Guid VehicleId, Guid DriverUserId, Guid ConcurrencyToken, string? Reason);
public sealed record FleetAvailabilityItem(Guid Id, string Code, string Name, bool IsAvailable, IReadOnlyList<string> Reasons, int MonthTripCount, DateTime? LastAssignmentAt);
public sealed record FleetAvailabilityResponse(IReadOnlyList<FleetAvailabilityItem> Vehicles, IReadOnlyList<FleetAvailabilityItem> Drivers);
public sealed record FleetWorkflowActionRequest(Guid ConcurrencyToken, string? Reason, string? ReturnTarget);
public sealed record FleetTripStartRequest(Guid ConcurrencyToken, decimal StartMileage, string? TripNotes);
public sealed record FleetTripCompleteRequest(Guid ConcurrencyToken, Guid TripConcurrencyToken, decimal EndMileage, decimal? FuelAmount, decimal? FuelCost, string? CompletionNotes, string? OverrideReason);
public sealed record FleetCancellationCreateRequest(Guid ConcurrencyToken, string Reason);
public sealed record FleetCancellationReviewRequest(Guid ConcurrencyToken, Guid RequestConcurrencyToken, string? ReviewReason);
public sealed record FleetDelegationSaveRequest(Guid DelegatorUserId, Guid DelegateUserId, string RequiredPermissionCode, DateTime StartAt, DateTime EndAt, bool IsActive, string Reason, Guid? ConcurrencyToken);
public sealed record FleetAssignmentReplaceRequest(Guid? VehicleId, Guid? DriverUserId, Guid RequestConcurrencyToken, Guid AssignmentConcurrencyToken, string Reason);
public sealed record FleetMileageOverrideRequest(Guid TripConcurrencyToken, decimal StartMileage, decimal EndMileage, string Reason);
public sealed record FleetTripAbortRequest(Guid RequestConcurrencyToken, Guid TripConcurrencyToken, string Reason);
