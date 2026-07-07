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
    public async Task<ActionResult<ApiResponse<object>>> GetRoles(
        int? page = null,
        int pageSize = 20,
        string? search = null,
        string? sort = "name",
        string? direction = "asc",
        string? status = null)
    {
        var query = db.Roles
            .Include(role => role.UserRoles)
            .Include(role => role.RolePermissions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(role =>
                role.Name.ToLower().Contains(keyword) ||
                (role.Description != null && role.Description.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var active = status.Equals("active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(role => role.IsActive == active);
        }

        query = ApplyRoleSorting(query, sort, direction);

        if (page is null)
        {
            var roles = await query.ToListAsync();
            return ApiResponse<object>.Ok(roles.Select(ToResponse).ToList());
        }

        var currentPage = Math.Max(1, page.Value);
        var currentPageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToListAsync();

        return ApiResponse<object>.Ok(new PagedResponse<RoleResponse>(
            items.Select(ToResponse).ToList(),
            currentPage,
            currentPageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)currentPageSize)));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("RoleManagement.View")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole(Guid id)
    {
        var role = await db.Roles
            .Include(item => item.UserRoles)
            .Include(item => item.RolePermissions)
            .FirstOrDefaultAsync(item => item.Id == id);
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
    public async Task<ActionResult<ApiResponse<DeleteResultResponse>>> DeleteRole(Guid id)
    {
        var role = await db.Roles
            .Include(item => item.UserRoles)
            .Include(item => item.RolePermissions)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (role is null)
        {
            return NotFound(ApiResponse<string>.Fail("Role not found."));
        }

        if (role.IsSystemRole || role.Name is "SuperAdmin" or "Admin")
        {
            return Conflict(ApiResponse<DeleteResultResponse>.Fail("ไม่สามารถลบบทบาทระบบหรือบทบาทหลักของระบบได้"));
        }

        var references = await GetRoleDeleteReferences(id);
        if (references.Any(item => item.Label == "Users" && item.Count > 0))
        {
            return Conflict(ApiResponse<DeleteResultResponse>.Ok(new DeleteResultResponse(
                "Blocked",
                "ไม่สามารถลบบทบาทได้ เนื่องจากมีผู้ใช้งานผูกอยู่",
                references)));
        }

        role.IsActive = false;
        role.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Role.SoftDelete", "Role", role.Id.ToString(), $"Soft deleted role {role.Name}.", "Success", HttpContext);

        return ApiResponse<DeleteResultResponse>.Ok(new DeleteResultResponse(
            "SoftDeleted",
            "ปิดใช้งานบทบาทเรียบร้อยแล้ว",
            references));
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
            role.UpdatedAt,
            role.UserRoles.Count,
            role.RolePermissions.Count
        );
    }

    private IQueryable<Role> ApplyRoleSorting(IQueryable<Role> query, string? sort, string? direction)
    {
        var descending = direction?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true;
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.Trim().ToLowerInvariant();

        return (normalizedSort, descending) switch
        {
            ("role", true) or ("name", true) => query.OrderByDescending(role => role.Name),
            ("role", false) or ("name", false) => query.OrderBy(role => role.Name),
            ("users", true) or ("userscount", true) => query.OrderByDescending(role => role.UserRoles.Count),
            ("users", false) or ("userscount", false) => query.OrderBy(role => role.UserRoles.Count),
            ("permissions", true) or ("permissionscount", true) => query.OrderByDescending(role => role.RolePermissions.Count),
            ("permissions", false) or ("permissionscount", false) => query.OrderBy(role => role.RolePermissions.Count),
            ("created", true) or ("createdat", true) => query.OrderByDescending(role => role.CreatedAt),
            ("created", false) or ("createdat", false) => query.OrderBy(role => role.CreatedAt),
            _ => descending ? query.OrderByDescending(role => role.Name) : query.OrderBy(role => role.Name)
        };
    }

    private async Task<IReadOnlyList<DeleteReferenceSummary>> GetRoleDeleteReferences(Guid id)
    {
        return
        [
            new DeleteReferenceSummary("Users", await db.UserRoles.CountAsync(item => item.RoleId == id)),
            new DeleteReferenceSummary("Permissions", await db.RolePermissions.CountAsync(item => item.RoleId == id)),
            new DeleteReferenceSummary("Approval Chain Steps", await db.ApprovalChainSteps.CountAsync(item => item.ApproverRoleId == id)),
            new DeleteReferenceSummary("Approval Escalation Rules", await db.ApprovalEscalationRules.CountAsync(item => item.EscalateToRoleId == id))
        ];
    }

    private static PermissionResponse ToPermissionResponse(Permission permission)
    {
        return new PermissionResponse(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Group,
            permission.Action,
            permission.IsActive,
            permission.RolePermissions.Count,
            permission.CreatedAt,
            permission.UpdatedAt
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
