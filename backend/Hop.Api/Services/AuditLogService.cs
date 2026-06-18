using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;

namespace Hop.Api.Services;

public sealed class AuditLogService(AppDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task WriteAsync(
        Guid? userId,
        string action,
        string resource,
        string? resourceId,
        string? detail,
        string result = "Success",
        HttpContext? httpContext = null)
    {
        var context = httpContext ?? httpContextAccessor.HttpContext;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = resource,
            EntityId = resourceId,
            Detail = detail,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            Result = result
        });

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Audit event recorded: {Action} {Resource} {ResourceId} {Result} UserId={UserId} IpAddress={IpAddress}",
            action,
            resource,
            resourceId,
            result,
            userId,
            context?.Connection.RemoteIpAddress?.ToString());
    }
}
