using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/fleet/trips/{tripId:guid}/participants")]
public sealed class FleetTripParticipantsController(
    IFleetTripParticipantService participantService,
    AppDbContext db) : ControllerBase
{
    [HttpPut("actual"), RequirePermission(FleetPermissions.DriverComplete)]
    public async Task<ActionResult<ApiResponse<object>>> FinalizeActualParticipants(
        Guid tripId,
        FinalizeFleetTripParticipantsRequest request,
        CancellationToken ct)
    {
        var actor = CurrentUserId();
        if (actor is null) return Unauthorized(ApiResponse<object>.Fail("Invalid access token."));

        try
        {
            var participants = await participantService.FinalizeEmployeesAsync(tripId, actor.Value, request.EmployeeUserIds, ct);
            db.AuditLogs.Add(new AuditLog
            {
                UserId = actor,
                EffectiveActorUserId = actor,
                Action = "Fleet.TripParticipantsFinalized",
                EntityName = "FleetTripRecord",
                EntityId = tripId.ToString(),
                Detail = $"Actual employee participant count: {participants.Count}",
                CorrelationId = HttpContext.TraceIdentifier,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            await db.SaveChangesAsync(ct);
            return ApiResponse<object>.Ok(new
            {
                TripId = tripId,
                ParticipantCount = participants.Count,
                Participants = participants.Select(x => new { x.UserId, x.IsRequester, x.ParticipantType })
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public sealed record FinalizeFleetTripParticipantsRequest(IReadOnlyList<Guid> EmployeeUserIds);
