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
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.ManageTypes)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveTypeResponse>>>> GetLeaveTypes()
    {
        var items = await db.LeaveTypes
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveTypeResponse>>.Ok(items);
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.ManageTypes)]
    public async Task<ActionResult<ApiResponse<LeaveTypeResponse>>> CreateLeaveType(SaveLeaveTypeRequest request)
    {
        var code = request.Code.Trim();
        if (await db.LeaveTypes.AnyAsync(item => item.Code == code))
        {
            return BadRequest(ApiResponse<LeaveTypeResponse>.Fail("Leave type code already exists."));
        }

        var leaveType = new LeaveType
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description,
            DefaultDaysPerYear = request.DefaultDaysPerYear,
            RequiresBalance = request.RequiresBalance,
            RequiresAttachment = request.RequiresAttachment,
            IsPaid = request.IsPaid,
            IsActive = request.IsActive
        };

        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveType.Create", "LeaveType", leaveType.Id.ToString(), $"Created leave type {leaveType.Code}.", "Success", HttpContext);

        return CreatedAtAction(nameof(GetLeaveTypes), ApiResponse<LeaveTypeResponse>.Ok(ToResponse(leaveType)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(LeavePermissions.ManageTypes)]
    public async Task<ActionResult<ApiResponse<LeaveTypeResponse>>> UpdateLeaveType(Guid id, SaveLeaveTypeRequest request)
    {
        var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveType is null)
        {
            return NotFound(ApiResponse<LeaveTypeResponse>.Fail("Leave type not found."));
        }

        var code = request.Code.Trim();
        if (await db.LeaveTypes.AnyAsync(item => item.Id != id && item.Code == code))
        {
            return BadRequest(ApiResponse<LeaveTypeResponse>.Fail("Leave type code already exists."));
        }

        leaveType.Code = code;
        leaveType.Name = request.Name.Trim();
        leaveType.Description = request.Description;
        leaveType.DefaultDaysPerYear = request.DefaultDaysPerYear;
        leaveType.RequiresBalance = request.RequiresBalance;
        leaveType.RequiresAttachment = request.RequiresAttachment;
        leaveType.IsPaid = request.IsPaid;
        leaveType.IsActive = request.IsActive;
        leaveType.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveType.Update", "LeaveType", leaveType.Id.ToString(), $"Updated leave type {leaveType.Code}.", "Success", HttpContext);

        return ApiResponse<LeaveTypeResponse>.Ok(ToResponse(leaveType));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(LeavePermissions.ManageTypes)]
    public async Task<IActionResult> DeleteLeaveType(Guid id)
    {
        var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveType is null)
        {
            return NotFound(ApiResponse<string>.Fail("Leave type not found."));
        }

        leaveType.IsActive = false;
        leaveType.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveType.Delete", "LeaveType", leaveType.Id.ToString(), $"Deactivated leave type {leaveType.Code}.", "Success", HttpContext);

        return NoContent();
    }

    private static LeaveTypeResponse ToResponse(LeaveType item)
    {
        return new LeaveTypeResponse(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.DefaultDaysPerYear,
            item.RequiresBalance,
            item.RequiresAttachment,
            item.IsPaid,
            item.IsActive
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
