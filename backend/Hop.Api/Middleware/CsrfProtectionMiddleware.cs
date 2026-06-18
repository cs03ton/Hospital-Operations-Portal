using System.Security.Claims;
using Hop.Api.Interfaces;

namespace Hop.Api.Middleware;

public sealed class CsrfProtectionMiddleware(RequestDelegate next, ILogger<CsrfProtectionMiddleware> logger)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IAuditLogService auditLogService)
    {
        if (!ShouldValidate(context, configuration))
        {
            await next(context);
            return;
        }

        var cookieName = configuration["Auth:Cookie:CsrfTokenName"] ?? configuration["AUTH_COOKIE_CSRF_TOKEN_NAME"] ?? "hop_csrf_token";
        var headerName = configuration["Auth:Cookie:CsrfHeaderName"] ?? configuration["AUTH_COOKIE_CSRF_HEADER_NAME"] ?? "X-CSRF-TOKEN";
        var cookieToken = context.Request.Cookies[cookieName];
        var headerToken = context.Request.Headers[headerName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(cookieToken) &&
            !string.IsNullOrWhiteSpace(headerToken) &&
            string.Equals(cookieToken, headerToken, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : (Guid?)null;
        logger.LogWarning(
            "CSRF validation failed for {Method} {Path}. HasCookie={HasCookie} HasHeader={HasHeader}",
            context.Request.Method,
            context.Request.Path,
            !string.IsNullOrWhiteSpace(cookieToken),
            !string.IsNullOrWhiteSpace(headerToken));
        await auditLogService.WriteAsync(userId, "Security.CsrfValidationFailed", "Auth", null, $"{context.Request.Method} {context.Request.Path}", "Denied", context);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid CSRF token." });
    }

    private static bool ShouldValidate(HttpContext context, IConfiguration configuration)
    {
        var mode = configuration["Auth:TokenStorageMode"] ?? configuration["AUTH_TOKEN_STORAGE_MODE"] ?? "LocalStorage";
        var enabled = configuration.GetValue("Auth:Cookie:CsrfEnabled", configuration.GetValue("AUTH_COOKIE_CSRF_ENABLED", true));

        if (!enabled ||
            !string.Equals(mode, "Cookie", StringComparison.OrdinalIgnoreCase) ||
            SafeMethods.Contains(context.Request.Method))
        {
            return false;
        }

        return !context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase);
    }
}
