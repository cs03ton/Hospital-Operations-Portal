using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hop.Api.Configuration;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/fleet/reports/feedback")]
[RequirePermission(FleetPermissions.FeedbackViewManagement)]
public sealed class FleetFeedbackReportsController(AppDbContext db, IOptions<FleetOperationsOptions> options) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<FleetFeedbackManagementSummaryDto>>> Summary([FromQuery] FleetFeedbackReportQuery query, CancellationToken ct)
    {
        var trips = FilterTrips(query);
        var tripCount = await trips.CountAsync(ct);
        var eligible = await trips.SelectMany(x => x.Participants)
            .CountAsync(x => x.IsActualParticipant && x.ParticipantType == FleetPassengerTypes.Employee && x.UserId != null && x.UserId != x.Trip!.DriverUserId, ct);
        var feedbacks = trips.SelectMany(x => x.Feedbacks);
        var count = await feedbacks.CountAsync(ct);
        var threshold = Math.Clamp(options.Value.FeedbackAttentionThreshold, 1, 5);
        var result = new FleetFeedbackManagementSummaryDto(
            tripCount, eligible, count, Rate(count, eligible),
            await feedbacks.Select(x => (double?)x.OverallRating).AverageAsync(ct),
            await feedbacks.Select(x => (double?)x.SafetyRating).AverageAsync(ct),
            await feedbacks.CountAsync(x => x.HasIncident, ct),
            await feedbacks.CountAsync(x => x.HasIncident || x.OverallRating <= threshold || x.SafetyRating <= threshold, ct),
            threshold);
        await Audit("Fleet.FeedbackViewedByManagement", "FleetFeedbackReport", "summary", query, ct);
        return ApiResponse<FleetFeedbackManagementSummaryDto>.Ok(result);
    }

    [HttpGet("attention")]
    public async Task<ActionResult<ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackAttentionDto>>>> Attention([FromQuery] FleetFeedbackReportQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page); var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var threshold = Math.Clamp(options.Value.FeedbackAttentionThreshold, 1, 5);
        var feedbacks = FilterTrips(query).SelectMany(x => x.Feedbacks)
            .Where(x => x.HasIncident || x.OverallRating <= threshold || x.SafetyRating <= threshold);
        var total = await feedbacks.CountAsync(ct);
        var items = await feedbacks.OrderByDescending(x => x.SubmittedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new FleetFeedbackAttentionDto(
                x.Id, x.TripId, x.FleetRequestId, x.FleetRequest!.RequestNo, x.Trip!.ActualEndAt!.Value,
                x.FleetRequest.Destination, x.Vehicle!.VehicleCode + " · " + x.Vehicle.RegistrationNumber,
                x.DriverUserId, x.DriverUser!.FullName, x.OverallRating, x.SafetyRating,
                x.HasIncident, x.IncidentCategory, x.Comment, x.SubmittedAt)).ToListAsync(ct);
        await Audit("Fleet.FeedbackViewedByManagement", "FleetFeedbackReport", "attention", query, ct);
        return ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackAttentionDto>>.Ok(new(items, page, pageSize, total, Pages(total, pageSize)));
    }

    [HttpGet("drivers/{driverUserId:guid}/trend")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetFeedbackTrendPointDto>>>> DriverTrend(Guid driverUserId, [FromQuery] FleetFeedbackReportQuery query, CancellationToken ct)
    {
        var rows = await FilterTrips(query).Where(x => x.DriverUserId == driverUserId)
            .SelectMany(x => x.Feedbacks)
            .GroupBy(x => new { x.Trip!.ActualEndAt!.Value.Year, x.Trip.ActualEndAt.Value.Month })
            .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
            .Select(x => new FleetFeedbackTrendPointDto(x.Key.Year, x.Key.Month, x.Count(),
                x.Select(f => (double?)f.OverallRating).Average(),
                x.Select(f => (double?)f.SafetyRating).Average(),
                x.Select(f => (double?)f.PunctualityRating).Average())).ToListAsync(ct);
        await Audit("Fleet.FeedbackViewedByManagement", "FleetFeedbackReport", $"driver-trend:{driverUserId}", query, ct);
        return ApiResponse<IReadOnlyList<FleetFeedbackTrendPointDto>>.Ok(rows);
    }

    [HttpGet("trips")]
    public async Task<ActionResult<ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackTripSummaryDto>>>> Trips([FromQuery] FleetFeedbackReportQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page); var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var trips = FilterTrips(query);
        var total = await trips.CountAsync(ct);
        var rows = await trips.OrderByDescending(x => x.ActualEndAt).ThenBy(x => x.FleetRequest!.RequestNo)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.FleetRequestId, x.FleetRequest!.RequestNo, CompletedAt = x.ActualEndAt!.Value,
                x.FleetRequest.Destination, x.FleetRequest.MissionType,
                Department = x.FleetRequest.RequesterDepartment != null ? x.FleetRequest.RequesterDepartment.Name : "ไม่ระบุหน่วยงาน",
                VehicleId = x.Assignment!.VehicleId,
                Vehicle = x.Assignment.Vehicle!.VehicleCode + " · " + x.Assignment.Vehicle.RegistrationNumber,
                x.DriverUserId, Driver = x.DriverUser!.FullName,
                Eligible = x.Participants.Count(p => p.IsActualParticipant && p.ParticipantType == FleetPassengerTypes.Employee && p.UserId != null && p.UserId != x.DriverUserId),
                Count = x.Feedbacks.Count(), Incidents = x.Feedbacks.Count(f => f.HasIncident),
                Overall = x.Feedbacks.Select(f => (double?)f.OverallRating).Average(),
                Safety = x.Feedbacks.Select(f => (double?)f.SafetyRating).Average(),
                Punctuality = x.Feedbacks.Select(f => (double?)f.PunctualityRating).Average(),
                Service = x.Feedbacks.Select(f => (double?)f.ServiceRating).Average(),
                Condition = x.Feedbacks.Select(f => (double?)f.VehicleConditionRating).Average(),
                Cleanliness = x.Feedbacks.Select(f => (double?)f.VehicleCleanlinessRating).Average()
            }).ToListAsync(ct);
        var items = rows.Select(x => new FleetFeedbackTripSummaryDto(x.Id, x.FleetRequestId, x.RequestNo, x.CompletedAt, x.Destination,
            x.VehicleId, x.Vehicle, x.DriverUserId, x.Driver, x.Department, x.MissionType, x.Eligible, x.Count,
            Rate(x.Count, x.Eligible), x.Overall, x.Safety, x.Punctuality, x.Service, x.Condition, x.Cleanliness, x.Incidents)).ToList();
        await Audit("Fleet.FeedbackViewedByManagement", "FleetFeedbackReport", "trips", query, ct);
        return ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackTripSummaryDto>>.Ok(new(items, page, pageSize, total, Pages(total, pageSize)));
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackDriverSummaryDto>>>> Drivers([FromQuery] FleetFeedbackReportQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page); var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var trips = FilterTrips(query);
        var groups = trips.GroupBy(x => new { x.DriverUserId, Driver = x.DriverUser!.FullName });
        var total = await groups.CountAsync(ct);
        var rows = await groups.OrderBy(x => x.Key.Driver).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => new
            {
                g.Key.DriverUserId, g.Key.Driver, Trips = g.Count(),
                Eligible = g.SelectMany(x => x.Participants).Count(p => p.IsActualParticipant && p.ParticipantType == FleetPassengerTypes.Employee && p.UserId != null && p.UserId != g.Key.DriverUserId),
                Count = g.SelectMany(x => x.Feedbacks).Count(), Incidents = g.SelectMany(x => x.Feedbacks).Count(f => f.HasIncident),
                Punctuality = g.SelectMany(x => x.Feedbacks).Select(f => (double?)f.PunctualityRating).Average(),
                Safety = g.SelectMany(x => x.Feedbacks).Select(f => (double?)f.SafetyRating).Average(),
                Service = g.SelectMany(x => x.Feedbacks).Select(f => (double?)f.ServiceRating).Average(),
                Overall = g.SelectMany(x => x.Feedbacks).Select(f => (double?)f.OverallRating).Average()
            }).ToListAsync(ct);
        var items = rows.Select(x => new FleetFeedbackDriverSummaryDto(x.DriverUserId, x.Driver, x.Trips, x.Eligible, x.Count,
            Rate(x.Count, x.Eligible), x.Punctuality, x.Safety, x.Service, x.Overall, x.Incidents)).ToList();
        await Audit("Fleet.FeedbackViewedByManagement", "FleetFeedbackReport", "drivers", query, ct);
        return ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackDriverSummaryDto>>.Ok(new(items, page, pageSize, total, Pages(total, pageSize)));
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackVehicleSummaryDto>>>> Vehicles([FromQuery] FleetFeedbackReportQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page); var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var groups = FilterTrips(query).GroupBy(x => new { x.Assignment!.VehicleId, x.Assignment.Vehicle!.VehicleCode, x.Assignment.Vehicle.RegistrationNumber });
        var total = await groups.CountAsync(ct);
        var rows = await groups.OrderBy(x => x.Key.VehicleCode).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => new FleetFeedbackVehicleSummaryDto(g.Key.VehicleId, g.Key.VehicleCode + " · " + g.Key.RegistrationNumber,
                g.Key.RegistrationNumber, g.Count(), g.SelectMany(x => x.Feedbacks).Count(),
                g.SelectMany(x => x.Feedbacks).Select(f => (double?)f.VehicleConditionRating).Average(),
                g.SelectMany(x => x.Feedbacks).Select(f => (double?)f.VehicleCleanlinessRating).Average(),
                g.SelectMany(x => x.Feedbacks).Count(f => f.HasIncident))).ToListAsync(ct);
        await Audit("Fleet.FeedbackViewedByManagement", "FleetFeedbackReport", "vehicles", query, ct);
        return ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackVehicleSummaryDto>>.Ok(new(rows, page, pageSize, total, Pages(total, pageSize)));
    }

    private IQueryable<FleetTripRecord> FilterTrips(FleetFeedbackReportQuery q)
    {
        var rows = db.FleetTripRecords.AsNoTracking().Where(x => x.ActualEndAt != null && x.FleetRequest!.Status == FleetRequestStatuses.Completed);
        if (q.StartDate is not null) { var start = q.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); rows = rows.Where(x => x.ActualEndAt >= start); }
        if (q.EndDate is not null) { var end = q.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); rows = rows.Where(x => x.ActualEndAt < end); }
        if (q.DriverUserId is not null) rows = rows.Where(x => x.DriverUserId == q.DriverUserId);
        if (q.VehicleId is not null) rows = rows.Where(x => x.Assignment!.VehicleId == q.VehicleId);
        if (q.DepartmentId is not null) rows = rows.Where(x => x.FleetRequest!.RequesterDepartmentId == q.DepartmentId);
        if (!string.IsNullOrWhiteSpace(q.MissionType)) rows = rows.Where(x => x.FleetRequest!.MissionType == q.MissionType);
        if (!string.IsNullOrWhiteSpace(q.RequestNo)) { var term = q.RequestNo.Trim().ToLower(); rows = rows.Where(x => x.FleetRequest!.RequestNo.ToLower().Contains(term)); }
        if (q.OverallRating is not null) rows = rows.Where(x => x.Feedbacks.Any(f => f.OverallRating == q.OverallRating));
        if (q.SafetyRating is not null) rows = rows.Where(x => x.Feedbacks.Any(f => f.SafetyRating == q.SafetyRating));
        if (q.HasIncident is not null) rows = rows.Where(x => x.Feedbacks.Any(f => f.HasIncident == q.HasIncident));
        if (!string.IsNullOrWhiteSpace(q.IncidentCategory)) rows = rows.Where(x => x.Feedbacks.Any(f => f.IncidentCategory == q.IncidentCategory));
        return rows;
    }

    private async Task Audit(string action, string entityName, string entityId, object detail, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return;
        db.AuditLogs.Add(new AuditLog { UserId = actor, EffectiveActorUserId = actor, Action = action, EntityName = entityName, EntityId = entityId, Detail = System.Text.Json.JsonSerializer.Serialize(detail), CorrelationId = HttpContext.TraceIdentifier });
        await db.SaveChangesAsync(ct);
    }
    private static decimal Rate(int count, int eligible) => eligible == 0 ? 0 : Math.Round(count * 100m / eligible, 2);
    private static int Pages(int total, int size) => total == 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}

public sealed class FleetFeedbackReportQuery
{
    public DateOnly? StartDate { get; set; } public DateOnly? EndDate { get; set; }
    public Guid? DriverUserId { get; set; } public Guid? VehicleId { get; set; } public Guid? DepartmentId { get; set; }
    public string? MissionType { get; set; } public int? OverallRating { get; set; } public int? SafetyRating { get; set; }
    public bool? HasIncident { get; set; } public string? IncidentCategory { get; set; } public string? RequestNo { get; set; }
    public int Page { get; set; } = 1; public int PageSize { get; set; } = 20;
}
