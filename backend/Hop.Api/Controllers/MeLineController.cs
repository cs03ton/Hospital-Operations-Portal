using System.Security.Claims;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/me/line")]
[Authorize]
public class MeLineController(
    ILineUserBindingService lineUserBindingService,
    ILineLiffAuthenticationService lineLiffAuthenticationService,
    IAuditLogService auditLogService) : ControllerBase
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

    [HttpPost("link")]
    public async Task<ActionResult<ApiResponse<LineMeStatusResponse>>> Link(LineLinkRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LineMeStatusResponse>.Fail("Invalid access token."));
        }

        try
        {
            var identity = await lineLiffAuthenticationService.VerifyIdTokenAsync(request.IdToken, cancellationToken);
            var status = await lineUserBindingService.LinkVerifiedIdentityAsync(userId.Value, identity, cancellationToken);
            return ApiResponse<LineMeStatusResponse>.Ok(status, "เชื่อมบัญชี LINE สำเร็จ");
        }
        catch (UnauthorizedAccessException ex)
        {
            await auditLogService.WriteAsync(userId.Value, "Auth.LineTokenRejected", "LineUserBinding", null, ex.Message, "Denied", HttpContext);
            return Unauthorized(ApiResponse<LineMeStatusResponse>.Fail("LINE_TOKEN_INVALID"));
        }
        catch (InvalidOperationException ex) when (ex.Message is "LINE_LINK_CONFLICT" or "LINE_USER_ALREADY_LINKED")
        {
            await auditLogService.WriteAsync(userId.Value, "User.LineLinkConflict", "LineUserBinding", null, ex.Message, "Denied", HttpContext);
            return Conflict(ApiResponse<LineMeStatusResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message == "LINE_SERVICE_UNAVAILABLE")
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<LineMeStatusResponse>.Fail("LINE_SERVICE_UNAVAILABLE"));
        }
    }

    [HttpDelete("unlink")]
    public async Task<ActionResult<ApiResponse<LineMeStatusResponse>>> Unlink(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LineMeStatusResponse>.Fail("Invalid access token."));
        }

        await lineUserBindingService.UnbindAsync(userId.Value, cancellationToken);
        return ApiResponse<LineMeStatusResponse>.Ok(await lineUserBindingService.GetMyLineStatusAsync(userId.Value, cancellationToken), "ยกเลิกการเชื่อมบัญชี LINE แล้ว");
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
