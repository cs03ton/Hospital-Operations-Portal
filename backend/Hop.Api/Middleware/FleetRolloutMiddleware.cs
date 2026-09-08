using Hop.Api.DTOs;
using Hop.Api.Services;

namespace Hop.Api.Middleware;

public sealed class FleetRolloutMiddleware(RequestDelegate next, ILogger<FleetRolloutMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IFleetRolloutService rollout)
    {
        if (!context.Request.Path.StartsWithSegments("/api/fleet") || context.Request.Path.StartsWithSegments("/api/fleet/rollout"))
        {
            await next(context); return;
        }
        var state = await rollout.GetAsync(context.User, context.RequestAborted);
        if (state.IsAllowed) { await next(context); return; }
        logger.LogWarning("Fleet rollout denied. CorrelationId={CorrelationId} ActorUserId={ActorUserId} Mode={RolloutMode} Path={Path}", context.TraceIdentifier, context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, state.Mode, context.Request.Path.Value);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(state.Reason), context.RequestAborted);
    }
}
