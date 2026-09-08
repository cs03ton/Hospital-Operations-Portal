using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetMilestone4RolloutHealthTests
{
    [Fact]
    public async Task Rollout_Disabled_DeniesEveryUser()
    {
        await using var db = CreateDb(); var service = Service(db, "Disabled");
        Assert.False((await service.GetAsync(User(Guid.NewGuid(), "FleetAdmin"), default)).IsAllowed);
    }

    [Fact]
    public async Task Rollout_UatOnly_AllowsConfiguredUserOrRoleOnly()
    {
        await using var db = CreateDb(); var id = Guid.NewGuid();
        var options = Options.Create(new FleetRolloutOptions { RolloutMode = "UATOnly", UatUserIds = [id], UatRoleCodes = ["FleetDispatcher"] }); var service = new FleetRolloutService(db, options);
        Assert.True((await service.GetAsync(User(id, "Staff"), default)).IsAllowed);
        Assert.True((await service.GetAsync(User(Guid.NewGuid(), "FleetDispatcher"), default)).IsAllowed);
        Assert.False((await service.GetAsync(User(Guid.NewGuid(), "Staff"), default)).IsAllowed);
    }

    [Fact]
    public async Task Rollout_Enabled_AllowsAuthenticatedUser()
    {
        await using var db = CreateDb(); Assert.True((await Service(db, "Enabled").GetAsync(User(Guid.NewGuid(), "Staff"), default)).IsAllowed);
    }

    [Fact]
    public async Task Health_DetectsStuckWorkflow_AndDoesNotWarnEarlyRetry()
    {
        await using var db = CreateDb(); var user = new User { Id = Guid.NewGuid(), Username = "u", FullName = "U" }; db.Users.Add(user);
        db.FleetRequests.Add(new FleetRequest { Id = Guid.NewGuid(), RequestNo = "VH-202608-9000", RequesterUserId = user.Id, CreatedByUserId = user.Id, Purpose = "x", MissionType = "NORMAL", Destination = "x", ContactPersonName = "x", ContactPhone = "1", DepartureAt = DateTime.UtcNow.AddDays(1), ExpectedReturnAt = DateTime.UtcNow.AddDays(1).AddHours(1), PassengerCount = 1, Status = FleetRequestStatuses.PendingDispatch, CreatedAt = DateTime.UtcNow.AddHours(-30) });
        var eventId = Guid.NewGuid(); db.DomainEvents.Add(new DomainEventRecord { EventId = eventId, EventType = "Fleet.RequestSubmitted", Scope = "FLEET", AggregateType = "FleetRequest", AggregateId = Guid.NewGuid(), CorrelationId = "c" }); var outbox = new OutboxMessage { EventId = eventId, EventType = "Fleet.RequestSubmitted", Scope = "FLEET", Status = "RETRY" }; outbox.Deliveries.Add(new NotificationDelivery { RecipientUserId = user.Id, Channel = "LINE", Status = "RETRY", AttemptCount = 1, IdempotencyKey = "unique" }); db.OutboxMessages.Add(outbox); await db.SaveChangesAsync();
        var health = new FleetHealthService(db, Options.Create(new FleetRolloutOptions { WarningStuckHours = 24, CriticalStuckHours = 72, DeliveryRetryWarningCount = 3 }), NullLogger<FleetHealthService>.Instance);
        var issues = await health.GetIssuesAsync(null, default); Assert.Contains(issues, x => x.Code == "STUCK_WORKFLOW" && x.Severity == "Warning"); Assert.DoesNotContain(issues, x => x.Code == "NOTIFICATION_DELIVERY");
    }

    [Fact]
    public async Task PermissionDiagnostics_FlagsFleetPermissionOnProductionRole()
    {
        await using var db = CreateDb(); var role = new Role { Id = Guid.NewGuid(), Name = "Staff" }; var permission = new Permission { Id = Guid.NewGuid(), Code = FleetPermissions.RequestViewOwn, Name = "Fleet", Group = "Fleet", Action = "View" }; db.AddRange(role, permission, new RolePermission { RoleId = role.Id, PermissionId = permission.Id }); await db.SaveChangesAsync();
        var result = await new FleetPermissionDiagnosticsService(db).GetAsync(default); Assert.Equal("Warning", result.Status); Assert.Contains(result.Roles, x => x.RoleName == "Staff" && x.UnexpectedPermissions.Contains(FleetPermissions.RequestViewOwn)); Assert.False(result.DirectUserPermissionsSupported);
    }

    private static FleetRolloutService Service(AppDbContext db, string mode) => new(db, Options.Create(new FleetRolloutOptions { RolloutMode = mode }));
    private static ClaimsPrincipal User(Guid id, string role) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role)], "test"));
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
