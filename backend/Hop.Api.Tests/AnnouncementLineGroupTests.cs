using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class AnnouncementLineGroupTests
{
    [Fact]
    public async Task Options_and_validation_allow_only_active_custom_central_groups_and_never_expose_credentials()
    {
        await using var db = Database();
        var valid = Group("Allowed");
        valid.EndpointUrl = "https://private.example"; valid.ClientId = "secret-client"; valid.ClientSecretProtected = "secret-value";
        db.AddRange(valid, Group("Disabled", status: "Disabled"), Group("Legacy", module: "REPAIR_IT"), Group("Native", provider: "LINE_MESSAGING_API"));
        await db.SaveChangesAsync();
        var service = new AnnouncementLineGroupService(db, Configuration());
        var option = Assert.Single(await service.OptionsAsync(default));
        Assert.Equal(valid.Id, option.Id);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(option));
        Assert.DoesNotContain("Endpoint", JsonSerializer.Serialize(option));
        Assert.Null(await service.ValidateAsync(true, [valid.Id, valid.Id], default));
        Assert.NotNull(await service.ValidateAsync(true, [], default));
        Assert.NotNull(await service.ValidateAsync(true, [Guid.NewGuid()], default));
        Assert.Null(await service.ValidateAsync(false, [], default));
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Scheduled")]
    public async Task Draft_and_scheduled_announcements_do_not_queue_until_published(string status)
    {
        await using var db = Database();
        var (item, groups) = await Seed(db, 1);
        item.Status = status; await db.SaveChangesAsync();
        var service = new AnnouncementLineGroupService(db, Configuration());
        Assert.Equal(0, await service.QueueAsync(item.Id, null, default));
        Assert.Empty(await db.LineGroupDeliveryLogs.ToListAsync());
        item.Status = AnnouncementStatuses.Published; await db.SaveChangesAsync();
        Assert.Equal(1, await service.QueueAsync(item.Id, null, default));
        Assert.Equal(groups[0].Id, (await db.LineGroupDeliveryLogs.SingleAsync()).DestinationId);
    }

    [Fact]
    public async Task Queues_only_selected_groups_once_and_freezes_content_for_retry_and_later_publication()
    {
        await using var db = Database();
        var (item, groups) = await Seed(db, 3);
        // Leave group 3 unselected, despite being active.
        db.AnnouncementLineGroupTargets.Remove(item.LineGroupTargets.Single(x => x.DestinationId == groups[2].Id));
        await db.SaveChangesAsync();
        var service = new AnnouncementLineGroupService(db, Configuration());
        Assert.Equal(2, await service.QueueAsync(item.Id, null, default));
        var firstSnapshot = await db.DomainEvents.SingleAsync();
        var firstPayload = firstSnapshot.Payload;
        item.Title = "Edited after publication"; await db.SaveChangesAsync();
        Assert.Equal(0, await service.QueueAsync(item.Id, null, default));
        Assert.Equal(firstPayload, (await db.DomainEvents.SingleAsync()).Payload);
        db.AnnouncementLineGroupTargets.Add(new AnnouncementLineGroupTarget { AnnouncementId = item.Id, DestinationId = groups[2].Id });
        await db.SaveChangesAsync();
        Assert.Equal(1, await service.QueueAsync(item.Id, null, default));
        Assert.Equal(3, await db.LineGroupDeliveryLogs.CountAsync());
        Assert.Equal(2, await db.DomainEvents.CountAsync());
        Assert.Empty(await db.LineGroupEventSubscriptions.ToListAsync());
        var template = new FleetLineGroupMessageTemplateService(db, Configuration());
        var rendered = await template.RenderAsync(firstSnapshot, AnnouncementLineGroupService.EventType, default);
        Assert.NotNull(rendered?.FlexContentsJson);
        Assert.Contains("Original announcement", rendered!.AltText);
        Assert.DoesNotContain("Edited after publication", rendered.Text);
        Assert.Null(new FleetLineGroupEventMapper().ToCanonical("ANNOUNCEMENT", AnnouncementLineGroupService.EventType));
    }

    [Theory]
    [InlineData("https://hop.example.org", true)]
    [InlineData("", false)]
    [InlineData("/relative", false)]
    public async Task Group_flex_has_a_link_only_when_a_valid_public_web_URL_exists(string url, bool hasLink)
    {
        await using var db = Database();
        var (item, _) = await Seed(db, 1);
        await new AnnouncementLineGroupService(db, Configuration(url)).QueueAsync(item.Id, null, default);
        var snapshot = await db.DomainEvents.SingleAsync();
        var rendered = await new FleetLineGroupMessageTemplateService(db, Configuration(url)).RenderAsync(snapshot, AnnouncementLineGroupService.EventType, default);
        using var contents = JsonDocument.Parse(rendered!.FlexContentsJson!);
        Assert.Equal("bubble", contents.RootElement.GetProperty("type").GetString());
        Assert.Equal(hasLink, contents.RootElement.TryGetProperty("footer", out _));
        if (hasLink) Assert.Contains($"{url}/announcements/{item.Id}", rendered.FlexContentsJson);
    }

    [Fact]
    public async Task Group_disabled_after_selection_is_recorded_as_skipped_and_not_backfilled()
    {
        await using var db = Database();
        var (item, groups) = await Seed(db, 1);
        groups[0].Status = "Disabled"; await db.SaveChangesAsync();
        var service = new AnnouncementLineGroupService(db, Configuration());
        Assert.Equal(0, await service.QueueAsync(item.Id, null, default));
        Assert.Equal("Skipped", (await db.LineGroupDeliveryLogs.SingleAsync()).Status);
        groups[0].Status = "Active"; await db.SaveChangesAsync();
        Assert.Equal(0, await service.QueueAsync(item.Id, null, default));
    }

    [Theory]
    [InlineData("Cancelled", false)]
    [InlineData("Draft", false)]
    [InlineData("Archived", false)]
    [InlineData("Published", true)]
    public async Task Worker_skips_removed_or_expired_publication_before_calling_LINE(string status, bool expired)
    {
        await using var db = Database();
        var (item, _) = await Seed(db, 1);
        await new AnnouncementLineGroupService(db, Configuration()).QueueAsync(item.Id, null, default);
        item.Status = status; if (expired) item.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var client = new CaptureClient();
        await Worker(db, client).ProcessAsync(default);
        Assert.Empty(client.Messages);
        Assert.Equal("Skipped", (await db.LineGroupDeliveryLogs.SingleAsync()).Status);
    }

    [Fact]
    public async Task Worker_retries_a_group_with_the_same_flex_snapshot_and_records_delivery_result()
    {
        await using var db = Database();
        var (item, _) = await Seed(db, 1);
        await new AnnouncementLineGroupService(db, Configuration()).QueueAsync(item.Id, null, default);
        var client = new CaptureClient(failFirst: true);
        var worker = Worker(db, client);
        await worker.ProcessAsync(default);
        var log = await db.LineGroupDeliveryLogs.SingleAsync();
        Assert.Equal("Retry", log.Status);
        item.Title = "Changed while retrying";
        log.AvailableAt = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        await worker.ProcessAsync(default);
        Assert.Equal("Sent", log.Status);
        Assert.Equal(2, log.AttemptCount);
        Assert.Equal(client.Messages[0].FlexContentsJson, client.Messages[1].FlexContentsJson);
        Assert.DoesNotContain("Changed while retrying", client.Messages[1].Text);
    }

    [Fact]
    public void Group_endpoints_require_the_existing_announcement_permissions_and_public_details_omit_destinations()
    {
        Assert.Contains(AnnouncementPermissions.NotificationSendLine, typeof(AdminAnnouncementsController)
            .GetMethod(nameof(AdminAnnouncementsController.LineGroupOptions))!.GetCustomAttribute<RequireAnyPermissionAttribute>()!.PermissionCodes);
        Assert.Contains(AnnouncementPermissions.NotificationViewDelivery, typeof(AdminAnnouncementsController)
            .GetMethod(nameof(AdminAnnouncementsController.LineGroupDeliveries))!.GetCustomAttribute<RequireAnyPermissionAttribute>()!.PermissionCodes);
        var announcement = new Announcement { NotifyViaLineGroup = true, LineGroupTargets = [new AnnouncementLineGroupTarget { DestinationId = Guid.NewGuid() }] };
        var publicDetail = AnnouncementsController.ToDetail(announcement, Guid.NewGuid());
        Assert.Null(publicDetail.LineGroupIds);
        Assert.Null(publicDetail.LineGroups);
    }

    [Fact]
    public async Task Create_checks_send_permission_and_preserves_selected_group_IDs_without_sending_a_draft()
    {
        await using var db = Database();
        var group = Group("Selected"); db.Add(group);
        var actor = new User { Username = "editor", UserRoles = [new UserRole { Role = new Role { Name = "Editor", IsActive = true,
            RolePermissions = [new RolePermission { Permission = new Permission { Code = AnnouncementPermissions.NotificationSendLine, Name = "Send LINE", IsActive = true } }] } }] };
        db.Add(actor); await db.SaveChangesAsync();
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString())], "test")) };
        var controller = new AdminAnnouncementsController(db, new AuditStub(), null!, null!, new AnnouncementLineGroupService(db, Configuration()))
        { ControllerContext = new ControllerContext { HttpContext = http }, Url = new UrlHelper(new ActionContext(http, new RouteData(), new ActionDescriptor())) };
        var request = new CreateAnnouncementRequest("Title", "Summary", "Body", "Normal", null, null, null, false, false, false, false, null, false, false, [], true, [group.Id]);
        var created = await controller.Create(request, default);
        Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal(group.Id, (await db.AnnouncementLineGroupTargets.SingleAsync()).DestinationId);
        Assert.Empty(await db.LineGroupDeliveryLogs.ToListAsync());
        actor.UserRoles.Clear(); await db.SaveChangesAsync();
        var denied = await controller.Create(request, default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(denied.Result).StatusCode);
    }

    internal static LineConfigurationResolver Configuration(string url = "https://hop.example.org") => new(
        Options.Create(new LineOptions()), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PUBLIC_APP_URL"] = url }).Build());
    internal static LineGroupDestination Group(string name, string status = "Active", string provider = "CUSTOM_ENDPOINT", string module = "CENTRAL") =>
        new() { DisplayName = name, LineGroupId = $"C-{Guid.NewGuid()}", Status = status, DeliveryProvider = provider, Module = module };
    internal static async Task<(Announcement Item, LineGroupDestination[] Groups)> Seed(AppDbContext db, int count)
    {
        var creator = new User { Username = $"creator-{Guid.NewGuid()}" }; db.Add(creator); await db.SaveChangesAsync();
        var groups = Enumerable.Range(0, count).Select(x => Group($"Group {x}")).ToArray();
        var item = new Announcement { Title = "Original announcement", Summary = "Summary", Body = "Body", Status = "Published",
            CreatedByUserId = creator.Id, NotifyInApp = false, NotifyViaLineGroup = true,
            LineGroupTargets = groups.Select(group => new AnnouncementLineGroupTarget { Destination = group }).ToList() };
        db.Add(item); await db.SaveChangesAsync(); return (item, groups);
    }
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase($"announcement-groups-{Guid.NewGuid()}").Options);
    private static FleetLineGroupDeliveryService Worker(AppDbContext db, ILineGroupPushClient client) => new(db,
        new FleetLineGroupEventMapper(), new FleetEffectivePermissionService(db), new FleetLineGroupMessageTemplateService(db, Configuration()), client,
        Options.Create(new LineGroupNotificationsOptions { Enabled = true, BatchSize = 20, MaxAttempts = 3 }), NullLogger<FleetLineGroupDeliveryService>.Instance);
    private sealed class CaptureClient(bool failFirst = false) : ILineGroupPushClient
    {
        public List<FleetGroupRenderedMessage> Messages { get; } = [];
        public Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct) => throw new InvalidOperationException();
        public Task<LineGroupPushResult> PushMessageAsync(LineGroupDestination destination, FleetGroupRenderedMessage message, CancellationToken ct)
        {
            Messages.Add(message);
            return Task.FromResult(failFirst && Messages.Count == 1 ? new LineGroupPushResult(false, true, false, "CUSTOM_HTTP_503", "temporary") : new LineGroupPushResult(true, false, false, null, null));
        }
    }
    private sealed class AuditStub : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null) => Task.CompletedTask;
    }
}
