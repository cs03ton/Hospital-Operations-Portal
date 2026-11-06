using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

[Collection("FleetPostgreSql")]
public sealed class FleetAssignmentSwapTests
{
    internal sealed class Permission(params string[] denied) : IWorkflowPermissionValidator
    { public Task<bool> HasAsync(Guid userId, string permission, CancellationToken ct) => Task.FromResult(!denied.Contains(permission)); }
    internal sealed class Publisher(AppDbContext db, bool fail = false) : IDomainEventPublisher
    {
        public Task PublishAsync(DomainEventEnvelope e, CancellationToken ct) {
            if (fail) throw new InvalidOperationException("simulated outbox failure");
            db.DomainEvents.Add(new() { EventId = Guid.NewGuid(), AggregateId = e.AggregateId, EventType = e.EventType, Scope = e.Scope,
                AggregateType = e.AggregateType, CorrelationId = e.CorrelationId, Payload = System.Text.Json.JsonSerializer.Serialize(e.Payload) });
            return Task.CompletedTask;
        }
    }
    internal sealed record Fixture(FleetAssignment A, FleetAssignment B, Guid Actor)
    {
        public FleetSwapCommand Command(bool drivers = false) => new(B.Id, drivers, "ปรับภารกิจ", A.FleetRequest!.ConcurrencyToken, A.ConcurrencyToken, B.FleetRequest!.ConcurrencyToken, B.ConcurrencyToken);
    }
    internal static FleetAssignmentSwapService Service(AppDbContext db, IWorkflowPermissionValidator? permission = null, bool fail = false) => new(db, new FleetAvailabilityService(db), permission ?? new Permission(), new Publisher(db, fail));
    private static AppDbContext Memory() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    internal static async Task<Fixture> Seed(AppDbContext db)
    {
        var type = new FleetVehicleType { Code = "VAN", Name = "รถตู้", IsActive = true };
        var actor = new User { Username = "swap-admin", FullName = "ผู้จัด", PasswordHash = "test", IsActive = true }; db.Users.Add(actor);
        var start = new DateTime(2026, 11, 5, 1, 0, 0, DateTimeKind.Utc);
        FleetAssignment Assignment(string suffix) {
            var driver = new User { Username = "swap-driver-" + suffix, FullName = "คนขับ " + suffix, PasswordHash = "test", IsActive = true };
            db.FleetDriverProfiles.Add(new() { User = driver, CanDriveVan = true });
            return new() { FleetRequest = new() { RequestNo = "VH-SWAP-" + suffix, RequesterUser = actor, CreatedByUserId = actor.Id,
                MissionType = "GENERAL", RequestedVehicleType = type, Status = FleetRequestStatuses.Ready, DepartureAt = start, ExpectedReturnAt = start.AddHours(2), PassengerCount = 2, Destination = "ทดสอบ" },
                Vehicle = new() { VehicleCode = "SWAP-" + suffix, RegistrationNumber = suffix, VehicleType = type, PassengerCapacity = 5, SeatCapacityTotal = 6 },
                DriverUser = driver, AssignedByUser = actor };
        }
        var a = Assignment("A"); var b = Assignment("B"); db.FleetAssignments.AddRange(a, b); await db.SaveChangesAsync(); return new(a, b, actor.Id);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Swaps_atomically_with_history_and_original_approval_state(bool drivers)
    {
        await using var db = Memory(); var f = await Seed(db); var command = f.Command(drivers); var service = Service(db);
        var candidates = await service.CandidatesAsync(f.A.Id, "SWAP-B", f.Actor, default); Assert.Equal(f.B.Id, Assert.Single(candidates).AssignmentId);
        await service.SwapAsync(f.A.Id, command, f.Actor, "swap-test", default);
        var current = await db.FleetAssignments.Where(x => x.IsActive).ToListAsync(); Assert.Equal(2, current.Count);
        var a = current.Single(x => x.FleetRequestId == f.A.FleetRequestId); var b = current.Single(x => x.FleetRequestId == f.B.FleetRequestId);
        Assert.Equal(f.B.VehicleId, a.VehicleId); Assert.Equal(f.A.VehicleId, b.VehicleId);
        Assert.Equal(drivers ? f.B.DriverUserId : f.A.DriverUserId, a.DriverUserId);
        Assert.Equal(drivers ? FleetRequestStatuses.PendingDriverAck : FleetRequestStatuses.Ready, f.A.FleetRequest!.Status);
        Assert.Equal(f.A.Id, a.ReplacedAssignmentId); Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.Equal(2, await db.DomainEvents.CountAsync());
        Assert.All(await db.FleetRequestStatusHistories.ToListAsync(), h => { Assert.Equal("Fleet.AssignmentReplaced", h.Action); Assert.Contains("VH-SWAP", h.Reason); Assert.Equal(FleetRequestStatuses.Ready, h.FromStatus); });
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.SwapAsync(f.A.Id, command, f.Actor, "stale", default));
    }

