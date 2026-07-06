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
public class LeaveBalancesController(
    AppDbContext db,
    IAuditLogService auditLogService,
    ILeaveBalanceRolloverService rolloverService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceResponse>>>> GetBalances(
        [FromQuery] int? year = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? leaveTypeId = null)
    {
        var query = db.LeaveBalances
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
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

        if (departmentId is not null)
        {
            query = query.Where(item => item.User != null && item.User.DepartmentId == departmentId.Value);
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
    [RequirePermission(LeavePermissions.ViewOwn)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceResponse>>>> GetMyBalances([FromQuery] int? year = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Fail("Invalid access token."));
        }

        return ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Ok(await LoadBalances(userId.Value, year ?? FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(DateTime.UtcNow))));
    }

    [HttpGet("user/{userId:guid}")]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceResponse>>>> GetUserBalances(Guid userId, [FromQuery] int? year = null)
    {
        if (!await db.Users.AnyAsync(item => item.Id == userId))
        {
            return NotFound(ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Fail("User not found."));
        }

        return ApiResponse<IReadOnlyList<LeaveBalanceResponse>>.Ok(await LoadBalances(userId, year ?? FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(DateTime.UtcNow))));
    }

    [HttpGet("import-template")]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public IActionResult DownloadImportTemplate()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "ตัวอย่างนำเข้ายอดวันลา" },
            new[] { "username", "leaveTypeCode", "fiscalYear", "entitledDays", "carriedOverDays", "usedDays", "pendingDays" },
            new[] { "staff.it01", "AnnualLeave", FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(DateTime.UtcNow)).ToString(), "10", "0", "0", "0" },
            new[] { "staff.it01", "SickLeave", FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(DateTime.UtcNow)).ToString(), "30", "0", "0", "0" },
            Array.Empty<string>(),
            new[] { "หมายเหตุ: username และ leaveTypeCode ต้องตรงกับข้อมูลในระบบ" }
        };

        var bytes = SimpleXlsxWriter.CreateWorkbook(rows, [22, 22, 14, 18, 18, 14, 16]);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leave-balance-import-template.xlsx");
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.ManageBalances)]
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
            CarriedOverDays = request.CarriedOverDays,
            AdjustedDays = request.AdjustedDays,
            UsedDays = request.UsedDays,
            PendingDays = request.PendingDays,
            Notes = request.Notes
        };

        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.Create", "LeaveBalance", balance.Id.ToString(), $"Created leave balance for {request.Year}.", "Success", HttpContext);

        var response = await LoadBalance(balance.Id);
        return CreatedAtAction(nameof(GetBalances), new { id = balance.Id }, ApiResponse<LeaveBalanceResponse>.Ok(response!));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(LeavePermissions.ManageBalances)]
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
        balance.CarriedOverDays = request.CarriedOverDays;
        balance.AdjustedDays = request.AdjustedDays;
        balance.UsedDays = request.UsedDays;
        balance.PendingDays = request.PendingDays;
        balance.Notes = request.Notes;
        balance.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.Update", "LeaveBalance", balance.Id.ToString(), $"Updated leave balance for {request.Year}.", "Success", HttpContext);

        return ApiResponse<LeaveBalanceResponse>.Ok((await LoadBalance(balance.Id))!);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(LeavePermissions.ManageBalances)]
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

    [HttpPost("{id:guid}/adjust")]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceResponse>>> AdjustBalance(Guid id, LeaveBalanceAdjustmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("กรุณาระบุเหตุผลการปรับยอด"));
        }

        var actorUserId = GetCurrentUserId();
        if (actorUserId is null)
        {
            return Unauthorized(ApiResponse<LeaveBalanceResponse>.Fail("Invalid access token."));
        }

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(item => item.Id == id);
        if (balance is null)
        {
            return NotFound(ApiResponse<LeaveBalanceResponse>.Fail("Leave balance not found."));
        }

        balance.AdjustedDays += request.AdjustmentDays;
        balance.Notes = request.Reason;
        balance.UpdatedAt = DateTime.UtcNow;
        db.LeaveBalanceAdjustments.Add(new LeaveBalanceAdjustment
        {
            UserId = balance.UserId,
            LeaveTypeId = balance.LeaveTypeId,
            Year = balance.Year,
            AdjustmentDays = request.AdjustmentDays,
            Reason = request.Reason,
            AdjustedByUserId = actorUserId.Value
        });

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(actorUserId, "LeaveBalance.Adjusted", "LeaveBalance", id.ToString(), $"Adjusted leave balance. targetUserId={balance.UserId}, leaveTypeId={balance.LeaveTypeId}, fiscalYear={balance.Year}, adjustmentDays={request.AdjustmentDays:0.##}, reason={request.Reason}.", "Success", HttpContext);

        return ApiResponse<LeaveBalanceResponse>.Ok((await LoadBalance(balance.Id))!);
    }

    [HttpPost("rollover")]
    [RequireAnyPermission(LeavePermissions.ManageBalances, LeavePermissions.RolloverBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceRolloverResponse>>> RolloverBalances(LeaveBalanceRolloverRequest request)
    {
        try
        {
            var previousFiscalYear = request.TargetFiscalYear - 1;
            var result = await rolloverService.ConfirmAsync(
                new LeaveBalanceRolloverConfirmBatchRequest(previousFiscalYear, request.TargetFiscalYear, null, null, null, null, $"ยกยอดวันลาปีงบประมาณ {request.TargetFiscalYear + 543}"),
                GetCurrentUserId(),
                HttpContext.RequestAborted);
            return ApiResponse<LeaveBalanceRolloverResponse>.Ok(new LeaveBalanceRolloverResponse(request.TargetFiscalYear, previousFiscalYear, result.Created, result.Skipped + result.Blocked));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveBalanceRolloverResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("rollover/preview")]
    [RequireAnyPermission(LeavePermissions.ManageBalances, LeavePermissions.RolloverBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceRolloverBatchResponse>>> PreviewRollover(LeaveBalanceRolloverFilterRequest request)
    {
        try
        {
            return ApiResponse<LeaveBalanceRolloverBatchResponse>.Ok(await rolloverService.PreviewAsync(request, GetCurrentUserId(), HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveBalanceRolloverBatchResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("rollover/confirm")]
    [RequireAnyPermission(LeavePermissions.ManageBalances, LeavePermissions.RolloverBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceRolloverBatchResponse>>> ConfirmRollover(LeaveBalanceRolloverConfirmBatchRequest request)
    {
        try
        {
            return ApiResponse<LeaveBalanceRolloverBatchResponse>.Ok(await rolloverService.ConfirmAsync(request, GetCurrentUserId(), HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveBalanceRolloverBatchResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("rollover/export-preview")]
    [RequireAnyPermission(LeavePermissions.ManageBalances, LeavePermissions.RolloverBalances)]
    public async Task<IActionResult> ExportRolloverPreview(LeaveBalanceRolloverFilterRequest request)
    {
        try
        {
            var bytes = await rolloverService.ExportPreviewCsvAsync(request, GetCurrentUserId(), HttpContext.RequestAborted);
            return File(bytes, "text/csv; charset=utf-8", $"leave-rollover-preview-{request.FromFiscalYear}-{request.ToFiscalYear}.csv");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("{id:guid}/rollover-preview")]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceRolloverPreviewResponse>>> PreviewIndividualRollover(Guid id)
    {
        var balance = await LoadBalanceEntity(id, asTracking: false);
        if (balance is null)
        {
            return NotFound(ApiResponse<LeaveBalanceRolloverPreviewResponse>.Fail("Leave balance not found."));
        }

        if (balance.LeaveType is null || !balance.LeaveType.AllowCarryOver)
        {
            await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.RolloverBlocked", "LeaveBalance", id.ToString(), $"Rollover blocked because leave type does not allow carry over. userId={balance.UserId}, leaveTypeId={balance.LeaveTypeId}, fromFiscalYear={balance.Year}.", "Failed", HttpContext);
            return BadRequest(ApiResponse<LeaveBalanceRolloverPreviewResponse>.Fail("ประเภทลานี้ไม่รองรับการยกยอด"));
        }

        var preview = await BuildRolloverPreview(balance, balance.Year + 1, balance.LeaveType.DefaultDaysPerYear);
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.RolloverPreviewed", "LeaveBalance", id.ToString(), $"Previewed rollover. targetUserId={balance.UserId}, leaveTypeId={balance.LeaveTypeId}, fromFiscalYear={preview.FromFiscalYear}, toFiscalYear={preview.ToFiscalYear}, carriedOverDays={preview.CarryOverDays:0.##}, forfeitedDays={preview.ForfeitedDays:0.##}.", "Success", HttpContext);

        return ApiResponse<LeaveBalanceRolloverPreviewResponse>.Ok(preview);
    }

    [HttpPost("{id:guid}/rollover-confirm")]
    [RequirePermission(LeavePermissions.ManageBalances)]
    public async Task<ActionResult<ApiResponse<LeaveBalanceResponse>>> ConfirmIndividualRollover(Guid id, LeaveBalanceRolloverConfirmRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("กรุณาระบุเหตุผลการยกยอด"));
        }

        if (request.ToFiscalYear < 2000 || request.ToFiscalYear > 2200)
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("Target fiscal year is invalid."));
        }

        if (request.NewEntitlementDays < 0)
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("New entitlement days cannot be negative."));
        }

        var sourceBalance = await LoadBalanceEntity(id, asTracking: false);
        if (sourceBalance is null)
        {
            return NotFound(ApiResponse<LeaveBalanceResponse>.Fail("Leave balance not found."));
        }

        if (sourceBalance.LeaveType is null || !sourceBalance.LeaveType.AllowCarryOver)
        {
            await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveBalance.RolloverBlocked", "LeaveBalance", id.ToString(), $"Rollover blocked because leave type does not allow carry over. userId={sourceBalance.UserId}, leaveTypeId={sourceBalance.LeaveTypeId}, fromFiscalYear={sourceBalance.Year}, toFiscalYear={request.ToFiscalYear}.", "Failed", HttpContext);
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("ประเภทลานี้ไม่รองรับการยกยอด"));
        }

        if (request.ToFiscalYear <= sourceBalance.Year)
        {
            return BadRequest(ApiResponse<LeaveBalanceResponse>.Fail("ปีงบประมาณปลายทางต้องมากกว่าปีงบประมาณต้นทาง"));
        }

        var preview = await BuildRolloverPreview(sourceBalance, request.ToFiscalYear, request.NewEntitlementDays);
        var existingTarget = await db.LeaveBalances
            .FirstOrDefaultAsync(item =>
                item.UserId == sourceBalance.UserId &&
                item.LeaveTypeId == sourceBalance.LeaveTypeId &&
                item.Year == request.ToFiscalYear);

        if (existingTarget is not null && !request.UpdateExistingCarriedOverOnly)
        {
            return Conflict(ApiResponse<LeaveBalanceResponse>.Fail("พบยอดวันลาของปีงบประมาณปลายทางอยู่แล้ว กรุณาเลือกอัปเดตเฉพาะยอดยกมา"));
        }

        LeaveBalance targetBalance;
        var auditAction = "LeaveBalance.RolloverConfirmed";
        if (existingTarget is not null)
        {
            existingTarget.CarriedOverDays = preview.CarryOverDays;
            existingTarget.Notes = request.Reason;
            existingTarget.UpdatedAt = DateTime.UtcNow;
            targetBalance = existingTarget;
            auditAction = "LeaveBalance.RolloverUpdatedExistingBalance";
        }
        else
        {
            targetBalance = new LeaveBalance
            {
                UserId = sourceBalance.UserId,
                LeaveTypeId = sourceBalance.LeaveTypeId,
                Year = request.ToFiscalYear,
                EntitledDays = request.NewEntitlementDays,
                CarriedOverDays = preview.CarryOverDays,
                AdjustedDays = 0,
                UsedDays = 0,
                PendingDays = 0,
                Notes = request.Reason
            };
            db.LeaveBalances.Add(targetBalance);
        }

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), auditAction, "LeaveBalance", targetBalance.Id.ToString(), $"Confirmed individual rollover. targetUserId={sourceBalance.UserId}, leaveTypeId={sourceBalance.LeaveTypeId}, fromFiscalYear={preview.FromFiscalYear}, toFiscalYear={preview.ToFiscalYear}, carriedOverDays={preview.CarryOverDays:0.##}, forfeitedDays={preview.ForfeitedDays:0.##}, newEntitlementDays={request.NewEntitlementDays:0.##}, reason={request.Reason}.", "Success", HttpContext);

        return ApiResponse<LeaveBalanceResponse>.Ok((await LoadBalance(targetBalance.Id))!);
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
            var carriedOver = balance?.CarriedOverDays ?? 0;
            var used = balance?.UsedDays ?? 0;
            var pending = balance?.PendingDays ?? 0;
            var adjusted = balance?.AdjustedDays ?? 0;
            var available = FiscalYearHelper.CalculateAvailableDays(entitled, carriedOver, used, pending, adjusted);

            return new LeaveBalanceResponse(
                balance?.Id,
                userId,
                null,
                null,
                null,
                leaveType.Id,
                leaveType.Name,
                year,
                entitled,
                carriedOver,
                adjusted,
                used,
                pending,
                available,
                available,
                balance?.Notes
            );
        }).ToList();
    }

    private async Task<LeaveBalanceResponse?> LoadBalance(Guid id)
    {
        return await db.LeaveBalances
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
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

        if (request.EntitledDays < 0 || request.CarriedOverDays < 0 || request.UsedDays < 0 || request.PendingDays < 0)
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
            balance.User?.DepartmentId,
            balance.User?.Department?.Name,
            balance.LeaveTypeId,
            balance.LeaveType?.Name ?? "-",
            balance.Year,
            balance.EntitledDays,
            balance.CarriedOverDays,
            balance.AdjustedDays,
            balance.UsedDays,
            balance.PendingDays,
            FiscalYearHelper.CalculateAvailableDays(balance.EntitledDays, balance.CarriedOverDays, balance.UsedDays, balance.PendingDays, balance.AdjustedDays),
            FiscalYearHelper.CalculateAvailableDays(balance.EntitledDays, balance.CarriedOverDays, balance.UsedDays, balance.PendingDays, balance.AdjustedDays),
            balance.Notes
        );
    }

    private async Task<LeaveBalance?> LoadBalanceEntity(Guid id, bool asTracking)
    {
        var query = db.LeaveBalances
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Where(item => item.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task<LeaveBalanceRolloverPreviewResponse> BuildRolloverPreview(
        LeaveBalance balance,
        int toFiscalYear,
        decimal newEntitlementDays)
    {
        var leaveType = balance.LeaveType!;
        var targetExists = await db.LeaveBalances
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == balance.UserId &&
                item.LeaveTypeId == balance.LeaveTypeId &&
                item.Year == toFiscalYear);
        var endYearRemaining = FiscalYearHelper.CalculateAvailableDays(balance.EntitledDays, balance.CarriedOverDays, balance.UsedDays, balance.PendingDays, balance.AdjustedDays);
        var carryOverMaxDays = leaveType.CarryOverMaxDays > 0 ? leaveType.CarryOverMaxDays : FiscalYearHelper.CarryOverDefaultMaxDays;
        var carryOverDays = FiscalYearHelper.CalculateCarryOver(endYearRemaining, leaveType);
        var forfeitedDays = Math.Max(endYearRemaining - carryOverMaxDays, 0);
        var newAvailableDays = FiscalYearHelper.CalculateAvailableDays(newEntitlementDays, carryOverDays, 0, 0, 0);
        var warnings = new List<string>();

        if (targetExists)
        {
            warnings.Add("พบยอดวันลาของปีงบประมาณปลายทางอยู่แล้ว ระบบจะไม่ overwrite โดยอัตโนมัติ");
        }

        if (forfeitedDays > 0)
        {
            warnings.Add($"มียอดคงเหลือเกินสิทธิ์ยกยอด ระบบจะตัดออก {forfeitedDays:0.##} วัน");
        }

        return new LeaveBalanceRolloverPreviewResponse(
            balance.UserId,
            balance.User?.FullName,
            balance.LeaveTypeId,
            leaveType.Name,
            balance.Year,
            toFiscalYear,
            balance.EntitledDays,
            balance.CarriedOverDays,
            balance.AdjustedDays,
            balance.UsedDays,
            balance.PendingDays,
            endYearRemaining,
            carryOverMaxDays,
            carryOverDays,
            forfeitedDays,
            newEntitlementDays,
            newAvailableDays,
            targetExists,
            warnings);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
