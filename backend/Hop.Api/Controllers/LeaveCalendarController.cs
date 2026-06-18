using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-calendar")]
[Authorize]
public class LeaveCalendarController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("LeaveManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveCalendarItemResponse>>>> GetCalendar(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? leaveTypeId)
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;
        var startDate = new DateOnly(selectedYear, selectedMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Where(item => item.Status == "Pending" || item.Status == "Approved")
            .Where(item => item.StartDate <= endDate && item.EndDate >= startDate);

        if (departmentId is not null)
        {
            query = query.Where(item => item.User != null && item.User.DepartmentId == departmentId);
        }

        if (leaveTypeId is not null)
        {
            query = query.Where(item => item.LeaveTypeId == leaveTypeId);
        }

        var items = await query
            .OrderBy(item => item.StartDate)
            .Select(item => new LeaveCalendarItemResponse(
                item.Id,
                item.UserId,
                item.User != null ? item.User.FullName : null,
                item.User != null ? item.User.DepartmentId : null,
                item.User != null && item.User.Department != null ? item.User.Department.Name : null,
                item.LeaveTypeId,
                item.LeaveType != null ? item.LeaveType.Name : null,
                item.StartDate,
                item.EndDate,
                item.TotalDays,
                item.Status
            ))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveCalendarItemResponse>>.Ok(items);
    }
}
