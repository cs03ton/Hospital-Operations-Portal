using System.Net;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetLineGroupDeliveryTests
{
    [Fact]
    public async Task Room_subscriptions_route_to_multiple_selected_rooms_and_keep_legacy_all_rooms_once()
    {
        await using var db = Database(); var now = DateTime.UtcNow;
        var administration = new MeetingRoom { Name = "ห้องบริหาร", Code = "ADMIN" };
        var promotion = new MeetingRoom { Name = "ห้องส่งเสริม", Code = "PROMOTION" };
        LineGroupDestination Group(string name, params string[] events) => new() { LineGroupId = name, DisplayName = name, Module = "CENTRAL", Status = "Active", ConfirmedAt = now.AddDays(-1),
            EventSubscriptions = events.Select(type => new LineGroupEventSubscription { EventType = type, IsEnabled = true, CreatedAt = now.AddDays(-1) }).ToList() };
        var all = Group("all", MeetingRoomGroupSubscriptions.EventType);
        var adminOnly = Group("admin", MeetingRoomGroupSubscriptions.ForRoom(administration.Id));
        var both = Group("both", MeetingRoomGroupSubscriptions.ForRoom(administration.Id), MeetingRoomGroupSubscriptions.ForRoom(promotion.Id));
        var overlap = Group("overlap", MeetingRoomGroupSubscriptions.EventType, MeetingRoomGroupSubscriptions.ForRoom(administration.Id));
        var late = Group("late", MeetingRoomGroupSubscriptions.ForRoom(administration.Id)); late.EventSubscriptions.First().UpdatedAt = now.AddSeconds(1);
        db.AddRange(administration, promotion, all, adminOnly, both, overlap, late);
        foreach (var room in new[] { administration, promotion })
        {
            var booking = new MeetingRoomBooking { RoomId = room.Id }; db.Add(booking);
            var id = Guid.NewGuid(); db.DomainEvents.Add(new() { EventId = id, EventType = MeetingRoomGroupSubscriptions.EventType, Scope = "MEETING_ROOM", AggregateId = booking.Id, OccurredAt = now });
            db.OutboxMessages.Add(new() { EventId = id, EventType = MeetingRoomGroupSubscriptions.EventType, Scope = "MEETING_ROOM" });
        }
        await db.SaveChangesAsync(); var service = Service(db, new SequenceClient(Success()));
        Assert.Equal(7, await service.DiscoverAsync(default)); Assert.Equal(0, await service.DiscoverAsync(default));
        Assert.Equal(2, await db.LineGroupDeliveryLogs.CountAsync(x => x.DestinationId == all.Id));
        Assert.Equal(1, await db.LineGroupDeliveryLogs.CountAsync(x => x.DestinationId == adminOnly.Id));
        Assert.Equal(2, await db.LineGroupDeliveryLogs.CountAsync(x => x.DestinationId == both.Id));
        Assert.Equal(2, await db.LineGroupDeliveryLogs.CountAsync(x => x.DestinationId == overlap.Id));
        Assert.False(await db.LineGroupDeliveryLogs.AnyAsync(x => x.DestinationId == late.Id));
        var definitions = await MeetingRoomGroupSubscriptions.DefinitionsAsync(db, default);
        Assert.False(definitions[MeetingRoomGroupSubscriptions.ForRoom(administration.Id)]);
        Assert.True(definitions[MeetingRoomGroupSubscriptions.EventType]);
    }

    [Fact]
    public async Task Daily_digest_adds_afternoon_snapshot_with_current_jobs_and_slot_recipients()
    {
        await using var db = Database();
        var morning = new DateTime(2026, 10, 29, 23, 0, 0, DateTimeKind.Utc); // 30 Oct 06:00
        LineGroupDestination Group(string name, DateTime enabled) => new() { LineGroupId = name, Module = "CENTRAL", Status = "Active", ConfirmedAt = morning.AddDays(-1),
            EventSubscriptions = [new() { EventType = FleetDailyDriverJobsDigest.EventType, IsEnabled = true, CreatedAt = enabled }] };
        var original = Group("original", morning.AddDays(-1)); var midday = Group("midday", morning.AddHours(6)); var late = Group("late", morning.AddHours(7).AddMinutes(1));
        FleetRequest Request(string number) => new() { RequestNo = number, DepartureAt = morning.AddHours(8), Status = FleetRequestStatuses.Ready, Destination = "ทดสอบ",
            Assignments = [new() { IsActive = true, Vehicle = new() { RegistrationNumber = "QA" }, DriverUser = new() { FullName = "คนขับ" } }] };
        var done = Request("DONE-AM"); var remaining = Request("REMAINING");
        db.AddRange(original, midday, late, done, remaining); await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));
        Assert.Equal(1, await service.ProjectDailyDriverJobsAsync(morning, default));
        var oldPayload = (await db.DomainEvents.SingleAsync(x => x.EventType == FleetDailyDriverJobsDigest.EventType)).Payload;
        done.Status = FleetRequestStatuses.Completed; db.Add(Request("NEW-PM")); await db.SaveChangesAsync();
        Assert.Equal(0, await service.ProjectDailyDriverJobsAsync(morning.AddHours(6).AddMinutes(59), default));
        Assert.Equal(1, await service.ProjectDailyDriverJobsAsync(morning.AddHours(7), default));
        Assert.Equal(0, await service.ProjectDailyDriverJobsAsync(morning.AddHours(8), default));
        var afternoon = await db.DomainEvents.SingleAsync(x => x.EventId == FleetDailyDriverJobsDigest.StableId(new(2026, 10, 30), 0, 1));
        Assert.Contains("13:00", afternoon.Payload); Assert.DoesNotContain("DONE-AM", afternoon.Payload); Assert.Contains("REMAINING", afternoon.Payload); Assert.Contains("NEW-PM", afternoon.Payload);
        Assert.Equal(2, await db.LineGroupDeliveryLogs.CountAsync(x => x.EventId == afternoon.EventId));
        Assert.False(await db.LineGroupDeliveryLogs.AnyAsync(x => x.DestinationId == late.Id));
        Assert.Equal(oldPayload, (await db.DomainEvents.SingleAsync(x => x.EventId == FleetDailyDriverJobsDigest.StableId(new(2026, 10, 30), 0, 0))).Payload);
    }

    [Fact]
    public async Task Late_start_creates_only_latest_slot_and_empty_slots_are_recorded_once()
    {
        await using var db = Database(); var service = Service(db, new SequenceClient(Success()));
        var now = new DateTime(2026, 10, 30, 7, 0, 0, DateTimeKind.Utc); // 14:00
        Assert.Equal(0, await service.ProjectDailyDriverJobsAsync(now, default));
        Assert.Equal(0, await service.ProjectDailyDriverJobsAsync(now.AddMinutes(1), default));
        var marker = Assert.Single(await db.DomainEvents.ToListAsync());
        Assert.Equal(FleetDailyDriverJobsDigest.StableId(new(2026, 10, 30), -1, 1), marker.EventId);
        Assert.Empty(await db.LineGroupDeliveryLogs.ToListAsync());
        Assert.Equal(-1, FleetDailyDriverJobsDigest.CurrentSlot(new(5, 59)));
        Assert.Equal(0, FleetDailyDriverJobsDigest.CurrentSlot(new(6, 0)));
        Assert.Equal(0, FleetDailyDriverJobsDigest.CurrentSlot(new(12, 59)));
        Assert.Equal(1, FleetDailyDriverJobsDigest.CurrentSlot(new(13, 0)));
        var key = "HOP:FleetDailyDriverJobs:2026-10-30:0";
        Assert.Equal(new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)).AsSpan(0,16)), FleetDailyDriverJobsDigest.StableId(new(2026,10,30),0));
        var messages = FleetDailyDriverJobsDigest.BuildMessages(new(2026,10,30), Enumerable.Range(0,100).Select(x => $"{x}:" + new string('ก',100)).ToArray(),1);
        Assert.True(messages.Count > 1); Assert.All(messages, text => { Assert.Contains("13:00",text); Assert.True(text.Length <= 4000); });
    }

    [Fact]
    public async Task Approval_digest_prefers_highest_heads_over_other_department_heads_in_both_departments()
    {
        await using var db = Database();
        var now = new DateTime(2026, 10, 21, 5, 31, 0, DateTimeKind.Utc);
        var highestRole = new Role { Name = "หัวหน้าหน่วยงานสูงสุด", IsActive = true };
        var genericRole = new Role { Name = "DepartmentHead", IsActive = true };
        var reviewRole = new Role { Name = "FleetAdminReviewer", IsActive = true,
            RolePermissions = [new RolePermission { Permission = new Permission
            { Code = "FleetAdminReview.Approve", Name = "Review", IsActive = true } }] };
        var nursing = new Department { Name = "กลุ่มงานการพยาบาล", IsActive = true };
        var administration = new Department { Name = "กลุ่มงานบริหารทั่วไป", IsActive = true };
        User Head(string name, Department department, Role role) => new()
        {
            Username = name, FullName = name, IsActive = true, Department = department,
            UserRoles = [new UserRole { Role = role }]
        };
        var nurse = Head("highest-nurse", nursing, highestRole);
        var admin = Head("highest-admin", administration, highestRole);
        admin.UserRoles.Add(new UserRole { Role = reviewRole });
        db.Users.AddRange(nurse, admin, Head("other-nurse", nursing, genericRole), Head("other-admin", administration, genericRole));
        db.LineGroupDestinations.Add(new LineGroupDestination
        {
            LineGroupId = "highest-head-digest", Module = "CENTRAL", Status = LineGroupDestinationStatuses.Active,
            ConfirmedAt = now.AddDays(-1), EventSubscriptions = [new LineGroupEventSubscription
            { EventType = PendingApprovalDigest.EventType, IsEnabled = true, CreatedAt = now.AddDays(-1) }]
        });
        db.LeaveApprovals.Add(new LeaveApproval
        {
            ApproverId = nurse.Id, Status = "Pending",
            LeaveRequest = new LeaveRequest { Status = "Pending", CurrentApproverId = nurse.Id }
        });
        db.LeaveCancellationApprovals.Add(new LeaveCancellationApproval
        {
            ApproverId = admin.Id, Status = "Pending",
            LeaveCancellationRequest = new LeaveCancellationRequest
            { Status = LeaveCancellationStatuses.Pending, CurrentApproverId = admin.Id }
        });
        db.FleetRequests.Add(new FleetRequest { RequestNo = "VH-ADMIN-DIGEST", Status = FleetRequestStatuses.PendingAdminReview });
        await db.SaveChangesAsync();

        Assert.Equal(1, await Service(db, new SequenceClient(Success())).ProjectPendingApprovalsAsync(now, default));
        var text = (await db.LineGroupDeliveryLogs.SingleAsync()).MessageText;
        Assert.Contains("👤 หัวหน้าสูงสุดฝ่ายพยาบาล\n📄 คำขอลา: 1 คำขอ", text);
        Assert.Contains("👤 หัวหน้าสูงสุดฝ่ายบริหาร\n↩️ ยกเลิกใบลา: 1 คำขอ\n🚗 คำขอใช้รถ: 1 คำขอ", text);
    }

    [Fact]
    public async Task Pending_approval_digest_snapshots_current_queue_once_per_slot_for_subscribed_group()
    {
        await using var db = Database();
        var now = new DateTime(2026, 10, 21, 1, 1, 0, DateTimeKind.Utc); // 08:01 Bangkok
        var director = new User
        {
            Id = Guid.NewGuid(), Username = "director-digest", FullName = "ผู้อำนวยการ", IsActive = true,
            UserRoles = [new UserRole { Role = new Role
            {
                Name = "Director", IsActive = true,
                RolePermissions = [new RolePermission { Permission = new Permission
                { Code = "FleetDirector.Approve", Name = "FleetDirector.Approve", IsActive = true } }]
            } }]
        };
        var group = new LineGroupDestination
        {
            LineGroupId = "digest-group", Module = "CENTRAL", Status = LineGroupDestinationStatuses.Active,
            ConfirmedAt = now.AddDays(-1),
            EventSubscriptions = [new LineGroupEventSubscription
            { EventType = PendingApprovalDigest.EventType, IsEnabled = true, CreatedAt = now.AddDays(-1) }]
        };
        db.Users.Add(director);
        db.LineGroupDestinations.Add(group);
        db.LeaveApprovals.Add(new LeaveApproval
        {
            ApproverId = director.Id, Status = "Pending",
            LeaveRequest = new LeaveRequest { Status = "Pending", CurrentApproverId = director.Id }
        });
        db.LeaveApprovals.Add(new LeaveApproval
        {
            ApproverId = director.Id, Status = "Approved",
            LeaveRequest = new LeaveRequest { Status = "Approved", CurrentApproverId = director.Id }
        });
        db.FleetRequests.Add(new FleetRequest { RequestNo = "VH-DIGEST-1", Status = FleetRequestStatuses.PendingDirector });
        db.FleetRequests.Add(new FleetRequest { RequestNo = "VH-DIGEST-2", Status = FleetRequestStatuses.Cancelled });
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));

        Assert.Equal(1, await service.ProjectPendingApprovalsAsync(now, default));
        Assert.Equal(0, await service.ProjectPendingApprovalsAsync(now.AddMinutes(10), default));
        var log = Assert.Single(await db.LineGroupDeliveryLogs.ToListAsync());
        Assert.Contains("📄 คำขอลา: 1 คำขอ", log.MessageText);
        Assert.Contains("🚗 คำขอใช้รถ: 1 คำขอ", log.MessageText);
        Assert.DoesNotContain("หัวหน้าสูงสุดฝ่าย", log.MessageText);
        var snapshot = await db.DomainEvents.SingleAsync(x => x.EventId == log.EventId);
        Assert.Equal(log.MessageText, JsonDocument.Parse(snapshot.Payload).RootElement.GetProperty("Text").GetString());
    }

    [Fact]
    public async Task Pending_approval_digest_with_no_work_creates_no_delivery_or_backfill()
    {
        await using var db = Database();
        var now = new DateTime(2026, 10, 21, 5, 31, 0, DateTimeKind.Utc); // 12:31 Bangkok
        var group = new LineGroupDestination
        {
            LineGroupId = "empty-digest", Module = "CENTRAL", Status = LineGroupDestinationStatuses.Active,
            ConfirmedAt = now.AddDays(-1),
            EventSubscriptions = [new LineGroupEventSubscription
            { EventType = PendingApprovalDigest.EventType, IsEnabled = true, CreatedAt = now.AddDays(-1) }]
        };
        db.LineGroupDestinations.Add(group);
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));

        Assert.Equal(0, await service.ProjectPendingApprovalsAsync(now, default));
        Assert.Equal(0, await service.ProjectPendingApprovalsAsync(now.AddMinutes(1), default));
        Assert.Empty(db.LineGroupDeliveryLogs);
        Assert.Single(db.DomainEvents.Where(x => x.EventType == PendingApprovalDigest.MarkerType));
    }

    [Fact]
    public async Task Daily_digest_snapshots_only_assigned_approved_jobs_for_enabled_groups_once()
    {
        await using var db = Database();
        var now = new DateTime(2026, 10, 14, 23, 1, 0, DateTimeKind.Utc); // 06:01 Bangkok
        foreach (var (name, enabled, active) in new[]
                 { ("subscribed", true, true), ("unsubscribed", false, true), ("disabled", true, false) })
        {
            var group = new LineGroupDestination
            {
                LineGroupId = name, Module = "CENTRAL", DisplayName = name,
                Status = active ? LineGroupDestinationStatuses.Active : LineGroupDestinationStatuses.Disabled,
                ConfirmedAt = now.AddDays(-1)
            };
            group.EventSubscriptions.Add(new LineGroupEventSubscription
                { EventType = FleetDailyDriverJobsDigest.EventType, IsEnabled = enabled, CreatedAt = now.AddDays(-1) });
            db.LineGroupDestinations.Add(group);
        }
        foreach (var (status, departure, assigned) in new[]
                 {
                     (FleetRequestStatuses.Approved, now.AddHours(3), true),
                     (FleetRequestStatuses.InProgress, now.AddHours(4), true),
                     (FleetRequestStatuses.PendingDispatch, now.AddHours(5), true),
                     (FleetRequestStatuses.Approved, now.AddHours(6), false),
                     (FleetRequestStatuses.Approved, now.AddDays(1), true),
                     (FleetRequestStatuses.Cancelled, now.AddHours(7), true)
                 })
        {
            var request = new FleetRequest
            {
                Id = Guid.NewGuid(), RequestNo = $"VH-{Guid.NewGuid():N}", Status = status,
                DepartureAt = departure, Destination = "โรงพยาบาลปลายทาง"
            };
            if (assigned) request.Assignments.Add(new FleetAssignment
            {
                IsActive = true, Vehicle = new FleetVehicle { RegistrationNumber = "กข 1234" },
                DriverUser = new User { FullName = "คนขับทดสอบ" }
            });
            db.FleetRequests.Add(request);
        }
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));

        Assert.Equal(0, await service.ProjectDailyDriverJobsAsync(now.AddMinutes(-2), default));
        Assert.Equal(1, await service.ProjectDailyDriverJobsAsync(now, default));
        Assert.Equal(0, await service.ProjectDailyDriverJobsAsync(now.AddHours(1), default));
        var log = Assert.Single(await db.LineGroupDeliveryLogs.ToListAsync());
        Assert.Equal("subscribed", (await db.LineGroupDestinations.FindAsync(log.DestinationId))!.LineGroupId);
        var snapshot = await db.DomainEvents.SingleAsync(x => x.EventId == log.EventId);
        using var payload = JsonDocument.Parse(snapshot.Payload);
        var text = payload.RootElement.GetProperty("Text").GetString()!;
        Assert.Contains("รวม 2 ภารกิจ", text);
        Assert.Contains("กข 1234", text);
        Assert.DoesNotContain("PendingDispatch", text);
    }

    [Theory]
    [InlineData("Fleet.RequestSubmitted", "Fleet.RequestSubmitted", "delivered")]
    [InlineData("Fleet.RequestCancelled", "Fleet.Cancelled", "delivered")]
    [InlineData("Fleet.RequestSubmitted", "Fleet.RequestSubmitted", "unknown")]
    [InlineData("Fleet.RequestSubmitted", "Fleet.RequestSubmitted", "unsubscribed")]
    public async Task Discovery_reaches_new_events_after_more_than_a_batch_of_old_events(
        string source, string canonical, string oldKind)
    {
        await using var db = Database();
        var fixture = await Seed(db, "FLEET", source, true, false);
        fixture.Destination.EventSubscriptions.Clear();
        db.LineGroupEventSubscriptions.RemoveRange(await db.LineGroupEventSubscriptions.ToListAsync());
        db.LineGroupEventSubscriptions.Add(new LineGroupEventSubscription { DestinationId = fixture.Destination.Id, EventType = canonical, IsEnabled = true });
        for (var i = 0; i < 150; i++)
        {
            var eventId = Guid.NewGuid();
            var eventType = oldKind == "unknown" ? "Fleet.Unknown" : oldKind == "unsubscribed" ? "Fleet.TripCompleted" : source;
            db.DomainEvents.Add(new DomainEventRecord { EventId = eventId, EventType = eventType, Scope = "FLEET", AggregateType = "FleetRequest", AggregateId = Guid.NewGuid(), OccurredAt = fixture.Event.OccurredAt.AddSeconds(-200 + i), Payload = "{}" });
            db.OutboxMessages.Add(new OutboxMessage { EventId = eventId, EventType = eventType, Scope = "FLEET", Payload = "{}" });
            if (oldKind == "delivered")
                db.LineGroupDeliveryLogs.Add(new LineGroupDeliveryLog { EventId = eventId, DestinationId = fixture.Destination.Id, CanonicalEventType = canonical, SourceEventType = source, Status = "Sent", DeduplicationKey = $"{eventId}:{fixture.Destination.Id}:{canonical}" });
        }
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));
        Assert.Equal(1, await service.DiscoverAsync(default));
        Assert.Equal(0, await service.DiscoverAsync(default));
        Assert.Single(await db.LineGroupDeliveryLogs.Where(x => x.EventId == fixture.Event.EventId).ToListAsync());
    }

    [Fact]
    public async Task Repair_events_reach_only_active_subscribed_groups_for_the_assigned_team_after_assignment()
    {
        await using var db = Database();
        var now = DateTime.UtcNow;
        foreach (var (team, offset) in new[] { ("IT", 0), ("GENERAL", 1) })
        {
            var eventId = Guid.NewGuid();
            var aggregateId = Guid.NewGuid();
            var occurredAt = now.AddSeconds(offset);
            var payload = JsonSerializer.Serialize(new { TeamCode = team });
            db.DomainEvents.Add(new DomainEventRecord { EventId = eventId, EventType = "Repair.Submitted", Scope = "REPAIR", AggregateType = "RepairRequest", AggregateId = aggregateId, OccurredAt = occurredAt, Payload = payload });
            db.OutboxMessages.Add(new OutboxMessage { EventId = eventId, EventType = "Repair.Submitted", Scope = "REPAIR", Payload = payload });
        }
        var groups = new[]
        {
            new LineGroupDestination { LineGroupId = "repair-it", Module = "CENTRAL", Status = "Active", ConfirmedAt = now.AddMinutes(-1), RepairTeamCode = "IT", RepairTeamAssignedAt = now.AddMinutes(-1) },
            new LineGroupDestination { LineGroupId = "repair-general", Module = "CENTRAL", Status = "Active", ConfirmedAt = now.AddMinutes(-1), RepairTeamCode = "GENERAL", RepairTeamAssignedAt = now.AddMinutes(-1) },
            new LineGroupDestination { LineGroupId = "repair-unassigned", Module = "CENTRAL", Status = "Active", ConfirmedAt = now.AddMinutes(-1) },
            new LineGroupDestination { LineGroupId = "repair-disabled", Module = "CENTRAL", Status = "Disabled", ConfirmedAt = now.AddMinutes(-1), RepairTeamCode = "IT", RepairTeamAssignedAt = now.AddMinutes(-1) },
            new LineGroupDestination { LineGroupId = "repair-unsubscribed", Module = "CENTRAL", Status = "Active", ConfirmedAt = now.AddMinutes(-1), RepairTeamCode = "IT", RepairTeamAssignedAt = now.AddMinutes(-1) },
            new LineGroupDestination { LineGroupId = "repair-late-assignment", Module = "CENTRAL", Status = "Active", ConfirmedAt = now.AddMinutes(-1), RepairTeamCode = "IT", RepairTeamAssignedAt = now.AddMinutes(1) }
        };
        foreach (var group in groups)
        {
            group.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = "Repair.Submitted", IsEnabled = group.LineGroupId != "repair-unsubscribed" });
            db.LineGroupDestinations.Add(group);
        }
        await db.SaveChangesAsync();

        var service = Service(db, new SequenceClient(Success()));
        Assert.Equal(2, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(0, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(new[] { "repair-general", "repair-it" },
            await db.LineGroupDeliveryLogs.OrderBy(x => x.Destination!.LineGroupId).Select(x => x.Destination!.LineGroupId).ToArrayAsync());
    }

    [Fact]
    public async Task Discovery_creates_exact_deduplicated_delivery_for_active_fleet_subscription()
    {
        await using var db = Database();
        var fixture = await Seed(db, "FLEET", "Fleet.RequestSubmitted", active: true, subscribed: true);
        var service = Service(db, new SequenceClient(Success()));

        Assert.Equal(1, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(0, await service.DiscoverAsync(CancellationToken.None));

        var log = await db.LineGroupDeliveryLogs.SingleAsync();
        Assert.Equal($"{fixture.Event.EventId}:{fixture.Destination.Id}:Fleet.RequestSubmitted", log.DeduplicationKey);
        Assert.Equal("Fleet.RequestSubmitted", log.CanonicalEventType);
    }

    [Fact]
    public async Task Discovery_fans_out_meeting_booking_to_every_subscribed_group()
    {
        await using var db = Database();
        var fixture = await Seed(db, "MEETING_ROOM", "MeetingRoom.BookingCreated", active: true, subscribed: false);
        db.LineGroupEventSubscriptions.Add(new LineGroupEventSubscription { DestinationId = fixture.Destination.Id, EventType = "MeetingRoom.BookingCreated", IsEnabled = true });
        var second = new LineGroupDestination
        {
            LineGroupId = "C22222222222222222222", DisplayName = "Meeting Group", Status = LineGroupDestinationStatuses.Active,
            Module = "CENTRAL", ConfirmedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        second.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = "MeetingRoom.BookingCreated", IsEnabled = true });
        db.Add(second); await db.SaveChangesAsync();

        Assert.Equal(2, await Service(db, new SequenceClient(Success())).DiscoverAsync(CancellationToken.None));
        Assert.Equal(2, await db.LineGroupDeliveryLogs.CountAsync());
        Assert.Equal(0, await Service(db, new SequenceClient(Success())).DiscoverAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("LEAVE", "LeaveSubmitted", true, true)]
    [InlineData("FLEET", "Fleet.Unknown", true, true)]
    [InlineData("FLEET", "Fleet.RequestSubmitted", false, true)]
    [InlineData("FLEET", "Fleet.RequestSubmitted", true, false)]
    public async Task Discovery_does_not_create_delivery_for_non_opted_in_event(string scope, string eventType, bool active, bool subscribed)
    {
        await using var db = Database();
        await Seed(db, scope, eventType, active, subscribed);

        Assert.Equal(0, await Service(db, new SequenceClient(Success())).DiscoverAsync(CancellationToken.None));
        Assert.Empty(db.LineGroupDeliveryLogs);
    }

    [Fact]
    public async Task Feature_flag_off_is_a_no_op()
    {
        await using var db = Database();
        await Seed(db, "FLEET", "Fleet.RequestSubmitted", true, true);
        var service = Service(db, new SequenceClient(Success()), enabled: false);

        Assert.Equal(0, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(0, await service.ProcessAsync(CancellationToken.None));
        Assert.Empty(db.LineGroupDeliveryLogs);
    }

    [Fact]
    public async Task Disabled_trip_overdue_does_not_project_or_discover_and_skips_queued_deliveries()
    {
        await using var db = Database();
        var fixture = await Seed(db, "FLEET", "Fleet.TripOverdue", true, false);
        db.LineGroupEventSubscriptions.Add(new LineGroupEventSubscription
        {
            DestinationId = fixture.Destination.Id, EventType = "Fleet.TripOverdue", IsEnabled = true
        });
        var overdueRequest = new FleetRequest
        {
            RequestNo = "VH-OVERDUE", Status = FleetRequestStatuses.InProgress,
            ExpectedReturnAt = DateTime.UtcNow.AddMinutes(-1)
        };
        db.FleetRequests.Add(overdueRequest);
        db.LineGroupDeliveryLogs.AddRange(
            new LineGroupDeliveryLog { EventId = fixture.Event.EventId, DestinationId = fixture.Destination.Id,
                CanonicalEventType = "Fleet.TripOverdue", Status = "Pending", AvailableAt = DateTime.UtcNow.AddDays(1), DeduplicationKey = "overdue-pending" },
            new LineGroupDeliveryLog { EventId = Guid.NewGuid(), DestinationId = fixture.Destination.Id,
                CanonicalEventType = "Fleet.TripOverdue", Status = "Retry", AvailableAt = DateTime.UtcNow.AddDays(1), DeduplicationKey = "overdue-retry" });
        await db.SaveChangesAsync();
        var client = new SequenceClient(Success());
        var service = Service(db, client);

        Assert.Equal(0, await service.ProjectMissingEventsAsync(CancellationToken.None));
        Assert.False(await db.DomainEvents.AnyAsync(x => x.AggregateId == overdueRequest.Id));
        Assert.Equal(0, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(2, await service.ProcessAsync(CancellationToken.None));
        Assert.Equal(0, client.CallCount);
        Assert.All(await db.LineGroupDeliveryLogs.ToListAsync(), log =>
        {
            Assert.Equal("Skipped", log.Status);
            Assert.Equal("TRIP_OVERDUE_DISABLED", log.ErrorCode);
        });
        Assert.True(fixture.Destination.EventSubscriptions.Single(x => x.EventType == "Fleet.TripOverdue").IsEnabled);
    }

    [Fact]
    public async Task Disabled_trip_overdue_does_not_block_other_fleet_notifications()
    {
        await using var db = Database();
        await Seed(db, "FLEET", "Fleet.RequestSubmitted", true, true);
        var client = new SequenceClient(Success());
        var service = Service(db, client);

        Assert.Equal(1, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(1, await service.ProcessAsync(CancellationToken.None));
        Assert.Equal("Sent", (await db.LineGroupDeliveryLogs.SingleAsync()).Status);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task Reenabled_trip_overdue_only_handles_requests_and_events_after_cutoff()
    {
        await using var db = Database();
        var now = DateTime.UtcNow;
        var fixture = await Seed(db, "FLEET", "Fleet.TripOverdue", true, false);
        fixture.Event.OccurredAt = now.AddMinutes(-10);
        db.LineGroupEventSubscriptions.Add(new LineGroupEventSubscription
        {
            DestinationId = fixture.Destination.Id, EventType = "Fleet.TripOverdue", IsEnabled = true
        });
        db.FleetRequests.AddRange(
            new FleetRequest { RequestNo = "OLD", Status = FleetRequestStatuses.InProgress, ExpectedReturnAt = now.AddMinutes(-10) },
            new FleetRequest { RequestNo = "NEW", Status = FleetRequestStatuses.InProgress, ExpectedReturnAt = now.AddMinutes(-1) });
        db.LineGroupDeliveryLogs.Add(new LineGroupDeliveryLog
        {
            EventId = fixture.Event.EventId, DestinationId = fixture.Destination.Id,
            CanonicalEventType = "Fleet.TripOverdue", Status = "Pending", CreatedAt = now,
            DeduplicationKey = "old-overdue"
        });
        await db.SaveChangesAsync();
        var cutoff = new DateTimeOffset(now.AddMinutes(-5), TimeSpan.Zero);
        var client = new SequenceClient(Success());
        var service = Service(db, client, tripOverdueEnabled: true, tripOverdueEnabledFromUtc: cutoff);

        Assert.Equal(1, await service.ProjectMissingEventsAsync(CancellationToken.None));
        Assert.Equal(1, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(2, await service.ProcessAsync(CancellationToken.None));
        Assert.Equal(1, client.CallCount);
        Assert.Equal("Skipped", (await db.LineGroupDeliveryLogs.SingleAsync(x => x.DeduplicationKey == "old-overdue")).Status);
        Assert.Equal("Sent", (await db.LineGroupDeliveryLogs.SingleAsync(x => x.DeduplicationKey != "old-overdue")).Status);
        Assert.Equal(2, await db.DomainEvents.CountAsync(x => x.EventType == "Fleet.TripOverdue"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_only_notifies_after_submission(bool submitted)
    {
        await using var db = Database();
        var now = DateTime.UtcNow;
        var request = new FleetRequest { Status = FleetRequestStatuses.Cancelled, SubmittedAt = submitted ? now.AddMinutes(-1) : null };
        var history = new FleetRequestStatusHistory { FleetRequest = request, FromStatus = submitted ? FleetRequestStatuses.PendingDispatch : FleetRequestStatuses.Draft,
            ToStatus = FleetRequestStatuses.Cancelled, Action = "Fleet.RequestCancelled", CreatedAt = now };
        var destination = new LineGroupDestination { LineGroupId = "C12345678901234567890", Module = "FLEET", Status = LineGroupDestinationStatuses.Active, ConfirmedAt = now.AddMinutes(-2) };
        destination.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = "Fleet.Cancelled", IsEnabled = true });
        db.AddRange(request, history, destination);
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));
        Assert.Equal(submitted ? 1 : 0, await service.ProjectMissingEventsAsync(default));
        Assert.Equal(0, await service.ProjectMissingEventsAsync(default));
        Assert.Equal(submitted ? 1 : 0, await service.DiscoverAsync(default));
        Assert.Equal(0, await service.DiscoverAsync(default));
        Assert.Equal(submitted ? 1 : 0, await db.LineGroupDeliveryLogs.CountAsync());
    }

    [Fact]
    public async Task Missing_request_submitted_event_is_projected_after_commit_without_user_deliveries()
    {
        await using var db = Database();
        var now = DateTime.UtcNow;
        var department = new Department { Id = Guid.NewGuid(), Name = "Fleet" };
        var user = new User { Id = Guid.NewGuid(), Username = "requester", FullName = "Requester", PasswordHash = "hash", Department = department, DepartmentId = department.Id };
        var request = new FleetRequest
        {
            Id = Guid.NewGuid(), RequestNo = "VH-202608-0999", RequesterUser = user, RequesterUserId = user.Id,
            RequesterDepartment = department, RequesterDepartmentId = department.Id, RequestDate = DateOnly.FromDateTime(now),
            Purpose = "งาน", MissionType = "ราชการ", Destination = "ปลายทาง", ContactPersonName = "ผู้ประสานงาน", ContactPhone = "0",
            DepartureAt = now.AddHours(1), ExpectedReturnAt = now.AddHours(2), PassengerCount = 1, CreatedByUserId = user.Id
        };
        var history = new FleetRequestStatusHistory
        {
            Id = Guid.NewGuid(), FleetRequest = request, FleetRequestId = request.Id, FromStatus = FleetRequestStatuses.Draft,
            ToStatus = FleetRequestStatuses.PendingDispatch, Action = "Fleet.RequestSubmitted", ActorUserId = user.Id,
            CreatedAt = now, CorrelationId = "projection-test"
        };
        var destination = new LineGroupDestination
        {
            LineGroupId = "C12345678901234567890", Status = LineGroupDestinationStatuses.Active, Module = "FLEET", ConfirmedAt = now.AddMinutes(-1)
        };
        destination.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = "Fleet.RequestSubmitted", IsEnabled = true });
        db.AddRange(department, user, request, history, destination);
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));

        Assert.Equal(1, await service.ProjectMissingEventsAsync(CancellationToken.None));
        Assert.Equal(0, await service.ProjectMissingEventsAsync(CancellationToken.None));
        Assert.Equal(1, await service.DiscoverAsync(CancellationToken.None));

        var domainEvent = await db.DomainEvents.SingleAsync(x => x.EventType == "Fleet.RequestSubmitted");
        Assert.Equal(history.Id, domainEvent.EventId);
        Assert.Empty((await db.OutboxMessages.Include(x => x.Deliveries).SingleAsync(x => x.EventType == "Fleet.RequestSubmitted")).Deliveries);
        Assert.Single(db.LineGroupDeliveryLogs);
    }

    [Fact]
    public async Task Transactional_request_submitted_event_is_not_projected_or_delivered_twice()
    {
        await using var db = Database();
        var now = DateTime.UtcNow;
        var department = new Department { Id = Guid.NewGuid(), Name = "Fleet" };
        var user = new User { Id = Guid.NewGuid(), Username = "urgent-requester", FullName = "Urgent Requester", PasswordHash = "hash", Department = department, DepartmentId = department.Id };
        var request = new FleetRequest
        {
            Id = Guid.NewGuid(), RequestNo = "VH-202610-0001", RequesterUser = user, RequesterUserId = user.Id,
            RequesterDepartment = department, RequesterDepartmentId = department.Id, RequestDate = DateOnly.FromDateTime(now),
            Purpose = "งานเร่งด่วน", MissionType = "ราชการ", Destination = "ปลายทาง", ContactPersonName = "ผู้ประสานงาน", ContactPhone = "0",
            DepartureAt = now.AddHours(1), ExpectedReturnAt = now.AddHours(2), PassengerCount = 1, IsUrgent = true,
            UrgentReason = "ต้องเดินทางภายในวันนี้", Status = FleetRequestStatuses.PendingDispatch, CreatedByUserId = user.Id
        };
        var eventId = Guid.NewGuid();
        var history = new FleetRequestStatusHistory
        {
            Id = eventId, FleetRequest = request, FleetRequestId = request.Id, FromStatus = FleetRequestStatuses.Draft,
            ToStatus = FleetRequestStatuses.PendingDispatch, Action = "Fleet.RequestSubmitted", ActorUserId = user.Id,
            CreatedAt = now, CorrelationId = "transactional-submit-test"
        };
        var payload = JsonSerializer.Serialize(new { FleetRequestId = request.Id, request.RequestNo, request.Status, request.IsUrgent, request.UrgentReason });
        var domainEvent = new DomainEventRecord
        {
            EventId = eventId, EventType = "Fleet.RequestSubmitted", Scope = "FLEET", AggregateType = "FleetRequest",
            AggregateId = request.Id, OccurredAt = now, ActorUserId = user.Id, CorrelationId = history.CorrelationId, Payload = payload
        };
        var destination = new LineGroupDestination
        {
            LineGroupId = "C12345678901234567890", Status = LineGroupDestinationStatuses.Active, Module = "FLEET", ConfirmedAt = now.AddMinutes(-1)
        };
        destination.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = "Fleet.RequestSubmitted", IsEnabled = true });
        db.AddRange(department, user, request, history, domainEvent,
            new OutboxMessage { EventId = eventId, EventType = domainEvent.EventType, Scope = domainEvent.Scope, Payload = payload },
            destination);
        await db.SaveChangesAsync();
        var service = Service(db, new SequenceClient(Success()));

        Assert.Equal(0, await service.ProjectMissingEventsAsync(CancellationToken.None));
        Assert.Equal(1, await service.DiscoverAsync(CancellationToken.None));
        Assert.Equal(0, await service.DiscoverAsync(CancellationToken.None));
        Assert.Single(await db.DomainEvents.Where(x => x.EventType == "Fleet.RequestSubmitted").ToListAsync());
        Assert.Single(await db.LineGroupDeliveryLogs.ToListAsync());
    }

    [Fact]
    public async Task Transient_failure_retries_then_marks_sent_without_duplicate_send_after_success()
    {
        await using var db = Database();
        await Seed(db, "FLEET", "Fleet.RequestSubmitted", true, true);
        var client = new SequenceClient(new LineGroupPushResult(false, true, false, "LINE_HTTP_429", "temporary"), Success());
        var service = Service(db, client);
        await service.DiscoverAsync(CancellationToken.None);

        await service.ProcessAsync(CancellationToken.None);
        var log = await db.LineGroupDeliveryLogs.SingleAsync();
        Assert.Equal("Retry", log.Status);
        log.AvailableAt = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        await service.ProcessAsync(CancellationToken.None);
        await service.ProcessAsync(CancellationToken.None);

        Assert.Equal("Sent", log.Status);
        Assert.Equal(2, log.AttemptCount);
        Assert.Equal(2, client.CallCount);
    }

    [Fact]
    public async Task Permanent_destination_failure_does_not_retry_and_disables_destination()
    {
        await using var db = Database();
        var fixture = await Seed(db, "FLEET", "Fleet.RequestSubmitted", true, true);
        var client = new SequenceClient(new LineGroupPushResult(false, false, true, "LINE_HTTP_404", "rejected"));
        var service = Service(db, client);
        await service.DiscoverAsync(CancellationToken.None);

        await service.ProcessAsync(CancellationToken.None);
        await service.ProcessAsync(CancellationToken.None);

        var log = await db.LineGroupDeliveryLogs.SingleAsync();
        Assert.Equal("Failed", log.Status);
        Assert.Equal(1, client.CallCount);
        Assert.Equal(LineGroupDestinationStatuses.Disabled, fixture.Destination.Status);
        Assert.True(fixture.Destination.AttentionRequired);
    }

    [Fact]
    public async Task Permanent_delivery_failures_keep_subscription_token_until_destination_is_disabled()
    {
        await using var db = Database();
        var fixture = await Seed(db, "FLEET", "Fleet.RequestSubmitted", true, true);
        var originalToken = fixture.Destination.ConcurrencyToken;
        var client = new SequenceClient(new LineGroupPushResult(false, false, false, "LINE_HTTP_400", "rejected"));
        var service = Service(db, client);
        await service.DiscoverAsync(CancellationToken.None);
        await service.ProcessAsync(CancellationToken.None);

        Assert.Equal(originalToken, fixture.Destination.ConcurrencyToken);
        Assert.True(fixture.Destination.AttentionRequired);
        Assert.Equal("Failed", (await db.LineGroupDeliveryLogs.SingleAsync()).Status);

        var nextEvent = new DomainEventRecord
        {
            EventId = Guid.NewGuid(), EventType = "Fleet.RequestSubmitted", Scope = "FLEET",
            AggregateType = "FleetRequest", AggregateId = Guid.NewGuid(), OccurredAt = DateTime.UtcNow,
            CorrelationId = "second-failure", Payload = "{}"
        };
        db.DomainEvents.Add(nextEvent);
        db.LineGroupDeliveryLogs.Add(new LineGroupDeliveryLog
        {
            EventId = nextEvent.EventId, DestinationId = fixture.Destination.Id,
            CanonicalEventType = "Fleet.RequestSubmitted", SourceEventType = "Fleet.RequestSubmitted",
            RequestId = nextEvent.AggregateId, DeduplicationKey = $"second-failure:{nextEvent.EventId}",
            CorrelationId = nextEvent.CorrelationId, AvailableAt = DateTime.UtcNow.AddSeconds(-1)
        });
        await db.SaveChangesAsync();
        await service.ProcessAsync(CancellationToken.None);
        Assert.Equal(originalToken, fixture.Destination.ConcurrencyToken);
        Assert.Equal(2, client.CallCount);
    }

    [Fact]
    public async Task Group_failure_does_not_change_user_delivery_or_domain_event_outbox()
    {
        await using var db = Database();
        var fixture = await Seed(db, "FLEET", "Fleet.RequestSubmitted", true, true, withUserDelivery: true);
        var service = Service(db, new SequenceClient(new LineGroupPushResult(false, false, true, "LINE_HTTP_410", "gone")));
        await service.DiscoverAsync(CancellationToken.None);
        await service.ProcessAsync(CancellationToken.None);

        var userDelivery = await db.NotificationDeliveries.SingleAsync();
        Assert.Equal("PENDING", userDelivery.Status);
        Assert.Equal("LINE", userDelivery.Channel);
        Assert.True(await db.DomainEvents.AnyAsync(x => x.EventId == fixture.Event.EventId));
        Assert.True(await db.OutboxMessages.AnyAsync(x => x.EventId == fixture.Event.EventId));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true, false)]
    [InlineData(HttpStatusCode.InternalServerError, true, false)]
    [InlineData(HttpStatusCode.NotFound, false, true)]
    [InlineData(HttpStatusCode.BadRequest, false, false)]
    public async Task Low_level_client_classifies_failures(HttpStatusCode status, bool transient, bool unavailable)
    {
        var resolver = LineResolver();
        var client = new LineGroupPushClient(
            resolver,
            new HttpClient(new StatusHandler(status)),
            new EphemeralDataProtectionProvider(),
            NullLogger<LineGroupPushClient>.Instance);

        var result = await client.PushTextAsync("C12345678901234567890", "ทดสอบ", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(transient, result.IsTransient);
        Assert.Equal(unavailable, result.DestinationUnavailable);
        Assert.DoesNotContain("C12345678901234567890", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task Custom_endpoint_sends_only_line_messages_payload()
    {
        var protection = new EphemeralDataProtectionProvider();
        var protector = protection.CreateProtector("HOP.LineGroupDestinationCredentials.v1");
        var handler = new CaptureHandler();
        var client = new LineGroupPushClient(
            LineResolver(),
            new HttpClient(handler),
            protection,
            NullLogger<LineGroupPushClient>.Instance);
        var destination = new LineGroupDestination
        {
            DeliveryProvider = "CUSTOM_ENDPOINT",
            EndpointUrl = "https://notify.test/send",
            ClientId = "client-id",
            ClientSecretProtected = protector.Protect("client-secret"),
            LineGroupId = "group-id"
        };

        var result = await client.PushTextAsync(destination, "ข้อความทดสอบ", CancellationToken.None);

        Assert.True(result.Success);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal(["messages"], payload.RootElement.EnumerateObject().Select(x => x.Name).ToArray());
        var message = payload.RootElement.GetProperty("messages")[0];
        Assert.Equal("text", message.GetProperty("type").GetString());
        Assert.Equal("ข้อความทดสอบ", message.GetProperty("text").GetString());
        Assert.Equal("client-id", handler.ClientId);
        Assert.Equal("client-secret", handler.Secret);
        Assert.Null(handler.Authorization);
    }

    [Fact]
    public async Task Custom_endpoint_sends_flex_message_with_alt_text_and_contents()
    {
        var protection = new EphemeralDataProtectionProvider();
        var protector = protection.CreateProtector("HOP.LineGroupDestinationCredentials.v1");
        var handler = new CaptureHandler();
        var client = new LineGroupPushClient(LineResolver(), new HttpClient(handler), protection, NullLogger<LineGroupPushClient>.Instance);
        var destination = new LineGroupDestination
        {
            DeliveryProvider = "CUSTOM_ENDPOINT", EndpointUrl = "https://notify.test/send", ClientId = "client-id",
            ClientSecretProtected = protector.Protect("client-secret"), LineGroupId = "group-id"
        };
        var rendered = new FleetGroupRenderedMessage(
            "flex", "ข้อความสำรอง", "Fleet.RequestSubmitted", Guid.NewGuid(),
            "{\"type\":\"bubble\",\"body\":{\"type\":\"box\",\"layout\":\"vertical\",\"contents\":[]}}",
            "มีคำขอใช้รถใหม่");

        var result = await client.PushMessageAsync(destination, rendered, CancellationToken.None);

        Assert.True(result.Success);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal(["messages"], payload.RootElement.EnumerateObject().Select(x => x.Name).ToArray());
        var message = payload.RootElement.GetProperty("messages")[0];
        Assert.Equal("flex", message.GetProperty("type").GetString());
        Assert.Equal("มีคำขอใช้รถใหม่", message.GetProperty("altText").GetString());
        Assert.Equal("bubble", message.GetProperty("contents").GetProperty("type").GetString());
        Assert.Equal("client-id", handler.ClientId);
        Assert.Equal("client-secret", handler.Secret);
    }

    [Fact]
    public async Task Custom_endpoint_treats_http_200_with_unauthorized_body_as_failure()
    {
        var protection = new EphemeralDataProtectionProvider();
        var protector = protection.CreateProtector("HOP.LineGroupDestinationCredentials.v1");
        var client = new LineGroupPushClient(
            LineResolver(),
            new HttpClient(new CaptureHandler("{\"status\":401,\"message\":\"Unauthorized\"}")),
            protection,
            NullLogger<LineGroupPushClient>.Instance);
        var destination = new LineGroupDestination
        {
            DeliveryProvider = "CUSTOM_ENDPOINT", EndpointUrl = "https://notify.test/send", ClientId = "client-id",
            ClientSecretProtected = protector.Protect("client-secret"), LineGroupId = "group-id"
        };

        var result = await client.PushTextAsync(destination, "ข้อความทดสอบ", CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(result.IsTransient);
        Assert.Equal("CUSTOM_RESPONSE_401", result.ErrorCode);
        Assert.DoesNotContain("Unauthorized", result.ErrorMessage ?? string.Empty);
    }

    private static FleetLineGroupDeliveryService Service(AppDbContext db, ILineGroupPushClient client, bool enabled = true,
        bool tripOverdueEnabled = false, DateTimeOffset? tripOverdueEnabledFromUtc = null) => new(
        db, new FleetLineGroupEventMapper(), new FleetEffectivePermissionService(db), new StubTemplate(), client,
        Options.Create(new LineGroupNotificationsOptions { Enabled = enabled, BatchSize = 20, MaxAttempts = 3,
            TripOverdueEnabled = tripOverdueEnabled, TripOverdueEnabledFromUtc = tripOverdueEnabledFromUtc }),
        NullLogger<FleetLineGroupDeliveryService>.Instance);

    private static async Task<Fixture> Seed(AppDbContext db, string scope, string eventType, bool active, bool subscribed, bool withUserDelivery = false)
    {
        var requestId = Guid.NewGuid(); var eventId = Guid.NewGuid(); var now = DateTime.UtcNow;
        var destination = new LineGroupDestination
        {
            LineGroupId = "C12345678901234567890", DisplayName = "Fleet Group",
            Status = active ? LineGroupDestinationStatuses.Active : LineGroupDestinationStatuses.Pending,
            Module = "FLEET", ConfirmedAt = now.AddMinutes(-5)
        };
        destination.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = "Fleet.RequestSubmitted", IsEnabled = subscribed });
        var domainEvent = new DomainEventRecord
        {
            EventId = eventId, EventType = eventType, Scope = scope, AggregateType = "FleetRequest", AggregateId = requestId,
            OccurredAt = now, CorrelationId = "group-m3-test", Payload = "{}"
        };
        var outbox = new OutboxMessage { EventId = eventId, EventType = eventType, Scope = scope, Payload = "{}" };
        if (withUserDelivery)
        {
            var user = new User { Id = Guid.NewGuid(), Username = "user", FullName = "User", PasswordHash = "hash" };
            db.Users.Add(user);
            outbox.Deliveries.Add(new NotificationDelivery { RecipientUserId = user.Id, Channel = "LINE", Status = "PENDING", IdempotencyKey = $"{eventId}:{user.Id}:LINE" });
        }
        db.AddRange(destination, domainEvent, outbox);
        await db.SaveChangesAsync();
        return new Fixture(destination, domainEvent);
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"fleet-line-group-delivery-{Guid.NewGuid()}").Options);
    private static LineGroupPushResult Success() => new(true, false, false, null, null);
    private static LineConfigurationResolver LineResolver()
    {
        var values = new Dictionary<string, string?> { ["Line:Enabled"] = "true", ["Line:AccessToken"] = "token", ["Line:Endpoint"] = "https://line.test/push" };
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new LineConfigurationResolver(Options.Create(new LineOptions { Enabled = true, AccessToken = "token", Endpoint = "https://line.test/push" }), configuration);
    }

    private sealed record Fixture(LineGroupDestination Destination, DomainEventRecord Event);
    private sealed class StubTemplate : IFleetLineGroupMessageTemplateService
    {
        public Task<FleetGroupRenderedMessage?> RenderAsync(DomainEventRecord domainEvent, string canonicalEventType, CancellationToken ct) =>
            Task.FromResult<FleetGroupRenderedMessage?>(new("text", "ข้อความทดสอบที่ไม่มีข้อมูลอ่อนไหว", canonicalEventType, domainEvent.AggregateId));
    }
    private sealed class SequenceClient(params LineGroupPushResult[] results) : ILineGroupPushClient
    {
        private int index;
        public int CallCount { get; private set; }
        public Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(results[Math.Min(index++, results.Length - 1)]);
        }
    }
    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("{\"message\":\"sensitive remote response\"}") });
    }
    private sealed class CaptureHandler(string responseBody = "") : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? ClientId { get; private set; }
        public string? Secret { get; private set; }
        public string? Authorization { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            ClientId = request.Headers.TryGetValues("client-key", out var clientIds) ? clientIds.Single() : null;
            Secret = request.Headers.TryGetValues("secret-key", out var secrets) ? secrets.Single() : null;
            Authorization = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody) };
        }
    }
}
