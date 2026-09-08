using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/fleet/dashboard")]
public sealed class FleetDashboardController(IFleetKpiService kpi, FleetReportExportService export, AppDbContext db, IFleetDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<FleetDashboardDto>>> Get(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId)) return Unauthorized(ApiResponse<FleetDashboardDto>.Fail("Authentication required."));
        var result = await dashboard.GetAsync(userId, ct);
        return result is null ? Forbid() : ApiResponse<FleetDashboardDto>.Ok(result);
    }

    [HttpGet("summary"), RequirePermission(FleetPermissions.DashboardView)] public async Task<ActionResult<ApiResponse<object>>> Summary([FromQuery] FleetReportQuery q, CancellationToken ct) => ApiResponse<object>.Ok(await kpi.Summary(q.Filter(), ct));
    [HttpGet("kpis"), RequirePermission(FleetPermissions.DashboardView)] public async Task<ActionResult<ApiResponse<object>>> Kpis([FromQuery] FleetReportQuery q, CancellationToken ct) => ApiResponse<object>.Ok(await kpi.Summary(q.Filter(), ct));
    [HttpGet("utilization"), RequirePermission(FleetPermissions.ReportView)] public async Task<ActionResult<ApiResponse<object>>> Utilization([FromQuery] FleetReportQuery q, CancellationToken ct) => ApiResponse<object>.Ok(await kpi.Vehicles(q.Filter(), ct));
    [HttpGet("drivers"), RequirePermission(FleetPermissions.ReportView)] public async Task<ActionResult<ApiResponse<object>>> Drivers([FromQuery] FleetReportQuery q, CancellationToken ct) => ApiResponse<object>.Ok(await kpi.Drivers(q.Filter(), ct));
    [HttpGet("departments"), RequirePermission(FleetPermissions.ReportView)] public async Task<ActionResult<ApiResponse<object>>> Departments([FromQuery] FleetReportQuery q, CancellationToken ct) => ApiResponse<object>.Ok(await kpi.Departments(q.Filter(), ct));
    [HttpGet("routes"), RequirePermission(FleetPermissions.ReportView)] public async Task<ActionResult<ApiResponse<object>>> Routes([FromQuery] FleetReportQuery q, CancellationToken ct) => ApiResponse<object>.Ok(await kpi.Routes(q.Filter(), ct));
    [HttpGet("export"), RequirePermission(FleetPermissions.ReportExport)]
    public async Task<IActionResult> Export([FromQuery] FleetReportQuery q, [FromQuery] string reportType = "summary", CancellationToken ct = default)
    {
        try { var bytes = await export.Export(reportType, q.Filter(), ct); var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); db.AuditLogs.Add(new() { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.ReportExported", EntityName = "FleetReport", EntityId = reportType, Detail = System.Text.Json.JsonSerializer.Serialize(q), CorrelationId = HttpContext.TraceIdentifier }); await db.SaveChangesAsync(ct); return File(bytes, "text/csv; charset=utf-8", $"fleet-{Safe(reportType)}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv"); } catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }
    private static string Safe(string value) => new(value.Where(char.IsLetterOrDigit).ToArray());
}

public sealed class FleetReportQuery
{
    public string? Preset { get; set; } public DateOnly? StartDate { get; set; } public DateOnly? EndDate { get; set; } public Guid? VehicleId { get; set; } public Guid? DriverUserId { get; set; } public Guid? DepartmentId { get; set; } public string? Status { get; set; }
    public FleetReportFilter Filter() => new(Preset, StartDate, EndDate, VehicleId, DriverUserId, DepartmentId, Status);
}
