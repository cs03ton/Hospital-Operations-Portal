using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController, Route("api/fleet/assignments"), Authorize]
public sealed class FleetAssignmentsController(FleetAssignmentReplacementService replacementService) : ControllerBase
{
    [HttpPost("{id:guid}/replace"), RequireAnyPermission(FleetPermissions.DispatchReplaceAssignment, FleetPermissions.DispatchReplaceApprovedAssignment)]
    public async Task<ActionResult<ApiResponse<object>>> Replace(Guid id, FleetAssignmentReplaceRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try
        {
            var result = await replacementService.ReplaceAsync(id, request, actor, HttpContext.TraceIdentifier, ct);
            return ApiResponse<object>.Ok(new { result.NewAssignment.Id, result.NewAssignment.FleetRequestId, result.NewAssignment.VehicleId, result.NewAssignment.DriverUserId, ReplacementOfAssignmentId = result.NewAssignment.ReplacedAssignmentId, result.NewAssignment.AssignmentReason, result.NewAssignment.IsActive, result.NewAssignment.ConcurrencyToken, RequestConcurrencyToken = result.Request.ConcurrencyToken });
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.Fail("Concurrency conflict.")); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
    }
}
