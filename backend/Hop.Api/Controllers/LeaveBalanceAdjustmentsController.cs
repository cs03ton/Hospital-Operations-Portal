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
[Route("api/leave-balance-adjustments")]
[Authorize]
public class LeaveBalanceAdjustmentsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceAdjustmentResponse>>>> GetAdjustments([FromQuery] int? year = null)
    {
        var query = db.LeaveBalanceAdjustments
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .Include(item => item.AdjustedByUser)
            .AsQueryable();

        if (year is not null)
        {
            query = query.Where(item => item.Year == year);
        }

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveBalanceAdjustmentResponse>>.Ok(items);
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceAdjustmentResponse>>> CreateAdjustment(CreateLeaveBalanceAdjustmentRequest request)
    {
        var adjustedByUserId = GetCurrentUserId();
        if (adjustedByUserId is null)
        {
            return Unauthorized(ApiResponse<LeaveBalanceAdjustmentResponse>.Fail("Invalid access token."));
        }

        if (!await db.Users.AnyAsync(item => item.Id == request.UserId && item.IsActive))
        {
            return BadRequest(ApiResponse<LeaveBalanceAdjustmentResponse>.Fail("User not found."));
        }

        var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(item => item.Id == request.LeaveTypeId && item.IsActive);
        if (leaveType is null)
        {
            return BadRequest(ApiResponse<LeaveBalanceAdjustmentResponse>.Fail("Leave type not found."));
        }

        if (request.Year < 2000 || request.Year > 2100)
        {
            return BadRequest(ApiResponse<LeaveBalanceAdjustmentResponse>.Fail("Year is invalid."));
        }

        if (request.AdjustmentDays == 0)
        {
            return BadRequest(ApiResponse<LeaveBalanceAdjustmentResponse>.Fail("Adjustment days must not be zero."));
        }

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(item =>
            item.UserId == request.UserId &&
            item.LeaveTypeId == request.LeaveTypeId &&
            item.Year == request.Year);

        if (balance is null)
        {
            balance = new LeaveBalance
            {
                UserId = request.UserId,
                LeaveTypeId = request.LeaveTypeId,
                Year = request.Year,
                EntitledDays = leaveType.DefaultDaysPerYear
            };
            db.LeaveBalances.Add(balance);
        }

        balance.EntitledDays += request.AdjustmentDays;
        balance.UpdatedAt = DateTime.UtcNow;

        var item = new LeaveBalanceAdjustment
        {
            UserId = request.UserId,
            LeaveTypeId = request.LeaveTypeId,
            Year = request.Year,
            AdjustmentDays = request.AdjustmentDays,
            Reason = request.Reason.Trim(),
            AdjustedByUserId = adjustedByUserId.Value
        };

        db.LeaveBalanceAdjustments.Add(item);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(adjustedByUserId, "LeaveBalance.Adjust", "LeaveBalance", balance.Id.ToString(), $"Adjusted leave balance by {request.AdjustmentDays} days.", "Success", HttpContext);

        var created = await db.LeaveBalanceAdjustments
            .AsNoTracking()
            .Include(adjustment => adjustment.User)
            .Include(adjustment => adjustment.LeaveType)
            .Include(adjustment => adjustment.AdjustedByUser)
            .SingleAsync(adjustment => adjustment.Id == item.Id);

        return ApiResponse<LeaveBalanceAdjustmentResponse>.Ok(ToResponse(created));
    }

    private static LeaveBalanceAdjustmentResponse ToResponse(LeaveBalanceAdjustment item)
    {
        return new LeaveBalanceAdjustmentResponse(
            item.Id,
            item.UserId,
            item.User?.FullName,
            item.LeaveTypeId,
            item.LeaveType?.Name,
            item.Year,
            item.AdjustmentDays,
            item.Reason,
            item.AdjustedByUserId,
            item.AdjustedByUser?.FullName,
            item.CreatedAt);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
