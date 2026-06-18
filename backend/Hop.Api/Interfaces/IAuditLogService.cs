namespace Hop.Api.Interfaces;

public interface IAuditLogService
{
    Task WriteAsync(
        Guid? userId,
        string action,
        string resource,
        string? resourceId,
        string? detail,
        string result = "Success",
        HttpContext? httpContext = null);
}
