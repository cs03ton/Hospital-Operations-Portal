using System.Security.Claims;
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
public class AuthController(AppDbContext db, IJwtTokenService jwtTokenService, IAuditLogService auditLogService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
    {
        var username = request.Username.Trim();
        var user = await LoadUserQuery()
            .FirstOrDefaultAsync(item => item.Username == username);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await auditLogService.WriteAsync(null, "Auth.LoginFailed", "Auth", null, $"Failed login for username: {username}", "Failed", HttpContext);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password."));
        }

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

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            refreshTokenValue,
            ToAuthUserDto(user, roleName)
        ));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken(RefreshTokenRequest request)
    {
        var storedToken = await db.RefreshTokens
            .Include(token => token.User)!
                .ThenInclude(user => user!.Department)
            .Include(token => token.User)!
                .ThenInclude(user => user!.UserRoles)
                .ThenInclude(userRole => userRole.Role)!
                    .ThenInclude(role => role!.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken);

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

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(
            jwtTokenService.GenerateAccessToken(storedToken.User, roleName),
            newRefreshTokenValue,
            ToAuthUserDto(storedToken.User, roleName)
        ));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> Logout(LogoutRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var storedToken = await db.RefreshTokens
                .FirstOrDefaultAsync(token => token.Token == request.RefreshToken);

            if (storedToken is not null && storedToken.RevokedAt is null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                storedToken.RevokedReason = "Logout";
                storedToken.LastUsedAt = DateTime.UtcNow;
            }
        }

        var userId = GetCurrentUserId();
        await db.SaveChangesAsync();
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

}
