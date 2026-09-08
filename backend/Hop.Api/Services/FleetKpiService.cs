using System.Text;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed record FleetReportFilter(string? Preset, DateOnly? StartDate, DateOnly? EndDate, Guid? VehicleId, Guid? DriverUserId, Guid? DepartmentId, string? Status);
public interface IFleetKpiService { Task<object> Summary(FleetReportFilter filter, CancellationToken ct); Task<object> Vehicles(FleetReportFilter filter, CancellationToken ct); Task<object> Drivers(FleetReportFilter filter, CancellationToken ct); Task<object> Departments(FleetReportFilter filter, CancellationToken ct); Task<object> Routes(FleetReportFilter filter, CancellationToken ct); }
public sealed class FleetKpiService(AppDbContext db, FleetDateRangeService ranges, FleetUtilizationQueryService utilization, FleetWorkflowDurationQueryService durations) : IFleetKpiService
{
    private IQueryable<FleetRequest> Requests(FleetReportFilter f, out FleetUtcRange range) { range = ranges.Resolve(f.Preset, f.StartDate, f.EndDate); var utcStart = range.Start; var utcEnd = range.End; var q = db.FleetRequests.AsNoTracking().Where(x => x.CreatedAt >= utcStart && x.CreatedAt < utcEnd); if (f.DepartmentId != null) q = q.Where(x => x.RequesterDepartmentId == f.DepartmentId); if (!string.IsNullOrWhiteSpace(f.Status)) q = q.Where(x => x.Status == f.Status); if (f.VehicleId != null) q = q.Where(x => x.Assignments.Any(a => a.VehicleId == f.VehicleId)); if (f.DriverUserId != null) q = q.Where(x => x.Assignments.Any(a => a.DriverUserId == f.DriverUserId)); return q; }
    public async Task<object> Summary(FleetReportFilter f, CancellationToken ct)
    {
        var q = Requests(f, out var range); var total = await q.CountAsync(ct); var completed = await q.CountAsync(x => x.Status == FleetRequestStatuses.Completed, ct); var rejected = await q.CountAsync(x => x.Status == FleetRequestStatuses.Rejected, ct); var cancelled = await q.CountAsync(x => x.Status == FleetRequestStatuses.Cancelled, ct); var decided = completed + rejected; var assignments = await q.SelectMany(x => x.Assignments).CountAsync(ct); var replacements = await q.SelectMany(x => x.Assignments).CountAsync(x => x.ReplacedAssignmentId != null, ct); var accepted = await q.SelectMany(x => x.StatusHistories).CountAsync(x => x.Action == "Fleet.DRIVER_ACCEPT", ct); var driverDecisions = await q.SelectMany(x => x.StatusHistories).CountAsync(x => x.Action == "Fleet.DRIVER_ACCEPT" || x.Action == "Fleet.DRIVER_DECLINE", ct);
        var deliveries = db.NotificationDeliveries.AsNoTracking().Where(x => x.CreatedAt >= range.Start && x.CreatedAt < range.End); var inAll = await deliveries.CountAsync(x => x.Channel == "IN_APP", ct); var lineAll = await deliveries.CountAsync(x => x.Channel == "LINE", ct); var inOk = await deliveries.CountAsync(x => x.Channel == "IN_APP" && x.Status == "SENT", ct); var lineOk = await deliveries.CountAsync(x => x.Channel == "LINE" && x.Status == "SENT", ct); var failures = await db.OutboxMessages.CountAsync(x => x.CreatedAt >= range.Start && x.CreatedAt < range.End && x.Status == "FAILED", ct);
        var durationStatistics = await durations.Query(range.Start, range.End, ct);
        var emergencyPending=await q.CountAsync(x=>x.Priority==FleetPriorities.Emergency&&x.Status!=FleetRequestStatuses.Completed&&x.Status!=FleetRequestStatuses.Cancelled&&x.Status!=FleetRequestStatuses.Rejected,ct);var emergencyBreaches=await q.CountAsync(x=>x.Priority==FleetPriorities.Emergency&&x.SubmittedAt!=null&&x.Assignments.Any()&&x.Assignments.Min(a=>a.AssignedAt)>x.SubmittedAt.Value.AddMinutes(15),ct);var bypasses=await q.SelectMany(x=>x.StatusHistories).CountAsync(x=>x.Action=="FleetEmergency.ApprovalBypassed",ct);var pendingReviews=await q.CountAsync(x=>x.Priority==FleetPriorities.Emergency&&x.RequiresPostReview,ct);var policyViolations=await db.FleetEmergencyPostReviews.CountAsync(x=>x.CreatedAt>=range.Start&&x.CreatedAt<range.End&&x.Outcome==FleetEmergencyReviewOutcomes.PolicyViolation,ct);
        return new { range.LocalStart, EndDate = range.LocalEndExclusive.AddDays(-1), TotalRequests = total, CompletedRequests = completed, RejectedRequests = rejected, CancelledRequests = cancelled, ApprovalRate = Rate(completed, decided), RejectionRate = Rate(rejected, decided), CancellationRate = Rate(cancelled, total), TripCompletionRate = Rate(completed, assignments), DriverAcceptanceRate = Rate(accepted, driverDecisions), AssignmentReplacementCount = replacements, AverageReplacementCountPerRequest = total == 0 ? 0 : Math.Round((decimal)replacements / total, 2), InAppDeliverySuccessRate = Rate(inOk, inAll), LineDeliverySuccessRate = Rate(lineOk, lineAll), OutboxFailureCount = failures, EmergencyPendingCount=emergencyPending,EmergencyResponseTargetBreaches=emergencyBreaches,ApprovalBypassCount=bypasses,PendingPostReviewCount=pendingReviews,EmergencyPolicyViolationCount=policyViolations, WorkflowDurations = durationStatistics };
    }
    public async Task<object> Vehicles(FleetReportFilter f, CancellationToken ct) { Requests(f, out var range); return await utilization.Query(range.Start, range.End, f.VehicleId, DateTime.UtcNow, ct); }
    public async Task<object> Drivers(FleetReportFilter f, CancellationToken ct)
    {
        var requests = Requests(f, out _);
        var assignments = requests.SelectMany(x => x.Assignments);
        var profiles = db.FleetDriverProfiles.AsNoTracking()
            .Where(x => x.IsActive && x.User != null && x.User.IsActive);
        if (f.DriverUserId != null) profiles = profiles.Where(x => x.UserId == f.DriverUserId);

        return await profiles
            .Select(profile => new
            {
                DriverUserId = profile.UserId,
                Driver = profile.User!.FullName,
                JobCount = assignments
                    .Where(assignment => assignment.DriverUserId == profile.UserId)
                    .Select(assignment => assignment.FleetRequestId)
                    .Distinct()
                    .Count(),
                CompletedTrips = assignments
                    .Where(assignment => assignment.DriverUserId == profile.UserId && assignment.FleetRequest!.Status == FleetRequestStatuses.Completed)
                    .Select(assignment => assignment.FleetRequestId)
                    .Distinct()
                    .Count()
            })
            .OrderBy(x => x.Driver)
            .ToListAsync(ct);
    }
    public async Task<object> Departments(FleetReportFilter f, CancellationToken ct) { var q = Requests(f, out _); return await q.GroupBy(x => new { x.RequesterDepartmentId, Name = x.RequesterDepartment != null ? x.RequesterDepartment.Name : "-" }).Select(g => new { g.Key.RequesterDepartmentId, Department = g.Key.Name, RequestCount = g.Count(), CompletedCount = g.Count(x => x.Status == FleetRequestStatuses.Completed), CancelledCount = g.Count(x => x.Status == FleetRequestStatuses.Cancelled) }).ToListAsync(ct); }
    public async Task<object> Routes(FleetReportFilter f, CancellationToken ct) { var q = Requests(f, out _); return await q.GroupBy(x => x.Destination.Trim().ToLower()).Select(g => new { Destination = g.Key, RequestCount = g.Count(), CompletedTripCount = g.Count(x => x.Status == FleetRequestStatuses.Completed) }).ToListAsync(ct); }
    private static decimal Rate(int n, int d) => d == 0 ? 0 : Math.Round(n * 100m / d, 2);
}

