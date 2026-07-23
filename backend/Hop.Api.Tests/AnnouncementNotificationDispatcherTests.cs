using Hop.Api.Data;
using Hop.Api.Configuration;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace Hop.Api.Tests;

public class AnnouncementNotificationDispatcherTests
{
    [Fact]
    public async Task PreviewAsync_CountsInAppAndLineRecipients()
    {
        await using var db = CreateDbContext();
        var announcement = SeedAnnouncementWithUsers(db);
        var dispatcher = CreateDispatcher(db);

        var preview = await dispatcher.PreviewAsync(announcement);

        Assert.Equal(3, preview.TotalTargetUsers);
        Assert.Equal(2, preview.InAppRecipientCount);
        Assert.Equal(1, preview.LineBoundRecipientCount);
        Assert.Equal(1, preview.LineUnboundRecipientCount);
        Assert.Equal(1, preview.InactiveUserCount);
        Assert.Equal(3, preview.EstimatedQueueItems);
    }

    [Fact]
    public async Task DispatchAsync_CreatesBellAndLineQueueWithoutDuplicates()
    {
        await using var db = CreateDbContext();
        var announcement = SeedAnnouncementWithUsers(db);
        var dispatcher = CreateDispatcher(db);

        await dispatcher.DispatchAsync(announcement, null);
        await dispatcher.DispatchAsync(announcement, null);

        Assert.Equal(2, await db.Notifications.CountAsync());
        Assert.Single(await db.LineDeliveryLogs.ToListAsync());
        Assert.Equal(3, await db.AnnouncementNotificationDeliveries.CountAsync());
        Assert.Equal("Queued", announcement.NotificationDispatchStatus);

        var lineLog = await db.LineDeliveryLogs.SingleAsync();
        using var payload = JsonDocument.Parse(lineLog.Payload);
        var message = payload.RootElement.GetProperty("messages")[0];
        var contents = message.GetProperty("contents");
        Assert.Contains("\"type\":\"flex\"", lineLog.Payload);
        Assert.Equal("flex", message.GetProperty("type").GetString());
        Assert.Equal("bubble", contents.GetProperty("type").GetString());
        Assert.Contains("ประกาศสำคัญจาก HOP", message.GetProperty("altText").GetString());
        Assert.Contains("https://hop.example.org/announcements/", lineLog.Payload);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IAnnouncementNotificationDispatcher CreateDispatcher(AppDbContext db)
    {
        return new AnnouncementNotificationDispatcher(
            db,
            new AnnouncementAudienceResolver(db),
            new FakeLineMessagingService(db),
            CreateLineConfiguration(),
            new NoopAuditLogService(),
            NullLogger<AnnouncementNotificationDispatcher>.Instance);
    }

    private static LineConfigurationResolver CreateLineConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PUBLIC_APP_URL"] = "https://hop.example.org"
            })
            .Build();
        return new LineConfigurationResolver(Options.Create(new LineOptions()), configuration);
    }

    private static Announcement SeedAnnouncementWithUsers(AppDbContext db)
    {
        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), FullName = "LINE User", Username = "line_user", LineUserId = "U12345678901234567890", IsActive = true },
            new User { Id = Guid.NewGuid(), FullName = "Bell User", Username = "bell_user", IsActive = true },
            new User { Id = Guid.NewGuid(), FullName = "Inactive User", Username = "inactive_user", LineUserId = "U09876543210987654321", IsActive = false });

        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = "ประกาศทดสอบ",
            Summary = "สรุปประกาศ",
            Body = "เนื้อหาประกาศ",
            Status = AnnouncementStatuses.Published,
            Priority = AnnouncementPriorities.Important,
            CreatedByUserId = Guid.NewGuid(),
            NotifyInApp = true,
            NotifyViaLine = true,
            Targets = [new AnnouncementTarget { TargetType = AnnouncementTargetTypes.Everyone }]
        };
        db.Announcements.Add(announcement);
        db.SaveChanges();
        return announcement;
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLineMessagingService(AppDbContext db) : ILineMessagingService
    {
        public Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task NotifyUserAsync(Guid userId, string eventName, string message, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task<LineDeliveryLog> NotifyUserPayloadAsync(Guid userId, string eventName, string payload, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
        {
            var log = new LineDeliveryLog
            {
                RecipientUserId = userId,
                LeaveRequestId = leaveRequestId,
                EventName = eventName,
                Status = "Queued",
                Payload = payload,
                ResponseDetail = "Queued by fake LINE service.",
                AttemptCount = 0,
                NextRetryAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.LineDeliveryLogs.Add(log);
            await db.SaveChangesAsync(cancellationToken);
            return log;
        }

        public Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, string eventName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LineTestSendResponse> SendRawPayloadToLineUserAsync(string toUserId, string payload, string eventName, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LineConnectionValidationResponse> ValidateConnectionAsync(IReadOnlyList<LineChecklistItemResponse> checklist, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> RetryPendingDeliveriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }
}
