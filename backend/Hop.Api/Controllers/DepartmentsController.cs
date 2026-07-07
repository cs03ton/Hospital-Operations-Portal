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
public class DepartmentsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("DepartmentManagement.View")]
    public async Task<ActionResult<ApiResponse<object>>> GetDepartments(
        int? page = null,
        int pageSize = 20,
        string? search = null,
        string? sort = "name",
        string? direction = "asc",
        string? status = null)
    {
        var query = db.Departments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(department =>
                department.Name.ToLower().Contains(keyword) ||
                (department.Description != null && department.Description.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var active = status.Equals("active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(department => department.IsActive == active);
        }

        query = ApplyDepartmentSorting(query, sort, direction);

        if (page is null)
        {
            var departments = await query.ToListAsync();
            var items = new List<DepartmentDto>();
            foreach (var department in departments)
            {
                items.Add(await ToDtoAsync(department));
            }

            return ApiResponse<object>.Ok(items);
        }

        var currentPage = Math.Max(1, page.Value);
        var currentPageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = await query.CountAsync();
        var pageItems = await query
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToListAsync();
        var resultItems = new List<DepartmentDto>();
        foreach (var department in pageItems)
        {
            resultItems.Add(await ToDtoAsync(department));
        }

        return ApiResponse<object>.Ok(new PagedResponse<DepartmentDto>(
            resultItems,
            currentPage,
            currentPageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)currentPageSize)));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("DepartmentManagement.View")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(Guid id)
    {
        var department = await db.Departments.FirstOrDefaultAsync(item => item.Id == id);
        if (department is null)
        {
            return NotFound(ApiResponse<DepartmentDto>.Fail("Department not found."));
        }

        return ApiResponse<DepartmentDto>.Ok(await ToDtoAsync(department));
    }

    [HttpPost]
    [RequirePermission("DepartmentManagement.Create")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(CreateDepartmentRequest request)
    {
        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(department => department.Name == name))
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Department name already exists."));
        }

        var department = new Department
        {
            Name = name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Department.Create", "Department", department.Id.ToString(), $"Created department {department.Name}.", "Success", HttpContext);

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, ApiResponse<DepartmentDto>.Ok(await ToDtoAsync(department)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("DepartmentManagement.Edit")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(Guid id, UpdateDepartmentRequest request)
    {
        var department = await db.Departments.FirstOrDefaultAsync(item => item.Id == id);
        if (department is null)
        {
            return NotFound(ApiResponse<DepartmentDto>.Fail("Department not found."));
        }

        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(item => item.Id != id && item.Name == name))
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Department name already exists."));
        }

        department.Name = name;
        department.Description = request.Description;
        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Department.Edit", "Department", department.Id.ToString(), $"Updated department {department.Name}.", "Success", HttpContext);

        return ApiResponse<DepartmentDto>.Ok(await ToDtoAsync(department));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("DepartmentManagement.Delete")]
    public async Task<ActionResult<ApiResponse<DeleteResultResponse>>> DeleteDepartment(Guid id)
    {
        var department = await db.Departments.FirstOrDefaultAsync(item => item.Id == id);
        if (department is null)
        {
            return NotFound(ApiResponse<string>.Fail("Department not found."));
        }

        var references = await GetDepartmentDeleteReferences(id);
        if (references.Any(item => item.Count > 0))
        {
            return Conflict(ApiResponse<DeleteResultResponse>.Ok(new DeleteResultResponse(
                "Blocked",
                "ไม่สามารถลบหน่วยงานได้ เนื่องจากมีข้อมูลอ้างอิงในระบบ",
                references)));
        }

        db.Departments.Remove(department);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Department.Delete", "Department", department.Id.ToString(), $"Deleted department {department.Name}.", "Success", HttpContext);

        return ApiResponse<DeleteResultResponse>.Ok(new DeleteResultResponse(
            "Deleted",
            "ลบหน่วยงานเรียบร้อยแล้ว",
            references));
    }

    private async Task<DepartmentDto> ToDtoAsync(Department department)
    {
        var usersCount = await db.Users.CountAsync(item => item.DepartmentId == department.Id);
        return new DepartmentDto(
            department.Id,
            department.Name,
            department.Description,
            department.IsActive,
            department.CreatedAt,
            department.UpdatedAt,
            usersCount
        );
    }

    private IQueryable<Department> ApplyDepartmentSorting(IQueryable<Department> query, string? sort, string? direction)
    {
        var descending = direction?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true;
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.Trim().ToLowerInvariant();

        return (normalizedSort, descending) switch
        {
            ("code", true) => query.OrderByDescending(department => department.Id),
            ("code", false) => query.OrderBy(department => department.Id),
            ("name", true) => query.OrderByDescending(department => department.Name),
            ("name", false) => query.OrderBy(department => department.Name),
            ("userscount", true) or ("users", true) => query.OrderByDescending(department => db.Users.Count(user => user.DepartmentId == department.Id)),
            ("userscount", false) or ("users", false) => query.OrderBy(department => db.Users.Count(user => user.DepartmentId == department.Id)),
            ("created", true) or ("createdat", true) => query.OrderByDescending(department => department.CreatedAt),
            ("created", false) or ("createdat", false) => query.OrderBy(department => department.CreatedAt),
            _ => descending ? query.OrderByDescending(department => department.Name) : query.OrderBy(department => department.Name)
        };
    }

    private async Task<IReadOnlyList<DeleteReferenceSummary>> GetDepartmentDeleteReferences(Guid id)
    {
        return
        [
            new DeleteReferenceSummary("Users", await db.Users.CountAsync(item => item.DepartmentId == id)),
            new DeleteReferenceSummary("Approval Chains", await db.ApprovalChains.CountAsync(item => item.DepartmentId == id)),
            new DeleteReferenceSummary("Approval Escalation Rules", await db.ApprovalEscalationRules.CountAsync(item => item.DepartmentId == id)),
            new DeleteReferenceSummary("Leave Requests", await db.LeaveRequests.CountAsync(item => item.User != null && item.User.DepartmentId == id)),
            new DeleteReferenceSummary("Audit Logs", await db.AuditLogs.CountAsync(item => item.EntityName == "Department" && item.EntityId == id.ToString()))
        ];
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
