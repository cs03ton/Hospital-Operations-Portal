using Hop.Api.Interfaces;
using Hop.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hop.Api.Tests;

public class CsrfProtectionTests
{
    [Fact]
    public async Task InvokeAsync_AllowsMatchingDoubleSubmitToken()
    {
        var called = false;
        var middleware = new CsrfProtectionMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<CsrfProtectionMiddleware>.Instance);
        var context = CreateContext("POST");
        context.Request.Headers["X-CSRF-TOKEN"] = "token";
        context.Request.Headers.Cookie = "hop_csrf_token=token";

        await middleware.InvokeAsync(context, CreateConfiguration("Cookie"), new NoopAuditLogService());

        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_RejectsMissingTokenInCookieMode()
    {
        var middleware = new CsrfProtectionMiddleware(_ => Task.CompletedTask, NullLogger<CsrfProtectionMiddleware>.Instance);
        var context = CreateContext("POST");

        await middleware.InvokeAsync(context, CreateConfiguration("Cookie"), new NoopAuditLogService());

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SkipsValidationInLocalStorageMode()
    {
        var called = false;
        var middleware = new CsrfProtectionMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<CsrfProtectionMiddleware>.Instance);
        var context = CreateContext("POST");

        await middleware.InvokeAsync(context, CreateConfiguration("LocalStorage"), new NoopAuditLogService());

        Assert.True(called);
    }

    private static DefaultHttpContext CreateContext(string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/api/leave-requests";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static IConfiguration CreateConfiguration(string mode)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:TokenStorageMode"] = mode,
                ["Auth:Cookie:CsrfEnabled"] = "true",
                ["Auth:Cookie:CsrfTokenName"] = "hop_csrf_token",
                ["Auth:Cookie:CsrfHeaderName"] = "X-CSRF-TOKEN"
            })
            .Build();
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }
}
