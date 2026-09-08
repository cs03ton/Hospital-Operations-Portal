using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController, Authorize]
public sealed class FleetHealthController(IFleetHealthService health, FleetPermissionDiagnosticsService permissions, IFleetRolloutService rollout) : ControllerBase
{
    [HttpGet("/api/fleet/health"), RequireAnyPermission(FleetPermissions.HealthView, FleetPermissions.HealthManage)] public async Task<ActionResult<ApiResponse<FleetHealthResponse>>> Get(CancellationToken ct) => ApiResponse<FleetHealthResponse>.Ok(await health.GetAsync(ct));
    [HttpGet("/api/fleet/health/issues"), RequireAnyPermission(FleetPermissions.HealthView, FleetPermissions.HealthManage)] public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetHealthIssue>>>> Issues([FromQuery] string? severity, CancellationToken ct) => ApiResponse<IReadOnlyList<FleetHealthIssue>>.Ok(await health.GetIssuesAsync(severity, ct));
    [HttpGet("/api/fleet/health/outbox"), RequireAnyPermission(FleetPermissions.HealthView, FleetPermissions.HealthManage)] public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetOutboxHealthItem>>>> Outbox(CancellationToken ct) => ApiResponse<IReadOnlyList<FleetOutboxHealthItem>>.Ok(await health.GetOutboxAsync(ct));
    [HttpGet("/api/fleet/diagnostics/permissions"), RequireAnyPermission(FleetPermissions.HealthManage, "SystemSettings.View")] public async Task<ActionResult<ApiResponse<FleetPermissionDiagnostic>>> PermissionDiagnostics(CancellationToken ct) => ApiResponse<FleetPermissionDiagnostic>.Ok(await permissions.GetAsync(ct));
    [HttpGet("/api/fleet/diagnostics/summary"), RequireAnyPermission(FleetPermissions.HealthView, FleetPermissions.HealthManage)] public async Task<ActionResult<ApiResponse<FleetDiagnosticsSummary>>> Summary(CancellationToken ct) { var h = await health.GetAsync(ct); var o = await health.GetOutboxAsync(ct); var r = await rollout.GetAsync(User, ct); var issues = await health.GetIssuesAsync(null, ct); return ApiResponse<FleetDiagnosticsSummary>.Ok(new(r.Mode, r.IsAllowed, h.Status, o.LongCount(x => x.Status is "PENDING" or "RETRY"), o.LongCount(x => x.Status == "FAILED"), o.Sum(x => x.InAppFailed + x.LineFailed), issues.LongCount(x => x.Code == "STUCK_WORKFLOW"), DateTime.UtcNow)); }
    [HttpGet("/api/fleet/diagnostics/outbox"), RequireAnyPermission(FleetPermissions.HealthView, FleetPermissions.HealthManage)] public Task<ActionResult<ApiResponse<IReadOnlyList<FleetOutboxHealthItem>>>> DiagnosticsOutbox(CancellationToken ct) => Outbox(ct);
    [HttpGet("/api/fleet/diagnostics/stuck-workflows"), RequireAnyPermission(FleetPermissions.HealthView, FleetPermissions.HealthManage)] public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetHealthIssue>>>> Stuck(CancellationToken ct) => ApiResponse<IReadOnlyList<FleetHealthIssue>>.Ok((await health.GetIssuesAsync(null, ct)).Where(x => x.Code == "STUCK_WORKFLOW").ToList());
}
