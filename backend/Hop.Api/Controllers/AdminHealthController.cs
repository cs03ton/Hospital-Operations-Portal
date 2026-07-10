using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/admin/health")]
[Authorize]
public class AdminHealthController(
    AppDbContext db,
    IHealthCenterService healthCenterService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminHealthResponse>>> Get(CancellationToken cancellationToken)
    {
        if (!await CanAccessHealthCenter(cancellationToken))
        {
            return Forbid();
        }

        var response = await healthCenterService.GetHealthAsync(cancellationToken);
        return ApiResponse<AdminHealthResponse>.Ok(response);
    }

    private async Task<bool> CanAccessHealthCenter(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .AnyAsync(rolePermission => rolePermission.Permission!.Code == "System.Health.View", cancellationToken);
    }
}
