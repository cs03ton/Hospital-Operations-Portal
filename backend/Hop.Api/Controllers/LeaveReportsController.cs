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
    private const string AnalyticsPermission = "LeaveAnalytics.View";
    private const string ReportViewPermission = "ReportManagement.View";
    private const string ReportExportPermission = "ReportManagement.Export";
    private static readonly HashSet<string> ReportRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Director",
        "Admin",
        "SuperAdmin"
    };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<LeaveReportResponse>>> GetReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanAccessReport(currentUserId.Value, cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<LeaveReportResponse>.Ok(await BuildReport(from, to, departmentId, leaveTypeId));
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? departmentId, [FromQuery] Guid? leaveTypeId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanExportReport(currentUserId.Value, cancellationToken))
        {
            return Forbid();
        }

        var report = await BuildReport(from, to, departmentId, leaveTypeId);
        var bytes = BuildExcelWorkbook(report);
        await auditLogService.WriteAsync(currentUserId, "LeaveReport.ExportExcel", "LeaveReport", null, "Exported leave report Excel.", "Success", HttpContext);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leave-report.xlsx");
    }

    [HttpGet("export-pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? departmentId, [FromQuery] Guid? leaveTypeId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanExportReport(currentUserId.Value, cancellationToken))
        {
            return Forbid();
        }

        var report = await BuildReport(from, to, departmentId, leaveTypeId);
        var pages = BuildPdfPages(report);

        await auditLogService.WriteAsync(currentUserId, "LeaveReport.ExportPdf", "LeaveReport", null, "Exported leave report PDF.", "Success", HttpContext);
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
                item.RequestNumber,
                item.User != null ? item.User.FullName : null,
                item.User != null && item.User.Department != null ? item.User.Department.Name : null,
                item.LeaveType != null ? item.LeaveType.Name : null,
                item.StartDate,
                item.EndDate,
                item.DurationType,
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
            new[] { "เลขที่คำขอ", "ชื่อ", "หน่วยงาน", "ประเภทลา", "วันที่เริ่ม", "วันที่สิ้นสุด", "ประเภทช่วงเวลา", "จำนวนวัน", "สถานะ" }
        };

        foreach (var item in report.LeaveRequests)
        {
            rows.Add([
                SafeExcelCell(item.RequestNumber),
                SafeExcelCell(item.Fullname),
                SafeExcelCell(item.DepartmentName),
                SafeExcelCell(item.LeaveTypeName),
                item.StartDate.ToString("dd/MM/yyyy"),
                item.EndDate.ToString("dd/MM/yyyy"),
                TranslateDurationType(item.DurationType),
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

        return SimpleXlsxWriter.CreateWorkbook(rows, [18, 24, 24, 18, 14, 14, 18, 12, 14]);
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

    internal static string TranslateDurationType(string? durationType)
    {
        return durationType switch
        {
            "HALF_DAY_AM" => "ครึ่งวัน (เช้า)",
            "HALF_DAY_PM" => "ครึ่งวัน (บ่าย)",
            "FULL_DAY" or null or "" => "เต็มวัน",
            _ => durationType
        };
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
                new("เลขที่คำขอ | ชื่อ | หน่วยงาน | ประเภทลา | ช่วงเวลา | วันที่ | จำนวนวัน | สถานะ", 50, 718, 10)
            };

            var y = 696;
            foreach (var item in chunks[pageIndex])
            {
                lines.Add(new($"{item.RequestNumber ?? "-"} | {item.Fullname ?? "-"} | {item.DepartmentName ?? "-"} | {item.LeaveTypeName ?? "-"} | {TranslateDurationType(item.DurationType)} | {item.StartDate:dd/MM/yyyy}-{item.EndDate:dd/MM/yyyy} | {item.TotalDays:0.##} | {item.Status}", 50, y, 9));
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

    private async Task<bool> CanAccessReport(Guid userId, CancellationToken cancellationToken)
    {
        if (await HasAnyRoleAsync(userId, ReportRoles, cancellationToken))
        {
            return true;
        }

        return await HasAnyPermissionAsync(userId, [ReportViewPermission, AnalyticsPermission], cancellationToken);
    }

    private async Task<bool> CanExportReport(Guid userId, CancellationToken cancellationToken)
    {
        if (await HasAnyRoleAsync(userId, ReportRoles, cancellationToken))
        {
            return true;
        }

        return await HasAnyPermissionAsync(userId, [ReportExportPermission], cancellationToken);
    }

    private Task<bool> HasAnyRoleAsync(Guid userId, IReadOnlySet<string> roleNames, CancellationToken cancellationToken)
    {
        return db.UserRoles
            .AsNoTracking()
            .Include(item => item.Role)
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .AnyAsync(item => item.Role != null && roleNames.Contains(item.Role.Name), cancellationToken);
    }

    private Task<bool> HasAnyPermissionAsync(Guid userId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken)
    {
        return db.RolePermissions
            .AsNoTracking()
            .Include(item => item.Permission)
            .Where(item => item.Role != null && item.Role.UserRoles.Any(userRole => userRole.UserId == userId))
            .AnyAsync(item =>
                item.Permission != null &&
                permissionCodes.Contains(item.Permission.Code),
                cancellationToken);
    }
}
