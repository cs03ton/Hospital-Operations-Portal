using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-holidays")]
[Authorize]
public class LeaveHolidaysController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    private static readonly string[] TemplateHeaders = ["วันที่", "ชื่อวันหยุด", "ประเภทวันหยุด"];
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [HttpGet]
    [RequirePermission("LeaveManagement.View")]
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

    [HttpGet("import-template")]
    [RequirePermission("LeaveHoliday.Manage")]
    public IActionResult DownloadImportTemplate()
    {
        var rows = new[]
        {
            "วันที่,ชื่อวันหยุด,ประเภทวันหยุด",
            "2027-01-01,วันขึ้นปีใหม่,National",
            "2027-04-06,วันจักรี,National",
            "2027-04-13,วันสงกรานต์,National",
            "",
            "คำอธิบาย: วันที่ใช้รูปแบบ yyyy-MM-dd, วันที่และชื่อวันหยุดเป็นฟิลด์บังคับ, ประเภทวันหยุดใช้เช่น National หรือ Local"
        };
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, rows))).ToArray();
        return File(bytes, "text/csv; charset=utf-8", "leave-holiday-import-template.csv");
    }

    [HttpPost("import/preview")]
    [RequirePermission("LeaveHoliday.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveHolidayImportPreviewResponse>>> PreviewImport(IFormFile file)
    {
        var parsedRows = await ParseImportFile(file);
        var response = await ValidateRows(parsedRows);
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveHoliday.ImportPreview", "LeaveHoliday", null, $"Previewed {response.TotalRows} holiday rows.", "Success", HttpContext);
        return ApiResponse<LeaveHolidayImportPreviewResponse>.Ok(response);
    }

    [HttpPost("import/confirm")]
    [RequirePermission("LeaveHoliday.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveHolidayImportConfirmResponse>>> ConfirmImport(LeaveHolidayImportConfirmRequest request)
    {
        var parsedRows = request.Rows.Select((row, index) => new ParsedHolidayRow(index + 2, row.HolidayDate.ToString("yyyy-MM-dd"), row.Name, row.HolidayType)).ToList();
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

        await db.SaveChangesAsync();
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

    private async Task<IReadOnlyList<ParsedHolidayRow>> ParseImportFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return [];
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        return extension switch
        {
            ".csv" => await ParseCsv(stream),
            ".xlsx" => ParseXlsx(stream),
            _ => [new ParsedHolidayRow(1, string.Empty, string.Empty, string.Empty, "รองรับเฉพาะไฟล์ .csv หรือ .xlsx")]
        };
    }

    private static async Task<IReadOnlyList<ParsedHolidayRow>> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = new List<ParsedHolidayRow>();
        var rowNumber = 0;
        while (!reader.EndOfStream)
        {
            rowNumber++;
            var line = await reader.ReadLineAsync() ?? string.Empty;
            if (rowNumber == 1 || string.IsNullOrWhiteSpace(line) || line.StartsWith("คำอธิบาย:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cells = SplitCsv(line);
            rows.Add(new ParsedHolidayRow(rowNumber, cells.ElementAtOrDefault(0) ?? string.Empty, cells.ElementAtOrDefault(1) ?? string.Empty, cells.ElementAtOrDefault(2) ?? string.Empty));
        }

        return rows;
    }

    private static IReadOnlyList<ParsedHolidayRow> ParseXlsx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry is null)
        {
            return [new ParsedHolidayRow(1, string.Empty, string.Empty, string.Empty, "ไม่พบ worksheet ในไฟล์ .xlsx")];
        }

        using var sheetStream = sheetEntry.Open();
        var document = XDocument.Load(sheetStream);
        var rows = new List<ParsedHolidayRow>();
        foreach (var row in document.Descendants(Spreadsheet + "row"))
        {
            var rowNumber = int.TryParse(row.Attribute("r")?.Value, out var parsedNumber) ? parsedNumber : rows.Count + 1;
            if (rowNumber == 1)
            {
                continue;
            }

            var cells = row.Elements(Spreadsheet + "c").Select(cell => ReadCellValue(cell, sharedStrings)).ToList();
            if (cells.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ParsedHolidayRow(rowNumber, cells.ElementAtOrDefault(0) ?? string.Empty, cells.ElementAtOrDefault(1) ?? string.Empty, cells.ElementAtOrDefault(2) ?? string.Empty));
        }

        return rows;
    }

    private async Task<LeaveHolidayImportPreviewResponse> ValidateRows(IReadOnlyList<ParsedHolidayRow> rows)
    {
        var existing = await db.LeaveHolidays.AsNoTracking().Select(item => new { item.HolidayDate, item.Name }).ToListAsync();
        var duplicateDates = new HashSet<DateOnly>();
        var duplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenDates = new HashSet<DateOnly>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentYear = DateTime.UtcNow.Year;
        var result = new List<LeaveHolidayImportPreviewRow>();

        foreach (var row in rows)
        {
            var errors = new List<string>();
            if (!string.IsNullOrWhiteSpace(row.Error))
            {
                errors.Add(row.Error);
            }

            var name = row.Name.Trim();
            var holidayType = string.IsNullOrWhiteSpace(row.HolidayType) ? "National" : row.HolidayType.Trim();
            DateOnly? date = null;

            if (!DateOnly.TryParseExact(row.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                errors.Add("วันที่ไม่ถูกต้อง ต้องใช้รูปแบบ yyyy-MM-dd");
            }
            else
            {
                date = parsedDate;
                if (parsedDate.Year < currentYear - 1)
                {
                    errors.Add("ปีไม่ถูกต้อง ต้องเป็นปีปัจจุบันหรือปีในอนาคต");
                }

                if (existing.Any(item => item.HolidayDate == parsedDate) || !seenDates.Add(parsedDate))
                {
                    duplicateDates.Add(parsedDate);
                    errors.Add("วันที่ซ้ำ");
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("ชื่อวันหยุดห้ามว่าง");
            }
            else if (existing.Any(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) || !seenNames.Add(name))
            {
                duplicateNames.Add(name);
                errors.Add("ชื่อวันหยุดซ้ำ");
            }

            result.Add(new LeaveHolidayImportPreviewRow(row.RowNumber, date, name, holidayType, errors.Count == 0, errors));
        }

        return new LeaveHolidayImportPreviewResponse(result.Count, result.Count(item => item.IsValid), result.Count(item => !item.IsValid), result);
    }

    private static IReadOnlyList<string> SplitCsv(string line)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];
            if (current == '"' && index + 1 < line.Length && line[index + 1] == '"')
            {
                builder.Append('"');
                index++;
            }
            else if (current == '"')
            {
                quoted = !quoted;
            }
            else if (current == ',' && !quoted)
            {
                values.Add(builder.ToString().Trim());
                builder.Clear();
            }
            else
            {
                builder.Append(current);
            }
        }

        values.Add(builder.ToString().Trim());
        return values;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Descendants(Spreadsheet + "si").Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value))).ToList();
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (type == "s" && int.TryParse(cell.Element(Spreadsheet + "v")?.Value, out var sharedIndex))
        {
            return sharedStrings.ElementAtOrDefault(sharedIndex) ?? string.Empty;
        }

        if (type == "inlineStr")
        {
            return string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value)).Trim();
        }

        return (cell.Element(Spreadsheet + "v")?.Value ?? string.Empty).Trim();
    }

    private record ParsedHolidayRow(int RowNumber, string Date, string Name, string HolidayType, string? Error = null);

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
