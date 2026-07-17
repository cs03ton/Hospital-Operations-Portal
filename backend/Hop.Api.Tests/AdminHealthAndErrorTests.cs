using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Middleware;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Xunit;

namespace Hop.Api.Tests;

public class AdminHealthAndErrorTests
{
    [Fact]
    public async Task Get_AdminRoleCanAccessHealthCenter()
    {
        await using var db = CreateDbContext("health-admin-access");
        var storagePath = Path.Combine(Path.GetTempPath(), $"hop-health-{Guid.NewGuid():N}");
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = storagePath,
            ["Line:Enabled"] = "false"
        });
        var controller = new AdminHealthController(db, CreateService(db, configuration, "Production"));
        controller.ControllerContext = CreateControllerContext(Guid.NewGuid(), "Admin");

        var result = await controller.Get(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<AdminHealthResponse>>(result.Value);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data?.Memory);
        Assert.NotNull(response.Data?.Cpu);
        SafeDeleteDirectory(storagePath);
    }

    [Fact]
    public async Task Get_StaffWithoutSystemHealthPermissionReturnsForbid()
    {
        await using var db = CreateDbContext("health-staff-forbid");
        var userId = Guid.NewGuid();
        await SeedUserWithRole(db, userId, "Staff", withHealthPermission: false);
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = Path.GetTempPath(),
            ["Line:Enabled"] = "false"
        });
        var controller = new AdminHealthController(db, CreateService(db, configuration, "Production"));
        controller.ControllerContext = CreateControllerContext(userId, "Staff");

        var result = await controller.Get(CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsSafeHealthPayloadWithoutSecrets()
    {
        await using var db = CreateDbContext("health-safe");
        var storagePath = Path.Combine(Path.GetTempPath(), $"hop-health-{Guid.NewGuid():N}");
        var accessToken = $"line-access-{Guid.NewGuid():N}";
        var channelSecret = $"line-secret-{Guid.NewGuid():N}";
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = storagePath,
            ["Line:Enabled"] = "true",
            ["Line:AccessToken"] = accessToken,
            ["Line:ChannelSecret"] = channelSecret
        });
        var service = CreateService(db, configuration, "Production");

        var response = await service.GetHealthAsync(CancellationToken.None);

        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain(accessToken, json);
        Assert.DoesNotContain(channelSecret, json);
        Assert.Equal("Healthy", response.Api.Status);
        Assert.NotNull(response.Queue);
        Assert.NotNull(response.Memory);
        Assert.NotNull(response.Cpu);

        SafeDeleteDirectory(storagePath);
    }

    [Fact]
    public async Task Get_WhenDatabaseCheckFails_ReturnsUnhealthyDatabaseStatus()
    {
        var db = CreateDbContext("health-db-fail");
        await db.DisposeAsync();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = Path.GetTempPath(),
            ["Line:Enabled"] = "false"
        });
        var service = CreateService(db, configuration, "Production");

        var response = await service.GetHealthAsync(CancellationToken.None);

        Assert.Equal("Unhealthy", response.Database.Status);
    }

    [Fact]
    public async Task Get_ReturnsQueueHealthWithoutSecrets()
    {
        await using var db = CreateDbContext("health-queue");
        db.LineDeliveryLogs.Add(new Models.LineDeliveryLog
        {
            EventName = "LeaveApprovalFlexCardSent",
            Status = "Queued",
            Payload = "{}",
            NextRetryAt = DateTime.UtcNow.AddMinutes(-1)
        });
        db.LineDeliveryLogs.Add(new Models.LineDeliveryLog
        {
            EventName = "Line.TestMessageFailed",
            Status = "Failed",
            Payload = "{}",
            ResponseDetail = "HTTP 400",
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = Path.GetTempPath(),
            ["Line:Enabled"] = "false",
            ["LineRetry:Enabled"] = "true",
            ["ApprovalEscalation:Enabled"] = "false"
        });
        var service = CreateService(db, configuration, "Production");

        var response = await service.GetHealthAsync(CancellationToken.None);

        Assert.Equal("Warning", response.Queue.Status);
        Assert.True(response.Queue.LineRetryEnabled);
        Assert.Equal(1, response.Queue.PendingLineDeliveries);
        Assert.Equal(1, response.Queue.FailedLineDeliveries);
        Assert.Equal(2, response.Queue.PendingRetries);
    }

    [Fact]
    public async Task CheckLine_WhenEnabledButMissingToken_ReturnsUnhealthyWithoutSecretValues()
    {
        await using var db = CreateDbContext("health-line-missing-token");
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = Path.GetTempPath(),
            ["Line:Enabled"] = "true"
        });
        var service = CreateService(db, configuration, "Production");

        var line = await service.CheckLineAsync(CancellationToken.None);

        Assert.Equal("Unhealthy", line.Status);
        Assert.False(line.HasAccessToken);
        Assert.False(line.HasChannelSecret);
    }

    [Fact]
    public void CheckBackup_UsesPostgresDirectoryAsDatabaseBackupSource()
    {
        using var db = CreateDbContext("health-backup-postgres");
        var backupRoot = Path.Combine(Path.GetTempPath(), $"hop-backup-root-{Guid.NewGuid():N}");
        var postgresDirectory = Path.Combine(backupRoot, "postgres");
        var storageDirectory = Path.Combine(backupRoot, "storage");
        Directory.CreateDirectory(postgresDirectory);
        Directory.CreateDirectory(storageDirectory);
        var backupFile = Path.Combine(postgresDirectory, "hopdb_20260717_104612.backup");
        File.WriteAllText(backupFile, "postgres backup");
        File.WriteAllText(Path.Combine(storageDirectory, "hop_uploads_20260717_104612.tar.gz"), "storage backup");

        try
        {
            var configuration = CreateConfiguration(new Dictionary<string, string?>
            {
                ["Backup:RootPath"] = backupRoot,
                ["Line:Enabled"] = "false"
            });
            var service = CreateService(db, configuration, "Production");

            var backup = service.CheckBackup();

            Assert.Equal("Healthy", backup.Status);
            Assert.Equal(postgresDirectory, backup.BackupDirectory);
            Assert.Equal("hopdb_20260717_104612.backup", backup.LatestBackupFile);
            Assert.Contains("postgres/hopdb_20260717_104612.backup", backup.Message);
        }
        finally
        {
            SafeDeleteDirectory(backupRoot);
        }
    }

    [Fact]
    public void CheckStorage_WhenPathIsNotWritable_ReturnsUnhealthy()
    {
        var storageFile = Path.Combine(Path.GetTempPath(), $"hop-storage-file-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(storageFile, "not a directory");
        try
        {
            using var db = CreateDbContext("health-storage-unhealthy");
            var configuration = CreateConfiguration(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = storageFile,
                ["Line:Enabled"] = "false"
            });
            var service = CreateService(db, configuration, "Production");

            var storage = service.CheckStorage();

            Assert.Equal("Unhealthy", storage.Status);
            Assert.False(storage.Writable);
        }
        finally
        {
            if (File.Exists(storageFile))
            {
                File.Delete(storageFile);
            }
        }
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_ProductionResponseIncludesReferenceIdAndNoStackTrace()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("sensitive stack detail"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            new TestWebHostEnvironment("Production"));
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-test-001"
        };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<SafeErrorResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("trace-test-001", payload?.ReferenceId);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("sensitive stack detail", body);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_UsesIncomingHeaderAsTraceIdentifierAndResponseHeader()
    {
        var middleware = new CorrelationIdMiddleware(
            context =>
            {
                Assert.Equal("test-hop-001", context.TraceIdentifier);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "test-hop-001";

        await middleware.InvokeAsync(context);

        Assert.Equal("test-hop-001", context.TraceIdentifier);
        Assert.Equal("test-hop-001", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"{name}-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static HealthCenterService CreateService(AppDbContext db, IConfiguration configuration, string environmentName)
    {
        return new HealthCenterService(
            db,
            configuration,
            new TestWebHostEnvironment(environmentName),
            new LineConfigurationResolver(Options.Create(new LineOptions()), configuration));
    }

    private static ControllerContext CreateControllerContext(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private static async Task SeedUserWithRole(AppDbContext db, Guid userId, string roleName, bool withHealthPermission)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            IsActive = true
        };
        var user = new User
        {
            Id = userId,
            Username = $"user-{Guid.NewGuid():N}",
            FullName = "Test User",
            PasswordHash = "hash",
            IsActive = true
        };
        db.Users.Add(user);
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });

        if (withHealthPermission)
        {
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = "System.Health.View",
                Name = "ดู Health Center",
                Group = "System",
                Action = "HealthView"
            };
            db.Permissions.Add(permission);
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission });
        }

        await db.SaveChangesAsync();
    }

    private static void SafeDeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Hop.Api.Tests";
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
