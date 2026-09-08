using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController, Authorize]
public sealed class FleetOperationsController(AppDbContext db, IWorkflowTransitionService workflow, IDomainEventPublisher events, ILogger<FleetOperationsController> logger) : ControllerBase
{
    [HttpGet("/api/fleet/admin-review"), RequireAnyPermission(FleetPermissions.AdminReviewApprove, FleetPermissions.AdminReviewReturn, FleetPermissions.AdminReviewReject)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetQueueItem>>>> AdminQueue(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<FleetQueueItem>>.Ok(await Queue(FleetRequestStatuses.PendingAdminReview, ct));

    [HttpGet("/api/fleet/director-approval"), RequireAnyPermission(FleetPermissions.DirectorApprove, FleetPermissions.DirectorReturn, FleetPermissions.DirectorReject)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetQueueItem>>>> DirectorQueue(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<FleetQueueItem>>.Ok(await Queue(FleetRequestStatuses.PendingDirector, ct));

    [HttpPost("/api/fleet/admin-review/{id:guid}/{workflowAction}"), RequireAnyPermission(FleetPermissions.AdminReviewApprove, FleetPermissions.AdminReviewReturn, FleetPermissions.AdminReviewReject)]
    public Task<ActionResult<ApiResponse<object>>> AdminAction(Guid id, string workflowAction, FleetWorkflowActionRequest body, CancellationToken ct) => Transition(id, workflowAction.ToUpperInvariant() switch { "APPROVE" => ("ADMIN_APPROVE", FleetRequestStatuses.PendingDirector, FleetPermissions.AdminReviewApprove), "RETURN" => ("ADMIN_RETURN", FleetRequestStatuses.Returned, FleetPermissions.AdminReviewReturn), "REJECT" => ("ADMIN_REJECT", FleetRequestStatuses.Rejected, FleetPermissions.AdminReviewReject), _ => ("", "", "") }, body, ct);

    [HttpPost("/api/fleet/director-approval/{id:guid}/{workflowAction}"), RequireAnyPermission(FleetPermissions.DirectorApprove, FleetPermissions.DirectorReturn, FleetPermissions.DirectorReject)]
    public Task<ActionResult<ApiResponse<object>>> DirectorAction(Guid id, string workflowAction, FleetWorkflowActionRequest body, CancellationToken ct) => Transition(id, workflowAction.ToUpperInvariant() switch { "APPROVE" => ("DIRECTOR_APPROVE", FleetRequestStatuses.PendingDriverAck, FleetPermissions.DirectorApprove), "RETURN" => ("DIRECTOR_RETURN", FleetRequestStatuses.Returned, FleetPermissions.DirectorReturn), "REJECT" => ("DIRECTOR_REJECT", FleetRequestStatuses.Rejected, FleetPermissions.DirectorReject), _ => ("", "", "") }, body, ct);

    [HttpGet("/api/fleet/driver-jobs"), RequirePermission(FleetPermissions.DriverViewOwnJobs)]
    public async Task<ActionResult<ApiResponse<object>>> DriverJobs(CancellationToken ct)
    {
        var actor = Actor(); if (actor is null) return Unauthorized();
        var actorId = actor.Value;
        var rows = await db.FleetRequests
            .AsNoTracking()
            .Where(x => x.Assignments.Any(a => a.DriverUserId == actorId))
            .OrderBy(x => x.Status == FleetRequestStatuses.PendingDriverAck ||
                          x.Status == FleetRequestStatuses.Ready ||
                          x.Status == FleetRequestStatuses.InProgress ? 0 : 1)
            .ThenBy(x => x.Priority == FleetPriorities.Emergency ? 0 : x.Priority == FleetPriorities.Urgent ? 1 : 2)
            .ThenByDescending(x => x.DepartureAt)
            .Select(x => new
            {
                x.Id,
                x.RequestNo,
                x.Priority,
                x.Status,
                Origin = x.IncidentLocation,
                x.Destination,
                x.DepartureAt,
                x.ExpectedReturnAt,
                x.PassengerCount,
                Vehicle = x.Assignments
                    .Where(a => a.DriverUserId == actorId)
                    .OrderByDescending(a => a.IsActive)
                    .ThenByDescending(a => a.AssignedAt)
                    .Select(a => a.Vehicle!.VehicleCode + " · " + a.Vehicle.RegistrationNumber)
                    .FirstOrDefault(),
                RequiredCapabilities = x.RequiredCapabilities.Select(c => c.Capability!.Name).ToArray(),
                x.ConcurrencyToken
            })
            .ToListAsync(ct);
        return ApiResponse<object>.Ok(rows);
    }

    [HttpGet("/api/fleet/driver-jobs/{id:guid}"), RequirePermission(FleetPermissions.DriverViewOwnJobs)]
    public async Task<ActionResult<ApiResponse<object>>> DriverJob(Guid id, CancellationToken ct)
    {
        var actor = Actor(); var row = await db.FleetRequests.AsNoTracking().Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return NotFound(ApiResponse<object>.Fail("Fleet request not found."));
        if (!row.Assignments.Any(x => x.IsActive && x.DriverUserId == actor)) return Forbid();
        var active=row.Assignments.Single(x=>x.IsActive);var vehicle=await db.FleetVehicles.AsNoTracking().Where(x=>x.Id==active.VehicleId).Select(x=>new{x.Id,x.VehicleCode,x.RegistrationNumber,x.CurrentMileage}).SingleAsync(ct);var recordedMileage=await db.FleetTripRecords.AsNoTracking().Where(x=>x.Assignment!.VehicleId==active.VehicleId).Select(x=>(decimal?)(x.EndMileage??x.StartMileage)).MaxAsync(ct)??0;var effectiveMileage=Math.Max(vehicle.CurrentMileage,recordedMileage);var trip=await db.FleetTripRecords.AsNoTracking().Where(x=>x.FleetRequestId==id).Select(x=>new{x.Id,x.StartMileage,x.EndMileage,x.ActualStartAt,x.ActualEndAt,x.ConcurrencyToken}).FirstOrDefaultAsync(ct);
        return ApiResponse<object>.Ok(new { row.Id, row.RequestNo,row.Priority, row.Status, row.Purpose,Origin=row.IncidentLocation, row.Destination, row.DepartureAt, row.ExpectedReturnAt,row.PassengerCount,EmergencyReason=row.Priority==FleetPriorities.Emergency?row.EmergencyReason:null, row.ConcurrencyToken,Vehicle=new{vehicle.Id,vehicle.VehicleCode,vehicle.RegistrationNumber,CurrentMileage=effectiveMileage},Trip=trip,ExternalMapUrl="https://www.google.com/maps/dir/?api=1&destination="+Uri.EscapeDataString(row.Destination) });
    }

    [HttpPost("/api/fleet/driver-jobs/{id:guid}/accept"), RequirePermission(FleetPermissions.DriverAcknowledge)]
    public Task<ActionResult<ApiResponse<object>>> Accept(Guid id, FleetWorkflowActionRequest body, CancellationToken ct) => Transition(id, ("DRIVER_ACCEPT", FleetRequestStatuses.Ready, FleetPermissions.DriverAcknowledge), body, ct, true);

    [HttpPost("/api/fleet/driver-jobs/{id:guid}/decline"), RequirePermission(FleetPermissions.DriverAcknowledge)]
    public Task<ActionResult<ApiResponse<object>>> Decline(Guid id, FleetWorkflowActionRequest body, CancellationToken ct) => Transition(id, ("DRIVER_DECLINE", FleetRequestStatuses.Returned, FleetPermissions.DriverAcknowledge), body with { ReturnTarget = FleetReturnTargets.Dispatcher }, ct, true);

    [HttpPost("/api/fleet/trips/{id:guid}/start"), RequirePermission(FleetPermissions.DriverStart)]
    public async Task<ActionResult<ApiResponse<object>>> StartTrip(Guid id, FleetTripStartRequest body, CancellationToken ct)
    {
        if (body.StartMileage < 0) return BadRequest(ApiResponse<object>.Fail("Start mileage cannot be negative."));
        var idempotencyKey=Request.Headers["Idempotency-Key"].ToString();if(!string.IsNullOrWhiteSpace(idempotencyKey)){var replay=await db.FleetTripRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.StartIdempotencyKey==idempotencyKey,ct);if(replay is not null)return ApiResponse<object>.Ok(new{Id=replay.FleetRequestId,Status=FleetRequestStatuses.InProgress,TripId=replay.Id,TripConcurrencyToken=replay.ConcurrencyToken,IdempotentReplay=true});}
        var item = await LoadForAction(id, ct); if (item is null) return NotFound(ApiResponse<object>.Fail("Fleet request not found."));
        var assignment = item.Assignments.SingleOrDefault(x => x.IsActive); if (assignment is null || assignment.DriverUserId != Actor()) return Forbid();
        var vehicle=await db.FleetVehicles.AsNoTracking().SingleAsync(x=>x.Id==assignment.VehicleId,ct);var recordedMileage=await db.FleetTripRecords.AsNoTracking().Where(x=>x.Assignment!.VehicleId==assignment.VehicleId).Select(x=>(decimal?)(x.EndMileage??x.StartMileage)).MaxAsync(ct)??0;var minimumMileage=Math.Max(vehicle.CurrentMileage,recordedMileage);if(body.StartMileage<minimumMileage)return BadRequest(ApiResponse<object>.Fail("เลขไมล์เริ่มเดินทางต้องไม่น้อยกว่าเลขไมล์ล่าสุดของรถ"));
        var result = await Validate(item, "TRIP_START", FleetRequestStatuses.InProgress, FleetPermissions.DriverStart, body.ConcurrencyToken, null, null, ct);
        var trip = new FleetTripRecord { FleetRequestId = id, AssignmentId = assignment.Id, DriverUserId = assignment.DriverUserId, ActualStartAt = DateTime.UtcNow, StartMileage = body.StartMileage, TripNotes = Clean(body.TripNotes),StartIdempotencyKey=string.IsNullOrWhiteSpace(idempotencyKey)?null:idempotencyKey };
        AddDefaultTripParticipants(trip, item, result.ActorUserId);
        db.FleetTripRecords.Add(trip);
        Apply(item, result, null, null); await Publish("Fleet.TripStarted", item, result.ActorUserId, ct); await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.Status, RequestConcurrencyToken = item.ConcurrencyToken, TripId = trip.Id, TripConcurrencyToken = trip.ConcurrencyToken });
    }

    [HttpPost("/api/fleet/trips/{id:guid}/complete"), RequirePermission(FleetPermissions.DriverComplete)]
    public async Task<ActionResult<ApiResponse<object>>> CompleteTrip(Guid id, FleetTripCompleteRequest body, CancellationToken ct)
    {
        var idempotencyKey=Request.Headers["Idempotency-Key"].ToString();if(!string.IsNullOrWhiteSpace(idempotencyKey)){var replay=await db.FleetTripRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.CompletionIdempotencyKey==idempotencyKey,ct);if(replay is not null)return ApiResponse<object>.Ok(new{Id=replay.FleetRequestId,Status=FleetRequestStatuses.Completed,replay.ActualEndAt,replay.EndMileage,IdempotentReplay=true});}
        var item = await LoadForAction(id, ct); if (item is null) return NotFound(ApiResponse<object>.Fail("Fleet request not found."));
        var assignment = item.Assignments.SingleOrDefault(x => x.IsActive); if (assignment is null || assignment.DriverUserId != Actor()) return Forbid();
        var trip = await db.FleetTripRecords.Include(x => x.Participants).SingleOrDefaultAsync(x => x.FleetRequestId == id, ct); if (trip is null) return Conflict(ApiResponse<object>.Fail("Trip was not started."));
        if (trip.ConcurrencyToken != body.TripConcurrencyToken || item.ConcurrencyToken != body.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        if (body.EndMileage < trip.StartMileage) return BadRequest(ApiResponse<object>.Fail("End mileage must be greater than or equal to start mileage."));
        var result = await Validate(item, "TRIP_COMPLETE", FleetRequestStatuses.Completed, FleetPermissions.DriverComplete, body.ConcurrencyToken, null, null, ct);
        if (trip.Participants.Count == 0) AddDefaultTripParticipants(trip, item, result.ActorUserId);
        trip.ActualEndAt = DateTime.UtcNow; trip.EndMileage = body.EndMileage; trip.FuelAmount = body.FuelAmount; trip.FuelCost = body.FuelCost; trip.CompletionNotes = Clean(body.CompletionNotes); trip.CompletionIdempotencyKey=string.IsNullOrWhiteSpace(idempotencyKey)?null:idempotencyKey; trip.CompletedByUserId = Actor(); trip.UpdatedAt = DateTime.UtcNow; trip.ConcurrencyToken = Guid.NewGuid(); assignment.IsActive = false; assignment.AssignmentStatus = FleetAssignmentStatuses.Completed;
        var completedVehicle = await db.FleetVehicles.SingleAsync(x => x.Id == assignment.VehicleId, ct); completedVehicle.CurrentMileage = Math.Max(completedVehicle.CurrentMileage, body.EndMileage); completedVehicle.UpdatedAt = DateTime.UtcNow; completedVehicle.UpdatedByUserId = Actor();
        Apply(item, result, null, body.CompletionNotes); await Publish("Fleet.TripCompleted", item, result.ActorUserId, ct); if(item.Priority==FleetPriorities.Emergency)await Publish("FleetEmergency.TripCompleted",item,result.ActorUserId,ct); await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { item.Id, item.Status, item.ConcurrencyToken, trip.ActualEndAt, trip.EndMileage });
    }

