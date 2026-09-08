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

[ApiController, Route("api/fleet/delegations"), Authorize]
public sealed class FleetDelegationsController(AppDbContext db, IWorkflowPermissionValidator permissions) : ControllerBase
{
    private static readonly HashSet<string> Allowed = [FleetPermissions.AdminReviewApprove, FleetPermissions.AdminReviewReturn, FleetPermissions.AdminReviewReject, FleetPermissions.DirectorApprove, FleetPermissions.DirectorReturn, FleetPermissions.DirectorReject];

    [HttpGet, RequireAnyPermission(FleetPermissions.DelegationView, FleetPermissions.DelegationManage)]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct)
    {
        var rows = await db.ApprovalDelegations.AsNoTracking().Where(x => x.Scope == "FLEET").OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, DelegatorUserId = x.ApproverUserId, DelegatorName = x.ApproverUser!.FullName, x.DelegateUserId, DelegateName = x.DelegateUser!.FullName, x.Scope, x.RequiredPermissionCode, x.StartAt, x.EndAt, x.IsActive, x.Reason, x.ConcurrencyToken }).ToListAsync(ct);
        return ApiResponse<object>.Ok(rows);
    }

    [HttpPost, RequirePermission(FleetPermissions.DelegationManage)]
    public async Task<ActionResult<ApiResponse<object>>> Create(FleetDelegationSaveRequest body, CancellationToken ct)
    {
        var error = await Validate(body, null, ct); if (error is not null) return error;
        var entity = new ApprovalDelegation { ApproverUserId = body.DelegatorUserId, DelegateUserId = body.DelegateUserId, Scope = "FLEET", RequiredPermissionCode = body.RequiredPermissionCode, StartAt = body.StartAt, EndAt = body.EndAt, StartDate = DateOnly.FromDateTime(body.StartAt), EndDate = DateOnly.FromDateTime(body.EndAt.AddTicks(-1)), IsActive = body.IsActive, Reason = body.Reason.Trim(), CreatedByUserId = Actor() };
        db.ApprovalDelegations.Add(entity); Audit("Fleet.DelegationCreated", entity, null, entity.RequiredPermissionCode, entity.Reason); await db.SaveChangesAsync(ct);
        return ApiResponse<object>.Ok(new { entity.Id, DelegatorUserId = entity.ApproverUserId, entity.DelegateUserId, entity.Scope, entity.RequiredPermissionCode, entity.StartAt, entity.EndAt, entity.IsActive, entity.ConcurrencyToken });
    }

    [HttpPut("{id:guid}"), RequirePermission(FleetPermissions.DelegationManage)]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, FleetDelegationSaveRequest body, CancellationToken ct)
    {
        var entity = await db.ApprovalDelegations.SingleOrDefaultAsync(x => x.Id == id && x.Scope == "FLEET", ct); if (entity is null) return NotFound(ApiResponse<object>.Fail("Fleet delegation not found."));
        if (body.ConcurrencyToken != entity.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict."));
        var error = await Validate(body, id, ct); if (error is not null) return error;
        var old = entity.RequiredPermissionCode; entity.ApproverUserId = body.DelegatorUserId; entity.DelegateUserId = body.DelegateUserId; entity.RequiredPermissionCode = body.RequiredPermissionCode; entity.StartAt = body.StartAt; entity.EndAt = body.EndAt; entity.StartDate = DateOnly.FromDateTime(body.StartAt); entity.EndDate = DateOnly.FromDateTime(body.EndAt.AddTicks(-1)); entity.IsActive = body.IsActive; entity.Reason = body.Reason.Trim(); entity.UpdatedAt = DateTime.UtcNow; entity.ConcurrencyToken = Guid.NewGuid();
        Audit("Fleet.DelegationUpdated", entity, old, entity.RequiredPermissionCode, entity.Reason); await db.SaveChangesAsync(ct); return ApiResponse<object>.Ok(new { entity.Id, entity.ConcurrencyToken });
    }

    [HttpDelete("{id:guid}"), RequirePermission(FleetPermissions.DelegationManage)]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid concurrencyToken, CancellationToken ct)
    {
        var entity = await db.ApprovalDelegations.SingleOrDefaultAsync(x => x.Id == id && x.Scope == "FLEET", ct); if (entity is null) return NotFound(); if (entity.ConcurrencyToken != concurrencyToken) return Conflict(ApiResponse<object>.Fail("Concurrency conflict.")); entity.IsActive = false; entity.CancelledAt = DateTime.UtcNow; entity.UpdatedAt = DateTime.UtcNow; entity.ConcurrencyToken = Guid.NewGuid(); Audit("Fleet.DelegationDeactivated", entity, "ACTIVE", "INACTIVE", entity.Reason); await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<ActionResult<ApiResponse<object>>?> Validate(FleetDelegationSaveRequest x, Guid? excludeId, CancellationToken ct)
    {
        if (!Allowed.Contains(x.RequiredPermissionCode)) return BadRequest(ApiResponse<object>.Fail("Permission is not allowed for Fleet delegation."));
        if (x.DelegatorUserId == x.DelegateUserId || x.StartAt.Kind != DateTimeKind.Utc || x.EndAt.Kind != DateTimeKind.Utc || x.EndAt <= x.StartAt || string.IsNullOrWhiteSpace(x.Reason)) return BadRequest(ApiResponse<object>.Fail("Invalid delegation data."));
        if (!await permissions.HasAsync(x.DelegateUserId, x.RequiredPermissionCode, ct)) return BadRequest(ApiResponse<object>.Fail("Delegate does not have the required permission."));
        var overlap = await db.ApprovalDelegations.AnyAsync(d => d.Id != excludeId && d.IsActive && d.Scope == "FLEET" && d.ApproverUserId == x.DelegatorUserId && d.RequiredPermissionCode == x.RequiredPermissionCode && d.StartAt < x.EndAt && d.EndAt > x.StartAt, ct);
        if (overlap) return Conflict(ApiResponse<object>.Fail("Overlapping delegation exists."));
        return null;
    }
    private void Audit(string action, ApprovalDelegation d, string? oldValue, string? newValue, string? reason) => db.AuditLogs.Add(new AuditLog { UserId = Actor(), Action = action, EntityName = "ApprovalDelegation", EntityId = d.Id.ToString(), OldValue = oldValue, NewValue = newValue, Reason = reason, CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
    private Guid? Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
