using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record FleetSwapCommand(Guid OtherAssignmentId, bool IncludeDrivers, string Reason,
    Guid RequestConcurrencyToken, Guid AssignmentConcurrencyToken, Guid OtherRequestConcurrencyToken, Guid OtherAssignmentConcurrencyToken);
public sealed record FleetSwapCandidate(Guid AssignmentId, Guid RequestId, string RequestNo, DateTime DepartureAt, DateTime ExpectedReturnAt,
    string Destination, string Status, Guid VehicleId, string Vehicle, Guid DriverUserId, string Driver,
    Guid RequestConcurrencyToken, Guid AssignmentConcurrencyToken);

public sealed class FleetAssignmentSwapService(AppDbContext db, FleetAvailabilityService availability, IWorkflowPermissionValidator permissions, IDomainEventPublisher events)
{
    internal static readonly string[] AllowedStates = [FleetRequestStatuses.PendingAdminReview, FleetRequestStatuses.PendingDirector,
        FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready];
    private static bool Approved(string status) => status is FleetRequestStatuses.Approved or FleetRequestStatuses.PendingDriverAck or FleetRequestStatuses.Ready;
    private async Task<bool> CanManage(Guid actor, string status, CancellationToken ct) =>
        await permissions.HasAsync(actor, Approved(status) ? FleetPermissions.DispatchReplaceApprovedAssignment : FleetPermissions.DispatchReplaceAssignment, ct);
    private async Task RequireVisibility(Guid actor, CancellationToken ct)
    {
        if (!await permissions.HasAsync(actor, FleetPermissions.DispatchView, ct) && !await permissions.HasAsync(actor, FleetPermissions.RequestViewAll, ct))
            throw new UnauthorizedAccessException();
    }
    private IQueryable<FleetAssignment> Eligible() => db.FleetAssignments.Where(a => a.IsActive && a.FleetRequest != null && AllowedStates.Contains(a.FleetRequest.Status)
        && !db.FleetTripRecords.Any(t => t.FleetRequestId == a.FleetRequestId && t.ActualStartAt != null)
        && !db.FleetRequestStatusHistories.Any(h => h.FleetRequestId == a.FleetRequestId && (h.Action == "Fleet.TRIP_START" || h.Action == "Fleet.TripStarted")));

    public async Task<IReadOnlyList<FleetSwapCandidate>> CandidatesAsync(Guid assignmentId, string? search, Guid actor, CancellationToken ct)
    {
        await RequireVisibility(actor, ct);
        var source = await Eligible().AsNoTracking().Select(a => new { a.Id, a.FleetRequestId, a.FleetRequest!.Status }).SingleOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new InvalidOperationException("คำขอนี้ไม่อยู่ในขั้นตอนที่สลับรถได้ หรือเคยเริ่มทริปแล้ว");
        if (!await CanManage(actor, source.Status, ct)) throw new UnauthorizedAccessException();
        var normal = await permissions.HasAsync(actor, FleetPermissions.DispatchReplaceAssignment, ct);
        var approved = await permissions.HasAsync(actor, FleetPermissions.DispatchReplaceApprovedAssignment, ct);
        var query = Eligible().AsNoTracking().Where(a => a.FleetRequestId != source.FleetRequestId);
        if (!normal) query = query.Where(a => a.FleetRequest!.Status == FleetRequestStatuses.Approved || a.FleetRequest.Status == FleetRequestStatuses.PendingDriverAck || a.FleetRequest.Status == FleetRequestStatuses.Ready);
        if (!approved) query = query.Where(a => a.FleetRequest!.Status == FleetRequestStatuses.PendingAdminReview || a.FleetRequest.Status == FleetRequestStatuses.PendingDirector);
        if (!string.IsNullOrWhiteSpace(search)) { var text = search.Trim().ToLowerInvariant(); query = query.Where(a => a.FleetRequest!.RequestNo.ToLower().Contains(text)); }
        return await query.OrderByDescending(a => a.FleetRequest!.DepartureAt).ThenBy(a => a.Id).Take(50)
            .Select(a => new FleetSwapCandidate(a.Id, a.FleetRequestId, a.FleetRequest!.RequestNo, a.FleetRequest.DepartureAt, a.FleetRequest.ExpectedReturnAt,
                a.FleetRequest.Destination, a.FleetRequest.Status, a.VehicleId, a.Vehicle!.VehicleCode + " · " + a.Vehicle.RegistrationNumber,
                a.DriverUserId, a.DriverUser!.FullName, a.FleetRequest.ConcurrencyToken, a.ConcurrencyToken)).ToListAsync(ct);
    }

