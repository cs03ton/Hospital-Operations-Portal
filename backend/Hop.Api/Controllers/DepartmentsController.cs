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
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> GetDepartments()
    {
        var departments = await db.Departments
            .OrderBy(department => department.Name)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(departments.Select(ToDto).ToList());
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

        return ApiResponse<DepartmentDto>.Ok(ToDto(department));
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

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, ApiResponse<DepartmentDto>.Ok(ToDto(department)));
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

        return ApiResponse<DepartmentDto>.Ok(ToDto(department));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("DepartmentManagement.Delete")]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var department = await db.Departments.FirstOrDefaultAsync(item => item.Id == id);
        if (department is null)
        {
            return NotFound(ApiResponse<string>.Fail("Department not found."));
        }

        var userCount = await db.Users.CountAsync(item => item.DepartmentId == id);
        var approvalChainCount = await db.ApprovalChains.CountAsync(item => item.DepartmentId == id);
        var escalationRuleCount = await db.ApprovalEscalationRules.CountAsync(item => item.DepartmentId == id);
        if (userCount > 0 || approvalChainCount > 0 || escalationRuleCount > 0)
        {
            return Conflict(ApiResponse<string>.Fail(
                $"ไม่สามารถลบหน่วยงานนี้ได้ เนื่องจากมีข้อมูลที่ผูกอยู่ ผู้ใช้งาน {userCount} รายการ, กฎการอนุมัติ {approvalChainCount} รายการ, กฎ escalation {escalationRuleCount} รายการ"));
        }

        db.Departments.Remove(department);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "Department.Delete", "Department", department.Id.ToString(), $"Deleted department {department.Name}.", "Success", HttpContext);

        return NoContent();
    }

    private static DepartmentDto ToDto(Department department)
    {
        return new DepartmentDto(
            department.Id,
            department.Name,
            department.Description,
            department.IsActive,
            department.CreatedAt,
            department.UpdatedAt
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
