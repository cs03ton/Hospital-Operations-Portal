using Hop.Api.Configuration;
using System.Text.Json;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    public async Task<int> ProjectMissingEventsAsync(CancellationToken ct)
    {
        if (!Enabled) return 0;
        var earliest = await db.LineGroupDestinations.AsNoTracking()
            .Where(x => !x.Module.StartsWith("REPAIR_") && x.Status == LineGroupDestinationStatuses.Active && x.ConfirmedAt != null)
            .MinAsync(x => (DateTime?)x.ConfirmedAt, ct);
        if (earliest is null) return 0;
        string[] projectedActions = ["Fleet.RequestSubmitted", "Fleet.VehicleAssigned", "Fleet.RequestCancelled"];
        var histories = await db.FleetRequestStatusHistories.AsNoTracking().Include(x => x.FleetRequest)
            .Where(x => projectedActions.Contains(x.Action) && x.CreatedAt >= earliest && !db.DomainEvents.Any(e => e.EventId == x.Id))
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
        string[] overdueEligibleStatuses = [FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready, FleetRequestStatuses.InProgress];
        var overdueRequests = await db.FleetRequests.AsNoTracking()
            .Where(x => x.ExpectedReturnAt < DateTime.UtcNow && overdueEligibleStatuses.Contains(x.Status) &&
                        !db.DomainEvents.Any(e => e.AggregateId == x.Id && e.EventType == "Fleet.TripOverdue"))
            .OrderBy(x => x.ExpectedReturnAt)
            .Take(Math.Clamp(options.Value.BatchSize, 1, 100))
            .Select(x => new { x.Id, x.RequestNo, x.Status, x.ExpectedReturnAt })
            .ToListAsync(ct);
        foreach (var request in overdueRequests)
        {
            var eventId = Guid.NewGuid();
            var payload = System.Text.Json.JsonSerializer.Serialize(new { FleetRequestId = request.Id, request.RequestNo, request.Status, request.ExpectedReturnAt });
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
        if (projected > 0) await db.SaveChangesAsync(ct);
        return projected;
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
        foreach (var canonical in destinations.SelectMany(x => x.EventSubscriptions)
                     .Where(x => x.IsEnabled).Select(x => x.EventType).Distinct())
        {
            var sources = FleetLineGroupEventMapper.SourceEventsFor(canonical);
            if (sources.Length == 0) continue;
            // Filter BEFORE batching. Existing deliveries (including Retry/Failed)
            // must not hide newer events, or prevent fan-out to another group.
            candidates = candidates.Union(baseEvents.Where(e => sources.Contains(e.EventType) &&
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
        var logs = await db.LineGroupDeliveryLogs.Include(x => x.Destination)
            .Where(x => (x.Status == "Pending" || x.Status == "Retry") && x.AvailableAt <= now)
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
        return logs.Count;
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
