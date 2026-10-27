using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public static class FleetDaytimeReferNotification
{
    public const string EventType = "Fleet.DaytimeRefer";
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Bangkok);
    public static bool Eligible(FleetRequest request, DateTime occurredAt)
    {
        var local = Local(occurredAt);
        return FleetReferPolicy.IsRefer(request) && request.Status == FleetRequestStatuses.PendingDriverAck &&
            local.Hour >= 6 && local.Hour < 18 && Local(request.DepartureAt).Date == local.Date;
    }

    // Called inside the assignment transaction, before SaveChanges: recipients and text
    // are frozen alongside the successful assignment, without any external API call.
    public static async Task<int> QueueAsync(AppDbContext db, FleetRequest request, FleetAssignment assignment,
        Guid sourceEventId, DateTime occurredAt, string correlationId, CancellationToken ct)
    {
        if (!assignment.IsActive || !Eligible(request, occurredAt)) return 0;
        if (db.Database.IsNpgsql())
        {
            if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Refer queue requires the assignment transaction.");
            var key = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes($"DaytimeRefer:{request.Id}")), 0);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct);
        }
        var destinations = await db.LineGroupDestinations.AsNoTracking()
            .Where(x => !x.Module.StartsWith("REPAIR_") && x.Status == LineGroupDestinationStatuses.Active && x.ConfirmedAt <= occurredAt &&
                x.EventSubscriptions.Any(s => s.EventType == EventType && s.IsEnabled && s.CreatedAt <= occurredAt && (s.UpdatedAt == null || s.UpdatedAt <= occurredAt)))
            .Select(x => x.Id).ToListAsync(ct);
        var existing = await db.LineGroupDeliveryLogs.Where(x => x.RequestId == request.Id && x.CanonicalEventType == EventType).Select(x => x.DestinationId).ToListAsync(ct);
        var vehicle = await db.FleetVehicles.AsNoTracking().Where(x => x.Id == assignment.VehicleId).Select(x => x.RegistrationNumber).SingleAsync(ct);
        var driver = await db.Users.AsNoTracking().Where(x => x.Id == assignment.DriverUserId).Select(x => x.FullName).SingleAsync(ct);
        var departure = Local(request.DepartureAt);
        string Clip(string value, int length) => value.Length <= length ? value : value[..length] + "…";
        var text = $"🚑 ภารกิจ Refer ระหว่างวัน\n📄 เลขคำขอ: {Clip(request.RequestNo, 100)}\n🕒 เวลาเดินทาง: {departure.ToString("HH:mm", CultureInfo.InvariantCulture)} น.\n📍 จุดหมาย: {Clip(request.Destination, 1000)}\n🚐 รถ: {Clip(vehicle, 100)}\n👤 คนขับ: {Clip(driver, 300)}";
        var count = 0;
        foreach (var destination in destinations.Except(existing))
        {
            if (db.LineGroupDeliveryLogs.Local.Any(x => x.RequestId == request.Id && x.DestinationId == destination && x.CanonicalEventType == EventType)) continue;
            db.LineGroupDeliveryLogs.Add(new() { EventId = sourceEventId, RequestId = request.Id, DestinationId = destination,
                CanonicalEventType = EventType, SourceEventType = "Fleet.ReferAutoApproved", MessageText = text,
                DeduplicationKey = $"DaytimeRefer:{request.Id}:{destination}", CreatedAt = occurredAt, AvailableAt = occurredAt, CorrelationId = correlationId });
            count++;
        }
        return count;
    }

    internal static async Task<string?> SkipReasonAsync(AppDbContext db, LineGroupDeliveryLog log, DateTime now, CancellationToken ct)
    {
        var local = Local(now);
        if (local.Date != Local(log.CreatedAt).Date || local.Hour < 6 || local.Hour >= 18) return "REFER_WINDOW_EXPIRED";
        if (!await db.LineGroupEventSubscriptions.AsNoTracking().AnyAsync(s => s.DestinationId == log.DestinationId && s.EventType == EventType && s.IsEnabled &&
            s.CreatedAt <= log.CreatedAt && (s.UpdatedAt == null || s.UpdatedAt <= log.CreatedAt), ct)) return "SUBSCRIPTION_DISABLED";
        if (!await db.FleetRequests.AsNoTracking().AnyAsync(x => x.Id == log.RequestId &&
            x.Status != FleetRequestStatuses.Cancelled && x.Status != FleetRequestStatuses.Aborted && x.Status != FleetRequestStatuses.Completed &&
            x.Status != FleetRequestStatuses.Draft && x.Status != FleetRequestStatuses.Rejected, ct)) return "REFER_REQUEST_CLOSED";
        return null;
    }
}
