using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("RoleManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleResponse>>>> GetRoles()
    {
        var roles = await db.Roles
            .OrderBy(role => role.Name)
            .Select(role => ToResponse(role))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<RoleResponse>>.Ok(roles);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("RoleManagement.View")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole(Guid id)
    {
        var role = await db.Roles.FirstOrDefaultAsync(item => item.Id == id);
        if (role is null)
        {
            return NotFound(ApiResponse<RoleResponse>.Fail("Role not found."));
        }

        return ApiResponse<RoleResponse>.Ok(ToResponse(role));
    }

    [HttpPost]
    [RequirePermission("RoleManagement.Create")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole(CreateRoleRequest request)
    {
        var name = request.Name.Trim();
        if (await db.Roles.AnyAsync(role => role.Name == name))
        {
            return BadRequest(ApiResponse<RoleResponse>.Fail("Role name already exists."));
        }

        var role = new Role
        {
            Name = name,
            Description = request.Description,
            IsActive = request.IsActive,
            IsSystemRole = false
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Role.Create", "Role", role.Id.ToString(), $"Created role {role.Name}.", "Success", HttpContext);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, ApiResponse<RoleResponse>.Ok(ToResponse(role)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("RoleManagement.Edit")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole(Guid id, UpdateRoleRequest request)
    {
        var role = await db.Roles.FirstOrDefaultAsync(item => item.Id == id);
        if (role is null)
        {
            return NotFound(ApiResponse<RoleResponse>.Fail("Role not found."));
        }

        var name = request.Name.Trim();
        if (await db.Roles.AnyAsync(item => item.Id != id && item.Name == name))
        {
            return BadRequest(ApiResponse<RoleResponse>.Fail("Role name already exists."));
        }

        role.Name = name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;
        role.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Role.Edit", "Role", role.Id.ToString(), $"Updated role {role.Name}.", "Success", HttpContext);

        return ApiResponse<RoleResponse>.Ok(ToResponse(role));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("RoleManagement.Delete")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await db.Roles.FirstOrDefaultAsync(item => item.Id == id);
        if (role is null)
        {
            return NotFound(ApiResponse<string>.Fail("Role not found."));
        }

        if (role.IsSystemRole)
        {
            return BadRequest(ApiResponse<string>.Fail("System roles cannot be deleted."));
        }

        role.IsActive = false;
        role.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Role.Delete", "Role", role.Id.ToString(), $"Deactivated role {role.Name}.", "Success", HttpContext);

        return NoContent();
    }

    [HttpGet("{roleId:guid}/permissions")]
    [RequirePermission("RoleManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionResponse>>>> GetRolePermissions(Guid roleId)
    {
        if (!await db.Roles.AnyAsync(role => role.Id == roleId))
        {
            return NotFound(ApiResponse<IReadOnlyList<PermissionResponse>>.Fail("Role not found."));
        }

        var permissions = await db.RolePermissions
            .Where(item => item.RoleId == roleId)
            .Include(item => item.Permission)
            .Select(item => item.Permission!)
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Action)
            .Select(permission => ToPermissionResponse(permission))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<PermissionResponse>>.Ok(permissions);
    }

    [HttpPut("{roleId:guid}/permissions")]
    [RequirePermission("RoleManagement.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionResponse>>>> UpdateRolePermissions(
        Guid roleId,
        UpdateRolePermissionsRequest request)
    {
        var role = await db.Roles
            .Include(item => item.RolePermissions)
            .FirstOrDefaultAsync(item => item.Id == roleId);

        if (role is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<PermissionResponse>>.Fail("Role not found."));
        }

        var permissionIds = request.PermissionIds.Distinct().ToList();
        var permissions = await db.Permissions
            .Where(permission => permissionIds.Contains(permission.Id) && permission.IsActive)
            .ToListAsync();

        if (permissions.Count != permissionIds.Count)
        {
            return BadRequest(ApiResponse<IReadOnlyList<PermissionResponse>>.Fail("One or more permissions were not found."));
        }

        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Role.PermissionManage", "Role", role.Id.ToString(), $"Updated permissions for role {role.Name}.", "Success", HttpContext);

        return ApiResponse<IReadOnlyList<PermissionResponse>>.Ok(
            permissions
                .OrderBy(permission => permission.Group)
                .ThenBy(permission => permission.Action)
                .Select(ToPermissionResponse)
                .ToList()
        );
    }

    private static RoleResponse ToResponse(Role role)
    {
        return new RoleResponse(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt
        );
    }

    private static PermissionResponse ToPermissionResponse(Permission permission)
    {
        return new PermissionResponse(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Group,
            permission.Action,
            permission.IsActive
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
