using System.Security.Claims;
using Hop.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Authorization;

// A still-valid JWT must not let a disabled account operate repair endpoints.
public sealed class RepairActiveUserAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!Guid.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive, context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
            return;
        }
        await next();
    }
}
