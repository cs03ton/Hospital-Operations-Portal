namespace Hop.Api.Models;

public class FleetRequest
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = string.Empty;
    public Guid RequesterUserId { get; set; }
    public Guid? RequesterDepartmentId { get; set; }
    public DateOnly RequestDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string MissionType { get; set; } = string.Empty;
    public Guid? RequestedVehicleTypeId { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime DepartureAt { get; set; }
    public DateTime ExpectedReturnAt { get; set; }
    public int PassengerCount { get; set; }
    public string? SpecialRequirement { get; set; }
    public bool IsUrgent { get; set; }
    public string? UrgentReason { get; set; }
    public string Priority { get; set; } = FleetPriorities.Normal;
    public string? EmergencyReason { get; set; }
    public Guid? ReportedByUserId { get; set; }
    public DateTime? ReportedAt { get; set; }
    public string? IncidentLocation { get; set; }
    public DateTime? RequestedDepartureAt { get; set; }
    public string? EmergencyPolicyCode { get; set; }
    public Guid? EmergencyPolicyId { get; set; }
    public int? ResponseTargetMinutesSnapshot { get; set; }
    public int? DispatchTargetMinutesSnapshot { get; set; }
    public int? DriverAcknowledgementTargetMinutesSnapshot { get; set; }
    public bool? ApprovalBypassAllowedSnapshot { get; set; }
    public bool? PostReviewRequiredSnapshot { get; set; }
    public bool RequiresPostReview { get; set; }
    public Guid? EmergencyDeclaredByUserId { get; set; }
    public DateTime? EmergencyDeclaredAt { get; set; }
    public string Status { get; set; } = FleetRequestStatuses.Draft;
    public string? ReturnTarget { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public User? RequesterUser { get; set; }
    public Department? RequesterDepartment { get; set; }
    public FleetVehicleType? RequestedVehicleType { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public ICollection<FleetRequestPassenger> Passengers { get; set; } = [];
    public ICollection<FleetRequestStatusHistory> StatusHistories { get; set; } = [];
    public ICollection<FleetAssignment> Assignments { get; set; } = [];
    public ICollection<FleetCancellationRequest> CancellationRequests { get; set; } = [];
    public ICollection<FleetRequestRequiredCapability> RequiredCapabilities { get; set; } = [];
    public FleetEmergencyPolicy? EmergencyPolicy { get; set; }
}
