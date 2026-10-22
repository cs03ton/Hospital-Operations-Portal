using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class AnnouncementLineGroupService(AppDbContext db, LineConfigurationResolver lineConfiguration)
{
    public const string EventType = "Announcement.PublishedToGroup";

    public Task<List<AnnouncementLineGroupOptionResponse>> OptionsAsync(CancellationToken ct) => db.LineGroupDestinations.AsNoTracking()
        .Where(x => x.Status == LineGroupDestinationStatuses.Active && x.DeliveryProvider == "CUSTOM_ENDPOINT" && !x.Module.StartsWith("REPAIR_"))
        .OrderBy(x => x.DisplayName).Select(x => new AnnouncementLineGroupOptionResponse(x.Id, x.DisplayName, x.Status)).ToListAsync(ct);

    public async Task<string?> ValidateAsync(bool enabled, IReadOnlyList<Guid>? ids, CancellationToken ct)
    {
        if (!enabled) return null;
        var distinct = ids?.Distinct().ToArray() ?? [];
        if (distinct.Length == 0) return "กรุณาเลือกกลุ่ม LINE หมอพร้อมอย่างน้อยหนึ่งกลุ่ม";
        var valid = await db.LineGroupDestinations.CountAsync(x => distinct.Contains(x.Id) &&
            x.Status == LineGroupDestinationStatuses.Active && x.DeliveryProvider == "CUSTOM_ENDPOINT" && !x.Module.StartsWith("REPAIR_"), ct);
        return valid == distinct.Length ? null : "บางกลุ่ม LINE ปิดใช้งานหรือไม่ใช่กลุ่มหมอพร้อมส่วนกลาง กรุณาตรวจสอบกลุ่มที่เลือก";
    }

    public Task<List<AnnouncementLineGroupOptionResponse>> PreviewAsync(Guid announcementId, CancellationToken ct) =>
        db.AnnouncementLineGroupTargets.AsNoTracking().Where(x => x.AnnouncementId == announcementId)
            .OrderBy(x => x.Destination!.DisplayName)
            .Select(x => new AnnouncementLineGroupOptionResponse(x.DestinationId, x.Destination!.DisplayName,
                x.Destination.DeliveryProvider == "CUSTOM_ENDPOINT" && !x.Destination.Module.StartsWith("REPAIR_")
                    ? x.Destination.Status : LineGroupDestinationStatuses.Disabled)).ToListAsync(ct);

    public Task<List<AnnouncementLineGroupDeliveryResponse>> DeliveriesAsync(Guid announcementId, CancellationToken ct) =>
        db.LineGroupDeliveryLogs.AsNoTracking().Where(x => x.RequestId == announcementId && x.CanonicalEventType == EventType)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AnnouncementLineGroupDeliveryResponse(x.Id, x.DestinationId, x.Destination!.DisplayName,
                x.Status, x.AttemptCount, x.ErrorCode, x.ErrorMessage, x.CreatedAt, x.SentAt, x.FailedAt)).ToListAsync(ct);

    public Task<int> QueueCountAsync(Guid announcementId, CancellationToken ct) => db.AnnouncementLineGroupTargets.AsNoTracking()
        .CountAsync(x => x.AnnouncementId == announcementId && x.Destination!.Status == LineGroupDestinationStatuses.Active &&
            x.Destination.DeliveryProvider == "CUSTOM_ENDPOINT" && !x.Destination.Module.StartsWith("REPAIR_") &&
            !db.LineGroupDeliveryLogs.Any(log => log.RequestId == announcementId && log.CanonicalEventType == EventType && log.DestinationId == x.DestinationId), ct);

    public async Task<string> DispatchStatusAsync(Guid announcementId, CancellationToken ct)
    {
        var statuses = await db.LineGroupDeliveryLogs.AsNoTracking().Where(x => x.RequestId == announcementId && x.CanonicalEventType == EventType)
            .Select(x => x.Status).ToListAsync(ct);
        if (statuses.Any(x => x is "Pending" or "Retry")) return "Queued";
        if (statuses.Contains("Failed")) return "Failed";
        return statuses.Contains("Sent") ? "Sent" : "Skipped";
    }

    public async Task<int> QueueAsync(Guid announcementId, Guid? actor, CancellationToken ct)
    {
        // All publishers of one announcement serialize before checking the unique group keys.
        // The existing unique delivery key remains the final safeguard across instances.
        await using var transaction = db.Database.IsNpgsql() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql())
        {
            var lockKey = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes($"AnnouncementGroups:{announcementId}")), 0);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", ct);
        }
        var now = DateTime.UtcNow;
        var announcement = await db.Announcements.AsNoTracking().Include(x => x.Category)
            .Include(x => x.LineGroupTargets).ThenInclude(x => x.Destination)
            .SingleOrDefaultAsync(x => x.Id == announcementId && x.NotifyViaLineGroup &&
                x.Status == AnnouncementStatuses.Published && (x.ExpiresAt == null || x.ExpiresAt > now), ct);
        if (announcement is null) return 0;
        var existing = await db.LineGroupDeliveryLogs.Where(x => x.RequestId == announcementId && x.CanonicalEventType == EventType)
            .Select(x => x.DestinationId).ToListAsync(ct);
        var selected = announcement.LineGroupTargets.Where(x => !existing.Contains(x.DestinationId)).ToArray();
        if (selected.Length == 0) return 0;

        using var payload = JsonDocument.Parse(AnnouncementNotificationDispatcher.BuildLineFlexPayload(announcement, lineConfiguration.PublicAppUrl));
        var message = payload.RootElement.GetProperty("messages")[0];
        var plainText = $"ประกาศใหม่: {announcement.Title}\n{announcement.Summary}";
        if (plainText.Length > 4900) plainText = plainText[..4900];
        var eventId = Guid.NewGuid();
        db.DomainEvents.Add(new DomainEventRecord
        {
            EventId = eventId, EventType = EventType, Scope = "ANNOUNCEMENT", AggregateType = "Announcement", AggregateId = announcement.Id,
            OccurredAt = now, ActorUserId = actor, CorrelationId = $"announcement-groups:{eventId:N}",
            Payload = JsonSerializer.Serialize(new { Text = plainText, FlexContentsJson = message.GetProperty("contents").GetRawText(),
                AltText = message.GetProperty("altText").GetString() })
        });
        var queued = 0;
        foreach (var target in selected)
        {
            var active = target.Destination is { Status: LineGroupDestinationStatuses.Active, DeliveryProvider: "CUSTOM_ENDPOINT" } &&
                !target.Destination.Module.StartsWith("REPAIR_");
            db.LineGroupDeliveryLogs.Add(new LineGroupDeliveryLog
            {
                EventId = eventId, DestinationId = target.DestinationId, RequestId = announcement.Id,
                CanonicalEventType = EventType, SourceEventType = EventType, MessageText = plainText,
                DeduplicationKey = $"Announcement:Group:{announcement.Id}:{target.DestinationId}",
                CorrelationId = $"announcement-groups:{eventId:N}", Status = active ? "Pending" : "Skipped",
                ErrorCode = active ? null : "DESTINATION_DISABLED", FailedAt = active ? null : now, CreatedAt = now, AvailableAt = now
            });
            if (active) queued++;
        }
        db.AuditLogs.Add(new AuditLog { UserId = actor, Action = "Announcement.LineGroupsQueued", EntityName = "Announcement",
            EntityId = announcement.Id.ToString(), NewValue = $"Selected={selected.Length}; Queued={queued}", CreatedAt = now });
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return queued;
    }
}