    [HttpPost("/api/fleet/trips/{id:guid}/mileage-override"), RequirePermission(FleetPermissions.TripOverrideMileage)]
    public async Task<ActionResult<ApiResponse<object>>> OverrideMileage(Guid id, FleetMileageOverrideRequest body, CancellationToken ct)
    {
        if (body.StartMileage < 0 || body.EndMileage < body.StartMileage || string.IsNullOrWhiteSpace(body.Reason)) return BadRequest(ApiResponse<object>.Fail("Valid mileage and reason are required."));
        var actor = Actor()!.Value; var trip = await db.FleetTripRecords.Include(x => x.Assignment).SingleOrDefaultAsync(x => x.FleetRequestId == id, ct); if (trip?.Assignment is null) return NotFound(ApiResponse<object>.Fail("Trip not found.")); if (trip.ConcurrencyToken != body.TripConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        var old = new { trip.StartMileage, trip.EndMileage }; trip.StartMileage = body.StartMileage; trip.EndMileage = body.EndMileage; trip.OverrideReason = body.Reason.Trim(); trip.UpdatedAt = DateTime.UtcNow; trip.ConcurrencyToken = Guid.NewGuid(); var vehicle = await db.FleetVehicles.SingleAsync(x => x.Id == trip.Assignment.VehicleId, ct); vehicle.CurrentMileage = Math.Max(vehicle.CurrentMileage, body.EndMileage); vehicle.UpdatedAt = DateTime.UtcNow; vehicle.UpdatedByUserId = actor;
        db.AuditLogs.Add(new AuditLog { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.TripMileageOverridden", EntityName = "FleetTripRecord", EntityId = trip.Id.ToString(), OldValue = System.Text.Json.JsonSerializer.Serialize(old), NewValue = System.Text.Json.JsonSerializer.Serialize(new { trip.StartMileage, trip.EndMileage }), Reason = body.Reason.Trim(), CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
        await events.PublishAsync(new("Fleet.TripMileageOverridden", "FLEET", "FleetRequest", id, actor, HttpContext.TraceIdentifier, new { FleetRequestId = id }, []), ct); await db.SaveChangesAsync(ct); return ApiResponse<object>.Ok(new { trip.Id, trip.StartMileage, trip.EndMileage, trip.ConcurrencyToken });
    }

    [HttpPost("/api/fleet/trips/{id:guid}/abort"), RequirePermission(FleetPermissions.TripCloseByAdmin)]
    public async Task<ActionResult<ApiResponse<object>>> AbortTrip(Guid id, FleetTripAbortRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Reason)) return BadRequest(ApiResponse<object>.Fail("Abort reason is required.")); var actor = Actor()!.Value; var item = await LoadForAction(id, ct); if (item is null) return NotFound(ApiResponse<object>.Fail("Fleet request not found.")); if (item.Status != FleetRequestStatuses.InProgress || item.ConcurrencyToken != body.RequestConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Invalid state or concurrency conflict.")); var trip = await db.FleetTripRecords.SingleOrDefaultAsync(x => x.FleetRequestId == id, ct); if (trip is null || trip.ConcurrencyToken != body.TripConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Trip concurrency conflict."));
        var assignment = item.Assignments.SingleOrDefault(x => x.IsActive); if (assignment is not null) { assignment.IsActive = false; assignment.AssignmentStatus = FleetAssignmentStatuses.Aborted; assignment.ConcurrencyToken = Guid.NewGuid(); } trip.IsAborted = true; trip.AbortedAt = DateTime.UtcNow; trip.AbortedByUserId = actor; trip.AbortReason = body.Reason.Trim(); trip.ActualEndAt = DateTime.UtcNow; trip.UpdatedAt = DateTime.UtcNow; trip.ConcurrencyToken = Guid.NewGuid(); var previous = item.Status; item.Status = FleetRequestStatuses.Aborted; item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; item.ConcurrencyToken = Guid.NewGuid(); item.StatusHistories.Add(new FleetRequestStatusHistory { FromStatus = previous, ToStatus = item.Status, Action = "Fleet.TripClosedByAdmin", ActorUserId = actor, Reason = body.Reason.Trim(), CorrelationId = HttpContext.TraceIdentifier }); db.AuditLogs.Add(new AuditLog { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.TripClosedByAdmin", EntityName = "FleetTripRecord", EntityId = trip.Id.ToString(), OldValue = previous, NewValue = FleetRequestStatuses.Aborted, Reason = body.Reason.Trim(), CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() }); await events.PublishAsync(new("Fleet.TripAborted", "FLEET", "FleetRequest", id, actor, HttpContext.TraceIdentifier, new { item.RequestNo, item.Status }, []), ct); await db.SaveChangesAsync(ct); return ApiResponse<object>.Ok(new { item.Id, item.Status, RequestConcurrencyToken = item.ConcurrencyToken, TripConcurrencyToken = trip.ConcurrencyToken });
    }

    [HttpPost("/api/fleet/cancellations/{id:guid}"), RequirePermission(FleetPermissions.RequestCancel)]
    public async Task<ActionResult<ApiResponse<object>>> RequestCancellation(Guid id, FleetCancellationCreateRequest body, CancellationToken ct)
    {
        var actor = Actor(); var item = await LoadForAction(id, ct); if (item is null) return NotFound(ApiResponse<object>.Fail("Fleet request not found."));
        if (actor != item.RequesterUserId || item.ConcurrencyToken != body.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Unauthorized request or concurrency conflict."));
        if (!new[] { FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready }.Contains(item.Status) || string.IsNullOrWhiteSpace(body.Reason)) return BadRequest(ApiResponse<object>.Fail("Cancellation request is not allowed in this state."));
        var previous = item.Status; var cancellation = new FleetCancellationRequest { FleetRequestId = id, RequestedByUserId = actor.Value, PreviousStatus = previous, Reason = body.Reason.Trim(), Status = "PENDING" }; db.FleetCancellationRequests.Add(cancellation);
        item.Status = FleetRequestStatuses.CancellationPending; item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; item.ConcurrencyToken = Guid.NewGuid(); item.StatusHistories.Add(new FleetRequestStatusHistory { FromStatus = previous, ToStatus = item.Status, Action = "Fleet.CancellationRequested", ActorUserId = actor.Value, Reason = body.Reason.Trim(), CorrelationId = HttpContext.TraceIdentifier }); await Publish("Fleet.CancellationRequested", item, actor.Value, ct); await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { cancellation.Id, cancellation.Status, cancellation.ConcurrencyToken, RequestConcurrencyToken = item.ConcurrencyToken });
    }

    [HttpGet("/api/fleet/cancellations"), RequirePermission(FleetPermissions.CancellationReview)]
    public async Task<ActionResult<ApiResponse<object>>> CancellationQueue(CancellationToken ct) => ApiResponse<object>.Ok(await db.FleetCancellationRequests.AsNoTracking().Where(x => x.Status == "PENDING").OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.FleetRequestId, x.PreviousStatus, x.Reason, x.CreatedAt, x.ConcurrencyToken }).ToListAsync(ct));

