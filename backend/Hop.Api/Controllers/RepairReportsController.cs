using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController, Authorize, RepairActiveUser, RequirePermission(RepairPermissions.ViewAll)]
[Route("api/repairs/reports")]
public sealed class RepairReportsController(AppDbContext db, IAuditLogService audit) : ControllerBase
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    private static readonly string[] Statuses = ["Submitted", "InProgress", "WaitingParts", "Returned", "Resolved", "Closed", "Cancelled"];

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] RepairReportFilter filter, CancellationToken ct)
    {
        if (!TryRange(filter, out var from, out var to)) return BadRequest(ApiResponse<object>.Fail("ช่วงวันที่ไม่ถูกต้อง"));
        var rows = await Filtered(filter, from, to).Select(x => new { x.Status, x.CreatedAt, x.CategoryId, x.DepartmentId }).ToListAsync(ct);
        var categories = await db.Set<RepairCategory>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var departments = await db.Departments.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var counts = Statuses.Select(status => new RepairReportCount(status, rows.Count(x => x.Status == status))).ToArray();
        var months = new List<RepairReportMonth>();
        for (var month = new DateOnly(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1))
        {
            var group = rows.Where(x => { var day = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc), Bangkok); return day.Year == month.Year && day.Month == month.Month; }).ToArray();
            var eligible = group.Count(x => x.Status != "Cancelled");
            months.Add(new RepairReportMonth(month.ToString("yyyy-MM"), group.Length, group.Count(x => x.Status == "Closed"),
                group.Count(x => x.Status is "Submitted" or "InProgress" or "WaitingParts" or "Returned" or "Resolved"),
                eligible == 0 ? 0 : Math.Round(group.Count(x => x.Status == "Closed") * 100m / eligible, 1)));
        }
        var byCategory = rows.GroupBy(x => x.CategoryId).Select(g => new RepairReportGroup(g.Key.ToString(), categories.GetValueOrDefault(g.Key) ?? "ไม่พบประเภทงาน", g.Count())).OrderByDescending(x => x.Count).ToArray();
        var byDepartment = rows.GroupBy(x => x.DepartmentId).Select(g => new RepairReportGroup(g.Key?.ToString() ?? "", g.Key is { } id ? departments.GetValueOrDefault(id) ?? "ไม่พบหน่วยงาน" : "ไม่ระบุหน่วยงาน", g.Count())).OrderByDescending(x => x.Count).ToArray();
        var eligibleTotal = rows.Count(x => x.Status != "Cancelled");
        return Ok(ApiResponse<object>.Ok(new { from, to, total = rows.Count, counts, acceptanceRate = eligibleTotal == 0 ? 0 : Math.Round(rows.Count(x => x.Status == "Closed") * 100m / eligibleTotal, 1), months, byCategory, byDepartment,
            categories = categories.Select(x => new { id = x.Key, name = x.Value }).OrderBy(x => x.name),
            departments = departments.Select(x => new { id = x.Key, name = x.Value }).OrderBy(x => x.name) }));
    }

    [HttpGet("items")]
    public async Task<IActionResult> Items([FromQuery] RepairReportFilter filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!TryRange(filter, out var from, out var to)) return BadRequest(ApiResponse<object>.Fail("ช่วงวันที่ไม่ถูกต้อง"));
        var requests = Filtered(filter, from, to);
        var total = await requests.CountAsync(ct);
        var pageRequests = requests.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Number)
            .Skip((Math.Clamp(page, 1, 1_000_000) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100));
        var pageRows = await Rows(pageRequests).ToListAsync(ct);
        var items = await CompleteDates(pageRows, ct);
        return Ok(ApiResponse<object>.Ok(new { items, total, page = Math.Clamp(page, 1, 1_000_000), pageSize = Math.Clamp(pageSize, 1, 100) }));
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] RepairReportFilter filter, CancellationToken ct)
    {
        if (!TryRange(filter, out var from, out var to)) return BadRequest(ApiResponse<object>.Fail("ช่วงวันที่ไม่ถูกต้อง"));
        var items = await CompleteDates(await Rows(Filtered(filter, from, to).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Number)).ToListAsync(ct), ct);
        var rows = new List<IReadOnlyList<string>> { new[] { "รายงานการแจ้งซ่อม" }, new[] { "เลขที่", "หัวข้อ", "ประเภทงาน", "หน่วยงาน", "ผู้แจ้ง", "วันที่แจ้ง", "วันที่ปิดงาน", "สถานะ", "ความเร่งด่วน" } };
        var dates = new Dictionary<(int Row, int Column), DateOnly>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            rows.Add([Number(item.Number), Safe(item.Title), Safe(item.CategoryName), Safe(item.DepartmentName), Safe(item.RequesterName), ThaiDateDisplay.Instant(item.CreatedAt), item.ClosedAt is { } closed ? ThaiDateDisplay.Instant(closed) : "", item.Status, item.Priority ?? "ยังไม่กำหนด"]);
            dates[(i + 3, 6)] = ThaiDate(item.CreatedAt);
            if (item.ClosedAt is { } closedAt) dates[(i + 3, 7)] = ThaiDate(closedAt);
        }
        await audit.WriteAsync(Actor(), "RepairReport.ExportExcel", "RepairReport", null, $"From={from}; To={to}; Rows={items.Count}", "Success", HttpContext);
        return File(SimpleXlsxWriter.CreateWorkbook(rows, [17, 40, 24, 25, 24, 23, 23, 20, 18], dates, "รายงานการแจ้งซ่อม"),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "repair-report.xlsx");
    }

    [HttpGet("export-pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] RepairReportFilter filter, CancellationToken ct)
    {
        if (!TryRange(filter, out var from, out var to)) return BadRequest(ApiResponse<object>.Fail("ช่วงวันที่ไม่ถูกต้อง"));
        var items = await CompleteDates(await Rows(Filtered(filter, from, to).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Number)).ToListAsync(ct), ct);
        var chunks = items.Count == 0 ? [Array.Empty<RepairReportItem>()] : items.Chunk(20).ToArray();
        var pages = new List<IReadOnlyList<PdfLine>>();
        for (var page = 0; page < chunks.Length; page++)
        {
            var lines = new List<PdfLine> { new("รายงานการแจ้งซ่อม", 45, 790, 17), new($"ช่วงวันที่ {ThaiDateDisplay.Date(from)} - {ThaiDateDisplay.Date(to)} · {items.Count} รายการ", 45, 765, 11), new($"หน้า {page + 1}/{chunks.Length}", 500, 790, 10), new("เลขที่ | วันที่แจ้ง | วันที่ปิดงาน | สถานะ | ความเร่งด่วน", 45, 735, 10) };
            var y = 713;
            foreach (var item in chunks[page])
            {
                lines.Add(new($"{Number(item.Number)} | {ThaiDateDisplay.Date(ThaiDate(item.CreatedAt))} | {(item.ClosedAt is { } closed ? ThaiDateDisplay.Date(ThaiDate(closed)) : "-")} | {item.Status} | {item.Priority ?? "ยังไม่กำหนด"}", 45, y, 9));
                lines.Add(new($"{Clip(item.Title, 26)} | {Clip(item.CategoryName, 17)} | {Clip(item.DepartmentName, 17)} | {Clip(item.RequesterName, 17)}", 58, y - 13, 8));
                y -= 31;
            }
            if (chunks[page].Length == 0) lines.Add(new("ไม่พบรายการตามตัวกรอง", 45, y, 11));
            pages.Add(lines);
        }
        await audit.WriteAsync(Actor(), "RepairReport.ExportPdf", "RepairReport", null, $"From={from}; To={to}; Rows={items.Count}", "Success", HttpContext);
        return File(SimplePdfWriter.CreateA4Pages(pages, null), "application/pdf", "repair-report.pdf");
    }

    private IQueryable<RepairRequest> Filtered(RepairReportFilter filter, DateOnly from, DateOnly to)
    {
        var start = ToUtc(from);
        var end = ToUtc(to.AddDays(1));
        var query = db.Set<RepairRequest>().AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        if (filter.CategoryId is { } category) query = query.Where(x => x.CategoryId == category);
        if (filter.DepartmentId is { } department) query = query.Where(x => x.DepartmentId == department);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Priority)) query = filter.Priority == "Unset" ? query.Where(x => x.Priority == null) : query.Where(x => x.Priority == filter.Priority);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            var hasNumber = long.TryParse(term.Replace("REP-", "", StringComparison.OrdinalIgnoreCase), out var number);
            query = query.Where(x => x.Title.Contains(term) || x.Location.Contains(term) || (hasNumber && x.Number == number) || db.Users.Any(u => u.Id == x.RequesterId && u.FullName.Contains(term)));
        }
        return query;
    }

    private IQueryable<RepairReportItem> Rows(IQueryable<RepairRequest> requests) =>
        from r in requests
        join category in db.Set<RepairCategory>().AsNoTracking() on r.CategoryId equals category.Id
        join requester in db.Users.AsNoTracking() on r.RequesterId equals requester.Id
        join department in db.Departments.AsNoTracking() on r.DepartmentId equals department.Id into departments
        from department in departments.DefaultIfEmpty()
        select new RepairReportItem(r.Id, r.Number, r.Title, category.Name, department == null ? "ไม่ระบุหน่วยงาน" : department.Name, requester.FullName, r.CreatedAt, null, r.Status, r.Priority);

    private async Task<List<RepairReportItem>> CompleteDates(List<RepairReportItem> items, CancellationToken ct)
    {
        var closedIds = items.Where(x => x.Status == "Closed").Select(x => x.Id).ToArray();
        if (closedIds.Length == 0) return items;
        var accepted = await db.Set<RepairEvent>().AsNoTracking().Where(x => closedIds.Contains(x.RequestId) && x.Action == "accept")
            .GroupBy(x => x.RequestId).Select(g => new { Id = g.Key, ClosedAt = g.Max(x => x.CreatedAt) }).ToDictionaryAsync(x => x.Id, x => x.ClosedAt, ct);
        return items.Select(x => x.Status == "Closed" && accepted.TryGetValue(x.Id, out var closedAt) ? x with { ClosedAt = closedAt } : x).ToList();
    }

    private static bool TryRange(RepairReportFilter filter, out DateOnly from, out DateOnly to)
    {
        var today = HospitalTime.Today;
        from = filter.From ?? new DateOnly(today.Year, today.Month, 1);
        to = filter.To ?? today;
        return from.Year is >= 1900 and <= 2100 && to.Year is >= 1900 and <= 2100 && from <= to && to < DateOnly.MaxValue;
    }
    private static DateTime ToUtc(DateOnly date) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), Bangkok);
    private static DateOnly ThaiDate(DateTime utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Bangkok));
    private static string Number(long number) => $"REP-{number:D6}";
    private static string Safe(string value) => LeaveReportsController.SafeExcelCell(value);
    private static string Clip(string value, int length) => value.Length <= length ? value : value[..length] + "…";
    private Guid? Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public sealed class RepairReportFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Search { get; set; }
}
public sealed record RepairReportCount(string Status, int Count);
public sealed record RepairReportMonth(string Month, int Total, int Closed, int InProgress, decimal AcceptanceRate);
public sealed record RepairReportGroup(string Id, string Name, int Count);
public sealed record RepairReportItem(Guid Id, long Number, string Title, string CategoryName, string DepartmentName, string RequesterName, DateTime CreatedAt, DateTime? ClosedAt, string Status, string? Priority);
