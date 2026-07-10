using System.Security.Claims;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class SelfServicePasswordChangeTests
{
    [Fact]
    public async Task ChangePassword_WithCurrentPassword_UpdatesHashAndRevokesRefreshTokens()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "staff01",
            FullName = "เจ้าหน้าที่ 01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123"),
            IsActive = true
        };
        db.Users.Add(user);
        db.RefreshTokens.AddRange(
            new RefreshToken { UserId = user.Id, Token = "token-1", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = user.Id, Token = "token-2", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow.AddMinutes(-10), RevokedReason = "Logout" });
        await db.SaveChangesAsync();
        var audit = new CaptureAuditLogService();
        var controller = CreateController(db, audit, user.Id);

        var result = await controller.ChangePassword(new ChangePasswordRequest("OldPass@123", "NewPass@123", "NewPass@123"));

        var response = Assert.IsType<ApiResponse<string>>(result.Value);
        Assert.True(response.Success);
        var updatedUser = await db.Users.SingleAsync(item => item.Id == user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass@123", updatedUser.PasswordHash));
        Assert.NotNull(updatedUser.PasswordChangedAt);
        var revokedToken = await db.RefreshTokens.SingleAsync(item => item.Token == "token-1");
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal("Password changed", revokedToken.RevokedReason);
        Assert.Contains(audit.Events, item => item.Action == "User.PasswordChanged" && item.Result == "Success");
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_DoesNotUpdateHash()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "staff01",
            FullName = "เจ้าหน้าที่ 01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var audit = new CaptureAuditLogService();
        var controller = CreateController(db, audit, user.Id);

        var result = await controller.ChangePassword(new ChangePasswordRequest("WrongPass@123", "NewPass@123", "NewPass@123"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("รหัสผ่านปัจจุบันไม่ถูกต้อง", response.Message);
        var updatedUser = await db.Users.SingleAsync(item => item.Id == user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("OldPass@123", updatedUser.PasswordHash));
        Assert.Contains(audit.Events, item => item.Action == "User.PasswordChangeFailed" && item.Result == "Denied");
    }

    [Fact]
    public async Task ChangePassword_WithPolicyViolation_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "staff01",
            FullName = "เจ้าหน้าที่ 01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var controller = CreateController(db, new CaptureAuditLogService(), user.Id);

        var result = await controller.ChangePassword(new ChangePasswordRequest("OldPass@123", "short", "short"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("รหัสผ่านต้องมีความยาวอย่างน้อย", response.Message);
    }

    [Fact]
    public void PasswordPolicyService_DisallowsUsernameInPassword()
    {
        var service = CreatePasswordPolicyService();

        var errors = service.Validate("staff01@Password123", "staff01");

        Assert.Contains("รหัสผ่านต้องไม่มีชื่อผู้ใช้เป็นส่วนหนึ่งของรหัสผ่าน", errors);
    }

    [Fact]
    public async Task ChangePassword_WhenRateLimited_ReturnsTooManyRequests()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "staff01",
            FullName = "เจ้าหน้าที่ 01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var limiter = CreateRateLimiter();
        for (var i = 0; i < 5; i++)
        {
            limiter.RecordFailedAttempt("change-password:staff01", null, DateTime.UtcNow);
        }
        var controller = CreateController(db, new CaptureAuditLogService(), user.Id, limiter);

        var result = await controller.ChangePassword(new ChangePasswordRequest("OldPass@123", "NewPass@123", "NewPass@123"));

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
    }

    private static MeController CreateController(AppDbContext db, IAuditLogService audit, Guid userId, ILoginRateLimiter? limiter = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:TokenStorageMode"] = "LocalStorage"
            })
            .Build();
        var controller = new MeController(db, CreatePasswordPolicyService(), audit, limiter ?? CreateRateLimiter(), configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "TestAuth"))
                }
            }
        };
        return controller;
    }

    private static InMemoryLoginRateLimiter CreateRateLimiter()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoginRateLimit:Enabled"] = "true",
                ["LoginRateLimit:MaxFailedAttempts"] = "5",
                ["LoginRateLimit:WindowMinutes"] = "15",
                ["LoginRateLimit:LockoutMinutes"] = "15"
            })
            .Build();
        return new InMemoryLoginRateLimiter(configuration);
    }

    private static PasswordPolicyService CreatePasswordPolicyService()
    {
        return new PasswordPolicyService(Options.Create(new PasswordPolicyOptions
        {
            MinimumLength = 8,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true,
            DisallowUsername = true
        }));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class CaptureAuditLogService : IAuditLogService
    {
        public List<(Guid? UserId, string Action, string Result)> Events { get; } = [];

        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            Events.Add((userId, action, result));
            return Task.CompletedTask;
        }
    }
}
