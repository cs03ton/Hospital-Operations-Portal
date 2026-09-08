namespace Hop.Api.Models;

public class FleetCapability
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "GENERAL";
    public string DataType { get; set; } = FleetCapabilityDataTypes.Boolean;
    public string? Unit { get; set; }
    public decimal? MinimumNumericValue { get; set; }
    public decimal? MaximumNumericValue { get; set; }
    public string? EnumOptionsJson { get; set; }
    public bool IsRequiredSafetyCapability { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public class FleetVehicleCapability
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid CapabilityId { get; set; }
    public bool? BooleanValue { get; set; }
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public string? EnumValue { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public FleetVehicle? Vehicle { get; set; }
    public FleetCapability? Capability { get; set; }
}

public class FleetRequestRequiredCapability
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid CapabilityId { get; set; }
    public string Operator { get; set; } = FleetCapabilityOperators.Equals;
    public bool? RequiredBooleanValue { get; set; }
    public decimal? RequiredNumericValue { get; set; }
    public string? RequiredTextValue { get; set; }
    public string? RequiredEnumValue { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FleetRequest? FleetRequest { get; set; }
    public FleetCapability? Capability { get; set; }
}

public class FleetCompatibilityOverride
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string MismatchCapabilityIds { get; set; } = "[]";
    public string Reason { get; set; } = string.Empty;
    public Guid ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public FleetRequest? FleetRequest { get; set; }
    public FleetVehicle? Vehicle { get; set; }
    public FleetAssignment? Assignment { get; set; }
    public User? ApprovedByUser { get; set; }
}

public class FleetEmergencyPostReview
{
    public Guid Id { get; set; }
    public Guid FleetRequestId { get; set; }
    public Guid ReviewedByUserId { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public string Outcome { get; set; } = FleetEmergencyReviewOutcomes.Acceptable;
    public bool WasBypassAppropriate { get; set; }
    public string? ResponseTimeAssessment { get; set; }
    public string? SafetyIssues { get; set; }
    public string? FollowUpActions { get; set; }
    public string? Notes { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FleetRequest? FleetRequest { get; set; }
    public User? ReviewedByUser { get; set; }
}

public class FleetTripAttachment
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public FleetTripRecord? Trip { get; set; }
    public User? CreatedByUser { get; set; }
}

public class FleetEmergencyPolicy
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Priority { get; set; } = FleetPriorities.Emergency;
    public int ResponseTargetMinutes { get; set; }
    public int DispatchTargetMinutes { get; set; }
    public int DriverAcknowledgementTargetMinutes { get; set; }
    public bool ApprovalBypassAllowed { get; set; }
    public bool PostReviewRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public static class FleetCapabilityDataTypes { public const string Boolean="BOOLEAN"; public const string Number="NUMBER"; public const string Text="TEXT"; public const string Enum="ENUM"; public static readonly HashSet<string> All=[Boolean,Number,Text,Enum]; }
public static class FleetCapabilityOperators { public new const string Equals="EQUALS"; public const string GreaterThanOrEqual="GREATER_THAN_OR_EQUAL"; public const string LessThanOrEqual="LESS_THAN_OR_EQUAL"; public const string Contains="CONTAINS"; public const string In="IN"; public static readonly HashSet<string> All=[Equals,GreaterThanOrEqual,LessThanOrEqual,Contains,In]; }
public static class FleetCompatibilityStatuses { public const string Match="MATCH"; public const string PartialMatch="PARTIAL_MATCH"; public const string NotMatch="NOT_MATCH"; public const string Overridden="OVERRIDDEN"; }
public static class FleetPriorities { public const string Normal="NORMAL"; public const string Urgent="URGENT"; public const string Emergency="EMERGENCY"; public static readonly HashSet<string> All=[Normal,Urgent,Emergency]; }
public static class FleetEmergencyReviewOutcomes { public const string Acceptable="ACCEPTABLE"; public const string NeedsImprovement="NEEDS_IMPROVEMENT"; public const string PolicyViolation="POLICY_VIOLATION"; public static readonly HashSet<string> All=[Acceptable,NeedsImprovement,PolicyViolation]; }
