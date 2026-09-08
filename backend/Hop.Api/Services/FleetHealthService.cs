using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public interface IFleetHealthService
{
    Task<FleetHealthResponse> GetAsync(CancellationToken ct);
    Task<IReadOnlyList<FleetHealthIssue>> GetIssuesAsync(string? severity, CancellationToken ct);
    Task<IReadOnlyList<FleetOutboxHealthItem>> GetOutboxAsync(CancellationToken ct);
}

public sealed class FleetHealthService(AppDbContext db, IOptions<FleetRolloutOptions> options, ILogger<FleetHealthService> logger) : IFleetHealthService
{
    private static readonly string[] ActiveStates = [FleetRequestStatuses.PendingDispatch, FleetRequestStatuses.PendingAdminReview, FleetRequestStatuses.PendingDirector, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready, FleetRequestStatuses.InProgress, FleetRequestStatuses.CancellationPending];

    public async Task<FleetHealthResponse> GetAsync(CancellationToken ct)
    {
        var statusCounts = await db.FleetRequests.AsNoTracking().Where(x => ActiveStates.Contains(x.Status)).GroupBy(x => x.Status).Select(x => new { x.Key, Count = x.LongCount() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var outbox = await db.OutboxMessages.AsNoTracking().GroupBy(x => x.Status).Select(x => new { x.Key, Count = x.LongCount() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var delivery = await db.NotificationDeliveries.AsNoTracking().GroupBy(x => new { x.Channel, x.Status }).Select(x => new { x.Key.Channel, x.Key.Status, Count = x.LongCount() }).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var vehiclesUnavailable = await db.FleetVehicles.CountAsync(x => !x.IsActive || x.Status != FleetVehicleStatuses.Available, ct);
        var driversUnavailable = await db.FleetDriverProfiles.CountAsync(x => !x.IsActive || x.DriverStatus != FleetDriverStatuses.Available, ct);
        var expiredLicenses = await db.FleetDriverProfiles.CountAsync(x => x.LicenseExpiryDate != null && x.LicenseExpiryDate < today, ct);
        var invalidMileage = await db.FleetTripRecords.CountAsync(x => x.StartMileage < 0 || x.EndMileage != null && x.StartMileage != null && x.EndMileage < x.StartMileage, ct);
        var issues = await GetIssuesAsync(null, ct); var warnings = issues.Count(x => x.Severity == "Warning"); var critical = issues.Count(x => x.Severity == "Critical");
        var metrics = new List<FleetHealthMetric>
        {
            Metric("pending-dispatch", "Pending Dispatch", statusCounts.GetValueOrDefault(FleetRequestStatuses.PendingDispatch), "/fleet/dispatch"), Metric("pending-admin", "Pending Admin Review", statusCounts.GetValueOrDefault(FleetRequestStatuses.PendingAdminReview), "/fleet/review"), Metric("pending-director", "Pending Director Approval", statusCounts.GetValueOrDefault(FleetRequestStatuses.PendingDirector), "/fleet/director"), Metric("pending-driver", "Pending Driver Acknowledgement", statusCounts.GetValueOrDefault(FleetRequestStatuses.PendingDriverAck), "/fleet/driver"), Metric("ready", "Ready Trips", statusCounts.GetValueOrDefault(FleetRequestStatuses.Ready)), Metric("in-progress", "In Progress Trips", statusCounts.GetValueOrDefault(FleetRequestStatuses.InProgress)), Metric("cancellations", "Cancellation Requests", statusCounts.GetValueOrDefault(FleetRequestStatuses.CancellationPending)), Metric("outbox-pending", "Outbox Pending", outbox.GetValueOrDefault("PENDING"), "/fleet/health"), Metric("outbox-failed", "Outbox Failed", outbox.GetValueOrDefault("FAILED"), "/fleet/health"), Metric("delivery-failed", "Notification Deliveries Failed", delivery.Where(x => x.Status == "FAILED").Sum(x => x.Count)), Metric("line-retry", "LINE Retry Count", delivery.Where(x => x.Channel == "LINE" && x.Status == "RETRY").Sum(x => x.Count)), Metric("vehicles-unavailable", "Vehicles Unavailable", vehiclesUnavailable), Metric("drivers-unavailable", "Drivers Unavailable", driversUnavailable), Metric("expired-license", "Drivers with Expired License", expiredLicenses), Metric("invalid-mileage", "Trips with Invalid Mileage", invalidMileage)
        };
        var health = critical > 0 ? "Critical" : warnings > 0 ? "Warning" : "Healthy";
        logger.LogInformation("Fleet health generated. Status={HealthStatus} WarningCount={WarningCount} CriticalCount={CriticalCount} CorrelationScope=Fleet", health, warnings, critical);
        return new(health, DateTime.UtcNow, metrics, warnings, critical);
    }

    public async Task<IReadOnlyList<FleetHealthIssue>> GetIssuesAsync(string? severity, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var warningAt = now.AddHours(-options.Value.WarningStuckHours); var criticalAt = now.AddHours(-options.Value.CriticalStuckHours); var result = new List<FleetHealthIssue>();
        var stuck = await db.FleetRequests.AsNoTracking().Where(x => ActiveStates.Contains(x.Status) && (x.UpdatedAt ?? x.CreatedAt) <= warningAt).Select(x => new { x.Id, x.RequestNo, x.Status, At = x.UpdatedAt ?? x.CreatedAt }).ToListAsync(ct);
        result.AddRange(stuck.Select(x => new FleetHealthIssue("STUCK_WORKFLOW", x.At <= criticalAt ? "Critical" : "Warning", $"{x.RequestNo} ค้างที่ {x.Status}", "FleetRequest", x.Id.ToString(), now, $"/fleet/requests/{x.Id}")));
        var failed = await db.NotificationDeliveries.AsNoTracking().Where(x => x.Status == "FAILED" || x.Status == "RETRY" && x.AttemptCount >= options.Value.DeliveryRetryWarningCount).Select(x => new { x.Id, x.Channel, x.Status, x.AttemptCount }).ToListAsync(ct);
        result.AddRange(failed.Select(x => new FleetHealthIssue("NOTIFICATION_DELIVERY", x.Status == "FAILED" && x.AttemptCount >= options.Value.DeliveryFailedCriticalCount ? "Critical" : "Warning", $"{x.Channel} delivery {x.Status}, retry={x.AttemptCount}", "NotificationDelivery", x.Id.ToString(), now, "/fleet/health")));
        var expired = await db.FleetDriverProfiles.AsNoTracking().Where(x => x.IsActive && x.LicenseExpiryDate != null && x.LicenseExpiryDate < DateOnly.FromDateTime(now)).Select(x => new { x.Id, x.UserId }).ToListAsync(ct);
        result.AddRange(expired.Select(x => new FleetHealthIssue("LICENSE_EXPIRED", "Critical", "คนขับมีใบขับขี่หมดอายุ", "FleetDriverProfile", x.Id.ToString(), now, null)));
        return result.Where(x => string.IsNullOrWhiteSpace(severity) || x.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Severity == "Critical").ThenBy(x => x.Code).ToList();
    }

    public async Task<IReadOnlyList<FleetOutboxHealthItem>> GetOutboxAsync(CancellationToken ct) => await db.OutboxMessages.AsNoTracking().Include(x => x.Deliveries).Where(x => x.Status != "PROCESSED").OrderByDescending(x => x.CreatedAt).Take(200).Select(x => new FleetOutboxHealthItem(x.Id, x.EventId, x.EventType, x.Status, x.AttemptCount, x.LastError, x.CreatedAt, x.AvailableAt, x.ProcessedAt, x.Deliveries.Count(d => d.Channel == "IN_APP" && d.Status == "FAILED"), x.Deliveries.Count(d => d.Channel == "LINE" && d.Status == "FAILED"), x.Deliveries.Count(d => d.Channel == "IN_APP" && d.Status == "RETRY"), x.Deliveries.Count(d => d.Channel == "LINE" && d.Status == "RETRY"))).ToListAsync(ct);
    private static FleetHealthMetric Metric(string key, string label, long value, string? url = null) => new(key, label, value, "Healthy", url);
}
