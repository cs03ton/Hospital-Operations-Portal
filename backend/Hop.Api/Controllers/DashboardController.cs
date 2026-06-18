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
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    [RequirePermission("Dashboard.View")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary()
    {
        var userId = GetCurrentUserId();
        var totalUsers = await db.Users.CountAsync(user => user.IsActive);
        var totalDepartments = await db.Departments.CountAsync(department => department.IsActive);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-1 * (int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(6);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var pendingApprovals = userId is null
            ? 0
            : await db.LeaveRequests.CountAsync(item =>
                item.Status == "Pending" &&
                item.CurrentApproverId == userId);

        var staffOnLeaveToday = await CountDistinctApprovedLeaveUsers(today, today);
        var staffOnLeaveThisWeek = await CountDistinctApprovedLeaveUsers(weekStart, weekEnd);
        var staffOnLeaveThisMonth = await CountDistinctApprovedLeaveUsers(monthStart, monthEnd);

        var myRemainingLeaveDays = userId is null
            ? 0
            : await db.LeaveBalances
                .Where(item => item.UserId == userId && item.Year == today.Year)
                .SumAsync(item => item.EntitledDays - item.UsedDays - item.PendingDays);

        return ApiResponse<DashboardSummaryResponse>.Ok(new DashboardSummaryResponse(
            totalUsers,
            totalDepartments,
            pendingApprovals,
            OpenRepairRequests: 0,
            ActiveBorrowRequests: 0,
            InventoryItems: 0,
            staffOnLeaveToday,
            staffOnLeaveThisWeek,
            staffOnLeaveThisMonth,
            myRemainingLeaveDays
        ));
    }

    private Task<int> CountDistinctApprovedLeaveUsers(DateOnly startDate, DateOnly endDate)
    {
        return db.LeaveRequests
            .Where(item => item.Status == "Approved")
            .Where(item => item.StartDate <= endDate && item.EndDate >= startDate)
            .Select(item => item.UserId)
            .Distinct()
            .CountAsync();
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
