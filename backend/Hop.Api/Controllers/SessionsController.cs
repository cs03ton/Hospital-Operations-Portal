using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
[RequirePermission("SystemSettings.Manage")]
public class SessionsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SessionResponse>>>> GetSessions()
    {
        var sessions = await db.RefreshTokens
            .AsNoTracking()
            .Include(item => item.User)
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .Select(item => new SessionResponse(
                item.Id,
                item.UserId,
                item.User != null ? item.User.Username : null,
                item.User != null ? item.User.FullName : null,
                item.CreatedAt,
                item.ExpiresAt,
                item.RevokedAt,
                item.RevokedReason,
                item.CreatedByIp,
                item.UserAgent,
                item.LastUsedAt,
                item.RevokedAt == null && item.ExpiresAt > DateTime.UtcNow
            ))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<SessionResponse>>.Ok(sessions);
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<string>>> RevokeSession(Guid id)
    {
        var session = await db.RefreshTokens.FirstOrDefaultAsync(item => item.Id == id);
        if (session is null)
        {
            return NotFound(ApiResponse<string>.Fail("Session not found."));
        }

        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = "Revoked by administrator";
            await db.SaveChangesAsync();
            await auditLogService.WriteAsync(GetCurrentUserId(), "Session.Revoke", "RefreshToken", session.Id.ToString(), "Revoked user session.", "Success", HttpContext);
        }

        return ApiResponse<string>.Ok("Session revoked.");
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
