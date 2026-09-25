using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class RepairReportsTests
{
    [Fact]
    public async Task SeptemberReport_UsesBangkokSubmissionDateAndCurrentStatusWithLastAcceptedClose()
    {
        await using var db = Database();
        var (closed, reopened) = await Seed(db);
        var controller = Controller(db);
        var filter = new RepairReportFilter { From = new(2026, 9, 1), To = new(2026, 9, 30) };

        var summary = Data(await controller.Summary(filter, default));
        Assert.Equal(2, summary.GetProperty("total").GetInt32());
        Assert.Equal(1, summary.GetProperty("counts").EnumerateArray().Single(x => x.GetProperty("status").GetString() == "Closed").GetProperty("count").GetInt32());
        Assert.Equal(50m, summary.GetProperty("acceptanceRate").GetDecimal());
        Assert.Equal(2, summary.GetProperty("months")[0].GetProperty("total").GetInt32());

        var items = Data(await controller.Items(filter, ct: default)).GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);
        Assert.Equal(new DateTime(2026, 9, 3, 5, 0, 0, DateTimeKind.Utc), items.Single(x => x.GetProperty("id").GetGuid() == closed).GetProperty("closedAt").GetDateTime().ToUniversalTime());
        Assert.Equal(JsonValueKind.Null, items.Single(x => x.GetProperty("id").GetGuid() == reopened).GetProperty("closedAt").ValueKind);
    }

    [Fact]
    public async Task Exports_UseSameFilteredRowsAndRecordAudit()
    {
        await using var db = Database();
        await Seed(db);
        var audit = new CaptureAudit();
        var controller = Controller(db, audit);
        var filter = new RepairReportFilter { From = new(2026, 9, 1), To = new(2026, 9, 30), Status = "Closed" };

        var excel = Assert.IsType<FileContentResult>(await controller.ExportExcel(filter, default));
        using var archive = new ZipArchive(new MemoryStream(excel.FileContents), ZipArchiveMode.Read);
        var sheet = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open()).ReadToEnd();
        Assert.Contains("REP-000001", sheet);
        Assert.DoesNotContain("REP-000002", sheet);
        Assert.Contains("s=\"1\"", sheet); // Gregorian typed date with Thai display style.
        var pdf = Assert.IsType<FileContentResult>(await controller.ExportPdf(filter, default));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.FileContents, 0, 4));
        Assert.Equal(["RepairReport.ExportExcel", "RepairReport.ExportPdf"], audit.Actions);
    }

    [Fact]
    public void ReportEndpoints_RequireViewAll()
    {
        var permission = Assert.Single(typeof(RepairReportsController).GetCustomAttributes<RequirePermissionAttribute>());
        Assert.Equal(RepairPermissions.ViewAll, permission.PermissionCode);
    }

    [Fact]
    public async Task PostgreSqlReportItems_TranslateOrderingAndPaging()
    {
        var connection = Environment.GetEnvironmentVariable("HOP_REPORT_READONLY_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection)) return;

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
        var filter = new RepairReportFilter { From = new(2026, 9, 1), To = new(2026, 9, 30) };
        var result = Data(await Controller(db).Items(filter, page: 1, pageSize: 20));
        Assert.True(result.GetProperty("total").GetInt32() >= 0);
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static RepairReportsController Controller(AppDbContext db, CaptureAudit? audit = null)
    {
        return new(db, audit ?? new CaptureAudit()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test")) } } };
    }
    private static JsonElement Data(IActionResult result) => JsonSerializer.SerializeToElement(Assert.IsType<ApiResponse<object>>(Assert.IsType<OkObjectResult>(result).Value).Data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    private static async Task<(Guid Closed, Guid Reopened)> Seed(AppDbContext db)
    {
        var user = new User { Id = Guid.NewGuid(), Username = "reporter", FullName = "ผู้แจ้ง" };
        var department = new Department { Id = Guid.NewGuid(), Name = "หน่วยงานทดสอบ" };
        var category = new RepairCategory { Id = Guid.NewGuid(), Name = "งานไฟฟ้า", TeamCode = "GENERAL" };
        db.Users.Add(user); db.Departments.Add(department); db.Set<RepairCategory>().Add(category);
        RepairRequest Add(long number, string status, DateTime created)
        {
            var request = new RepairRequest { Id = Guid.NewGuid(), Number = number, RequesterId = user.Id, DepartmentId = department.Id, CategoryId = category.Id, TeamCode = "GENERAL", Title = $"งาน {number}", Location = "อาคาร", Status = status, CreatedAt = created };
            db.Set<RepairRequest>().Add(request); return request;
        }
        Add(3, "Submitted", new DateTime(2026, 8, 31, 16, 59, 0, DateTimeKind.Utc));
        var closed = Add(1, "Closed", new DateTime(2026, 8, 31, 17, 30, 0, DateTimeKind.Utc));
        var reopened = Add(2, "InProgress", new DateTime(2026, 9, 2, 1, 0, 0, DateTimeKind.Utc));
        db.Set<RepairEvent>().AddRange(new RepairEvent { RequestId = closed.Id, ActorId = user.Id, Action = "accept", CreatedAt = new DateTime(2026, 9, 3, 5, 0, 0, DateTimeKind.Utc) }, new RepairEvent { RequestId = reopened.Id, ActorId = user.Id, Action = "accept", CreatedAt = new DateTime(2026, 9, 3, 5, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
        return (closed.Id, reopened.Id);
    }
    private sealed class CaptureAudit : IAuditLogService
    {
        public List<string> Actions { get; } = [];
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null) { Actions.Add(action); return Task.CompletedTask; }
    }
}
