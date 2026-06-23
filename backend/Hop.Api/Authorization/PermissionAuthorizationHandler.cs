using System.Security.Claims;
using Hop.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Authorization;

public sealed class PermissionAuthorizationHandler(AppDbContext db)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var hasPermission = await db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId && userRole.Role != null && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .AnyAsync(rolePermission =>
                rolePermission.Permission != null &&
                rolePermission.Permission.IsActive &&
                requirement.PermissionCodes.Contains(rolePermission.Permission.Code));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
