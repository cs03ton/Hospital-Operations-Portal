namespace Hop.Api.Models;

public static class FleetVehicleStatuses
{
    public const string Available = "AVAILABLE";
    public const string Reserved = "RESERVED";
    public const string InUse = "IN_USE";
    public const string Maintenance = "MAINTENANCE";
    public const string TemporarilyUnavailable = "TEMPORARILY_UNAVAILABLE";
    public const string Decommissioned = "DECOMMISSIONED";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Available, Reserved, InUse, Maintenance, TemporarilyUnavailable, Decommissioned
    };
}

public static class FleetDriverStatuses
{
    public const string Available = "AVAILABLE";
    public const string Unavailable = "UNAVAILABLE";
    public const string Suspended = "SUSPENDED";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Available, Unavailable, Suspended
    };
}

public static class FleetVehicleUnavailabilityTypes
{
    public const string Maintenance = "MAINTENANCE";
    public const string Inspection = "INSPECTION";
    public const string RecurringDuty = "RECURRING_DUTY";
    public const string TemporarilyUnavailable = "TEMPORARILY_UNAVAILABLE";
    public const string Other = "OTHER";
}

public static class FleetRequestStatuses
{
    public const string Draft = "DRAFT";
    public const string PendingDispatch = "PENDING_DISPATCH";
    public const string PendingAdminReview = "PENDING_ADMIN_REVIEW";
    public const string PendingDirector = "PENDING_DIRECTOR";
    public const string Approved = "APPROVED";
    public const string PendingDriverAck = "PENDING_DRIVER_ACK";
    public const string Ready = "READY";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string CancellationPending = "CANCELLATION_PENDING";
    public const string Returned = "RETURNED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
    public const string Aborted = "ABORTED";
}

public static class FleetReturnTargets
{
    public const string Requester = "REQUESTER";
    public const string Dispatcher = "DISPATCHER";
    public const string AdminReview = "ADMIN_REVIEW";
}

public static class FleetPassengerTypes
{
    public const string Employee = "EMPLOYEE";
    public const string External = "EXTERNAL";
}

public static class FleetAssignmentStatuses
{
    public const string Assigned = "ASSIGNED";
    public const string Replaced = "REPLACED";
    public const string Cancelled = "CANCELLED";
    public const string Completed = "COMPLETED";
    public const string Aborted = "ABORTED";
}
