using System.Security.Claims;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(
    AppDbContext db,
    IPasswordPolicyService passwordPolicyService,
    IAuditLogService auditLogService,
    ILoginRateLimiter loginRateLimiter,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("password-policy")]
    public ActionResult<ApiResponse<PasswordPolicyResponse>> GetPasswordPolicy()
    {
        return ApiResponse<PasswordPolicyResponse>.Ok(passwordPolicyService.GetPolicy());
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword(ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<string>.Fail("กรุณาเข้าสู่ระบบใหม่"));
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive);
        if (user is null)
        {
            return Unauthorized(ApiResponse<string>.Fail("กรุณาเข้าสู่ระบบใหม่"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var limiterKey = BuildPasswordChangeLimiterKey(user.Username);
        if (loginRateLimiter.IsLocked(limiterKey, ipAddress, DateTime.UtcNow))
        {
            await auditLogService.WriteAsync(user.Id, "User.PasswordChangeLocked", "User", user.Id.ToString(), "Password change temporarily locked.", "Denied", HttpContext);
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<string>.Fail("เปลี่ยนรหัสผ่านผิดพลาดหลายครั้ง กรุณารอสักครู่แล้วลองใหม่อีกครั้ง"));
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest(ApiResponse<string>.Fail("กรุณากรอกข้อมูลรหัสผ่านให้ครบถ้วน"));
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            loginRateLimiter.RecordFailedAttempt(limiterKey, ipAddress, DateTime.UtcNow);
            await auditLogService.WriteAsync(user.Id, "User.PasswordChangeFailed", "User", user.Id.ToString(), "Current password verification failed.", "Denied", HttpContext);
            return BadRequest(ApiResponse<string>.Fail("รหัสผ่านปัจจุบันไม่ถูกต้อง"));
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(ApiResponse<string>.Fail("รหัสผ่านใหม่และยืนยันรหัสผ่านใหม่ไม่ตรงกัน"));
        }

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            return BadRequest(ApiResponse<string>.Fail("รหัสผ่านใหม่ต้องแตกต่างจากรหัสผ่านเดิม"));
        }

        var policyErrors = passwordPolicyService.Validate(request.NewPassword, user.Username);
        if (policyErrors.Count > 0)
        {
            return BadRequest(ApiResponse<string>.Fail(string.Join(" ", policyErrors)));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var activeTokens = await db.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ToListAsync();
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "Password changed";
            token.LastUsedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        loginRateLimiter.Reset(limiterKey, ipAddress);
        DeleteRefreshTokenCookie();
        DeleteCsrfCookie();

        await auditLogService.WriteAsync(user.Id, "User.PasswordChanged", "User", user.Id.ToString(), $"Password changed and revoked {activeTokens.Count} active refresh token(s).", "Success", HttpContext);

        return ApiResponse<string>.Ok("เปลี่ยนรหัสผ่านเรียบร้อยแล้ว", "เปลี่ยนรหัสผ่านเรียบร้อยแล้ว");
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private void DeleteRefreshTokenCookie()
    {
        if (!ShouldUseCookieTokenStorage())
        {
            return;
        }

        Response.Cookies.Delete(GetRefreshCookieName(), CreateAuthCookieOptions(DateTimeOffset.UtcNow.AddDays(-1), "/api/auth", httpOnly: true));
    }

    private void DeleteCsrfCookie()
    {
        if (!ShouldUseCookieTokenStorage())
        {
            return;
        }

        Response.Cookies.Delete(GetCsrfCookieName(), CreateAuthCookieOptions(DateTimeOffset.UtcNow.AddDays(-1), "/", httpOnly: false));
    }

    private CookieOptions CreateAuthCookieOptions(DateTimeOffset expires, string path, bool httpOnly)
    {
        var domain = configuration["Auth:Cookie:Domain"] ?? configuration["AUTH_COOKIE_DOMAIN"];
        var secureDefault = !HttpContext.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = configuration.GetValue("Auth:Cookie:Secure", configuration.GetValue("AUTH_COOKIE_SECURE", secureDefault)),
            SameSite = ParseSameSite(configuration["Auth:Cookie:SameSite"] ?? configuration["AUTH_COOKIE_SAMESITE"]),
            Expires = expires,
            Path = path,
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain
        };
    }

    private bool ShouldUseCookieTokenStorage()
    {
        var mode = configuration["Auth:TokenStorageMode"] ?? configuration["AUTH_TOKEN_STORAGE_MODE"] ?? "LocalStorage";
        return string.Equals(mode, "Cookie", StringComparison.OrdinalIgnoreCase);
    }

    private string GetRefreshCookieName()
    {
        return configuration["Auth:Cookie:RefreshTokenName"] ?? "hop_refresh_token";
    }

    private string GetCsrfCookieName()
    {
        return configuration["Auth:Cookie:CsrfTokenName"] ?? configuration["AUTH_COOKIE_CSRF_TOKEN_NAME"] ?? "hop_csrf_token";
    }

    private static SameSiteMode ParseSameSite(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "strict" => SameSiteMode.Strict,
            "none" => SameSiteMode.None,
            _ => SameSiteMode.Lax
        };
    }

    private static string BuildPasswordChangeLimiterKey(string username)
    {
        return $"change-password:{username}";
    }
}
