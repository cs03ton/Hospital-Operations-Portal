using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public static class FleetFeedbackStatuses
{
    public const string Available = "AVAILABLE";
    public const string Submitted = "SUBMITTED";
    public const string Expired = "EXPIRED";
    public const string NotEligible = "NOT_ELIGIBLE";
}

public sealed record FleetFeedbackEligibility(
    Guid TripId,
    Guid UserId,
    string Status,
    bool CanSubmit,
    DateTime? CompletedAt,
    DateTime? FeedbackDeadline,
    string? Reason);

public interface IFleetFeedbackEligibilityService
{
    Task<FleetFeedbackEligibility> EvaluateAsync(Guid tripId, Guid userId, DateTime? utcNow = null, CancellationToken ct = default);
}

public sealed class FleetFeedbackEligibilityService(
    AppDbContext db,
    IOptions<FleetOperationsOptions> options) : IFleetFeedbackEligibilityService
{
    public async Task<FleetFeedbackEligibility> EvaluateAsync(Guid tripId, Guid userId, DateTime? utcNow = null, CancellationToken ct = default)
    {
        var trip = await db.FleetTripRecords
            .AsNoTracking()
            .Where(x => x.Id == tripId)
            .Select(x => new
            {
                x.Id,
                x.DriverUserId,
                x.ActualEndAt,
                RequestStatus = x.FleetRequest!.Status,
                HasFeedback = x.Feedbacks.Any(f => f.SubmittedByUserId == userId),
                IsParticipant = x.Participants.Any(p =>
                    p.UserId == userId &&
                    p.ParticipantType == FleetPassengerTypes.Employee &&
                    p.IsActualParticipant)
            })
            .SingleOrDefaultAsync(ct);

        if (trip is null)
            return NotEligible(tripId, userId, null, null, "TRIP_NOT_FOUND");

        if (trip.DriverUserId == userId)
            return NotEligible(tripId, userId, trip.ActualEndAt, null, "DRIVER_CANNOT_REVIEW_OWN_TRIP");

        if (!trip.IsParticipant)
            return NotEligible(tripId, userId, trip.ActualEndAt, null, "NOT_ACTUAL_PARTICIPANT");

        if (trip.RequestStatus != FleetRequestStatuses.Completed || trip.ActualEndAt is null)
            return NotEligible(tripId, userId, trip.ActualEndAt, null, "TRIP_NOT_COMPLETED");

        var windowDays = Math.Max(1, options.Value.FeedbackWindowDays);
        var deadline = trip.ActualEndAt.Value.AddDays(windowDays);
        if (trip.HasFeedback)
            return new(tripId, userId, FleetFeedbackStatuses.Submitted, false, trip.ActualEndAt, deadline, "FEEDBACK_ALREADY_SUBMITTED");
        var now = utcNow ?? DateTime.UtcNow;
        if (now < trip.ActualEndAt.Value)
            return NotEligible(tripId, userId, trip.ActualEndAt, deadline, "FEEDBACK_NOT_OPEN");

        if (now > deadline)
            return new(tripId, userId, FleetFeedbackStatuses.Expired, false, trip.ActualEndAt, deadline, "FEEDBACK_WINDOW_EXPIRED");

        return new(tripId, userId, FleetFeedbackStatuses.Available, true, trip.ActualEndAt, deadline, null);
    }

    private static FleetFeedbackEligibility NotEligible(Guid tripId, Guid userId, DateTime? completedAt, DateTime? deadline, string reason) =>
        new(tripId, userId, FleetFeedbackStatuses.NotEligible, false, completedAt, deadline, reason);
}
