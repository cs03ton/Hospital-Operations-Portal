using System.Text.Json;
using Hop.Api.DTOs;

namespace Hop.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request was cancelled by the client. TraceId={TraceId} Method={Method} Path={Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (Exception ex)
        {
            var referenceId = context.TraceIdentifier;
            logger.LogError(ex, "Unhandled exception. ReferenceId={ReferenceId}", referenceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var message = environment.IsDevelopment()
                ? $"เกิดข้อผิดพลาด: {ex.Message}"
                : "เกิดข้อผิดพลาด กรุณาติดต่อผู้ดูแลระบบ";

            if (environment.IsDevelopment())
            {
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    new
                    {
                        message,
                        referenceId,
                        detail = ex.ToString()
                    },
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    context.RequestAborted);
                return;
            }

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                new SafeErrorResponse(message, referenceId),
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
    }
}
