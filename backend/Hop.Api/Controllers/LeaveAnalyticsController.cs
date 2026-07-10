using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/reports/leave-analytics")]
[Authorize]
public class LeaveAnalyticsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    private const string AnalyticsPermission = "LeaveAnalytics.View";
    private const string ReportPermission = "ReportManagement.View";
    private static readonly string[] CoreLeaveTypeCodes = ["SICK_LEAVE", "PERSONAL_LEAVE", "VACATION_LEAVE"];
    private static readonly HashSet<string> ReportRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Director",
        "Admin",
        "SuperAdmin"
    };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<LeaveAnalyticsResponse>>> GetAnalytics(
        [FromQuery] int? fiscalYear,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? leaveTypeId,
        [FromQuery] string? status,
        [FromQuery] bool coreOnly = true,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanAccessAnalytics(currentUserId.Value, cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<LeaveAnalyticsResponse>.Ok(await BuildAnalytics(
            fiscalYear,
            year,
            month,
            departmentId,
            leaveTypeId,
            status,
            coreOnly,
            cancellationToken));
    }

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<LeaveAnalyticsOptionsResponse>>> GetOptions(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanAccessAnalytics(currentUserId.Value, cancellationToken))
        {
            return Forbid();
        }

        var departments = await db.Departments
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new DepartmentDto(
                item.Id,
                item.Name,
                item.Description,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt,
                db.Users.Count(user => user.DepartmentId == item.Id)))
            .ToListAsync(cancellationToken);

        var leaveTypes = await db.LeaveTypes
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LeaveTypeResponse(
                item.Id,
                item.Code,
                item.Name,
                item.Description,
                item.DefaultDaysPerYear,
                item.RequiresBalance,
                item.AllowCarryOver,
                item.CarryOverMaxDays,
                item.UseFiscalYear,
                item.RequiresAttachment,
                item.IsPaid,
                item.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<LeaveAnalyticsOptionsResponse>.Ok(new LeaveAnalyticsOptionsResponse(departments, leaveTypes));
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] int? fiscalYear,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? leaveTypeId,
        [FromQuery] string? status,
        [FromQuery] bool coreOnly = true,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await CanAccessAnalytics(currentUserId.Value, cancellationToken))
        {
            return Forbid();
        }

        var report = await BuildAnalytics(fiscalYear, year, month, departmentId, leaveTypeId, status, coreOnly, cancellationToken);
        var bytes = BuildExcelWorkbook(report);
        await auditLogService.WriteAsync(currentUserId, "LeaveAnalytics.ExportExcel", "LeaveAnalytics", null, $"Exported leave analytics FY {report.Filters.FiscalYear}.", "Success", HttpContext);

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"leave-analytics-FY{ToThaiYear(report.Filters.FiscalYear)}.xlsx");
    }

    private async Task<LeaveAnalyticsResponse> BuildAnalytics(
        int? fiscalYear,
        int? year,
        int? month,
        Guid? departmentId,
        Guid? leaveTypeId,
        string? status,
        bool coreOnly,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedFiscalYear = NormalizeYear(fiscalYear ?? FiscalYearHelper.GetFiscalYear(today));
        var normalizedYear = year.HasValue ? NormalizeYear(year.Value) : (int?)null;
        var normalizedMonth = month is >= 1 and <= 12 ? month : null;
        var (startDate, endDate) = ResolvePeriod(normalizedFiscalYear, normalizedYear, normalizedMonth);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Approved" : status.Trim();

        var query = db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.LeaveType)
            .Where(item => item.StartDate <= endDate && item.EndDate >= startDate);

        if (!string.Equals(normalizedStatus, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => item.Status == normalizedStatus);
        }

        if (departmentId is not null)
        {
            query = query.Where(item => item.User != null && item.User.DepartmentId == departmentId);
        }

        if (leaveTypeId is not null)
        {
            query = query.Where(item => item.LeaveTypeId == leaveTypeId);
        }

        if (coreOnly)
        {
            query = query.Where(item => item.LeaveType != null && CoreLeaveTypeCodes.Contains(item.LeaveType.Code));
        }

        var rows = await query
            .Select(item => new AnalyticsRow(
                item.Id,
                item.RequestNumber,
                item.UserId,
                item.User != null ? item.User.FullName : null,
                item.User != null ? item.User.DepartmentId : null,
                item.User != null && item.User.Department != null ? item.User.Department.Name : "ไม่ระบุหน่วยงาน",
                item.LeaveTypeId,
                item.LeaveType != null ? item.LeaveType.Code : null,
                item.LeaveType != null ? item.LeaveType.Name : null,
                item.StartDate,
                item.EndDate,
                item.DurationType,
                item.TotalDays,
                item.Status,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        var summary = BuildSummary(rows);
        var monthlyTrend = BuildMonthlyTrend(rows, startDate, endDate);
        var departmentStacked = BuildDepartmentStacked(rows);
        var leaveTypeBreakdown = BuildLeaveTypeBreakdown(rows);
        var heatmap = BuildHeatmap(rows, startDate, endDate);
        var items = rows
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new LeaveAnalyticsTableItemResponse(
                item.Id,
                item.RequestNumber,
                item.Fullname,
                item.DepartmentName,
                item.LeaveTypeCode,
                item.LeaveTypeName,
                item.StartDate,
                item.EndDate,
                item.DurationType,
                item.TotalDays,
                item.Status))
            .ToList();

        return new LeaveAnalyticsResponse(
            new LeaveAnalyticsFilterResponse(
                normalizedFiscalYear,
                normalizedYear,
                normalizedMonth,
                departmentId,
                leaveTypeId,
                normalizedStatus,
                coreOnly,
                startDate,
                endDate),
            summary,
            monthlyTrend,
            departmentStacked,
            leaveTypeBreakdown,
            heatmap,
            items);
    }

    private static LeaveAnalyticsSummaryResponse BuildSummary(IReadOnlyList<AnalyticsRow> rows)
    {
        var topDepartment = rows
            .GroupBy(item => item.DepartmentName ?? "ไม่ระบุหน่วยงาน")
            .Select(group => new { Name = group.Key, Days = group.Sum(item => item.TotalDays) })
            .OrderByDescending(item => item.Days)
            .ThenBy(item => item.Name)
            .FirstOrDefault()?.Name;
        var topLeaveType = rows
            .GroupBy(item => item.LeaveTypeName ?? "ไม่ระบุประเภทลา")
            .Select(group => new { Name = group.Key, Days = group.Sum(item => item.TotalDays) })
            .OrderByDescending(item => item.Days)
            .ThenBy(item => item.Name)
            .FirstOrDefault()?.Name;

        return new LeaveAnalyticsSummaryResponse(
            rows.Count,
            rows.Select(item => item.UserId).Distinct().Count(),
            rows.Sum(item => item.TotalDays),
            SumByCode(rows, "SICK_LEAVE"),
            SumByCode(rows, "PERSONAL_LEAVE"),
            SumByCode(rows, "VACATION_LEAVE"),
            topDepartment,
            topLeaveType);
    }

    private static IReadOnlyList<LeaveAnalyticsMonthlyTrendResponse> BuildMonthlyTrend(IReadOnlyList<AnalyticsRow> rows, DateOnly startDate, DateOnly endDate)
    {
        var firstMonth = new DateOnly(startDate.Year, startDate.Month, 1);
        var monthCount = ((endDate.Year - firstMonth.Year) * 12) + endDate.Month - firstMonth.Month + 1;

        return Enumerable.Range(0, monthCount)
            .Select(index => firstMonth.AddMonths(index))
            .Select(month =>
            {
                var monthRows = rows
                    .Where(item => item.StartDate.Year == month.Year && item.StartDate.Month == month.Month)
                    .ToList();
                return new LeaveAnalyticsMonthlyTrendResponse(
                    month.ToString("yyyy-MM"),
                    monthRows.Count,
                    monthRows.Select(item => item.UserId).Distinct().Count(),
                    monthRows.Sum(item => item.TotalDays));
            })
            .ToList();
    }

    private static IReadOnlyList<LeaveAnalyticsDepartmentStackResponse> BuildDepartmentStacked(IReadOnlyList<AnalyticsRow> rows)
    {
        return rows
            .GroupBy(item => new { item.DepartmentId, item.DepartmentName })
            .Select(group => new LeaveAnalyticsDepartmentStackResponse(
                group.Key.DepartmentId,
                group.Key.DepartmentName ?? "ไม่ระบุหน่วยงาน",
                SumByCode(group, "SICK_LEAVE"),
                SumByCode(group, "PERSONAL_LEAVE"),
                SumByCode(group, "VACATION_LEAVE"),
                group.Sum(item => item.TotalDays)))
            .OrderByDescending(item => item.TotalDays)
            .ThenBy(item => item.DepartmentName)
            .Take(10)
            .ToList();
    }

    private static IReadOnlyList<LeaveAnalyticsLeaveTypeBreakdownResponse> BuildLeaveTypeBreakdown(IReadOnlyList<AnalyticsRow> rows)
    {
        return rows
            .Where(item => item.LeaveTypeId != Guid.Empty)
            .GroupBy(item => new { item.LeaveTypeId, item.LeaveTypeCode, item.LeaveTypeName })
            .Select(group => new LeaveAnalyticsLeaveTypeBreakdownResponse(
                group.Key.LeaveTypeId,
                group.Key.LeaveTypeCode ?? "-",
                group.Key.LeaveTypeName ?? "ไม่ระบุประเภทลา",
                group.Count(),
                group.Sum(item => item.TotalDays)))
            .OrderByDescending(item => item.TotalDays)
            .ThenBy(item => item.LeaveTypeName)
            .ToList();
    }

    private static IReadOnlyList<LeaveAnalyticsHeatmapResponse> BuildHeatmap(IReadOnlyList<AnalyticsRow> rows, DateOnly startDate, DateOnly endDate)
    {
        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;
        return Enumerable.Range(0, dayCount)
            .Select(offset => startDate.AddDays(offset))
            .Select(date =>
            {
                var dayRows = rows.Where(item => item.StartDate <= date && item.EndDate >= date).ToList();
                return new LeaveAnalyticsHeatmapResponse(
                    date,
                    dayRows.Count,
                    dayRows.Select(item => item.UserId).Distinct().Count(),
                    dayRows.Sum(item => item.TotalDays));
            })
            .ToList();
    }

    internal static byte[] BuildExcelWorkbook(LeaveAnalyticsResponse report)
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "Leave Analytics" },
            new[] { "ช่วงวันที่", $"{report.Filters.StartDate:dd/MM/yyyy} - {report.Filters.EndDate:dd/MM/yyyy}" },
            new[] { "ปีงบประมาณ", ToThaiYear(report.Filters.FiscalYear).ToString() },
            Array.Empty<string>(),
            new[] { "KPI", "ค่า" },
            new[] { "จำนวนรายการลา", report.Summary.TotalRequests.ToString() },
            new[] { "จำนวนบุคลากรที่ลาแบบ unique", report.Summary.UniqueUsers.ToString() },
            new[] { "จำนวนวันลารวม", report.Summary.TotalDays.ToString("0.##") },
            new[] { "ลาป่วยรวม", report.Summary.SickDays.ToString("0.##") },
            new[] { "ลากิจรวม", report.Summary.PersonalDays.ToString("0.##") },
            new[] { "ลาพักผ่อนรวม", report.Summary.VacationDays.ToString("0.##") },
            new[] { "หน่วยงานที่ลามากที่สุด", LeaveReportsController.SafeExcelCell(report.Summary.TopDepartment) },
            new[] { "ประเภทลาที่ใช้มากที่สุด", LeaveReportsController.SafeExcelCell(report.Summary.TopLeaveType) },
            Array.Empty<string>(),
            new[] { "รายการคำขอลา" },
            new[] { "วันที่", "ผู้ลา", "หน่วยงาน", "ประเภทลา", "จำนวนวัน", "สถานะ" }
        };

        foreach (var item in report.Items)
        {
            rows.Add([
                $"{item.StartDate:dd/MM/yyyy} - {item.EndDate:dd/MM/yyyy}",
                LeaveReportsController.SafeExcelCell(item.Fullname),
                LeaveReportsController.SafeExcelCell(item.DepartmentName),
                LeaveReportsController.SafeExcelCell(item.LeaveTypeName),
                item.TotalDays.ToString("0.##"),
                LeaveReportsController.SafeExcelCell(item.Status)
            ]);
        }

        return SimpleXlsxWriter.CreateWorkbook(rows, [24, 26, 26, 24, 14, 14]);
    }

    private async Task<bool> CanAccessAnalytics(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await db.UserRoles
            .AsNoTracking()
            .Include(item => item.Role)
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .Select(item => item.Role!.Name)
            .ToListAsync(cancellationToken);

        if (roles.Any(role => ReportRoles.Contains(role)))
        {
            return true;
        }

        return await db.RolePermissions
            .AsNoTracking()
            .Include(item => item.Permission)
            .Where(item => item.Role != null && item.Role.UserRoles.Any(userRole => userRole.UserId == userId))
            .AnyAsync(item =>
                item.Permission != null &&
                (item.Permission.Code == AnalyticsPermission || item.Permission.Code == ReportPermission),
                cancellationToken);
    }

    private static decimal SumByCode(IEnumerable<AnalyticsRow> rows, string leaveTypeCode)
    {
        return rows
            .Where(item => string.Equals(item.LeaveTypeCode, leaveTypeCode, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.TotalDays);
    }

    private static (DateOnly StartDate, DateOnly EndDate) ResolvePeriod(int fiscalYear, int? year, int? month)
    {
        if (year.HasValue && month.HasValue)
        {
            var start = new DateOnly(year.Value, month.Value, 1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

        if (year.HasValue)
        {
            return (new DateOnly(year.Value, 1, 1), new DateOnly(year.Value, 12, 31));
        }

        var fiscalStart = new DateOnly(fiscalYear - 1, FiscalYearHelper.StartMonth, FiscalYearHelper.StartDay);
        return (fiscalStart, fiscalStart.AddYears(1).AddDays(-1));
    }

    private static int NormalizeYear(int year)
    {
        return year >= 2400 ? year - 543 : year;
    }

    private static int ToThaiYear(int year)
    {
        return year >= 2400 ? year : year + 543;
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private sealed record AnalyticsRow(
        Guid Id,
        string? RequestNumber,
        Guid UserId,
        string? Fullname,
        Guid? DepartmentId,
        string? DepartmentName,
        Guid LeaveTypeId,
        string? LeaveTypeCode,
        string? LeaveTypeName,
        DateOnly StartDate,
        DateOnly EndDate,
        string DurationType,
        decimal TotalDays,
        string Status,
        DateTime CreatedAt);
}
