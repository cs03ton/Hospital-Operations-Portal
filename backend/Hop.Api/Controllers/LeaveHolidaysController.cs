using Hop.Api.Services;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-holidays")]
[Authorize]
public class LeaveHolidaysController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll, LeavePermissions.ManageHolidays)]
    public async Task<ActionResult<ApiResponse<object>>> GetHolidays(
        [FromQuery] int? year = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var query = db.LeaveHolidays.AsNoTracking();
        if (year is not null)
        {
            query = query.Where(item => item.HolidayDate.Year == year);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(keyword));
        }

        if (page is not null || pageSize is not null || !string.IsNullOrWhiteSpace(search))
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var currentPageSize = Math.Clamp(pageSize ?? 20, 1, 100);
            var totalItems = await query.CountAsync();
            var pagedItems = await query
                .OrderBy(item => item.HolidayDate)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(item => ToResponse(item))
                .ToListAsync();

            return ApiResponse<object>.Ok(new PagedResponse<LeaveHolidayResponse>(
                pagedItems,
                currentPage,
                currentPageSize,
                totalItems,
                (int)Math.Ceiling(totalItems / (double)currentPageSize)));
        }

        var items = await query
            .OrderBy(item => item.HolidayDate)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<object>.Ok(items);
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.ManageHolidays)]
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
    [RequirePermission(LeavePermissions.ManageHolidays)]
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
    [RequirePermission(LeavePermissions.ManageHolidays)]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        var item = await db.LeaveHolidays.FirstOrDefaultAsync(holiday => holiday.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<string>.Fail("Holiday not found."));
        }

        db.LeaveHolidays.Remove(item);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.Delete", "LeaveHoliday", item.Id.ToString(), $"Deleted holiday {item.Name}.", "Success", HttpContext);

        return NoContent();
    }

    [HttpGet("import-template")]
    [RequirePermission(LeavePermissions.ManageHolidays)]
    public IActionResult DownloadImportTemplate()
    {
        var rows = new[]
        {
            "วันที่,ชื่อวันหยุด",
            "1/1/2027,วันขึ้นปีใหม่",
            "6/4/2027,วันจักรี",
            "13/4/2027,วันสงกรานต์",
            "14/4/2027,วันสงกรานต์",
            "15/4/2027,วันสงกรานต์",
            "",
            "คำอธิบาย: บังคับคอลัมน์ วันที่ และ ชื่อวันหยุด; วันที่ใช้ d/M/yyyy หรือ yyyy-MM-dd ปี ค.ศ. หรือ พ.ศ.; ประเภทวันหยุดเป็นคอลัมน์เสริมของไฟล์เดิม"
        };
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, rows))).ToArray();
        return File(bytes, "text/csv; charset=utf-8", "leave-holiday-import-template.csv");
    }

    [HttpPost("import/preview")]
    [RequirePermission(LeavePermissions.ManageHolidays)]
    public async Task<ActionResult<ApiResponse<LeaveHolidayImportPreviewResponse>>> PreviewImport(IFormFile file)
    {
        var parsedRows = await LeaveHolidayImportReader.ReadAsync(file);
        var response = await ValidateRows(parsedRows);
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.ImportPreview", "LeaveHoliday", null, $"Previewed {response.TotalRows} holiday rows.", "Success", HttpContext);
        return ApiResponse<LeaveHolidayImportPreviewResponse>.Ok(response);
    }

    [HttpPost("import/confirm")]
    [RequirePermission(LeavePermissions.ManageHolidays)]
    public async Task<ActionResult<ApiResponse<LeaveHolidayImportConfirmResponse>>> ConfirmImport(LeaveHolidayImportConfirmRequest request)
    {
        if (request.Rows.Count == 0 || request.Rows.Count > LeaveHolidayImportReader.MaxRows)
            return BadRequest(ApiResponse<LeaveHolidayImportConfirmResponse>.Fail("กรุณาระบุรายการนำเข้า 1–5,000 รายการ"));

        var parsedRows = request.Rows.Select((row, index) => new ParsedHolidayRow(index + 2, row.HolidayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), row.Name, row.HolidayType)).ToList();
        var validation = await ValidateRows(parsedRows);
        var validRows = validation.Rows.Where(row => row.IsValid && row.HolidayDate is not null).ToList();

        if (validation.InvalidRows > 0)
        {
            return BadRequest(ApiResponse<LeaveHolidayImportConfirmResponse>.Ok(new LeaveHolidayImportConfirmResponse(0, validation.Rows.Where(row => !row.IsValid).ToList()), "Import validation failed."));
        }

        foreach (var row in validRows)
        {
            db.LeaveHolidays.Add(new LeaveHoliday
            {
                HolidayDate = row.HolidayDate!.Value,
                Name = row.Name.Trim(),
                IsActive = true
            });
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            return Conflict(ApiResponse<LeaveHolidayImportConfirmResponse>.Fail("มีวันที่ซ้ำจากการบันทึกอื่น กรุณาตรวจสอบ Preview ใหม่ ยังไม่ได้นำเข้าข้อมูลชุดนี้"));
        }
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.ImportConfirm", "LeaveHoliday", null, $"Imported {validRows.Count} holidays.", "Success", HttpContext);
        return ApiResponse<LeaveHolidayImportConfirmResponse>.Ok(new LeaveHolidayImportConfirmResponse(validRows.Count, []));
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

    private async Task<LeaveHolidayImportPreviewResponse> ValidateRows(IReadOnlyList<ParsedHolidayRow> rows)
    {
        var existing = (await db.LeaveHolidays.AsNoTracking().Select(item => item.HolidayDate).ToListAsync()).ToHashSet();
        var repeatedDates = rows.Select(row => LeaveHolidayImportReader.TryParseDate(row.Date, out var date) ? (DateOnly?)date : null)
            .Where(date => date.HasValue).GroupBy(date => date!.Value).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        var currentYear = Hop.Api.Services.HospitalTime.Today.Year;
        var result = new List<LeaveHolidayImportPreviewRow>();

        foreach (var row in rows)
        {
            var errors = new List<string>();
            if (!string.IsNullOrWhiteSpace(row.Error))
            {
                result.Add(new LeaveHolidayImportPreviewRow(row.RowNumber, null, row.Name, row.HolidayType, false, [row.Error]));
                continue;
            }

            var name = row.Name.Trim();
            var holidayType = string.IsNullOrWhiteSpace(row.HolidayType) ? "National" : row.HolidayType.Trim();
            DateOnly? date = null;

            if (!LeaveHolidayImportReader.TryParseDate(row.Date, out var parsedDate))
            {
                errors.Add("วันที่ไม่ถูกต้อง ใช้วัน/เดือน/ปี เช่น 6/4/2027 หรือ yyyy-MM-dd ปี ค.ศ. หรือ พ.ศ.");
            }
            else
            {
                date = parsedDate;
                if (parsedDate.Year < currentYear - 1)
                {
                    errors.Add("ปีไม่ถูกต้อง ต้องเป็นปีปัจจุบันหรือปีในอนาคต");
                }

                if (existing.Contains(parsedDate) || repeatedDates.Contains(parsedDate))
                {
                    errors.Add("วันที่ซ้ำ");
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("ชื่อวันหยุดห้ามว่าง");
            }

            result.Add(new LeaveHolidayImportPreviewRow(row.RowNumber, date, name, holidayType, errors.Count == 0, errors));
        }

        return new LeaveHolidayImportPreviewResponse(result.Count, result.Count(item => item.IsValid), result.Count(item => !item.IsValid), result);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
