using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/reports/leaves")]
[Authorize]
public class LeaveReportsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ReportManagement.View")]
    public async Task<ActionResult<ApiResponse<LeaveReportResponse>>> GetReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? leaveTypeId)
    {
        return ApiResponse<LeaveReportResponse>.Ok(await BuildReport(from, to, departmentId, leaveTypeId));
    }

    [HttpGet("export-excel")]
    [RequirePermission("ReportManagement.Export")]
    public async Task<IActionResult> ExportExcel([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? departmentId, [FromQuery] Guid? leaveTypeId)
    {
        var report = await BuildReport(from, to, departmentId, leaveTypeId);
        var bytes = BuildExcelWorkbook(report);
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveReport.ExportExcel", "LeaveReport", null, "Exported leave report Excel.", "Success", HttpContext);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leave-report.xlsx");
    }

    [HttpGet("export-pdf")]
    [RequirePermission("ReportManagement.Export")]
    public async Task<IActionResult> ExportPdf([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? departmentId, [FromQuery] Guid? leaveTypeId)
    {
        var report = await BuildReport(from, to, departmentId, leaveTypeId);
        var pages = BuildPdfPages(report);

        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveReport.ExportPdf", "LeaveReport", null, "Exported leave report PDF.", "Success", HttpContext);
        return File(SimplePdfWriter.CreateA4Pages(pages, logo: null), "application/pdf", "leave-report.pdf");
    }

    private async Task<LeaveReportResponse> BuildReport(DateOnly? from, DateOnly? to, Guid? departmentId, Guid? leaveTypeId)
    {
        var startDate = from ?? new DateOnly(DateTime.UtcNow.Year, 1, 1);
        var endDate = to ?? new DateOnly(DateTime.UtcNow.Year, 12, 31);

        var leaveQuery = db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover)
            .Where(item => item.StartDate <= endDate && item.EndDate >= startDate);

        if (departmentId is not null)
        {
            leaveQuery = leaveQuery.Where(item => item.User != null && item.User.DepartmentId == departmentId);
        }

        if (leaveTypeId is not null)
        {
            leaveQuery = leaveQuery.Where(item => item.LeaveTypeId == leaveTypeId);
        }

        var leaves = await leaveQuery
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new LeaveReportItemResponse(
                item.Id,
                item.User != null ? item.User.FullName : null,
                item.User != null && item.User.Department != null ? item.User.Department.Name : null,
                item.LeaveType != null ? item.LeaveType.Name : null,
                item.StartDate,
                item.EndDate,
                item.TotalDays,
                item.Status,
                item.CurrentApprover != null ? item.CurrentApprover.FullName : null
            ))
            .ToListAsync();

        var balanceQuery = db.LeaveBalances
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .Where(item => item.Year == startDate.Year);

        if (departmentId is not null)
        {
            balanceQuery = balanceQuery.Where(item => item.User != null && item.User.DepartmentId == departmentId);
        }

        var balances = await balanceQuery
            .OrderBy(item => item.User!.FullName)
            .Select(item => new LeaveBalanceReportItemResponse(
                item.UserId,
                item.User != null ? item.User.FullName : null,
                item.LeaveType != null ? item.LeaveType.Name : "-",
                item.Year,
                item.EntitledDays,
                item.UsedDays,
                item.PendingDays,
                item.EntitledDays - item.UsedDays - item.PendingDays
            ))
            .ToListAsync();

        return new LeaveReportResponse(leaves, balances, leaves.Count(item => item.Status == "Pending"));
    }

    internal static byte[] BuildExcelWorkbook(LeaveReportResponse report)
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "รายงานการลา" },
            new[] { "ชื่อ", "หน่วยงาน", "ประเภทลา", "วันที่เริ่ม", "วันที่สิ้นสุด", "จำนวนวัน", "สถานะ" }
        };

        foreach (var item in report.LeaveRequests)
        {
            rows.Add([
                SafeExcelCell(item.Fullname),
                SafeExcelCell(item.DepartmentName),
                SafeExcelCell(item.LeaveTypeName),
                item.StartDate.ToString("dd/MM/yyyy"),
                item.EndDate.ToString("dd/MM/yyyy"),
                item.TotalDays.ToString("0.##"),
                SafeExcelCell(item.Status)
            ]);
        }

        rows.Add(Array.Empty<string>());
        rows.Add(new[] { "ยอดวันลา" });
        rows.Add(new[] { "ชื่อ", "ประเภทลา", "ปี", "สิทธิ์", "ใช้แล้ว", "รออนุมัติ", "คงเหลือ" });
        foreach (var item in report.LeaveBalances)
        {
            rows.Add([
                SafeExcelCell(item.Fullname),
                SafeExcelCell(item.LeaveTypeName),
                item.Year.ToString(),
                item.EntitledDays.ToString("0.##"),
                item.UsedDays.ToString("0.##"),
                item.PendingDays.ToString("0.##"),
                item.RemainingDays.ToString("0.##")
            ]);
        }

        return SimpleXlsxWriter.CreateWorkbook(rows, [24, 24, 18, 14, 14, 12, 14]);
    }

    internal static string SafeExcelCell(string? value)
    {
        var normalized = value ?? "-";
        if (normalized.Length > 0 && "=+-@".Contains(normalized[0], StringComparison.Ordinal))
        {
            normalized = "'" + normalized;
        }

        return normalized;
    }

    internal static IReadOnlyList<IReadOnlyList<PdfLine>> BuildPdfPages(LeaveReportResponse report)
    {
        const int rowsPerPage = 32;
        var pages = new List<IReadOnlyList<PdfLine>>();
        var chunks = report.LeaveRequests.Count == 0
            ? [Array.Empty<LeaveReportItemResponse>()]
            : report.LeaveRequests.Chunk(rowsPerPage).ToArray();

        for (var pageIndex = 0; pageIndex < chunks.Length; pageIndex++)
        {
            var lines = new List<PdfLine>
            {
                new("รายงานการลา", 50, 790, 18),
                new($"จำนวนคำขอ: {report.LeaveRequests.Count}", 50, 760, 12),
                new($"คำขอรออนุมัติ: {report.PendingApprovalCount}", 50, 742, 12),
                new($"หน้า {pageIndex + 1}/{chunks.Length}", 500, 790, 10),
                new("ชื่อ | หน่วยงาน | ประเภทลา | วันที่ | สถานะ", 50, 718, 10)
            };

            var y = 696;
            foreach (var item in chunks[pageIndex])
            {
                lines.Add(new($"{item.Fullname ?? "-"} | {item.DepartmentName ?? "-"} | {item.LeaveTypeName ?? "-"} | {item.StartDate:dd/MM/yyyy}-{item.EndDate:dd/MM/yyyy} | {item.Status}", 50, y, 9));
                y -= 18;
            }

            if (chunks[pageIndex].Length == 0)
            {
                lines.Add(new("ไม่พบข้อมูลการลาในช่วงวันที่ที่เลือก", 50, y, 11));
            }

            pages.Add(lines);
        }

        return pages;
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
