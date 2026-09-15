namespace Hop.Api.Services;

public interface IFleetLineGroupEventMapper
{
    string? ToCanonical(string scope, string sourceEventType);
}

public sealed class FleetLineGroupEventMapper : IFleetLineGroupEventMapper
{
    private static readonly IReadOnlyDictionary<string, string> ExactMappings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Fleet.RequestSubmitted"] = "Fleet.RequestSubmitted",
        ["Fleet.VehicleAssigned"] = "Fleet.AssignmentCreated",
        ["Fleet.Assigned"] = "Fleet.AssignmentCreated",
        ["Fleet.AdminReviewApproved"] = "Fleet.AdminReviewed",
        ["Fleet.RequestReturned"] = "Fleet.Returned",
        ["Fleet.DirectorApproved"] = "Fleet.DirectorApproved",
        ["Fleet.RequestRejected"] = "Fleet.Rejected",
        ["Fleet.RequestCancelled"] = "Fleet.Cancelled",
        ["Fleet.CancellationApproved"] = "Fleet.Cancelled",
        ["Fleet.AssignmentReplaced"] = "Fleet.AssignmentChanged",
        ["Fleet.DriverAccepted"] = "Fleet.DriverAcknowledged",
        ["Fleet.TripCompleted"] = "Fleet.TripCompleted",
        ["Fleet.TripOverdue"] = "Fleet.TripOverdue",
        ["MeetingRoom.BookingCreated"] = "MeetingRoom.BookingCreated",
        ["Repair.Submitted"] = "Repair.Submitted", ["Repair.Resubmitted"] = "Repair.Resubmitted",
        ["Repair.Reopened"] = "Repair.Reopened", ["Repair.Started"] = "Repair.Started",
        ["Repair.Resumed"] = "Repair.Resumed", ["Repair.Solved"] = "Repair.Solved", ["Repair.Closed"] = "Repair.Closed"
    };

    public string? ToCanonical(string scope, string sourceEventType)
    {
        if (string.IsNullOrWhiteSpace(sourceEventType) || (scope != "FLEET" && scope != "MEETING_ROOM" && scope != "REPAIR")) return null;
        return ExactMappings.GetValueOrDefault(sourceEventType);
    }
}
