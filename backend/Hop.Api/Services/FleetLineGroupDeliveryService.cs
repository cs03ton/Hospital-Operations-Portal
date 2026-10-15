using Hop.Api.Configuration;
using System.Text.Json;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Globalization;

namespace Hop.Api.Services;

public sealed class FleetLineGroupDeliveryService(
    AppDbContext db,
    IFleetLineGroupEventMapper mapper,
    IFleetLineGroupMessageTemplateService templates,
    ILineGroupPushClient line,
    IOptions<LineGroupNotificationsOptions> options,
    ILogger<FleetLineGroupDeliveryService> logger)
{
    public bool Enabled => options.Value.Enabled;
    private DateTime? TripOverdueStartUtc => options.Value.TripOverdueEnabled
        ? options.Value.TripOverdueEnabledFromUtc?.UtcDateTime
        : null;

    public async Task<int> ProjectMissingEventsAsync(CancellationToken ct)
    {
        if (!Enabled) return 0;
        var dailyProjected = await ProjectDailyDriverJobsAsync(DateTime.UtcNow, ct);
        var earliest = await db.LineGroupDestinations.AsNoTracking()
            .Where(x => !x.Module.StartsWith("REPAIR_") && x.Status == LineGroupDestinationStatuses.Active && x.ConfirmedAt != null)
            .MinAsync(x => (DateTime?)x.ConfirmedAt, ct);
        if (earliest is null) return dailyProjected;
        string[] projectedActions = ["Fleet.RequestSubmitted", "Fleet.VehicleAssigned", "Fleet.RequestCancelled"];
        var histories = await db.FleetRequestStatusHistories.AsNoTracking().Include(x => x.FleetRequest)
            .Where(x => projectedActions.Contains(x.Action) && x.CreatedAt >= earliest &&
                (x.Action != "Fleet.RequestCancelled" || x.FromStatus != FleetRequestStatuses.Draft || x.FleetRequest!.SubmittedAt != null) &&
                !db.DomainEvents.Any(e => e.EventId == x.Id))
            .OrderBy(x => x.CreatedAt).Take(Math.Clamp(options.Value.BatchSize * 5, 20, 500)).ToListAsync(ct);
        foreach (var history in histories)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                history.FleetRequestId,
                history.FleetRequest!.RequestNo,
                Status = history.ToStatus,
                history.ReturnTarget,
                history.FleetRequest.IsUrgent,
                history.FleetRequest.UrgentReason
            });
            db.DomainEvents.Add(new DomainEventRecord
            {
                EventId = history.Id, EventType = history.Action, Scope = "FLEET", AggregateType = "FleetRequest",
                AggregateId = history.FleetRequestId, OccurredAt = history.CreatedAt, ActorUserId = history.ActorUserId,
                CorrelationId = history.CorrelationId ?? $"fleet-group-projection:{history.Id}", Payload = payload
            });
            db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = history.Id, EventType = history.Action, Scope = "FLEET", Payload = payload,
                CreatedAt = history.CreatedAt, AvailableAt = DateTime.UtcNow, Status = "PROCESSED", ProcessedAt = DateTime.UtcNow
            });
        }
        var projected = histories.Count;
        if (TripOverdueStartUtc is { } overdueStartUtc)
        {
            string[] overdueEligibleStatuses = [FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready, FleetRequestStatuses.InProgress];
            var overdueRequests = await db.FleetRequests.AsNoTracking()
                .Where(x => x.ExpectedReturnAt >= overdueStartUtc && x.ExpectedReturnAt < DateTime.UtcNow &&
                            overdueEligibleStatuses.Contains(x.Status) &&
                            !db.DomainEvents.Any(e => e.AggregateId == x.Id && e.EventType == "Fleet.TripOverdue"))
                .OrderBy(x => x.ExpectedReturnAt)
                .Take(Math.Clamp(options.Value.BatchSize, 1, 100))
                .Select(x => new { x.Id, x.RequestNo, x.Status, x.ExpectedReturnAt })
                .ToListAsync(ct);
            foreach (var request in overdueRequests)
            {
                var eventId = Guid.NewGuid();
                var payload = JsonSerializer.Serialize(new { FleetRequestId = request.Id, request.RequestNo, request.Status, request.ExpectedReturnAt });
                db.DomainEvents.Add(new DomainEventRecord
                {
                    EventId = eventId, EventType = "Fleet.TripOverdue", Scope = "FLEET", AggregateType = "FleetRequest",
                    AggregateId = request.Id, OccurredAt = DateTime.UtcNow, CorrelationId = $"fleet-overdue:{request.Id}", Payload = payload
                });
                db.OutboxMessages.Add(new OutboxMessage
                {
                    EventId = eventId, EventType = "Fleet.TripOverdue", Scope = "FLEET", Payload = payload,
                    AvailableAt = DateTime.UtcNow, Status = "PROCESSED", ProcessedAt = DateTime.UtcNow
                });
                projected++;
            }
        }
        if (projected > 0) await db.SaveChangesAsync(ct);
        return projected + dailyProjected;
    }

    internal async Task<int> ProjectDailyDriverJobsAsync(DateTime now, CancellationToken ct)
    {
        var zone = ResolveBangkokTimeZone();
        var local = TimeZoneInfo.ConvertTimeFromUtc(now, zone);
        if (local.Hour < 6) return 0;
        var day = DateOnly.FromDateTime(local);
        var markerId = FleetDailyDriverJobsDigest.StableId(day, -1);
        if (await db.DomainEvents.AsNoTracking().AnyAsync(x => x.EventId == markerId, ct)) return 0;

        var start = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        string[] statuses = [FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck,
            FleetRequestStatuses.Ready, FleetRequestStatuses.InProgress];
        var requests = await db.FleetRequests.AsNoTracking()
            .Where(x => x.DepartureAt >= start && x.DepartureAt < end && statuses.Contains(x.Status) &&
                x.Assignments.Any(a => a.IsActive))
            .Include(x => x.Assignments.Where(a => a.IsActive)).ThenInclude(a => a.Vehicle)
            .Include(x => x.Assignments.Where(a => a.IsActive)).ThenInclude(a => a.DriverUser)
            .OrderBy(x => x.DepartureAt).ThenBy(x => x.RequestNo).ToListAsync(ct);
        var destinations = await db.LineGroupDestinations.AsNoTracking().Include(x => x.EventSubscriptions)
            .Where(x => !x.Module.StartsWith("REPAIR_") && x.Status == LineGroupDestinationStatuses.Active &&
                x.ConfirmedAt != null && x.ConfirmedAt <= now &&
                x.EventSubscriptions.Any(s => s.EventType == FleetDailyDriverJobsDigest.EventType && s.IsEnabled))
            .ToListAsync(ct);

        var lines = requests.Select((request, index) =>
        {
            var assignment = request.Assignments.First(a => a.IsActive);
            var departure = TimeZoneInfo.ConvertTimeFromUtc(request.DepartureAt, zone);
            return $"{index + 1}. {departure:HH:mm} {request.RequestNo} → {request.Destination}\n   รถ: {assignment.Vehicle?.RegistrationNumber ?? "-"} · คนขับ: {assignment.DriverUser?.FullName ?? "-"}";
        }).ToArray();
        var messages = FleetDailyDriverJobsDigest.BuildMessages(day, lines);
        var dayKey = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var markerPayload = JsonSerializer.Serialize(new { Day = dayKey });
        db.DomainEvents.Add(new DomainEventRecord
        {
            EventId = markerId, EventType = FleetDailyDriverJobsDigest.MarkerType, Scope = "FLEET",
            AggregateType = "FleetDailyDigest", AggregateId = Guid.Empty, OccurredAt = now,
            CorrelationId = $"fleet-daily:{dayKey}", Payload = markerPayload
        });
        for (var part = 0; part < messages.Count; part++)
        {
            var eventId = FleetDailyDriverJobsDigest.StableId(day, part);
            var payload = JsonSerializer.Serialize(new { Text = messages[part] });
            db.DomainEvents.Add(new DomainEventRecord
            {
                EventId = eventId, EventType = FleetDailyDriverJobsDigest.EventType, Scope = "FLEET",
                AggregateType = "FleetDailyDigest", AggregateId = Guid.Empty, OccurredAt = now,
                CorrelationId = $"fleet-daily:{dayKey}:{part + 1}", Payload = payload
            });
            db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = eventId, EventType = FleetDailyDriverJobsDigest.EventType, Scope = "FLEET",
                Payload = payload, CreatedAt = now, AvailableAt = now, ProcessedAt = now, Status = "PROCESSED"
            });
            foreach (var destination in destinations)
                db.LineGroupDeliveryLogs.Add(new LineGroupDeliveryLog
                {
                    EventId = eventId, DestinationId = destination.Id,
                    CanonicalEventType = FleetDailyDriverJobsDigest.EventType,
                    SourceEventType = FleetDailyDriverJobsDigest.EventType, RequestId = Guid.Empty,
                    DeduplicationKey = $"{eventId}:{destination.Id}:{FleetDailyDriverJobsDigest.EventType}",
                    CorrelationId = $"fleet-daily:{dayKey}:{part + 1}"
                });
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another worker recorded this day's snapshot first.
            foreach (var entry in db.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).ToArray()) entry.State = EntityState.Detached;
            return 0;
        }
        return messages.Count;
    }

    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }

    public async Task<int> DiscoverAsync(CancellationToken ct)
    {
        if (!Enabled) return 0;
        var destinations = await db.LineGroupDestinations.AsNoTracking().Include(x => x.EventSubscriptions)
            .Where(x => !x.Module.StartsWith("REPAIR_") && x.Status == LineGroupDestinationStatuses.Active && x.ConfirmedAt != null)
            .ToListAsync(ct);
        if (destinations.Count == 0) return 0;
        var earliest = destinations.Min(x => x.ConfirmedAt!.Value);
        var baseEvents = db.DomainEvents.AsNoTracking()
            .Where(x => (x.Scope == "FLEET" || x.Scope == "MEETING_ROOM" || x.Scope == "REPAIR") && x.OccurredAt >= earliest && db.OutboxMessages.Any(o => o.EventId == x.EventId));
        var candidates = baseEvents.Where(x => false);
        var overdueStartUtc = TripOverdueStartUtc;
        foreach (var canonical in destinations.SelectMany(x => x.EventSubscriptions)
                     .Where(x => x.IsEnabled).Select(x => x.EventType).Distinct())
        {
            if (canonical == "Fleet.TripOverdue" && overdueStartUtc is null) continue;
            if (canonical == FleetDailyDriverJobsDigest.EventType) continue; // Recipients are frozen when the snapshot is created.
            var sources = FleetLineGroupEventMapper.SourceEventsFor(canonical);
            if (sources.Length == 0) continue;
            var eligibleEvents = baseEvents;
            if (canonical == "Fleet.TripOverdue")
            {
                var cutoff = overdueStartUtc!.Value;
                eligibleEvents = eligibleEvents.Where(x => x.OccurredAt >= cutoff);
            }
            // Filter BEFORE batching. Existing deliveries (including Retry/Failed)
            // must not hide newer events, or prevent fan-out to another group.
            candidates = candidates.Union(eligibleEvents.Where(e => sources.Contains(e.EventType) &&
                db.LineGroupDestinations.Any(d => !d.Module.StartsWith("REPAIR_") &&
                    d.Status == LineGroupDestinationStatuses.Active && d.ConfirmedAt != null && d.ConfirmedAt <= e.OccurredAt &&
                    d.EventSubscriptions.Any(s => s.IsEnabled && s.EventType == canonical) &&
                    !db.LineGroupDeliveryLogs.Any(l => l.EventId == e.EventId && l.DestinationId == d.Id && l.CanonicalEventType == canonical))));
        }
        var pageSize = Math.Clamp(options.Value.BatchSize * 5, 20, 500);
        var created = 0;
        for (var offset = 0; ; offset += pageSize)
        {
        var events = await candidates.OrderBy(x => x.OccurredAt).ThenBy(x => x.EventId)
            .Skip(offset).Take(pageSize).ToListAsync(ct);
        if (events.Count == 0) break;
        foreach (var domainEvent in events)
        {
            var canonical = mapper.ToCanonical(domainEvent.Scope, domainEvent.EventType);
            if (canonical is null) continue;
            string? repairTeam = null;
            if (domainEvent.Scope == "REPAIR")
            {
                try
                {
                    using var payload = JsonDocument.Parse(domainEvent.Payload);
                    repairTeam = payload.RootElement.GetProperty("TeamCode").GetString();
                }
                catch (JsonException) { continue; }
                catch (KeyNotFoundException) { continue; }
                if (repairTeam is not ("IT" or "GENERAL")) continue;
            }
            foreach (var destination in destinations.Where(x => x.ConfirmedAt <= domainEvent.OccurredAt &&
                         (repairTeam is null || (x.RepairTeamCode == repairTeam && x.RepairTeamAssignedAt <= domainEvent.OccurredAt)) &&
                         x.EventSubscriptions.Any(s => s.EventType == canonical && s.IsEnabled)))
            {
                var key = $"{domainEvent.EventId}:{destination.Id}:{canonical}";
                if (await db.LineGroupDeliveryLogs.AnyAsync(x => x.DeduplicationKey == key, ct)) continue;
                db.LineGroupDeliveryLogs.Add(new LineGroupDeliveryLog
                {
                    EventId = domainEvent.EventId,
                    DestinationId = destination.Id,
                    CanonicalEventType = canonical,
                    SourceEventType = domainEvent.EventType,
                    RequestId = domainEvent.AggregateId,
                    DeduplicationKey = key,
                    CorrelationId = domainEvent.CorrelationId
                });
                created++;
            }
        }
        // Repair team filtering happens above in memory. Continue past unrelated
        // pages so they cannot starve valid newer events either.
        if (created >= pageSize || events.Count < pageSize) break;
        }
        if (created > 0) await db.SaveChangesAsync(ct);
        return created;
    }

    public async Task<int> ProcessAsync(CancellationToken ct)
    {
        if (!Enabled) return 0;
        var now = DateTime.UtcNow;
        var overdueStartUtc = TripOverdueStartUtc;
        var skipped = await SkipSuppressedOverdueAsync(overdueStartUtc, now, ct);
        var deliverable = db.LineGroupDeliveryLogs.Include(x => x.Destination)
            .Where(x => (x.Status == "Pending" || x.Status == "Retry") && x.AvailableAt <= now);
        deliverable = overdueStartUtc is { } cutoff
            ? deliverable.Where(x => x.CanonicalEventType != "Fleet.TripOverdue" ||
                db.DomainEvents.Any(e => e.EventId == x.EventId && e.OccurredAt >= cutoff))
            : deliverable.Where(x => x.CanonicalEventType != "Fleet.TripOverdue");
        var logs = await deliverable
            .OrderBy(x => x.CreatedAt).Take(Math.Clamp(options.Value.BatchSize, 1, 100)).ToListAsync(ct);
        foreach (var log in logs)
        {
            if (log.Destination is null || log.Destination.Status != LineGroupDestinationStatuses.Active)
            {
                log.Status = "Skipped";
                log.FailedAt = now;
                log.ErrorCode = "DESTINATION_DISABLED";
                log.ErrorMessage = "Destination is not active.";
                continue;
            }
            if (log.CanonicalEventType == FleetDailyDriverJobsDigest.EventType &&
                !await db.LineGroupEventSubscriptions.AsNoTracking().AnyAsync(s => s.DestinationId == log.DestinationId &&
                    s.EventType == FleetDailyDriverJobsDigest.EventType && s.IsEnabled, ct))
            {
                log.Status = "Skipped";
                log.FailedAt = now;
                log.ErrorCode = "SUBSCRIPTION_DISABLED";
                continue;
            }
            FleetGroupRenderedMessage? rendered = null;
            var domainEvent = await db.DomainEvents.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == log.EventId, ct);
            if (domainEvent is not null)
                rendered = await templates.RenderAsync(domainEvent, log.CanonicalEventType, ct);
            if (string.IsNullOrWhiteSpace(log.MessageText))
            {
                if (domainEvent is null) { MarkPermanent(log, "EVENT_NOT_FOUND", "Domain event was not found.", now); continue; }
                if (rendered is null) { MarkPermanent(log, "TEMPLATE_NOT_FOUND", "Message template was not found.", now); continue; }
                log.MessageText = rendered.Text;
            }

            log.AttemptCount++;
            var outgoing = rendered ?? new FleetGroupRenderedMessage("text", log.MessageText, log.CanonicalEventType, log.RequestId);
            var result = await line.PushMessageAsync(log.Destination, outgoing, ct);
            log.UpdatedAt = DateTime.UtcNow;
            if (result.Success)
            {
                log.Status = "Sent";
                log.SentAt = DateTime.UtcNow;
                log.FailedAt = null;
                log.ErrorCode = null;
                log.ErrorMessage = null;
                continue;
            }

            log.ErrorCode = result.ErrorCode;
            log.ErrorMessage = Sanitize(result.ErrorMessage);
            if (result.IsTransient && log.AttemptCount < Math.Clamp(options.Value.MaxAttempts, 1, 10))
            {
                log.Status = "Retry";
                log.AvailableAt = DateTime.UtcNow.AddMinutes(Math.Min(60, 1 << Math.Min(log.AttemptCount, 5)));
            }
            else
            {
                log.Status = "Failed";
                log.FailedAt = DateTime.UtcNow;
                log.Destination.AttentionRequired = true;
                log.Destination.AttentionReason = log.ErrorCode;
                if (result.DestinationUnavailable)
                {
                    log.Destination.Status = LineGroupDestinationStatuses.Disabled;
                    log.Destination.DisabledAt = DateTime.UtcNow;
                }
                log.Destination.ConcurrencyToken = Guid.NewGuid();
            }
            logger.LogWarning("Fleet LINE group delivery issue. EventType={EventType} DestinationId={DestinationId} Attempt={Attempt} ErrorCode={ErrorCode}",
                log.CanonicalEventType, log.DestinationId, log.AttemptCount, log.ErrorCode);
        }
        if (logs.Count > 0) await db.SaveChangesAsync(ct);
        return skipped + logs.Count;
    }

    private async Task<int> SkipSuppressedOverdueAsync(DateTime? startUtc, DateTime now, CancellationToken ct)
    {
        var pending = db.LineGroupDeliveryLogs.Where(x => x.CanonicalEventType == "Fleet.TripOverdue" &&
            (x.Status == "Pending" || x.Status == "Retry"));
        if (startUtc is { } cutoff)
            pending = pending.Where(x => !db.DomainEvents.Any(e => e.EventId == x.EventId && e.OccurredAt >= cutoff));
        var skipped = 0;
        while (true)
        {
            var batch = await pending.Take(500).ToListAsync(ct);
            if (batch.Count == 0) break;
            foreach (var log in batch)
            {
                log.Status = "Skipped";
                log.FailedAt = now;
                log.UpdatedAt = now;
                log.ErrorCode = "TRIP_OVERDUE_DISABLED";
                log.ErrorMessage = "Trip overdue LINE group notifications are disabled.";
            }
            await db.SaveChangesAsync(ct);
            skipped += batch.Count;
        }
        return skipped;
    }

    private static void MarkPermanent(LineGroupDeliveryLog log, string code, string message, DateTime now)
    {
        log.Status = "Failed"; log.FailedAt = now; log.ErrorCode = code; log.ErrorMessage = message; log.UpdatedAt = now;
    }
    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var sanitized = value.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("access_token", "token", StringComparison.OrdinalIgnoreCase);
        return sanitized[..Math.Min(sanitized.Length, 900)];
    }
}
