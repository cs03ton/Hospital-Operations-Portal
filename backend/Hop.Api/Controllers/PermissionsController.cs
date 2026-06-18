using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("RoleManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionResponse>>>> GetPermissions()
    {
        var permissions = await db.Permissions
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Action)
            .Select(permission => new PermissionResponse(
                permission.Id,
                permission.Code,
                permission.Name,
                permission.Group,
                permission.Action,
                permission.IsActive
            ))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<PermissionResponse>>.Ok(permissions);
    }
}
