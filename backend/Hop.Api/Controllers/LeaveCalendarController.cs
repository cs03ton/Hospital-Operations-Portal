using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-calendar")]
[Authorize]
public class LeaveCalendarController(AppDbContext db, ILeaveRequestAccessService leaveRequestAccessService) : ControllerBase
{
    [HttpGet]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.SupportViewAll)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveCalendarItemResponse>>>> GetCalendar(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? leaveTypeId,
        [FromQuery] string? status)
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;
        var startDate = new DateOnly(selectedYear, selectedMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var currentUserId = GetCurrentUserId();
        var visibility = await leaveRequestAccessService.GetVisibilityAsync(currentUserId);
        var query = leaveRequestAccessService.ApplyVisibility(db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Where(item => item.StartDate <= endDate && item.EndDate >= startDate), currentUserId, visibility);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status.Trim());
        }
        else
        {
            query = query.Where(item => item.Status != "Draft");
        }

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
                item.DurationType,
                item.TotalDays,
                item.Status
            ))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveCalendarItemResponse>>.Ok(items);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
