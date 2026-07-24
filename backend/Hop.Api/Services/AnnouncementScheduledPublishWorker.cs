using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class AnnouncementScheduledPublishWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AnnouncementScheduledPublishWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishDueAnnouncementsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Scheduled announcement publish worker failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Scheduled announcement publish worker is stopping.");
        }
    }

    private async Task PublishDueAnnouncementsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IAnnouncementNotificationDispatcher>();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var now = DateTime.UtcNow;

        var dueItems = await db.Announcements
            .AsSplitQuery()
            .Include(item => item.Targets)
            .Include(item => item.NotificationDeliveries)
            .Where(item => item.Status == AnnouncementStatuses.Scheduled)
            .Where(item => item.PublishAt != null && item.PublishAt <= now)
            .OrderBy(item => item.PublishAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var item in dueItems)
        {
            item.Status = AnnouncementStatuses.Published;
            item.PublishedAt ??= now;
            item.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(null, "Announcement.ScheduledPublished", "Announcement", item.Id.ToString(), item.Title, "Success");
            await dispatcher.DispatchAsync(item, null, cancellationToken);
        }
    }
}
