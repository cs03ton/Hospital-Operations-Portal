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

        var domainEvent = await db.DomainEvents.SingleAsync();
        Assert.Equal(history.Id, domainEvent.EventId);
        Assert.Empty((await db.OutboxMessages.Include(x => x.Deliveries).SingleAsync()).Deliveries);
        Assert.Single(db.LineGroupDeliveryLogs);
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

    private static FleetLineGroupDeliveryService Service(AppDbContext db, ILineGroupPushClient client, bool enabled = true) => new(
        db, new FleetLineGroupEventMapper(), new StubTemplate(), client,
        Options.Create(new LineGroupNotificationsOptions { Enabled = enabled, BatchSize = 20, MaxAttempts = 3 }),
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
