using System.Security.Claims;
using System.Security.Cryptography;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, IJwtTokenService jwtTokenService, IAuditLogService auditLogService, ILoginRateLimiter loginRateLimiter, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
    {
        var username = request.Username.Trim();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (loginRateLimiter.IsLocked(username, ipAddress, DateTime.UtcNow))
        {
            await auditLogService.WriteAsync(null, "Auth.LoginLocked", "Auth", null, $"Login temporarily locked for username: {username}", "Denied", HttpContext);
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<LoginResponse>.Fail("เข้าสู่ระบบผิดพลาดหลายครั้ง กรุณารอสักครู่แล้วลองใหม่อีกครั้ง"));
        }

        var user = await LoadUserQuery()
            .FirstOrDefaultAsync(item => item.Username == username);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            loginRateLimiter.RecordFailedAttempt(username, ipAddress, DateTime.UtcNow);
            await auditLogService.WriteAsync(null, "Auth.LoginFailed", "Auth", null, $"Failed login for username: {username}", "Failed", HttpContext);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password."));
        }

        loginRateLimiter.Reset(username, ipAddress);

        var roleName = GetRoleName(user);
        var accessToken = jwtTokenService.GenerateAccessToken(user, roleName);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(user.Id, "Auth.LoginSuccess", "Auth", user.Id.ToString(), "User logged in.", "Success", HttpContext);
        AppendRefreshTokenCookie(refreshTokenValue);
        AppendCsrfCookie();

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            ShouldUseCookieTokenStorage() ? string.Empty : refreshTokenValue,
            ToAuthUserDto(user, roleName)
        ));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken(RefreshTokenRequest request)
    {
        var refreshToken = GetRefreshToken(request.RefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid refresh token."));
        }

        var storedToken = await db.RefreshTokens
            .Include(token => token.User)!
                .ThenInclude(user => user!.Department)
            .Include(token => token.User)!
                .ThenInclude(user => user!.UserRoles)
                .ThenInclude(userRole => userRole.Role)!
                    .ThenInclude(role => role!.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(token => token.Token == refreshToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null || !storedToken.User.IsActive)
        {
            if (storedToken is not null && storedToken.UserId != Guid.Empty &&
                HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetValue("RefreshToken:ReuseDetectionEnabled", true))
            {
                var activeTokens = await db.RefreshTokens
                    .Where(token => token.UserId == storedToken.UserId && token.RevokedAt == null)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedReason = "Refresh token reuse detected";
                }

                await db.SaveChangesAsync();
                await auditLogService.WriteAsync(storedToken.UserId, "Auth.RefreshTokenReuseDetected", "RefreshToken", storedToken.Id.ToString(), "Revoked active sessions after refresh token reuse.", "Denied", HttpContext);
            }

            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid refresh token."));
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedReason = "Rotated";
        storedToken.LastUsedAt = DateTime.UtcNow;

        var roleName = GetRoleName(storedToken.User);
        var newRefreshTokenValue = jwtTokenService.GenerateRefreshToken();
        storedToken.ReplacedByToken = newRefreshTokenValue;
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });

        await db.SaveChangesAsync();
        AppendRefreshTokenCookie(newRefreshTokenValue);
        AppendCsrfCookie();

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(
            jwtTokenService.GenerateAccessToken(storedToken.User, roleName),
            ShouldUseCookieTokenStorage() ? string.Empty : newRefreshTokenValue,
            ToAuthUserDto(storedToken.User, roleName)
        ));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> Logout(LogoutRequest request)
    {
        var refreshToken = GetRefreshToken(request.RefreshToken);
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var storedToken = await db.RefreshTokens
                .FirstOrDefaultAsync(token => token.Token == refreshToken);

            if (storedToken is not null && storedToken.RevokedAt is null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                storedToken.RevokedReason = "Logout";
                storedToken.LastUsedAt = DateTime.UtcNow;
            }
        }

        var userId = GetCurrentUserId();
        await db.SaveChangesAsync();
        DeleteRefreshTokenCookie();
        DeleteCsrfCookie();
        await auditLogService.WriteAsync(userId, "Auth.Logout", "Auth", userId?.ToString(), "User logged out.", "Success", HttpContext);

        return ApiResponse<string>.Ok("Logged out.");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Me()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<AuthUserDto>.Fail("Invalid access token."));
        }

        var user = await LoadUserQuery().FirstOrDefaultAsync(item => item.Id == userId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<AuthUserDto>.Fail("User not found."));
        }

        return ApiResponse<AuthUserDto>.Ok(ToAuthUserDto(user, GetRoleName(user)));
    }

    private IQueryable<User> LoadUserQuery()
    {
        return db.Users
            .Include(user => user.Department)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)!
                    .ThenInclude(role => role!.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission);
    }

    private static string GetRoleName(User user)
    {
        return user.UserRoles
            .Select(userRole => userRole.Role?.Name)
            .FirstOrDefault(role => !string.IsNullOrWhiteSpace(role)) ?? "Staff";
    }

    private static AuthUserDto ToAuthUserDto(User user, string roleName)
    {
        return new AuthUserDto(
            user.Id,
            user.FullName,
            user.Username,
            roleName,
            user.Department?.Name,
            user.UserRoles
                .Where(userRole => userRole.Role?.IsActive == true)
                .SelectMany(userRole => userRole.Role!.RolePermissions)
                .Where(rolePermission => rolePermission.Permission?.IsActive == true)
                .Select(rolePermission => rolePermission.Permission!.Code)
                .Distinct()
                .OrderBy(code => code)
                .ToList()
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private string? GetRefreshToken(string? requestRefreshToken)
    {
        if (!string.IsNullOrWhiteSpace(requestRefreshToken))
        {
            return requestRefreshToken;
        }

        return Request.Cookies[GetRefreshCookieName()];
    }

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        if (!ShouldUseCookieTokenStorage())
        {
            return;
        }

        Response.Cookies.Append(GetRefreshCookieName(), refreshToken, CreateRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));
    }

    private void AppendCsrfCookie()
    {
        if (!ShouldUseCookieTokenStorage())
        {
            return;
        }

        Response.Cookies.Append(GetCsrfCookieName(), GenerateCsrfToken(), CreateCsrfCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));
    }

    private void DeleteRefreshTokenCookie()
    {
        if (!ShouldUseCookieTokenStorage())
        {
            return;
        }

        Response.Cookies.Delete(GetRefreshCookieName(), CreateRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
    }

    private void DeleteCsrfCookie()
    {
        if (!ShouldUseCookieTokenStorage())
        {
            return;
        }

        Response.Cookies.Delete(GetCsrfCookieName(), CreateCsrfCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
    }

    private CookieOptions CreateRefreshCookieOptions(DateTimeOffset expires)
    {
        var domain = configuration["Auth:Cookie:Domain"] ?? configuration["AUTH_COOKIE_DOMAIN"];
        var secureDefault = !HttpContext.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("Auth:Cookie:Secure", configuration.GetValue("AUTH_COOKIE_SECURE", secureDefault)),
            SameSite = ParseSameSite(configuration["Auth:Cookie:SameSite"] ?? configuration["AUTH_COOKIE_SAMESITE"]),
            Expires = expires,
            Path = "/api/auth",
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain
        };
    }

    private CookieOptions CreateCsrfCookieOptions(DateTimeOffset expires)
    {
        var domain = configuration["Auth:Cookie:Domain"] ?? configuration["AUTH_COOKIE_DOMAIN"];
        var secureDefault = !HttpContext.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        return new CookieOptions
        {
            HttpOnly = false,
            Secure = configuration.GetValue("Auth:Cookie:Secure", configuration.GetValue("AUTH_COOKIE_SECURE", secureDefault)),
            SameSite = ParseSameSite(configuration["Auth:Cookie:SameSite"] ?? configuration["AUTH_COOKIE_SAMESITE"]),
            Expires = expires,
            Path = "/",
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

    private static string GenerateCsrfToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
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

}
