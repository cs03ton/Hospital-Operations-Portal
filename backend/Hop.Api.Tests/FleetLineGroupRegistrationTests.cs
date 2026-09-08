using System.Text.Json;
using System.Net;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetLineGroupRegistrationTests
{
    [Fact]
    public async Task Registration_command_creates_pending_destination_with_default_subscriptions()
    {
        await using var db = Database();
        var service = Service(db);
        using var json = JsonDocument.Parse(Event("message", "evt-register", "ลงทะเบียนกลุ่ม HOP"));

        await service.EnqueueAsync(json.RootElement, CancellationToken.None);
        await service.ProcessPendingAsync(CancellationToken.None);

        var destination = await db.LineGroupDestinations.Include(x => x.EventSubscriptions).SingleAsync();
        Assert.Equal(LineGroupDestinationStatuses.Pending, destination.Status);
        Assert.Equal("FLEET", destination.Module);
        Assert.Equal(FleetLineGroupEvents.Defaults.Count, destination.EventSubscriptions.Count);
        Assert.True(destination.EventSubscriptions.Single(x => x.EventType == "Fleet.RequestSubmitted").IsEnabled);
        Assert.True(destination.EventSubscriptions.Single(x => x.EventType == "Fleet.AdminReviewed").IsEnabled);
        Assert.True(destination.EventSubscriptions.Single(x => x.EventType == "Fleet.DriverAcknowledged").IsEnabled);
    }

    [Fact]
    public async Task Duplicate_webhook_event_is_enqueued_once()
    {
        await using var db = Database();
        var service = Service(db);
        using var json = JsonDocument.Parse(Event("message", "evt-duplicate", "ลงทะเบียนกลุ่ม HOP"));

        await service.EnqueueAsync(json.RootElement, CancellationToken.None);
        await service.EnqueueAsync(json.RootElement, CancellationToken.None);

        Assert.Equal(1, await db.LineWebhookInbox.CountAsync());
    }

    [Fact]
    public async Task Feature_flag_off_does_not_enqueue_or_register_group()
    {
        await using var db = Database();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var resolver = new LineConfigurationResolver(Options.Create(new LineOptions()), configuration);
        var service = new LineGroupRegistrationService(db, resolver, Options.Create(new LineGroupNotificationsOptions { Enabled = false }),
            new HttpClient(), NullLogger<LineGroupRegistrationService>.Instance);
        using var json = JsonDocument.Parse(Event("message", "evt-disabled", "ลงทะเบียนกลุ่ม HOP"));

        await service.EnqueueAsync(json.RootElement, CancellationToken.None);
        var processed = await service.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(0, processed);
        Assert.Empty(db.LineWebhookInbox);
        Assert.Empty(db.LineGroupDestinations);
    }

    [Fact]
    public async Task Legacy_leave_user_delivery_remains_sent_and_creates_no_group_state()
    {
        await using var db = Database();
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Username = "leave-line-user", FullName = "Leave User", PasswordHash = "hash", LineUserId = "U12345678901234567890" });
        await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Line:Enabled"] = "true", ["Line:AccessToken"] = "test-token", ["Line:Endpoint"] = "https://api.line.test/push"
        }).Build();
        var resolver = new LineConfigurationResolver(Options.Create(new LineOptions { Enabled = true, AccessToken = "test-token", Endpoint = "https://api.line.test/push" }), configuration);
        var legacy = new LineMessagingService(db, configuration, resolver, new HttpClient(new SuccessLineHandler()), NullLogger<LineMessagingService>.Instance);

        await legacy.NotifyLeaveRequestAsync(new Hop.Api.DTOs.LeaveNotificationMessage(
            Guid.NewGuid(), userId, "Leave User", "ลาป่วย", "Approved", new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 5), null));

        Assert.Equal("Sent", (await db.LineDeliveryLogs.SingleAsync()).Status);
        Assert.Empty(db.LineWebhookInbox);
        Assert.Empty(db.LineGroupDestinations);
    }

    [Fact]
    public async Task Fleet_admin_can_confirm_pending_group_and_audit_is_written()
    {
        await using var db = Database();
        var destination = new LineGroupDestination { LineGroupId = "C12345678901234567890123456789012" };
        db.LineGroupDestinations.Add(destination);
        await db.SaveChangesAsync();
        var controller = Controller(db);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var response = await controller.Confirm(destination.Id, new LineGroupStateRequest(destination.ConcurrencyToken, "ยืนยันกลุ่มงานยานพาหนะ"), CancellationToken.None);

        Assert.NotNull(response.Value);
        Assert.Equal(LineGroupDestinationStatuses.Active, destination.Status);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.Action == "Fleet.LineGroupConfirmed"));
    }

    [Fact]
    public async Task Fleet_admin_test_send_records_group_delivery_log()
    {
        await using var db = Database();
        var destination = new LineGroupDestination { LineGroupId = "C12345678901234567890123456789012", Status = LineGroupDestinationStatuses.Pending };
        db.LineGroupDestinations.Add(destination);
        await db.SaveChangesAsync();
        var controller = Controller(db);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var response = await controller.Test(destination.Id, new LineGroupTestRequest("ทดสอบกลุ่ม Fleet"), CancellationToken.None);

        Assert.NotNull(response.Value);
        var log = await db.LineGroupDeliveryLogs.SingleAsync();
        Assert.Equal("Sent", log.Status);
        Assert.Equal("Fleet.LineGroupTest", log.CanonicalEventType);
        Assert.NotNull(log.SentAt);
    }

    [Fact]
    public async Task Test_send_is_blocked_when_group_feature_flag_is_off()
    {
        await using var db = Database();
        var destination = new LineGroupDestination { LineGroupId = "C12345678901234567890123456789012", Status = LineGroupDestinationStatuses.Active };
        db.LineGroupDestinations.Add(destination);
        await db.SaveChangesAsync();
        var controller = new FleetLineGroupsController(db, new SuccessfulGroupClient(), Options.Create(new LineGroupNotificationsOptions { Enabled = false }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.Test(destination.Id, new LineGroupTestRequest(null), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response.Result);
        Assert.Empty(db.LineGroupDeliveryLogs);
    }

    [Fact]
    public async Task Test_send_rejects_sensitive_message()
    {
        await using var db = Database();
        var destination = new LineGroupDestination { LineGroupId = "C12345678901234567890123456789012", Status = LineGroupDestinationStatuses.Active };
        db.LineGroupDestinations.Add(destination);
        await db.SaveChangesAsync();
        var controller = Controller(db);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var response = await controller.Test(destination.Id, new LineGroupTestRequest("ข้อมูลผู้ป่วย HN 123456"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Empty(db.LineGroupDeliveryLogs);
    }

    [Fact]
    public async Task Leave_event_marks_destination_disabled_and_attention_required()
    {
        await using var db = Database();
        var destination = new LineGroupDestination { LineGroupId = "C12345678901234567890123456789012", Status = LineGroupDestinationStatuses.Active };
        db.LineGroupDestinations.Add(destination);
        await db.SaveChangesAsync();
        var service = Service(db);
        using var json = JsonDocument.Parse(Event("leave", "evt-leave"));

        await service.EnqueueAsync(json.RootElement, CancellationToken.None);
        await service.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(LineGroupDestinationStatuses.Disabled, destination.Status);
        Assert.True(destination.AttentionRequired);
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"fleet-line-group-{Guid.NewGuid()}").Options);

    private static FleetLineGroupsController Controller(AppDbContext db) => new(
        db, new SuccessfulGroupClient(), Options.Create(new LineGroupNotificationsOptions { Enabled = true }));

    private static LineGroupRegistrationService Service(AppDbContext db)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var resolver = new LineConfigurationResolver(Options.Create(new LineOptions()), configuration);
        return new LineGroupRegistrationService(db, resolver, Options.Create(new LineGroupNotificationsOptions { Enabled = true }),
            new HttpClient(), NullLogger<LineGroupRegistrationService>.Instance);
    }

    private static string Event(string type, string eventId, string? text = null) => JsonSerializer.Serialize(new
    {
        type,
        webhookEventId = eventId,
        source = new { type = "group", groupId = "C12345678901234567890123456789012" },
        message = text is null ? null : new { type = "text", text }
    });

    private sealed class SuccessLineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }

    private sealed class SuccessfulGroupClient : ILineGroupPushClient
    {
        public Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct) =>
            Task.FromResult(new LineGroupPushResult(true, false, false, null, null));
    }
}
