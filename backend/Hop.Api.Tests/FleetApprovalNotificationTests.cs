using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetApprovalNotificationTests
{
    [Theory]
    [InlineData("Sent", "PROCESSED")]
    [InlineData("Failed", "FAILED")]
    [InlineData("Disabled", "FAILED")]
    public async Task Approval_line_uses_flex_and_records_actual_delivery_result(string lineStatus, string expected)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var line = DispatchProxy.Create<ILineMessagingService, LineProxy>();
        var proxy = (LineProxy)(object)line;
        proxy.Status = lineStatus;
        var services = new ServiceCollection();
        services.AddScoped(_ => new AppDbContext(options));
        services.AddSingleton(line);
        services.AddSingleton(new FleetNotificationTemplateService());
        services.AddSingleton(new LineConfigurationResolver(Options.Create(new LineOptions()), new ConfigurationBuilder().Build()));
        services.AddSingleton<IFleetLineGroupMessageTemplateService, ApprovalTemplate>();
        using var provider = services.BuildServiceProvider();
        await using var db = new AppDbContext(options);
        var request = new FleetRequest { Id = Guid.NewGuid(), RequestNo = "VH-URGENT", IsUrgent = true };
        var message = new OutboxMessage { EventId = Guid.NewGuid(), EventType = "Fleet.Assigned", Scope = "FLEET", Payload = JsonSerializer.Serialize(new { FleetRequestId = request.Id }) };
        message.Deliveries.Add(new NotificationDelivery { RecipientUserId = Guid.NewGuid(), Channel = "LINE", IdempotencyKey = "test" });
        db.AddRange(request, message);
        await db.SaveChangesAsync();
        var processor = new OutboxProcessor(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance);
        var process = typeof(OutboxProcessor).GetMethod("ProcessBatch", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)process.Invoke(processor, [CancellationToken.None])!;
        await (Task)process.Invoke(processor, [CancellationToken.None])!;
        db.ChangeTracker.Clear();
        var delivery = await db.NotificationDeliveries.SingleAsync();
        Assert.Equal(expected, delivery.Status);
        Assert.Equal(1, proxy.Calls);
        using var payload = JsonDocument.Parse(proxy.Payload!);
        Assert.Equal("flex", payload.RootElement.GetProperty("messages")[0].GetProperty("type").GetString());
        if (expected == "FAILED") Assert.Null(delivery.ProcessedAt);
    }

    public class LineProxy : DispatchProxy
    {
        public string Status = "Sent";
        public string? Payload;
        public int Calls;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Assert.Equal(nameof(ILineMessagingService.NotifyUserPayloadAsync), method!.Name);
            Calls++;
            Payload = (string)args![2]!;
            return Task.FromResult(new LineDeliveryLog { Id = Guid.NewGuid(), Status = Status });
        }
    }

    private sealed class ApprovalTemplate : IFleetLineGroupMessageTemplateService
    {
        public Task<FleetGroupRenderedMessage?> RenderAsync(DomainEventRecord domainEvent, string canonicalEventType, CancellationToken ct) =>
            Task.FromResult<FleetGroupRenderedMessage?>(new("flex", "คำขอเร่งด่วนรออนุมัติ", canonicalEventType, domainEvent.AggregateId, "{\"type\":\"bubble\"}", "คำขอรออนุมัติ"));
    }

    [Theory]
    [InlineData(false, "Fleet.Assigned", true)]
    [InlineData(true, "Fleet.Assigned", true)]
    [InlineData(true, "Fleet.VehicleAssigned", true)]
    [InlineData(true, "Fleet.AssignmentCreated", true)]
    [InlineData(false, "Fleet.AdminReviewApproved", false)]
    [InlineData(true, "Fleet.AdminReviewApproved", false)]
    [InlineData(true, "Fleet.AdminReviewed", false)]
    public async Task Approval_recipients_follow_stage_for_normal_and_urgent_requests(bool urgent, string eventType, bool adminStage)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var admin = Approver("admin", FleetPermissions.AdminReviewApprove);
        var director = Approver("director", FleetPermissions.DirectorApprove);
        var request = new FleetRequest { Id = Guid.NewGuid(), RequestNo = "VH-TEST", RequesterUserId = Guid.NewGuid(), IsUrgent = urgent, UrgentReason = urgent ? "ภารกิจเร่งด่วน" : null };
        db.AddRange(admin, director, request);
        await db.SaveChangesAsync();
        var publisher = new DomainEventPublisher(db, new FleetNotificationRecipientResolver(db));
        await publisher.PublishAsync(new(eventType, "FLEET", "FleetRequest", request.Id, null, "test", new { FleetRequestId = request.Id }, []), default);
        await db.SaveChangesAsync();
        var outbox = await db.OutboxMessages.Include(x => x.Deliveries).SingleAsync();
        Assert.Equal(2, outbox.Deliveries.Count);
        Assert.All(outbox.Deliveries, x => Assert.Equal(adminStage ? admin.Id : director.Id, x.RecipientUserId));
        Assert.Contains(outbox.Deliveries, x => x.Channel == "LINE");
        Assert.Contains(outbox.Deliveries, x => x.Channel == "IN_APP");
    }

    private static User Approver(string name, string permission) => new()
    {
        Id = Guid.NewGuid(), Username = name, FullName = name, IsActive = true,
        UserRoles = [new UserRole { Role = new Role { Name = name, IsActive = true,
            RolePermissions = [new RolePermission { Permission = new Permission { Code = permission, Name = permission, IsActive = true } }] } }]
    };
}
