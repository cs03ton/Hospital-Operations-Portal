using System.Text.Json;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class AdminHealthAndErrorTests
{
    [Fact]
    public void AdminHealthController_RequiresSystemSettingsViewPermission()
    {
        var attribute = typeof(AdminHealthController)
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
            .Cast<RequirePermissionAttribute>()
            .Single();

        Assert.Equal("SystemSettings.View", attribute.PermissionCode);
    }

    [Fact]
    public async Task Get_ReturnsSafeHealthPayloadWithoutSecrets()
    {
        await using var db = CreateDbContext("health-safe");
        var storagePath = Path.Combine(Path.GetTempPath(), $"hop-health-{Guid.NewGuid():N}");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = storagePath,
                ["Line:Enabled"] = "true",
                ["Line:AccessToken"] = "secret-token-value",
                ["Line:ChannelSecret"] = "secret-channel-value"
            })
            .Build();
        var controller = new AdminHealthController(
            db,
            configuration,
            new TestWebHostEnvironment("Production"),
            new LineConfigurationResolver(Options.Create(new LineOptions()), configuration));

        var result = await controller.Get(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<AdminHealthResponse>>(result.Value);
        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain("secret-token-value", json);
        Assert.DoesNotContain("secret-channel-value", json);
        Assert.Equal("Healthy", response.Data?.Api.Status);
        Assert.NotNull(response.Data?.CurrentTimeServer);

        if (Directory.Exists(storagePath))
        {
            Directory.Delete(storagePath, recursive: true);
        }
    }

    [Fact]
    public async Task Get_WhenDatabaseCheckFails_ReturnsUnhealthyDatabaseStatus()
    {
        var db = CreateDbContext("health-db-fail");
        await db.DisposeAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = Path.GetTempPath(),
                ["Line:Enabled"] = "false"
            })
            .Build();
        var controller = new AdminHealthController(
            db,
            configuration,
            new TestWebHostEnvironment("Production"),
            new LineConfigurationResolver(Options.Create(new LineOptions()), configuration));

        var result = await controller.Get(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<AdminHealthResponse>>(result.Value);
        Assert.Equal("Unhealthy", response.Data?.Database.Status);
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

    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"{name}-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
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
