using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/fleet")]
public sealed class FleetFeedbackController(
    AppDbContext db,
    IFleetFeedbackEligibilityService eligibilityService,
    IOptions<FleetOperationsOptions> options) : ControllerBase
{
    [HttpGet("trips/{tripId:guid}/feedback-context"), RequirePermission(FleetPermissions.FeedbackViewOwn)]
    public async Task<ActionResult<ApiResponse<FleetFeedbackContextDto>>> Context(Guid tripId, CancellationToken ct)
    {
        var actor = Actor(); if (actor is null) return Unauthorized(ApiResponse<FleetFeedbackContextDto>.Fail("Authentication required."));
        var eligibility = await eligibilityService.EvaluateAsync(tripId, actor.Value, ct: ct);
        if (eligibility.Reason == "TRIP_NOT_FOUND") return NotFound(ApiResponse<FleetFeedbackContextDto>.Fail("ไม่พบข้อมูลการเดินทาง"));
        if (eligibility.Reason is "NOT_ACTUAL_PARTICIPANT" or "DRIVER_CANNOT_REVIEW_OWN_TRIP") return Forbid();

        var context = await ContextQuery(tripId).SingleAsync(ct);
        AddAudit(actor.Value, "Fleet.FeedbackViewedOwn", "FleetTripRecord", tripId, null);
        await db.SaveChangesAsync(ct);
        return ApiResponse<FleetFeedbackContextDto>.Ok(ToContext(context, eligibility));
    }

    [HttpGet("trips/{tripId:guid}/feedback/me"), RequirePermission(FleetPermissions.FeedbackViewOwn)]
    public async Task<ActionResult<ApiResponse<FleetFeedbackOwnDto>>> Mine(Guid tripId, CancellationToken ct)
    {
        var actor = Actor(); if (actor is null) return Unauthorized(ApiResponse<FleetFeedbackOwnDto>.Fail("Authentication required."));
        var row = await db.FleetTripFeedbacks.AsNoTracking()
            .Where(x => x.TripId == tripId && x.SubmittedByUserId == actor)
            .Select(x => new FleetFeedbackOwnDto(x.Id, x.TripId, x.FleetRequest!.RequestNo, x.PunctualityRating, x.SafetyRating, x.ServiceRating, x.OverallRating, x.VehicleConditionRating, x.VehicleCleanlinessRating, x.HasIncident, x.IncidentCategory, x.Comment, x.SubmittedAt))
            .SingleOrDefaultAsync(ct);
        return row is null ? NotFound(ApiResponse<FleetFeedbackOwnDto>.Fail("ยังไม่มี Feedback สำหรับการเดินทางนี้")) : ApiResponse<FleetFeedbackOwnDto>.Ok(row);
    }

    [HttpPost("trips/{tripId:guid}/feedback"), RequirePermission(FleetPermissions.FeedbackCreate)]
    public async Task<ActionResult<ApiResponse<FleetFeedbackOwnDto>>> Submit(Guid tripId, SubmitFleetTripFeedbackRequest request, CancellationToken ct)
    {
        var actor = Actor(); if (actor is null) return Unauthorized(ApiResponse<FleetFeedbackOwnDto>.Fail("Authentication required."));
        var eligibility = await eligibilityService.EvaluateAsync(tripId, actor.Value, ct: ct);
        if (!eligibility.CanSubmit)
            return eligibility.Status == FleetFeedbackStatuses.Submitted
                ? Conflict(ApiResponse<FleetFeedbackOwnDto>.Fail("คุณส่ง Feedback สำหรับการเดินทางนี้แล้ว"))
                : eligibility.Status == FleetFeedbackStatuses.Expired
                    ? BadRequest(ApiResponse<FleetFeedbackOwnDto>.Fail("หมดระยะเวลาให้ Feedback การเดินทางแล้ว"))
                    : Forbid();

        var snapshot = await db.FleetTripRecords.AsNoTracking()
            .Where(x => x.Id == tripId)
            .Select(x => new { x.Id, x.FleetRequestId, x.AssignmentId, VehicleId = x.Assignment!.VehicleId, x.DriverUserId, RequestNo = x.FleetRequest!.RequestNo })
            .SingleAsync(ct);
        var now = DateTime.UtcNow;
        var feedback = new FleetTripFeedback
        {
            Id = Guid.NewGuid(), TripId = snapshot.Id, FleetRequestId = snapshot.FleetRequestId,
            VehicleAssignmentId = snapshot.AssignmentId, VehicleId = snapshot.VehicleId, DriverUserId = snapshot.DriverUserId,
            SubmittedByUserId = actor.Value, PunctualityRating = request.PunctualityRating,
            SafetyRating = request.SafetyRating, ServiceRating = request.ServiceRating, OverallRating = request.OverallRating,
            VehicleConditionRating = request.VehicleConditionRating, VehicleCleanlinessRating = request.VehicleCleanlinessRating,
            HasIncident = request.HasIncident,
            IncidentCategory = request.HasIncident ? request.IncidentCategory?.Trim().ToUpperInvariant() : null,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            SubmittedAt = now, CreatedAt = now
        };
        db.FleetTripFeedbacks.Add(feedback);
        AddAudit(actor.Value, "Fleet.FeedbackCreated", "FleetTripFeedback", feedback.Id, $"TripId={tripId}; RequestNo={snapshot.RequestNo}");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse<FleetFeedbackOwnDto>.Fail("คุณส่ง Feedback สำหรับการเดินทางนี้แล้ว"));
        }
        var result = new FleetFeedbackOwnDto(feedback.Id, tripId, snapshot.RequestNo, feedback.PunctualityRating, feedback.SafetyRating, feedback.ServiceRating, feedback.OverallRating, feedback.VehicleConditionRating, feedback.VehicleCleanlinessRating, feedback.HasIncident, feedback.IncidentCategory, feedback.Comment, feedback.SubmittedAt);
        return CreatedAtAction(nameof(Mine), new { tripId }, ApiResponse<FleetFeedbackOwnDto>.Ok(result));
    }

    [HttpGet("my-feedback-eligible-trips"), RequirePermission(FleetPermissions.FeedbackViewOwn)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetFeedbackContextDto>>>> EligibleTrips(CancellationToken ct)
    {
        var actor = Actor(); if (actor is null) return Unauthorized(ApiResponse<IReadOnlyList<FleetFeedbackContextDto>>.Fail("Authentication required."));
        var now = DateTime.UtcNow;
        var earliest = now.AddDays(-Math.Max(1, options.Value.FeedbackWindowDays));
        var tripIds = await db.FleetTripParticipants.AsNoTracking()
            .Where(x => x.UserId == actor && x.IsActualParticipant && x.ParticipantType == FleetPassengerTypes.Employee)
            .Where(x => x.Trip!.DriverUserId != actor && x.Trip.ActualEndAt >= earliest && x.Trip.ActualEndAt <= now)
            .Where(x => x.Trip!.FleetRequest!.Status == FleetRequestStatuses.Completed)
            .Where(x => !x.Trip!.Feedbacks.Any(f => f.SubmittedByUserId == actor))
            .OrderByDescending(x => x.Trip!.ActualEndAt)
            .Select(x => x.TripId).Take(20).ToListAsync(ct);
        var items = new List<FleetFeedbackContextDto>();
        foreach (var tripId in tripIds)
        {
            var eligibility = await eligibilityService.EvaluateAsync(tripId, actor.Value, now, ct);
            if (!eligibility.CanSubmit) continue;
            items.Add(ToContext(await ContextQuery(tripId).SingleAsync(ct), eligibility));
        }
        return ApiResponse<IReadOnlyList<FleetFeedbackContextDto>>.Ok(items);
    }

    [HttpGet("my-feedback-trips"), RequirePermission(FleetPermissions.FeedbackViewOwn)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetFeedbackContextDto>>>> MyFeedbackTrips(CancellationToken ct)
    {
        var actor = Actor(); if (actor is null) return Unauthorized(ApiResponse<IReadOnlyList<FleetFeedbackContextDto>>.Fail("Authentication required."));
        var now = DateTime.UtcNow;
        var tripIds = await db.FleetTripParticipants.AsNoTracking()
            .Where(x => x.UserId == actor && x.IsActualParticipant && x.ParticipantType == FleetPassengerTypes.Employee)
            .Where(x => x.Trip!.DriverUserId != actor && x.Trip.ActualEndAt != null)
            .Where(x => x.Trip!.FleetRequest!.Status == FleetRequestStatuses.Completed)
            .OrderByDescending(x => x.Trip!.ActualEndAt)
            .Select(x => x.TripId)
            .Take(100)
            .ToListAsync(ct);
        var items = new List<FleetFeedbackContextDto>();
        foreach (var tripId in tripIds)
        {
            var eligibility = await eligibilityService.EvaluateAsync(tripId, actor.Value, now, ct);
            if (eligibility.Status == FleetFeedbackStatuses.NotEligible) continue;
            items.Add(ToContext(await ContextQuery(tripId).SingleAsync(ct), eligibility));
        }
        return ApiResponse<IReadOnlyList<FleetFeedbackContextDto>>.Ok(items);
    }

    private IQueryable<FeedbackContextProjection> ContextQuery(Guid tripId) => db.FleetTripRecords.AsNoTracking()
        .Where(x => x.Id == tripId)
        .Select(x => new FeedbackContextProjection(x.Id, x.FleetRequest!.RequestNo, x.FleetRequest.DepartureAt, x.FleetRequest.Destination,
            x.Assignment!.Vehicle!.VehicleCode + " · " + x.Assignment.Vehicle.RegistrationNumber,
            x.DriverUser!.FullName, x.ActualEndAt!.Value));

    private static FleetFeedbackContextDto ToContext(FeedbackContextProjection x, FleetFeedbackEligibility e) =>
        new(x.TripId, x.RequestNo, x.TripDate, x.Destination, x.VehicleDisplay, x.DriverDisplay, x.CompletedAt, e.FeedbackDeadline!.Value, e.CanSubmit, e.Status);

    private void AddAudit(Guid actor, string action, string entityName, Guid entityId, string? detail) => db.AuditLogs.Add(new AuditLog { UserId = actor, EffectiveActorUserId = actor, Action = action, EntityName = entityName, EntityId = entityId.ToString(), Detail = detail, CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
    private Guid? Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private sealed record FeedbackContextProjection(Guid TripId, string RequestNo, DateTime TripDate, string Destination, string VehicleDisplay, string DriverDisplay, DateTime CompletedAt);
}
