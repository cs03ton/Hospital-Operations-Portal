using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    [RequirePermission("LeaveManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceResponse>>>> GetMyBalances([FromQuery] int? year = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Fail("Invalid access token."));
        }

        return ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Ok(await LoadBalances(userId.Value, year ?? DateTime.UtcNow.Year));
    }

    [HttpGet("user/{userId:guid}")]
    [RequirePermission("LeaveManagement.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceResponse>>>> GetUserBalances(Guid userId, [FromQuery] int? year = null)
    {
        if (!await db.Users.AnyAsync(item => item.Id == userId))
        {
            return NotFound(ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Fail("User not found."));
        }

        return ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Ok(await LoadBalances(userId, year ?? DateTime.UtcNow.Year));
    }

    private async Task<IReadOnlyList<LeaveBalanceResponse>> LoadBalances(Guid userId, int year)
    {
        var leaveTypes = await db.LeaveTypes
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync();

        var balances = await db.LeaveBalances
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Year == year)
            .ToListAsync();

        return leaveTypes.Select(leaveType =>
        {
            var balance = balances.FirstOrDefault(item => item.LeaveTypeId == leaveType.Id);
            var entitled = balance?.EntitledDays ?? leaveType.DefaultDaysPerYear;
            var used = balance?.UsedDays ?? 0;
            var pending = balance?.PendingDays ?? 0;

            return new LeaveBalanceResponse(
                balance?.Id,
                userId,
                leaveType.Id,
                leaveType.Name,
                year,
                entitled,
                used,
                pending,
                entitled - used - pending
            );
        }).ToList();
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
