using System.Security.Claims;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/me/line")]
[Authorize]
public class MeLineController(ILineUserBindingService lineUserBindingService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<LineMeStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LineMeStatusResponse>.Fail("Invalid access token."));
        }

        return ApiResponse<LineMeStatusResponse>.Ok(await lineUserBindingService.GetMyLineStatusAsync(userId.Value, cancellationToken));
    }

    [HttpPost("connect-token")]
    public async Task<ActionResult<ApiResponse<LineConnectTokenResponse>>> CreateConnectToken(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LineConnectTokenResponse>.Fail("Invalid access token."));
        }

        try
        {
            return ApiResponse<LineConnectTokenResponse>.Ok(await lineUserBindingService.CreateConnectTokenAsync(
                userId.Value,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LineConnectTokenResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("disconnect")]
    public async Task<ActionResult<ApiResponse<LineMeStatusResponse>>> Disconnect(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LineMeStatusResponse>.Fail("Invalid access token."));
        }

        await lineUserBindingService.UnbindAsync(userId.Value, cancellationToken);
        return ApiResponse<LineMeStatusResponse>.Ok(await lineUserBindingService.GetMyLineStatusAsync(userId.Value, cancellationToken));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
