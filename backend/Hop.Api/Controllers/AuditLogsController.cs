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

[ApiController]
[Route("api/audit-logs")]
[Authorize]
[RequirePermission("SystemSettings.View")]
public class AuditLogsController(AppDbContext db, IAuditRetentionService auditRetentionService, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditLogResponse>>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.AuditLogs
            .AsNoTracking()
            .Include(auditLog => auditLog.User)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(auditLog => auditLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionKeyword = action.Trim().ToLower();
            query = query.Where(auditLog =>
                auditLog.Action.ToLower().Contains(actionKeyword) ||
                auditLog.EntityName.ToLower().Contains(actionKeyword) ||
                (auditLog.Detail != null && auditLog.Detail.ToLower().Contains(actionKeyword)));
        }

        if (from is not null)
        {
            query = query.Where(auditLog => auditLog.CreatedAt >= from.Value.ToUniversalTime());
        }

        if (to is not null)
        {
            query = query.Where(auditLog => auditLog.CreatedAt <= to.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(auditLog =>
                auditLog.Action.ToLower().Contains(keyword) ||
                auditLog.EntityName.ToLower().Contains(keyword) ||
                (auditLog.EntityId != null && auditLog.EntityId.ToLower().Contains(keyword)) ||
                (auditLog.Detail != null && auditLog.Detail.ToLower().Contains(keyword)) ||
                (auditLog.User != null && (
                    auditLog.User.Username.ToLower().Contains(keyword) ||
                    auditLog.User.FullName.ToLower().Contains(keyword))));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(auditLog => auditLog.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(auditLog => ToResponse(auditLog))
            .ToListAsync();

        return ApiResponse<PagedResponse<AuditLogResponse>>.Ok(new PagedResponse<AuditLogResponse>(
            items,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AuditLogResponse>>> GetAuditLog(Guid id)
    {
        var auditLog = await db.AuditLogs
            .AsNoTracking()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (auditLog is null)
        {
            return NotFound(ApiResponse<AuditLogResponse>.Fail("Audit log not found."));
        }

        return ApiResponse<AuditLogResponse>.Ok(ToResponse(auditLog));
    }

    [HttpGet("export")]
    [RequirePermission("SystemSettings.Export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? search = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = db.AuditLogs
            .AsNoTracking()
            .Include(item => item.User)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(item => item.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionKeyword = action.Trim().ToLower();
            query = query.Where(item =>
                item.Action.ToLower().Contains(actionKeyword) ||
                item.EntityName.ToLower().Contains(actionKeyword) ||
                (item.Detail != null && item.Detail.ToLower().Contains(actionKeyword)));
        }

        if (from is not null)
        {
            query = query.Where(item => item.CreatedAt >= from.Value.ToUniversalTime());
        }

        if (to is not null)
        {
            query = query.Where(item => item.CreatedAt <= to.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(item =>
                item.Action.ToLower().Contains(keyword) ||
                item.EntityName.ToLower().Contains(keyword) ||
                (item.Detail != null && item.Detail.ToLower().Contains(keyword)));
        }

        var rows = await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(10000)
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Timestamp,Username,Fullname,Action,Resource,ResourceId,IpAddress,Result,Detail");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', [
                Csv(row.CreatedAt.ToString("O")),
                Csv(row.User?.Username),
                Csv(row.User?.FullName),
                Csv(row.Action),
                Csv(row.EntityName),
                Csv(row.EntityId),
                Csv(row.IpAddress),
                Csv(row.Result),
                Csv(row.Detail)
            ]));
        }

        await auditLogService.WriteAsync(GetCurrentUserId(), "AuditLog.Export", "AuditLog", null, $"Exported {rows.Count} audit logs.", "Success", HttpContext);
        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("export-excel")]
    [RequirePermission("SystemSettings.Export")]
    public async Task<IActionResult> ExportAuditLogsExcel(
        [FromQuery] string? search = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var rows = await BuildExportRows(search, userId, action, from, to);
        var worksheetRows = new List<IReadOnlyList<string>>
        {
            new[] { "รายงานบันทึกการใช้งาน" },
            new[] { "วันที่", "ผู้ใช้งาน", "ชื่อ-สกุล", "การกระทำ", "ทรัพยากร", "Resource ID", "IP Address", "ผลลัพธ์", "รายละเอียด" }
        };

        foreach (var row in rows)
        {
            worksheetRows.Add(new[]
            {
                row.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                SafeExcelCell(row.User?.Username),
                SafeExcelCell(row.User?.FullName),
                SafeExcelCell(row.Action),
                SafeExcelCell(row.EntityName),
                SafeExcelCell(row.EntityId),
                SafeExcelCell(row.IpAddress),
                SafeExcelCell(row.Result),
                SafeExcelCell(row.Detail)
            });
        }

        await auditLogService.WriteAsync(GetCurrentUserId(), "AuditLog.ExportExcel", "AuditLog", null, $"Exported {rows.Count} audit logs to Excel.", "Success", HttpContext);
        return File(SimpleXlsxWriter.CreateWorkbook(worksheetRows, [18, 18, 24, 28, 18, 22, 18, 14, 42]), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet("export-pdf")]
    [RequirePermission("SystemSettings.Export")]
    public async Task<IActionResult> ExportAuditLogsPdf(
        [FromQuery] string? search = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        const int rowsPerPage = 30;
        var rows = await BuildExportRows(search, userId, action, from, to);
        var pages = new List<IReadOnlyList<PdfLine>>();
        var chunks = rows.Count == 0 ? [Array.Empty<AuditLog>()] : rows.Chunk(rowsPerPage).ToArray();

        for (var pageIndex = 0; pageIndex < chunks.Length; pageIndex++)
        {
            var lines = new List<PdfLine>
            {
                new("รายงานบันทึกการใช้งาน", 50, 790, 18),
                new($"จำนวนรายการ: {rows.Count}", 50, 762, 11),
                new($"หน้า {pageIndex + 1}/{chunks.Length}", 500, 790, 10),
                new("วันที่ | ผู้ใช้งาน | การกระทำ | ผลลัพธ์", 50, 735, 10)
            };

            var y = 713;
            foreach (var row in chunks[pageIndex])
            {
                lines.Add(new($"{row.CreatedAt:dd/MM/yyyy HH:mm} | {row.User?.Username ?? "-"} | {row.Action} | {row.Result}", 50, y, 9));
                y -= 20;
            }

            if (chunks[pageIndex].Length == 0)
            {
                lines.Add(new("ไม่พบบันทึกการใช้งานตามตัวกรองที่เลือก", 50, y, 11));
            }

            pages.Add(lines);
        }

        await auditLogService.WriteAsync(GetCurrentUserId(), "AuditLog.ExportPdf", "AuditLog", null, $"Exported {rows.Count} audit logs to PDF.", "Success", HttpContext);
        return File(SimplePdfWriter.CreateA4Pages(pages, logo: null), "application/pdf", $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    [HttpPost("retention/run")]
    [RequirePermission("SystemSettings.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> RunRetention()
    {
        var deletedCount = await auditRetentionService.RunAsync(HttpContext.RequestAborted);
        await auditLogService.WriteAsync(GetCurrentUserId(), "AuditLog.RetentionRun", "AuditLog", null, $"Deleted {deletedCount} expired audit logs.", "Success", HttpContext);
        return ApiResponse<object>.Ok(new { deletedCount });
    }

    private static AuditLogResponse ToResponse(AuditLog auditLog)
    {
        return new AuditLogResponse(
            auditLog.Id,
            auditLog.UserId,
            auditLog.User?.Username,
            auditLog.User?.FullName,
            auditLog.Action,
            auditLog.EntityName,
            auditLog.EntityId,
            auditLog.Detail,
            auditLog.IpAddress,
            auditLog.Result,
            auditLog.CreatedAt
        );
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private async Task<List<AuditLog>> BuildExportRows(string? search, Guid? userId, string? action, DateTime? from, DateTime? to)
    {
        var query = db.AuditLogs
            .AsNoTracking()
            .Include(item => item.User)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(item => item.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionKeyword = action.Trim().ToLower();
            query = query.Where(item =>
                item.Action.ToLower().Contains(actionKeyword) ||
                item.EntityName.ToLower().Contains(actionKeyword) ||
                (item.Detail != null && item.Detail.ToLower().Contains(actionKeyword)));
        }

        if (from is not null)
        {
            query = query.Where(item => item.CreatedAt >= from.Value.ToUniversalTime());
        }

        if (to is not null)
        {
            query = query.Where(item => item.CreatedAt <= to.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(item =>
                item.Action.ToLower().Contains(keyword) ||
                item.EntityName.ToLower().Contains(keyword) ||
                (item.EntityId != null && item.EntityId.ToLower().Contains(keyword)) ||
                (item.Detail != null && item.Detail.ToLower().Contains(keyword)) ||
                (item.User != null && (
                    item.User.Username.ToLower().Contains(keyword) ||
                    item.User.FullName.ToLower().Contains(keyword))));
        }

        return await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(10000)
            .ToListAsync();
    }

    private static string SafeExcelCell(string? value)
    {
        var normalized = value ?? "-";
        if (normalized.Length > 0 && "=+-@".Contains(normalized[0], StringComparison.Ordinal))
        {
            normalized = "'" + normalized;
        }

        return normalized;
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