    [HttpPost("/api/fleet/cancellations/{id:guid}/{decision}"), RequirePermission(FleetPermissions.CancellationReview)]
    public async Task<ActionResult<ApiResponse<object>>> ReviewCancellation(Guid id, string decision, FleetCancellationReviewRequest body, CancellationToken ct)
    {
        var actor = Actor()!.Value; var cancellation = await db.FleetCancellationRequests.Include(x => x.FleetRequest).ThenInclude(x => x!.Assignments).Include(x => x.FleetRequest).ThenInclude(x => x!.StatusHistories).SingleOrDefaultAsync(x => x.Id == id, ct); if (cancellation?.FleetRequest is null) return NotFound(ApiResponse<object>.Fail("Cancellation request not found."));
        var item = cancellation.FleetRequest; if (cancellation.Status != "PENDING" || cancellation.ConcurrencyToken != body.ConcurrencyToken || item.ConcurrencyToken != body.RequestConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        var approve = decision.Equals("approve", StringComparison.OrdinalIgnoreCase); if (!approve && !decision.Equals("reject", StringComparison.OrdinalIgnoreCase)) return BadRequest(ApiResponse<object>.Fail("Unknown decision."));
        var from = item.Status; item.Status = approve ? FleetRequestStatuses.Cancelled : cancellation.PreviousStatus; if (approve) foreach (var a in item.Assignments.Where(x => x.IsActive)) { a.IsActive = false; a.AssignmentStatus = FleetAssignmentStatuses.Cancelled; } cancellation.Status = approve ? "APPROVED" : "REJECTED"; cancellation.ReviewedByUserId = actor; cancellation.ReviewedAt = DateTime.UtcNow; cancellation.CompletedAt = DateTime.UtcNow; cancellation.ReviewReason = Clean(body.ReviewReason); cancellation.ConcurrencyToken = Guid.NewGuid(); item.ConcurrencyToken = Guid.NewGuid(); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; item.StatusHistories.Add(new FleetRequestStatusHistory { FromStatus = from, ToStatus = item.Status, Action = approve ? "Fleet.CancellationApproved" : "Fleet.CancellationRejected", ActorUserId = actor, Reason = Clean(body.ReviewReason), CorrelationId = HttpContext.TraceIdentifier }); await Publish(approve ? "Fleet.CancellationApproved" : "Fleet.CancellationRejected", item, actor, ct); await db.SaveChangesAsync(ct); return ApiResponse<object>.Ok(new { cancellation.Id, cancellation.Status, RequestStatus = item.Status, item.ConcurrencyToken });
    }

    [HttpGet("/api/fleet/dashboard/legacy-operations"), RequirePermission(FleetPermissions.DashboardView)]
    public async Task<ActionResult<ApiResponse<object>>> Dashboard(CancellationToken ct)
    {
        var counts = await db.FleetRequests.AsNoTracking().GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct); var today = DateTime.UtcNow.Date; var completedToday = await db.FleetTripRecords.CountAsync(x => x.ActualEndAt >= today && x.ActualEndAt < today.AddDays(1), ct); var outbox = await db.OutboxMessages.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct); return ApiResponse<object>.Ok(new { StatusCounts = counts, CompletedToday = completedToday, Outbox = outbox });
    }

    [HttpGet("/api/fleet/outbox"), RequirePermission(FleetPermissions.OutboxView)]
    public async Task<ActionResult<ApiResponse<object>>> Outbox([FromQuery] string? status, CancellationToken ct)
    {
        var query = db.OutboxMessages.AsNoTracking().AsQueryable(); if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status); var rows = await query.OrderByDescending(x => x.CreatedAt).Take(200).Select(x => new { x.Id, x.EventId, x.EventType, x.Scope, x.Status, x.AttemptCount, x.LastError, x.CreatedAt, x.AvailableAt, x.ProcessedAt }).ToListAsync(ct); return ApiResponse<object>.Ok(rows);
    }

    private async Task<ActionResult<ApiResponse<object>>> Transition(Guid id, (string action, string target, string permission) command, FleetWorkflowActionRequest body, CancellationToken ct, bool ownDriver = false)
    {
        if (string.IsNullOrEmpty(command.action)) return BadRequest(ApiResponse<object>.Fail("Unknown action."));
        var item = await LoadForAction(id, ct); if (item is null) return NotFound(ApiResponse<object>.Fail("Fleet request not found."));
        var active = item.Assignments.SingleOrDefault(x => x.IsActive); if (active is null) return Conflict(ApiResponse<object>.Fail("An active assignment is required."));
        if (ownDriver && active.DriverUserId != Actor()) return Forbid();
        try { var result = await Validate(item, command.action, command.target, command.permission, body.ConcurrencyToken, body.Reason, body.ReturnTarget, ct); Apply(item, result, body.ReturnTarget, body.Reason); var eventType = command.action switch { "ADMIN_APPROVE" => "Fleet.AdminReviewApproved", "DIRECTOR_APPROVE" => "Fleet.DirectorApproved", "DRIVER_ACCEPT" => "Fleet.DriverAccepted", "DRIVER_DECLINE" => "Fleet.DriverDeclined", _ when command.action.EndsWith("RETURN") => "Fleet.RequestReturned", _ => "Fleet.RequestRejected" }; await Publish(eventType, item, result.ActorUserId, ct); await db.SaveChangesAsync(ct); logger.LogInformation("Fleet workflow transitioned. CorrelationId={CorrelationId} RequestNumber={RequestNumber} FleetRequestId={FleetRequestId} ActorUserId={ActorUserId} EffectiveActorUserId={EffectiveActorUserId} DelegationId={DelegationId} WorkflowState={WorkflowState} EventType={EventType}", HttpContext.TraceIdentifier, item.RequestNo, item.Id, result.ActorUserId, result.EffectiveActorUserId, result.DelegationId, item.Status, eventType); return ApiResponse<object>.Ok(new { item.Id, item.Status, item.ReturnTarget, item.ConcurrencyToken }); }
        catch (UnauthorizedAccessException) { return Forbid(); } catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); } catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
    }

    private Task<WorkflowTransitionResult> Validate(FleetRequest item, string action, string target, string permission, Guid token, string? reason, string? returnTarget, CancellationToken ct)
    { if (item.ConcurrencyToken != token) throw new InvalidOperationException("Concurrency conflict."); var actor = Actor() ?? throw new UnauthorizedAccessException(); return workflow.ValidateAsync(new("FLEET", "FleetRequest", item.Id, item.Status, action, target, actor, permission, token, reason, returnTarget), ct); }
    private void Apply(FleetRequest item, WorkflowTransitionResult result, string? target, string? reason) { item.StatusHistories.Add(new FleetRequestStatusHistory { FromStatus = result.PreviousState, ToStatus = result.NewState, Action = $"Fleet.{result.Action}", ActorUserId = result.ActorUserId, ReturnTarget = target, Reason = Clean(reason), CorrelationId = HttpContext.TraceIdentifier }); item.Status = result.NewState; item.ReturnTarget = target; item.UpdatedAt = result.OccurredAt; item.UpdatedByUserId = result.ActorUserId; item.ConcurrencyToken = Guid.NewGuid(); db.AuditLogs.Add(new AuditLog { UserId = result.ActorUserId, EffectiveActorUserId = result.EffectiveActorUserId, DelegatorUserId = result.DelegatorUserId, DelegationId = result.DelegationId, Action = $"Fleet.{result.Action}", EntityName = "FleetRequest", EntityId = item.Id.ToString(), OldValue = result.PreviousState, NewValue = result.NewState, Reason = Clean(reason), CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() }); }
    private Task<FleetRequest?> LoadForAction(Guid id, CancellationToken ct) => db.FleetRequests.Include(x => x.Assignments).Include(x => x.StatusHistories).Include(x => x.Passengers).FirstOrDefaultAsync(x => x.Id == id, ct);

    private static void AddDefaultTripParticipants(FleetTripRecord trip, FleetRequest request, Guid actorUserId)
    {
        var now = DateTime.UtcNow;
        foreach (var passenger in request.Passengers
                     .Where(x => x.UserId.HasValue && x.PassengerType == FleetPassengerTypes.Employee && x.UserId != trip.DriverUserId)
                     .GroupBy(x => x.UserId!.Value)
                     .Select(x => x.First()))
        {
            trip.Participants.Add(new FleetTripParticipant
            {
                UserId = passenger.UserId,
                IsRequester = passenger.IsRequester,
                ParticipantType = FleetPassengerTypes.Employee,
                IsActualParticipant = true,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
        }
    }
    private Task<List<FleetQueueItem>> Queue(string status, CancellationToken ct) => db.FleetRequests.AsNoTracking().Where(x => x.Status == status).OrderBy(x => x.DepartureAt).Select(x => new FleetQueueItem(x.Id, x.RequestNo, x.Status, x.RequesterUser!.FullName, x.RequesterUser.Department != null ? x.RequesterUser.Department.Name : null, x.Purpose, x.MissionType, x.Destination, x.DepartureAt, x.ExpectedReturnAt, x.PassengerCount, x.Assignments.Where(a => a.IsActive).Select(a => a.Vehicle!.VehicleCode + " · " + a.Vehicle.RegistrationNumber).FirstOrDefault(), x.Assignments.Where(a => a.IsActive).Select(a => a.DriverUser!.FullName).FirstOrDefault(), x.ConcurrencyToken)).ToListAsync(ct);
    private Task Publish(string type, FleetRequest item, Guid actor, CancellationToken ct) => events.PublishAsync(new(type, "FLEET", "FleetRequest", item.Id, actor, HttpContext.TraceIdentifier, new { item.RequestNo, item.Status, item.ReturnTarget }, []), ct);
    private Guid? Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public sealed record FleetQueueItem(Guid Id, string RequestNo, string Status, string RequesterName, string? RequesterDepartmentName, string Purpose, string MissionType, string Destination, DateTime DepartureAt, DateTime ExpectedReturnAt, int PassengerCount, string? Vehicle, string? Driver, Guid ConcurrencyToken);
}