    public async Task<Guid[]> SwapAsync(Guid id, FleetSwapCommand command, Guid actor, string correlationId, CancellationToken ct)
    {
        if (id == command.OtherAssignmentId) throw new ArgumentException("ไม่สามารถสลับกับคำขอเดียวกันได้");
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 900) throw new ArgumentException("กรุณาระบุเหตุผลไม่เกิน 900 ตัวอักษร");
        await RequireVisibility(actor, ct);
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await FleetSchedulingLock.EnterAsync(db, ct);
        var pair = await db.FleetAssignments.Include(a => a.FleetRequest).Where(a => a.Id == id || a.Id == command.OtherAssignmentId).ToListAsync(ct);
        var a = pair.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("ไม่พบการจัดรถต้นทาง");
        var b = pair.SingleOrDefault(x => x.Id == command.OtherAssignmentId) ?? throw new KeyNotFoundException("ไม่พบการจัดรถปลายทาง");
        if (a.FleetRequestId == b.FleetRequestId) throw new ArgumentException("ไม่สามารถสลับกับคำขอเดียวกันได้");
        var requests = new[] { a.FleetRequest!, b.FleetRequest! };
        await FleetSchedulingLock.ResourcesAsync(db, requests.Select(x => x.Id), pair.Select(x => x.VehicleId), pair.Select(x => x.DriverUserId), ct);
        if (a.ConcurrencyToken != command.AssignmentConcurrencyToken || b.ConcurrencyToken != command.OtherAssignmentConcurrencyToken ||
            requests[0].ConcurrencyToken != command.RequestConcurrencyToken || requests[1].ConcurrencyToken != command.OtherRequestConcurrencyToken)
            throw new DbUpdateConcurrencyException();
        if (await Eligible().CountAsync(x => x.Id == id || x.Id == command.OtherAssignmentId, ct) != 2)
            throw new InvalidOperationException("ข้อมูลคำขอเปลี่ยนไป หรือมีการเริ่มทริปแล้ว กรุณาโหลดใหม่");
        foreach (var request in requests) if (!await CanManage(actor, request.Status, ct)) throw new UnauthorizedAccessException();
        var driverA = command.IncludeDrivers ? b.DriverUserId : a.DriverUserId;
        var driverB = command.IncludeDrivers ? a.DriverUserId : b.DriverUserId;
        if (a.VehicleId == b.VehicleId && driverA == a.DriverUserId && driverB == b.DriverUserId) throw new ArgumentException("การสลับนี้ไม่ทำให้รถหรือคนขับเปลี่ยน");
        if (requests[0].DepartureAt < requests[1].ExpectedReturnAt && requests[1].DepartureAt < requests[0].ExpectedReturnAt &&
            (a.VehicleId == b.VehicleId || driverA == driverB)) throw new InvalidOperationException("รถหรือคนขับหลังสลับมีงานชนเวลากัน");
        var exclude = new[] { a.Id, b.Id };
        await Check(requests[0], b.VehicleId, driverA); await Check(requests[1], a.VehicleId, driverB);
        var notifications = new List<DomainEventEnvelope>();
        // Persist the inactive rows first to satisfy the existing unique active-request index.
        foreach (var old in pair) { old.IsActive = false; old.AssignmentStatus = FleetAssignmentStatuses.Replaced; old.ConcurrencyToken = Guid.NewGuid(); }
        if (tx != null) await db.SaveChangesAsync(ct);
        await Apply(a, b.VehicleId, driverA, requests[1].RequestNo);
        await Apply(b, a.VehicleId, driverB, requests[0].RequestNo);
        await db.SaveChangesAsync(ct);
        foreach (var notification in notifications) await events.PublishAsync(notification, ct);
        await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        return requests.Select(x => x.Id).ToArray();

        async Task Check(FleetRequest request, Guid vehicle, Guid driver)
        {
            var available = await availability.GetAsync(request, ct, exclude);
            var v = available.Vehicles.SingleOrDefault(x => x.Id == vehicle); var d = available.Drivers.SingleOrDefault(x => x.Id == driver);
            if (v is null || !v.IsAvailable) throw new InvalidOperationException($"{request.RequestNo}: รถไม่พร้อมใช้งาน {string.Join(", ", v?.Reasons ?? [])}");
            if (d is null || !d.IsAvailable) throw new InvalidOperationException($"{request.RequestNo}: คนขับไม่พร้อมใช้งาน {string.Join(", ", d?.Reasons ?? [])}");
        }
        Task Apply(FleetAssignment old, Guid vehicle, Guid driver, string otherNo)
        {
            var request = old.FleetRequest!; var from = request.Status;
            var reason = $"สลับ{(command.IncludeDrivers ? "รถพร้อมคนขับ" : "รถ")}กับ {otherNo}: {command.Reason.Trim()}";
            var next = new FleetAssignment { Id = Guid.NewGuid(), FleetRequestId = request.Id, VehicleId = vehicle, DriverUserId = driver,
                AssignedByUserId = actor, ReplacedAssignmentId = old.Id, AssignmentReason = reason };
            db.FleetAssignments.Add(next);
            if (old.DriverUserId != driver && request.Status == FleetRequestStatuses.Ready) request.Status = FleetRequestStatuses.PendingDriverAck;
            request.UpdatedAt = DateTime.UtcNow; request.UpdatedByUserId = actor; request.ConcurrencyToken = Guid.NewGuid();
            db.FleetRequestStatusHistories.Add(new() { FleetRequestId = request.Id, FromStatus = from, ToStatus = request.Status,
                Action = "Fleet.AssignmentReplaced", ActorUserId = actor, Reason = reason, CorrelationId = correlationId });
            db.AuditLogs.Add(new() { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.AssignmentReplaced", EntityName = "FleetAssignment", EntityId = next.Id.ToString(),
                OldValue = $"{old.Id}:{old.VehicleId}:{old.DriverUserId}", NewValue = $"{next.Id}:{vehicle}:{driver}", Reason = reason, CorrelationId = correlationId });
            notifications.Add(new("Fleet.AssignmentReplaced", "FLEET", "FleetRequest", request.Id, actor, correlationId,
                new { request.RequestNo, PreviousAssignmentId = old.Id, NewAssignmentId = next.Id, SwapWithRequestNo = otherNo, command.IncludeDrivers }, []));
            return Task.CompletedTask;
        }
    }
}
