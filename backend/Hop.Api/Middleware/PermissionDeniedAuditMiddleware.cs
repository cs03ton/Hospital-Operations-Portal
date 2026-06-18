using System.Security.Claims;
using Hop.Api.Interfaces;

namespace Hop.Api.Middleware;

public sealed class PermissionDeniedAuditMiddleware(RequestDelegate next, ILogger<PermissionDeniedAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        await next(context);

        if (context.Response.StatusCode != StatusCodes.Status403Forbidden)
        {
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : (Guid?)null;
        var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value;
        logger.LogWarning("Permission denied for user {UserId} on {Method} {Path}. Endpoint={Endpoint}", userId, context.Request.Method, context.Request.Path, endpoint);

        await auditLogService.WriteAsync(
            userId,
            "Authorization.Denied",
            "Authorization",
            endpoint,
            $"{context.Request.Method} {context.Request.Path}",
            "Denied",
            context);
    }
}
