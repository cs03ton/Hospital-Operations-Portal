using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("RoleManagement.View")]
    public async Task<ActionResult<ApiResponse<object>>> GetPermissions(
        int? page = null,
        int pageSize = 20,
        string? search = null,
        string? sort = "code",
        string? direction = "asc",
        string? module = null,
        string? status = null)
    {
        var query = db.Permissions
            .Include(permission => permission.RolePermissions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(permission =>
                permission.Code.ToLower().Contains(keyword) ||
                permission.Name.ToLower().Contains(keyword) ||
                permission.Group.ToLower().Contains(keyword) ||
                permission.Action.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(permission => permission.Group == module);
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var active = status.Equals("active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(permission => permission.IsActive == active);
        }

        query = ApplyPermissionSorting(query, sort, direction);

        if (page is null)
        {
            var permissions = await query.ToListAsync();
            return ApiResponse<object>.Ok(permissions.Select(ToResponse).ToList());
        }

        var currentPage = Math.Max(1, page.Value);
        var currentPageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToListAsync();

        return ApiResponse<object>.Ok(new PagedResponse<PermissionResponse>(
            items.Select(ToResponse).ToList(),
            currentPage,
            currentPageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)currentPageSize)));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("RoleManagement.Delete")]
    public async Task<ActionResult<ApiResponse<DeleteResultResponse>>> DeletePermission(Guid id)
    {
        var permission = await db.Permissions
            .Include(item => item.RolePermissions)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (permission is null)
        {
            return NotFound(ApiResponse<string>.Fail("Permission not found."));
        }

        var references = new[]
        {
            new DeleteReferenceSummary("Roles", await db.RolePermissions.CountAsync(item => item.PermissionId == id))
        };

        if (references.Any(item => item.Count > 0))
        {
            return Conflict(ApiResponse<DeleteResultResponse>.Ok(new DeleteResultResponse(
                "Blocked",
                "ไม่สามารถลบสิทธิ์ได้ เนื่องจากมีบทบาทใช้งานอยู่",
                references)));
        }

        permission.IsActive = false;
        permission.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Permission.SoftDelete", "Permission", permission.Id.ToString(), $"Soft deleted permission {permission.Code}.", "Success", HttpContext);

        return ApiResponse<DeleteResultResponse>.Ok(new DeleteResultResponse(
            "SoftDeleted",
            "ปิดใช้งานสิทธิ์เรียบร้อยแล้ว",
            references));
    }

    private static PermissionResponse ToResponse(Models.Permission permission)
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
            permission.UpdatedAt);
    }

    private static IQueryable<Models.Permission> ApplyPermissionSorting(IQueryable<Models.Permission> query, string? sort, string? direction)
    {
        var descending = direction?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true;
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "code" : sort.Trim().ToLowerInvariant();

        return (normalizedSort, descending) switch
        {
            ("permission", true) or ("code", true) => query.OrderByDescending(permission => permission.Code),
            ("permission", false) or ("code", false) => query.OrderBy(permission => permission.Code),
            ("module", true) or ("group", true) => query.OrderByDescending(permission => permission.Group).ThenBy(permission => permission.Code),
            ("module", false) or ("group", false) => query.OrderBy(permission => permission.Group).ThenBy(permission => permission.Code),
            ("created", true) or ("createdat", true) => query.OrderByDescending(permission => permission.CreatedAt),
            ("created", false) or ("createdat", false) => query.OrderBy(permission => permission.CreatedAt),
            ("roles", true) or ("rolescount", true) => query.OrderByDescending(permission => permission.RolePermissions.Count),
            ("roles", false) or ("rolescount", false) => query.OrderBy(permission => permission.RolePermissions.Count),
            _ => descending ? query.OrderByDescending(permission => permission.Code) : query.OrderBy(permission => permission.Code)
        };
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
