using System.Security.Cryptography;
using System.Security.Claims;
using System.Net;
using System.Text;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class LineWebhookControllerTests
{
    private const string ChannelSecret = "test-line-channel-secret";

    [Fact]
    public void Receive_Endpoint_AllowsAnonymousAccess()
    {
        var controllerAttributes = typeof(LineWebhookController).GetCustomAttributes(inherit: true);
        var actionAttributes = typeof(LineWebhookController)
            .GetMethod(nameof(LineWebhookController.Receive))!
            .GetCustomAttributes(inherit: true);

        Assert.Contains(controllerAttributes, attribute => attribute is AllowAnonymousAttribute);
        Assert.Contains(actionAttributes, attribute => attribute is AllowAnonymousAttribute);
    }

    [Fact]
    public async Task Receive_WithValidSignatureAndEmptyEvents_ReturnsOk()
    {
        var body = "{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}";
        var controller = CreateController(body, Sign(body), new FakeLineUserBindingService());

        var result = await controller.Receive(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<IReadOnlyList<LineWebhookHandleResult>>>(result.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data!);
    }

    [Fact]
    public async Task Receive_WithValidSignatureAndFollowEvent_ReturnsOkAndProcessesEvent()
    {
        var body = """
        {"destination":"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx","events":[{"type":"follow","source":{"type":"user","userId":"U12345678901234567890123456789012"}}]}
        """;
        var service = new FakeLineUserBindingService();
        var controller = CreateController(body, Sign(body), service);

        var result = await controller.Receive(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<IReadOnlyList<LineWebhookHandleResult>>>(result.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!);
        Assert.Equal(1, service.FollowCount);
    }

    [Fact]
    public async Task Receive_WithInvalidSignature_ReturnsUnauthorized()
    {
        var body = "{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}";
        var controller = CreateController(body, "invalid-signature", new FakeLineUserBindingService());

        var result = await controller.Receive(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Receive_WithMissingSignature_ReturnsUnauthorized()
    {
        var body = "{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}";
        var controller = CreateController(body, null, new FakeLineUserBindingService());

        var result = await controller.Receive(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Receive_WithMalformedJsonAndValidSignature_ReturnsBadRequest()
    {
        var body = "{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[";
        var controller = CreateController(body, Sign(body), new FakeLineUserBindingService());

        var result = await controller.Receive(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PostWebhook_WithoutJwtAndValidSignature_ReturnsOk()
    {
        await using var factory = new LineWebhookApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var body = "{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}";
        using var request = CreateSignedWebhookRequest(body);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithoutSignature_ReturnsUnauthorized()
    {
        await using var factory = new LineWebhookApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent("{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}", Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidSignature_ReturnsUnauthorized()
    {
        await using var factory = new LineWebhookApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent("{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", Convert.ToBase64String("invalid-signature"u8.ToArray()));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithValidSignatureAndEmptyEvents_DoesNotRedirect()
    {
        await using var factory = new LineWebhookApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var body = "{\"destination\":\"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\",\"events\":[]}";
        using var request = CreateSignedWebhookRequest(body);

        var response = await client.SendAsync(request);

        Assert.False((int)response.StatusCode >= 300 && (int)response.StatusCode < 400);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedEndpoint_WithoutJwt_StillReturnsUnauthorized()
    {
        await using var factory = new LineWebhookApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static LineWebhookController CreateController(string body, string? signature, ILineUserBindingService service)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:ChannelSecret"] = ChannelSecret
            })
            .Build();
        var resolver = new LineConfigurationResolver(
            Options.Create(new LineOptions { ChannelSecret = ChannelSecret }),
            configuration);
        var controller = new LineWebhookController(
            resolver,
            service,
            NullLogger<LineWebhookController>.Instance);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        if (signature is not null)
        {
            context.Request.Headers["X-Line-Signature"] = signature;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        return controller;
    }

    private static HttpRequestMessage CreateSignedWebhookRequest(string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", Sign(body));
        return request;
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ChannelSecret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private sealed class LineWebhookApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=hop_line_webhook_test;Username=hop;Password=hop",
                    ["Jwt:Key"] = "line-webhook-test-secret-key-32-chars",
                    ["Jwt:Issuer"] = "Hop.Api.Tests",
                    ["Jwt:Audience"] = "Hop.Client.Tests",
                    ["Line:Enabled"] = "true",
                    ["Line:ChannelSecret"] = ChannelSecret,
                    ["Auth:TokenStorageMode"] = "LocalStorage",
                    ["FileScan:Enabled"] = "false",
                    ["Database:SeedOnStartup"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"hop-line-webhook-{Guid.NewGuid()}"));
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ILineUserBindingService>();
                services.AddScoped<ILineUserBindingService, FakeLineUserBindingService>();
            });
        }
    }

    private sealed class FakeLineUserBindingService : ILineUserBindingService
    {
        public int FollowCount { get; private set; }

        public Task<LineConnectTokenResponse> CreateConnectTokenAsync(Guid userId, string? ipAddress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LinePairingCodeResponse> CreatePairingCodeAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LineBindingStatusResponse> GetMyBindingStatusAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LineMeStatusResponse> GetMyLineStatusAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LineWebhookHandleResult> HandleFollowAsync(string lineUserId, CancellationToken cancellationToken = default)
        {
            FollowCount++;
            return Task.FromResult(new LineWebhookHandleResult("follow", "U1234...9012", "Pending", false, "ok"));
        }

        public Task<LineWebhookHandleResult> HandleMessageAsync(string lineUserId, string messageText, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LineWebhookHandleResult("message", "U1234...9012", "Ignored", false, "ok"));
        }

        public Task<LineWebhookHandleResult> HandleUnfollowAsync(string lineUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LineWebhookHandleResult("unfollow", "U1234...9012", "Unbound", false, "ok"));
        }

        public Task<LineBindingStatusResponse> UnbindAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
