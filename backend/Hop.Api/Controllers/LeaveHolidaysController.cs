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
[Route("api/leave-holidays")]
[Authorize]
public class LeaveHolidaysController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("LeaveHoliday.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveHolidayResponse>>>> GetHolidays([FromQuery] int? year = null)
    {
        var query = db.LeaveHolidays.AsNoTracking();
        if (year is not null)
        {
            query = query.Where(item => item.HolidayDate.Year == year);
        }

        var items = await query
            .OrderBy(item => item.HolidayDate)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveHolidayResponse>>.Ok(items);
    }

    [HttpPost]
    [RequirePermission("LeaveHoliday.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveHolidayResponse>>> CreateHoliday(SaveLeaveHolidayRequest request)
    {
        if (await db.LeaveHolidays.AnyAsync(item => item.HolidayDate == request.HolidayDate))
        {
            return BadRequest(ApiResponse<LeaveHolidayResponse>.Fail("Holiday date already exists."));
        }

        var item = new LeaveHoliday
        {
            HolidayDate = request.HolidayDate,
            Name = request.Name.Trim(),
            IsActive = request.IsActive
        };

        db.LeaveHolidays.Add(item);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.Create", "LeaveHoliday", item.Id.ToString(), $"Created holiday {item.Name}.", "Success", HttpContext);

        return ApiResponse<LeaveHolidayResponse>.Ok(ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("LeaveHoliday.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveHolidayResponse>>> UpdateHoliday(Guid id, SaveLeaveHolidayRequest request)
    {
        var item = await db.LeaveHolidays.FirstOrDefaultAsync(holiday => holiday.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<LeaveHolidayResponse>.Fail("Holiday not found."));
        }

        if (await db.LeaveHolidays.AnyAsync(holiday => holiday.Id != id && holiday.HolidayDate == request.HolidayDate))
        {
            return BadRequest(ApiResponse<LeaveHolidayResponse>.Fail("Holiday date already exists."));
        }

        item.HolidayDate = request.HolidayDate;
        item.Name = request.Name.Trim();
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.Edit", "LeaveHoliday", item.Id.ToString(), $"Updated holiday {item.Name}.", "Success", HttpContext);

        return ApiResponse<LeaveHolidayResponse>.Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("LeaveHoliday.Manage")]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        var item = await db.LeaveHolidays.FirstOrDefaultAsync(holiday => holiday.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<string>.Fail("Holiday not found."));
        }

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.Delete", "LeaveHoliday", item.Id.ToString(), $"Deactivated holiday {item.Name}.", "Success", HttpContext);

        return NoContent();
    }

    private static LeaveHolidayResponse ToResponse(LeaveHoliday item)
    {
        return new LeaveHolidayResponse(
            item.Id,
            item.HolidayDate,
            item.Name,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
