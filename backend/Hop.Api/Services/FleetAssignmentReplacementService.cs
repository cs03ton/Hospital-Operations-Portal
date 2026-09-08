using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record FleetAssignmentReplacementResult(FleetRequest Request, FleetAssignment PreviousAssignment, FleetAssignment NewAssignment);

public sealed class FleetAssignmentReplacementService(AppDbContext db, FleetAvailabilityService availability, IWorkflowPermissionValidator permissions, IDomainEventPublisher events, ILogger<FleetAssignmentReplacementService> logger)
{
    private static readonly HashSet<string> ApprovedStates = [FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready, FleetRequestStatuses.InProgress, FleetRequestStatuses.CancellationPending];

    public async Task<FleetAssignmentReplacementResult> ReplaceAsync(Guid assignmentId, FleetAssignmentReplaceRequest command, Guid actorUserId, string correlationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Reason)) throw new ArgumentException("Replacement reason is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({$"fleet-assignment:{assignmentId}"}, 0))", ct);

        var previous = await db.FleetAssignments.Include(x => x.FleetRequest).ThenInclude(x => x!.Assignments).SingleOrDefaultAsync(x => x.Id == assignmentId, ct)
            ?? throw new KeyNotFoundException("Assignment not found.");
        var request = previous.FleetRequest ?? throw new InvalidOperationException("Fleet request not found.");
        if (!previous.IsActive) throw new InvalidOperationException("Only the active assignment can be replaced.");
        if (request.ConcurrencyToken != command.RequestConcurrencyToken || previous.ConcurrencyToken != command.AssignmentConcurrencyToken) throw new DbUpdateConcurrencyException("Concurrency conflict.");

        var permission = ApprovedStates.Contains(request.Status) ? FleetPermissions.DispatchReplaceApprovedAssignment : FleetPermissions.DispatchReplaceAssignment;
        if (!await permissions.HasAsync(actorUserId, permission, ct)) throw new UnauthorizedAccessException("Permission denied.");

        var vehicleId = command.VehicleId ?? previous.VehicleId;
        var driverUserId = command.DriverUserId ?? previous.DriverUserId;
        if (vehicleId == previous.VehicleId && driverUserId == previous.DriverUserId) throw new ArgumentException("Vehicle or driver must change.");
        var available = await availability.GetAsync(request, ct);
        var vehicle = available.Vehicles.SingleOrDefault(x => x.Id == vehicleId);
        var driver = available.Drivers.SingleOrDefault(x => x.Id == driverUserId);
        if (vehicle is null || !vehicle.IsAvailable) throw new InvalidOperationException($"Vehicle is unavailable: {string.Join(", ", vehicle?.Reasons ?? [])}");
        if (driver is null || !driver.IsAvailable) throw new InvalidOperationException($"Driver is unavailable: {string.Join(", ", driver?.Reasons ?? [])}");

        previous.IsActive = false;
        previous.AssignmentStatus = FleetAssignmentStatuses.Replaced;
        previous.ConcurrencyToken = Guid.NewGuid();
        var replacement = new FleetAssignment { FleetRequestId = request.Id, VehicleId = vehicleId, DriverUserId = driverUserId, AssignedByUserId = actorUserId, AssignmentReason = command.Reason.Trim(), ReplacedAssignmentId = previous.Id };
        db.FleetAssignments.Add(replacement);
        request.UpdatedAt = DateTime.UtcNow; request.UpdatedByUserId = actorUserId; request.ConcurrencyToken = Guid.NewGuid();
        if (driverUserId != previous.DriverUserId && request.Status is FleetRequestStatuses.Ready or FleetRequestStatuses.PendingDriverAck) request.Status = FleetRequestStatuses.PendingDriverAck;
        request.StatusHistories.Add(new FleetRequestStatusHistory { FromStatus = request.Status, ToStatus = request.Status, Action = "Fleet.AssignmentReplaced", ActorUserId = actorUserId, Reason = command.Reason.Trim(), CorrelationId = correlationId });
        db.AuditLogs.Add(new AuditLog { UserId = actorUserId, EffectiveActorUserId = actorUserId, Action = "Fleet.AssignmentReplaced", EntityName = "FleetAssignment", EntityId = replacement.Id.ToString(), OldValue = $"{previous.Id}:{previous.VehicleId}:{previous.DriverUserId}", NewValue = $"{replacement.Id}:{vehicleId}:{driverUserId}", Reason = command.Reason.Trim(), CorrelationId = correlationId });
        await events.PublishAsync(new("Fleet.AssignmentReplaced", "FLEET", "FleetRequest", request.Id, actorUserId, correlationId, new { request.RequestNo, PreviousAssignmentId = previous.Id, NewAssignmentId = replacement.Id }, []), ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Fleet assignment replaced. CorrelationId={CorrelationId} RequestNumber={RequestNumber} FleetRequestId={FleetRequestId} AssignmentId={AssignmentId} PreviousAssignmentId={PreviousAssignmentId} ActorUserId={ActorUserId} WorkflowState={WorkflowState} EventType={EventType}", correlationId, request.RequestNo, request.Id, replacement.Id, previous.Id, actorUserId, request.Status, "Fleet.AssignmentReplaced");
        return new(request, previous, replacement);
    }
}
