using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController, Route("api/fleet/rollout"), Authorize]
public sealed class FleetRolloutController(AppDbContext db, IFleetRolloutService rollout) : ControllerBase
{
    [HttpGet("access")]
    public async Task<ActionResult<ApiResponse<FleetRolloutSnapshot>>> Access(CancellationToken ct) => ApiResponse<FleetRolloutSnapshot>.Ok(await rollout.GetAsync(User, ct));

    [HttpGet("status"), RequireAnyPermission("SystemSettings.View", FleetPermissions.HealthView, FleetPermissions.HealthManage)]
    public async Task<ActionResult<ApiResponse<FleetRolloutSnapshot>>> Status(CancellationToken ct) => ApiResponse<FleetRolloutSnapshot>.Ok(await rollout.GetAsync(User, ct));

    [HttpPut, RequireAnyPermission("SystemSettings.Manage", FleetPermissions.HealthManage)]
    public async Task<ActionResult<ApiResponse<object>>> Update(UpdateFleetRolloutRequest request, CancellationToken ct)
    {
        if (!FleetRolloutModes.All.Contains(request.Mode)) return BadRequest(ApiResponse<object>.Fail("Invalid rollout mode."));
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<object>.Fail("Reason is required."));
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var row = await db.FleetRolloutSettings.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (row is not null && request.ConcurrencyToken != row.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        var old = row?.Mode ?? "Configuration";
        if (row is null) { row = new FleetRolloutSetting(); db.FleetRolloutSettings.Add(row); }
        row.Mode = FleetRolloutModes.All.Single(x => x.Equals(request.Mode, StringComparison.OrdinalIgnoreCase)); row.UatUserIds = request.UatUserIds.Distinct().ToArray(); row.UatRoleCodes = request.UatRoleCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); row.UpdatedAt = DateTime.UtcNow; row.UpdatedByUserId = actor; row.ConcurrencyToken = Guid.NewGuid();
        db.AuditLogs.Add(new AuditLog { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.RolloutModeChanged", EntityName = "FleetRolloutSetting", EntityId = row.Id.ToString(), OldValue = old, NewValue = row.Mode, Reason = request.Reason.Trim(), CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
        await db.SaveChangesAsync(ct); return ApiResponse<object>.Ok(new { row.Mode, row.UatUserIds, row.UatRoleCodes, row.ConcurrencyToken });
    }
}

public sealed record UpdateFleetRolloutRequest(string Mode, Guid[] UatUserIds, string[] UatRoleCodes, Guid? ConcurrencyToken, string Reason);
