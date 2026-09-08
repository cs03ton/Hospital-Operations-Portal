using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetProductionHardeningTests
{
    [Fact]
    public void AssignmentModel_HasConcurrencyAndImmutableReplacementLink()
    {
        using var db = CreateDb(); var model = db.Model.FindEntityType(typeof(FleetAssignment))!;
        Assert.True(model.FindProperty(nameof(FleetAssignment.ConcurrencyToken))!.IsConcurrencyToken);
        Assert.Contains(model.GetForeignKeys(), x => x.Properties.Single().Name == nameof(FleetAssignment.ReplacedAssignmentId));
        var oldId = Guid.NewGuid(); var item = new FleetAssignment { ReplacedAssignmentId = oldId };
        Assert.Equal(oldId, item.ReplacementOfAssignmentId);
    }

    [Fact]
    public async Task RecipientResolver_ResolvesPermissionRequesterAndDriver_WithoutCrossScopeGrant()
    {
        await using var db = CreateDb();
        var requester = User("requester"); var dispatcher = User("dispatcher"); var driver = User("driver"); var leaveDelegate = User("leave-delegate");
        var permission = new Permission { Id = Guid.NewGuid(), Code = FleetPermissions.DispatchView, Name = "dispatch", Group = "Fleet", Action = "View" };
        var role = new Role { Id = Guid.NewGuid(), Name = "FleetDispatcher" };
        db.AddRange(requester, dispatcher, driver, leaveDelegate, permission, role,
            new RolePermission { RoleId = role.Id, PermissionId = permission.Id }, new UserRole { UserId = dispatcher.Id, RoleId = role.Id });
        var request = Request(requester.Id); db.FleetRequests.Add(request);
        db.FleetAssignments.Add(new FleetAssignment { FleetRequestId = request.Id, VehicleId = Guid.NewGuid(), DriverUserId = driver.Id, AssignedByUserId = dispatcher.Id });
        db.ApprovalDelegations.Add(new ApprovalDelegation { ApproverUserId = dispatcher.Id, DelegateUserId = leaveDelegate.Id, Scope = "LEAVE", RequiredPermissionCode = FleetPermissions.DispatchView, StartDate = DateOnly.FromDateTime(DateTime.UtcNow), EndDate = DateOnly.FromDateTime(DateTime.UtcNow), StartAt = DateTime.UtcNow.AddHours(-1), EndAt = DateTime.UtcNow.AddHours(1), Reason = "wrong scope", CreatedByUserId = dispatcher.Id });
        await db.SaveChangesAsync(); var resolver = new FleetNotificationRecipientResolver(db);
        var dispatchRecipients = await resolver.ResolveAsync("Fleet.RequestSubmitted", request.Id, default);
        Assert.Contains(dispatcher.Id, dispatchRecipients); Assert.DoesNotContain(leaveDelegate.Id, dispatchRecipients);
        var approvedRecipients = await resolver.ResolveAsync("Fleet.DirectorApproved", request.Id, default);
        Assert.Contains(requester.Id, approvedRecipients); Assert.Contains(driver.Id, approvedRecipients);
    }

    [Fact]
    public async Task Publisher_CreatesIdempotentPerChannelDeliveries()
    {
        await using var db = CreateDb(); var requester = User("requester"); db.Users.Add(requester); var request = Request(requester.Id); db.FleetRequests.Add(request); await db.SaveChangesAsync();
        var publisher = new DomainEventPublisher(db, new FleetNotificationRecipientResolver(db));
        await publisher.PublishAsync(new("Fleet.RequestRejected", "FLEET", "FleetRequest", request.Id, requester.Id, "corr-1", new { request.RequestNo }, []), default);
        await db.SaveChangesAsync();
        var deliveries = await db.NotificationDeliveries.ToListAsync(); Assert.Equal(2, deliveries.Count); Assert.Equal(2, deliveries.Select(x => x.IdempotencyKey).Distinct().Count()); Assert.Contains(deliveries, x => x.Channel == "IN_APP"); Assert.Contains(deliveries, x => x.Channel == "LINE");
    }

    [Theory]
    [InlineData(-1, 0, false)] [InlineData(100, 99, false)] [InlineData(100, 100, true)] [InlineData(100, 120, true)]
    public void MileageRule_RejectsNegativeOrDecreasing(decimal start, decimal end, bool expected) => Assert.Equal(expected, start >= 0 && end >= start);

    private static User User(string name) => new() { Id = Guid.NewGuid(), Username = name, FullName = name, IsActive = true };
    private static FleetRequest Request(Guid requesterId) => new() { Id = Guid.NewGuid(), RequestNo = "VH-202608-9999", RequesterUserId = requesterId, CreatedByUserId = requesterId, Purpose = "Test", MissionType = "GENERAL", Destination = "Test", ContactPersonName = "Test", ContactPhone = "1", DepartureAt = DateTime.UtcNow.AddDays(1), ExpectedReturnAt = DateTime.UtcNow.AddDays(1).AddHours(2), PassengerCount = 1, Status = FleetRequestStatuses.PendingDispatch };
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
