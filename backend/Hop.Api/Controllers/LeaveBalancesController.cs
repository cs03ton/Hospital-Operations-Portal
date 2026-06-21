using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hop.Api.Services;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("LeaveBalance.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceResponse>>>> GetBalances(
        [FromQuery] int? year = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? leaveTypeId = null)
    {
        var query = db.LeaveBalances
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .AsQueryable();

        if (year is not null)
        {
            query = query.Where(item => item.Year == year.Value);
        }

        if (userId is not null)
        {
            query = query.Where(item => item.UserId == userId.Value);
        }

        if (leaveTypeId is not null)
        {
            query = query.Where(item => item.LeaveTypeId == leaveTypeId.Value);
        }

        var rows = await query
            .OrderBy(item => item.User!.FullName)
            .ThenBy(item => item.LeaveType!.Name)
            .ThenByDescending(item => item.Year)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Ok(rows);
    }

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

    [HttpGet("import-template")]
    [RequirePermission("LeaveBalance.Manage")]
    public IActionResult DownloadImportTemplate()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "ตัวอย่างนำเข้ายอดวันลา" },
            new[] { "username", "leaveTypeCode", "year", "entitledDays", "usedDays", "pendingDays" },
            new[] { "staff.it01", "AnnualLeave", DateTime.UtcNow.Year.ToString(), "10", "0", "0" },
            new[] { "staff.it01", "SickLeave", DateTime.UtcNow.Year.ToString(), "30", "0", "0" },
            Array.Empty<string>(),
            new[] { "หมายเหตุ: username และ leaveTypeCode ต้องตรงกับข้อมูลในระบบ" }
        };

        var bytes = SimpleXlsxWriter.CreateWorkbook(rows, [22, 22, 12, 18, 14, 16]);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leave-balance-import-template.xlsx");
    }

    [HttpPost]
    [RequirePermission("LeaveBalance.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveBalanceResponse>>> CreateBalance(SaveLeaveBalanceRequest request)
    {
        var validation = await ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        if (await db.LeaveBalances.AnyAsync(item =>
            item.UserId == request.UserId &&
            item.LeaveTypeId == request.LeaveTypeId &&
            item.Year == request.Year))
        {
            return Conflict(ApiResponse<LeaveBalanceResponse>.Fail("Leave balance already exists."));
        }

        var balance = new LeaveBalance
        {
            UserId = request.UserId,
            LeaveTypeId = request.LeaveTypeId,
            Year = request.Year,
            EntitledDays = request.EntitledDays,
            UsedDays = request.UsedDays,
            PendingDays = request.PendingDays
        };

        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.Create", "LeaveBalance", balance.Id.ToString(), $"Created leave balance for {request.Year}.", "Success", HttpContext);

        var response = await LoadBalance(balance.Id);
        return CreatedAtAction(nameof(GetBalances), new { id = balance.Id }, ApiResponse<LeaveBalanceResponse>.Ok(response!));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("LeaveBalance.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveBalanceResponse>>> UpdateBalance(Guid id, SaveLeaveBalanceRequest request)
    {
        var balance = await db.LeaveBalances.FirstOrDefaultAsync(item => item.Id == id);
        if (balance is null)
        {
            return NotFound(ApiResponse<LeaveBalanceResponse>.Fail("Leave balance not found."));
        }

        var validation = await ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var duplicate = await db.LeaveBalances.AnyAsync(item =>
            item.Id != id &&
            item.UserId == request.UserId &&
            item.LeaveTypeId == request.LeaveTypeId &&
            item.Year == request.Year);
        if (duplicate)
        {
            return Conflict(ApiResponse<LeaveBalanceResponse>.Fail("Leave balance already exists."));
        }

        balance.UserId = request.UserId;
        balance.LeaveTypeId = request.LeaveTypeId;
        balance.Year = request.Year;
        balance.EntitledDays = request.EntitledDays;
        balance.UsedDays = request.UsedDays;
        balance.PendingDays = request.PendingDays;
        balance.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.Update", "LeaveBalance", balance.Id.ToString(), $"Updated leave balance for {request.Year}.", "Success", HttpContext);

        return ApiResponse<LeaveBalanceResponse>.Ok((await LoadBalance(balance.Id))!);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("LeaveBalance.Manage")]
    public async Task<IActionResult> DeleteBalance(Guid id)
    {
        var balance = await db.LeaveBalances.FirstOrDefaultAsync(item => item.Id == id);
        if (balance is null)
        {
            return NotFound(ApiResponse<object>.Fail("Leave balance not found."));
        }

        db.LeaveBalances.Remove(balance);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.Delete", "LeaveBalance", id.ToString(), "Deleted leave balance.", "Success", HttpContext);
        return NoContent();
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
                null,
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

    private async Task<LeaveBalanceResponse?> LoadBalance(Guid id)
    {
        return await db.LeaveBalances
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .Where(item => item.Id == id)
            .Select(item => ToResponse(item))
            .FirstOrDefaultAsync();
    }

    private async Task<ActionResult<ApiResponse<LeaveBalanceResponse>>?> ValidateRequest(SaveLeaveBalanceRequest request)
    {
        if (request.Year < 2000 || request.Year > 2200)
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("Year is invalid."));
        }

        if (request.EntitledDays < 0 || request.UsedDays < 0 || request.PendingDays < 0)
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("Leave balance values cannot be negative."));
        }

        if (!await db.Users.AnyAsync(item => item.Id == request.UserId))
        {
            return NotFound(ApiResponse<LeaveBalanceResponse>.Fail("User not found."));
        }

        if (!await db.LeaveTypes.AnyAsync(item => item.Id == request.LeaveTypeId))
        {
            return NotFound(ApiResponse<LeaveBalanceResponse>.Fail("Leave type not found."));
        }

        return null;
    }

    private static LeaveBalanceResponse ToResponse(LeaveBalance balance)
    {
        return new LeaveBalanceResponse(
            balance.Id,
            balance.UserId,
            balance.User?.FullName,
            balance.LeaveTypeId,
            balance.LeaveType?.Name ?? "-",
            balance.Year,
            balance.EntitledDays,
            balance.UsedDays,
            balance.PendingDays,
            balance.EntitledDays - balance.UsedDays - balance.PendingDays
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