public sealed class FleetReportExportService(IFleetKpiService kpi, IOptions<FleetOperationsOptions> options)
{
    public async Task<byte[]> Export(string type, FleetReportFilter f, CancellationToken ct)
    {
        object data = type.ToLowerInvariant() switch { "vehicle" => await kpi.Vehicles(f, ct), "driver" => await kpi.Drivers(f, ct), "department" => await kpi.Departments(f, ct), "routes" => await kpi.Routes(f, ct), _ => await kpi.Summary(f, ct) };
        using var document = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(data));
        var elements = document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
            ? document.RootElement.EnumerateArray().ToArray()
            : [document.RootElement];
        var rows = elements.Select(element => element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ValueKind == System.Text.Json.JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString())).ToList();
        if (rows.Count > options.Value.MaximumExportRows) throw new ArgumentException("Export row limit exceeded.");
        var sb = new StringBuilder(); if (rows.Count > 0) { sb.AppendLine(string.Join(',', rows[0].Keys.Select(Escape))); foreach (var row in rows) sb.AppendLine(string.Join(',', row.Values.Select(Escape))); } else sb.AppendLine("NoData");
        return new UTF8Encoding(true).GetBytes(sb.ToString());
    }
    public static string Escape(string value) { if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value; return '"' + value.Replace("\"", "\"\"") + '"'; }
}