    [Theory]
    [InlineData("IN_PROGRESS")] [InlineData("CANCELLATION_PENDING")] [InlineData("RETURNED")] [InlineData("REJECTED")]
    [InlineData("COMPLETED")] [InlineData("ABORTED")] [InlineData("CANCELLED")] [InlineData("DRAFT")]
    public async Task Rejects_ineligible_states_without_modification(string state)
    {
        await using var db = Memory(); var f = await Seed(db); f.B.FleetRequest!.Status = state; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SwapAsync(f.A.Id, f.Command(), f.Actor, "test", default));
        Assert.Equal(2, await db.FleetAssignments.CountAsync(x => x.IsActive)); Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task Rejects_actual_start_history_tokens_permission_and_third_party_conflicts()
    {
        await using var db = Memory(); var f = await Seed(db); var command = f.Command();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(db, new Permission(FleetPermissions.DispatchReplaceApprovedAssignment)).SwapAsync(f.A.Id, command, f.Actor, "test", default));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Service(db).SwapAsync(f.A.Id, command with { OtherRequestConcurrencyToken = Guid.NewGuid() }, f.Actor, "test", default));
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).SwapAsync(f.A.Id, command with { Reason = " " }, f.Actor, "test", default));
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).SwapAsync(f.A.Id, command with { OtherAssignmentId = f.A.Id }, f.Actor, "test", default));
        var history = new FleetRequestStatusHistory { FleetRequestId = f.B.FleetRequestId, Action = "Fleet.TRIP_START", ActorUserId = f.Actor, ToStatus = "IN_PROGRESS" }; db.Add(history); await db.SaveChangesAsync();
        Assert.Empty(await Service(db).CandidatesAsync(f.A.Id, null, f.Actor, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SwapAsync(f.A.Id, command, f.Actor, "test", default));
        db.Remove(history); await db.SaveChangesAsync();
        var trip = new FleetTripRecord { FleetRequestId = f.B.FleetRequestId, AssignmentId = f.B.Id, DriverUserId = f.B.DriverUserId, ActualStartAt = DateTime.UtcNow }; db.Add(trip); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SwapAsync(f.A.Id, command, f.Actor, "test", default));
        db.Remove(trip); await db.SaveChangesAsync();
        var third = new FleetAssignment { VehicleId = f.B.VehicleId, DriverUserId = f.B.DriverUserId, AssignedByUserId = f.Actor,
            FleetRequest = new() { RequestNo = "THIRD", Status = "READY", DepartureAt = f.A.FleetRequest!.DepartureAt, ExpectedReturnAt = f.A.FleetRequest.ExpectedReturnAt } };
        db.Add(third); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SwapAsync(f.A.Id, command, f.Actor, "test", default));
        Assert.Empty(db.AuditLogs); Assert.True(f.A.IsActive); Assert.True(f.B.IsActive);
    }

    [Fact, Trait("Category", "PostgreSqlIntegration")]
    public async Task PostgreSql_rollback_and_parallel_swaps_preserve_both_assignments()
    {
        var connection = Environment.GetEnvironmentVariable("HOP_SWAP_QA_CONNECTION_STRING"); if (connection == null) return;
        var cs = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (cs.Host != "127.0.0.1" || cs.Port != 56556 || cs.Database != "hop_swap_qa") throw new InvalidOperationException("Only the isolated swap QA database is allowed");
        AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
        await using var db = Open(); await db.Database.EnsureDeletedAsync(); await db.Database.MigrateAsync(); var f = await Seed(db); var command = f.Command(true);
        Assert.Single(await Service(db).CandidatesAsync(f.A.Id, "SWAP-B", f.Actor, default));
        await using (var fail = Open()) await Assert.ThrowsAsync<InvalidOperationException>(() => Service(fail, fail: true).SwapAsync(f.A.Id, command, f.Actor, "rollback", default));
        await using (var check = Open()) { Assert.Equal(2, await check.FleetAssignments.CountAsync(x => x.IsActive)); Assert.Equal(2, await check.FleetAssignments.CountAsync()); Assert.Empty(check.AuditLogs); }
        async Task<bool> Swap() { await using var context = Open(); try { await Service(context).SwapAsync(f.A.Id, command, f.Actor, "parallel", default); return true; } catch (DbUpdateConcurrencyException) { return false; } }
        Assert.Single(await Task.WhenAll(Swap(), Swap()), x => x);
        await using var final = Open(); Assert.Equal(2, await final.FleetAssignments.CountAsync(x => x.IsActive)); Assert.Equal(4, await final.FleetAssignments.CountAsync()); Assert.Equal(2, await final.DomainEvents.CountAsync());
        var activeA = await final.FleetAssignments.Include(x => x.FleetRequest).SingleAsync(x => x.FleetRequestId == f.A.FleetRequestId && x.IsActive);
        var activeB = await final.FleetAssignments.Include(x => x.FleetRequest).SingleAsync(x => x.FleetRequestId == f.B.FleetRequestId && x.IsActive);
        var nextCommand = new Fixture(activeA, activeB, f.Actor).Command();
        var spare = new FleetVehicle { VehicleCode = "SPARE", RegistrationNumber = "SPARE", VehicleTypeId = activeA.FleetRequest!.RequestedVehicleTypeId!.Value, PassengerCapacity = 5, SeatCapacityTotal = 6 };
        final.Add(spare); await final.SaveChangesAsync();
        async Task<bool> SwapAgain() { await using var context = Open(); try { await Service(context).SwapAsync(activeA.Id, nextCommand, f.Actor, "vs-replace", default); return true; } catch (DbUpdateConcurrencyException) { return false; } catch (InvalidOperationException) { return false; } }
        async Task<bool> Replace() {
            await using var context = Open();
            var service = new FleetAssignmentReplacementService(context, new FleetAvailabilityService(context), new Permission(), new Publisher(context), Microsoft.Extensions.Logging.Abstractions.NullLogger<FleetAssignmentReplacementService>.Instance);
            try { await service.ReplaceAsync(activeA.Id, new(spare.Id, null, nextCommand.RequestConcurrencyToken, nextCommand.AssignmentConcurrencyToken, "replace"), f.Actor, "vs-swap", default); return true; }
            catch (DbUpdateConcurrencyException) { return false; } catch (InvalidOperationException) { return false; }
        }
        Assert.Single(await Task.WhenAll(SwapAgain(), Replace()), x => x);
        await using var checkedDb = Open(); Assert.Equal(2, await checkedDb.FleetAssignments.CountAsync(x => x.IsActive));
    }

    [Theory]
    [InlineData("capacity")] [InlineData("driver")] [InlineData("maintenance")]
    public async Task Rechecks_availability_and_preserves_assignments_on_failure(string kind)
    {
        await using var db = Memory(); var f = await Seed(db);
        if (kind == "capacity") f.B.Vehicle!.PassengerCapacity = 1;
        if (kind == "driver") (await db.FleetDriverProfiles.SingleAsync(x => x.UserId == f.B.DriverUserId)).IsActive = false;
        if (kind == "maintenance") f.B.Vehicle!.Status = FleetVehicleStatuses.Maintenance;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SwapAsync(f.A.Id, f.Command(true), f.Actor, "test", default));
        Assert.True(f.A.IsActive); Assert.True(f.B.IsActive); Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task Separate_permissions_for_both_jobs_and_noop_are_enforced()
    {
        await using var db = Memory(); var f = await Seed(db); f.B.FleetRequest!.Status = FleetRequestStatuses.PendingAdminReview; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(db, new Permission(FleetPermissions.DispatchReplaceAssignment)).SwapAsync(f.A.Id, f.Command(), f.Actor, "test", default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(db, new Permission(FleetPermissions.DispatchView, FleetPermissions.RequestViewAll)).CandidatesAsync(f.A.Id, null, f.Actor, default));
        Assert.Empty(await Service(db, new Permission(FleetPermissions.DispatchReplaceAssignment)).CandidatesAsync(f.A.Id, null, f.Actor, default));
        f.B.VehicleId = f.A.VehicleId; f.B.Vehicle = f.A.Vehicle; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).SwapAsync(f.A.Id, f.Command(), f.Actor, "test", default));
    }
}
