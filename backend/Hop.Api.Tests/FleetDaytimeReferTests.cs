using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class FleetDaytimeReferTests
{
    private static readonly DateTime Morning = new(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc); // 07:00 Bangkok
    [Theory]
    [InlineData(-61, false)] // 05:59
    [InlineData(-60, true)] // 06:00
    [InlineData(659, true)] // 17:59
    [InlineData(660, false)] // 18:00
    public void Window_is_Bangkok_six_to_before_eighteen(int minutes, bool expected)
    {
        var request = new FleetRequest { MissionType = FleetReferPolicy.MissionType, DepartureAt = Morning.AddHours(1), Status = FleetRequestStatuses.PendingDriverAck };
        Assert.Equal(expected, FleetDaytimeReferNotification.Eligible(request, Morning.AddMinutes(minutes)));
        request.DepartureAt = Morning.AddDays(1); Assert.False(FleetDaytimeReferNotification.Eligible(request, Morning));
        request.DepartureAt = Morning; request.MissionType = "ประชุม"; Assert.False(FleetDaytimeReferNotification.Eligible(request, Morning));
    }

    [Fact]
    public async Task Queue_freezes_recipients_text_and_deduplicates_without_personal_or_generic_duplicate()
    {
        await using var db = Db(); var f = await Seed(db);
        var late = Group(Morning.AddMinutes(1)); db.LineGroupDestinations.Add(late);
        var disabled = Group(Morning.AddDays(-1)); disabled.Status = "Disabled"; db.Add(disabled);
        var off = Group(Morning.AddDays(-1)); off.EventSubscriptions.First().IsEnabled = false; db.Add(off);
        await db.SaveChangesAsync();
        Assert.Equal(1, await Queue(db, f)); await db.SaveChangesAsync();
        Assert.Equal(0, await Queue(db, f));
        var log = await db.LineGroupDeliveryLogs.SingleAsync(); var text = log.MessageText;
        Assert.Contains("🚑 ภารกิจ Refer ระหว่างวัน", text); Assert.Contains("ทดสอบปลายทาง", text); Assert.Contains("ทะเบียนทดสอบ", text); Assert.Contains("คนขับทดสอบ", text);
        Assert.DoesNotContain("ชื่อผู้ป่วยลับ", text); Assert.DoesNotContain("เหตุผลลับ", text);
        f.Request.Destination = "เปลี่ยนจุดหมาย"; await db.SaveChangesAsync();
        Assert.Equal(2, await Service(db, new Client()).DiscoverAsync(default)); // Other groups retain their generic assignment subscription.
        Assert.Single(await db.LineGroupDeliveryLogs.Where(x => x.DestinationId == f.Group.Id).ToListAsync()); Assert.Empty(await db.LineDeliveryLogs.ToListAsync());
        Assert.Equal(text, log.MessageText);
    }

    [Fact]
    public async Task No_historical_queue_for_subscription_enabled_after_event_and_generic_still_works()
    {
        await using var db = Db(); var f = await Seed(db);
        var subscription = f.Group.EventSubscriptions.First(); subscription.UpdatedAt = Morning.AddSeconds(1); await db.SaveChangesAsync();
        Assert.Equal(0, await Queue(db, f));
        Assert.Equal(1, await Service(db, new Client()).DiscoverAsync(default));
        Assert.Equal("Fleet.AssignmentCreated", (await db.LineGroupDeliveryLogs.SingleAsync()).CanonicalEventType);
    }

    [Fact]
    public async Task Retry_uses_same_snapshot_and_only_during_same_day_window()
    {
        await using var db = Db(); var f = await Seed(db); await Queue(db, f); await db.SaveChangesAsync();
        var client = new Client { FailFirst = true }; var service = Service(db, client);
        await service.ProcessAtAsync(Morning, default);
        var log = await db.LineGroupDeliveryLogs.SingleAsync(); Assert.Equal("Retry", log.Status);
        f.Request.Destination = "เปลี่ยนหลังส่ง"; log.AvailableAt = Morning; await db.SaveChangesAsync();
        await service.ProcessAtAsync(Morning.AddMinutes(1), default);
        Assert.Equal("Sent", log.Status); Assert.Equal(2, client.Messages.Count); Assert.Equal(client.Messages[0], client.Messages[1]);
    }

    [Theory]
    [InlineData("window", "REFER_WINDOW_EXPIRED")]
    [InlineData("tomorrow", "REFER_WINDOW_EXPIRED")]
    [InlineData("subscription", "SUBSCRIPTION_DISABLED")]
    [InlineData("reenabled", "SUBSCRIPTION_DISABLED")]
    [InlineData("CANCELLED", "REFER_REQUEST_CLOSED")]
    [InlineData("COMPLETED", "REFER_REQUEST_CLOSED")]
    [InlineData("ABORTED", "REFER_REQUEST_CLOSED")]
    [InlineData("destination", "DESTINATION_DISABLED")]
    public async Task Closed_or_expired_pending_notifications_are_terminal_without_API_call(string change, string error)
    {
        await using var db = Db(); var f = await Seed(db); await Queue(db, f); await db.SaveChangesAsync();
        var now = change == "window" ? Morning.AddHours(11) : change == "tomorrow" ? Morning.AddDays(1) : Morning.AddMinutes(1);
        if (change == "subscription") f.Group.EventSubscriptions.First().IsEnabled = false;
        if (change == "reenabled") f.Group.EventSubscriptions.First().UpdatedAt = Morning.AddSeconds(1);
        if (change == "destination") f.Group.Status = "Disabled";
        if (change is "CANCELLED" or "COMPLETED" or "ABORTED") f.Request.Status = change;
        await db.SaveChangesAsync(); var client = new Client(); var service = Service(db, client);
        await service.ProcessAtAsync(now, default); await service.ProcessAtAsync(now, default);
        var log = await db.LineGroupDeliveryLogs.SingleAsync(); Assert.Equal("Skipped", log.Status); Assert.Equal(error, log.ErrorCode); Assert.Empty(client.Messages);
    }

    internal static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    internal sealed record Fixture(FleetRequest Request, FleetAssignment Assignment, LineGroupDestination Group, Guid EventId);
    internal static async Task<Fixture> Seed(AppDbContext db)
    {
        var group = Group(Morning.AddDays(-1));
        var creator = new User { Username = "refer-requester", FullName = "ผู้ขอทดสอบ", PasswordHash = "test" }; db.Add(creator);
        var request = new FleetRequest { RequestNo = "VH-REFER", MissionType = FleetReferPolicy.MissionType, PatientName = "ชื่อผู้ป่วยลับ", UrgentReason = "เหตุผลลับ",
            RequesterUser = creator, CreatedByUserId = creator.Id, PassengerCount = 1, Destination = "ทดสอบปลายทาง", DepartureAt = Morning.AddHours(1), ExpectedReturnAt = Morning.AddHours(2), Status = FleetRequestStatuses.PendingDriverAck };
        var assignment = new FleetAssignment { FleetRequest = request, Vehicle = new() { VehicleCode = "REFER-QA", SeatCapacityTotal = 2, PassengerCapacity = 1, RegistrationNumber = "ทะเบียนทดสอบ", VehicleType = new() { Code = "REFER-QA", Name = "QA" } }, DriverUser = new() { Username = "driver", FullName = "คนขับทดสอบ", PasswordHash = "test" }, AssignedByUser = new() { Username = "dispatcher", FullName = "ผู้จัดรถ", PasswordHash = "test" }, IsActive = true };
        var id = Guid.NewGuid(); db.AddRange(request, assignment, group);
        db.DomainEvents.Add(new() { EventId = id, EventType = "Fleet.ReferAutoApproved", Scope = "FLEET", AggregateType = "FleetRequest", AggregateId = request.Id, OccurredAt = Morning });
        db.OutboxMessages.Add(new() { EventId = id, EventType = "Fleet.ReferAutoApproved", Scope = "FLEET" });
        await db.SaveChangesAsync(); return new(request, assignment, group, id);
    }
    private static LineGroupDestination Group(DateTime enabled) => new() { Status = "Active", Module = "CENTRAL", ConfirmedAt = Morning.AddDays(-1),
        EventSubscriptions = [new() { EventType = FleetDaytimeReferNotification.EventType, IsEnabled = true, CreatedAt = enabled },
            new() { EventType = "Fleet.AssignmentCreated", IsEnabled = true }] };
    private static Task<int> Queue(AppDbContext db, Fixture f) => FleetDaytimeReferNotification.QueueAsync(db, f.Request, f.Assignment, f.EventId, Morning, "refer-test", default);
    internal static FleetLineGroupDeliveryService Service(AppDbContext db, ILineGroupPushClient client) => new(db, new FleetLineGroupEventMapper(), new FleetEffectivePermissionService(db), new NeverTemplate(), client,
        Options.Create(new LineGroupNotificationsOptions { Enabled = true, MaxAttempts = 3 }), NullLogger<FleetLineGroupDeliveryService>.Instance);
    private sealed class NeverTemplate : IFleetLineGroupMessageTemplateService
    {
        public Task<FleetGroupRenderedMessage?> RenderAsync(DomainEventRecord domainEvent, string canonicalEventType, CancellationToken ct) => throw new Exception("Snapshot must be used");
    }
    internal sealed class Client : ILineGroupPushClient
    {
        public bool FailFirst { get; init; }
        public List<string> Messages { get; } = [];
        public Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct)
        {
            Messages.Add(text); return Task.FromResult(FailFirst && Messages.Count == 1 ? new LineGroupPushResult(false, true, false, "TRANSIENT", "test") : new LineGroupPushResult(true, false, false, null, null));
        }
    }
}
