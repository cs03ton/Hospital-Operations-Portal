using System.IO.Compression;
using System.Globalization;
using System.Xml.Linq;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Hop.Api.Tests;

public sealed class OvertimeWorkReportTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static OvertimeWorkReportService Service(AppDbContext db) => new(new CorrespondenceMemoService(db, new ConfigurationBuilder().Build()));
    private static async Task<(User Author, User Head)> Seed(AppDbContext db)
    {
        var department = new Department { Name = "กลุ่มงานสุขภาพดิจิทัล" };
        var author = new User { Username = "reporter", FullName = "นายทดสอบ ผู้รายงาน", Position = "นักวิชาการคอมพิวเตอร์", Department = department };
        var head = new User { Username = "head", FullName = "นายหัวหน้า ผู้ควบคุม", Position = "หัวหน้ากลุ่มงาน", Department = department, UserRoles = [new() { Role = new() { Name = "DepartmentHead" } }] };
        db.AddRange(author, head); await db.SaveChangesAsync(); return (author, head);
    }
    private static OvertimeReportEntry Entry(int day = 18) => new() { Date = new(2026, 10, day), EndDate = new(2026, 10, day), StartTime = "08:29", EndTime = "16:34", Hours = 7,
        Tasks = [new("กวาดส่งข้อมูลผู้ป่วยนอกสิทธิ UC ข้อมูลวันที่ 9–17 ตุลาคม 2569", "จำนวน 1 งาน"), new("ดึงลูกหนี้ค่ารักษาพยาบาลผู้ป่วยนอก", "จำนวน 159 ราย")] };
    private static OvertimeReportRequest Request(User author, User head) => new() { DepartmentId = author.DepartmentId!.Value, ProjectName = "งานประกันสุขภาพ ยุทธศาสตร์และสารสนเทศทางการแพทย์", SupervisorUserId = head.Id, Entries = [Entry(23), Entry()] };
    [Fact]
    public async Task Resolves_real_reporter_and_department_head_sorts_entries_and_does_not_save_content()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var service = Service(db); var request = Request(author, head);
        var report = await service.ResolveAsync(author.Id, request, default);
        Assert.Equal(author.Id, report.Reporter.Id); Assert.Equal(head.Id, report.Supervisor.Id); Assert.Equal(18, report.Entries[0].Date.Day);
        Assert.Equal(8, report.Entries[0].Hours); Assert.Empty(db.AuditLogs); Assert.Empty(db.DomainEvents);
        request.SupervisorUserId = author.Id; await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(author.Id, request, default));
        request.SupervisorUserId = head.Id; head.Department = new Department { Name = "หน่วยงานอื่น" }; await db.SaveChangesAsync();
        Assert.Equal(head.Id, (await service.ResolveAsync(author.Id, request, default)).Supervisor.Id);
        author.IsActive = false; await db.SaveChangesAsync(); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ResolveAsync(author.Id, request, default));
        Assert.Equal(CorrespondenceAccess.Permission, typeof(OvertimeWorkReportController).GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>().Single().PermissionCode);
    }
    [Fact]
    public async Task Offers_active_approvers_across_departments_and_rechecks_roles_when_rendering()
    {
        await using var db = Db(); var (author, head) = await Seed(db);
        var permission = new Permission { Code = LeavePermissions.ApproveCurrentStep, Name = "อนุมัติลา" };
        var customRole = new Role { Name = "CustomApproval", RolePermissions = [new() { Permission = permission }] };
        var other = new Department { Name = "หน่วยงานอื่น" };
        var director = new User { Username = "director", FullName = "ผู้อำนวยการ", Position = "แพทย์", Department = other, UserRoles = [new() { Role = new() { Name = "Director" } }, new() { Role = customRole }] };
        var custom = new User { Username = "custom", FullName = "ผู้อนุมัติต่างหน่วยงาน", Position = "หัวหน้าฝ่าย", Department = other, UserRoles = [new() { Role = customRole }] };
        var inactive = new User { Username = "inactive", FullName = "ปิดบัญชี", IsActive = false, UserRoles = [new() { Role = customRole }] };
        var inactiveRole = new User { Username = "inactive-role", FullName = "บทบาทปิด", UserRoles = [new() { Role = new() { Name = "DepartmentHead", IsActive = false } }] };
        var inactivePermission = new User { Username = "inactive-permission", FullName = "สิทธิ์ปิด", UserRoles = [new() { Role = new() { Name = "InactiveApproval", RolePermissions = [new() { Permission = new() { Code = FleetPermissions.AdminReviewApprove, Name = "ตรวจรถ", IsActive = false } }] } }] };
        db.AddRange(director, custom, inactive, inactiveRole, inactivePermission); await db.SaveChangesAsync();
        var memos = new CorrespondenceMemoService(db, new ConfigurationBuilder().Build());
        var options = await memos.OptionsAsync(author.Id, default);
        Assert.Equal(new[] { head.Id, director.Id, custom.Id }.Order(), options.Approvers!.Select(p => p.Id).Order());
        Assert.Single(options.Heads); Assert.Single(options.Directors);
        var request = Request(author, custom);
        Assert.Equal(custom.Id, (await Service(db).ResolveAsync(author.Id, request, default)).Supervisor.Id);
        request.SupervisorUserId = director.Id;
        Assert.Equal(director.Id, (await Service(db).ResolveAsync(author.Id, request, default)).Supervisor.Id);
        permission.IsActive = false; await db.SaveChangesAsync(); request.SupervisorUserId = custom.Id;
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, request, default));
        permission.IsActive = true; customRole.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, request, default));
        Assert.Empty(db.AuditLogs); Assert.Empty(db.DomainEvents);
    }
    [Theory]
    [InlineData("date")][InlineData("time")][InlineData("minutes")][InlineData("reverse")][InlineData("quantity")][InlineData("tasks")][InlineData("duplicates")][InlineData("limit")][InlineData("xml")]
    public async Task Rejects_invalid_or_incomplete_reports_without_partial_writes(string invalid)
    {
        await using var db = Db(); var (author, head) = await Seed(db); var request = Request(author, head);
        switch (invalid)
        {
            case "date": request.Entries[0].Date = default; break;
            case "time": request.Entries[0].StartTime = "24:00"; break;
            case "reverse": request.Entries[0].EndTime = "07:00"; break;
            case "minutes": request.Entries[0].StartTime = "08:60"; break;
            case "quantity": request.Entries[0].Tasks[0] = new("งานทดสอบ", ""); break;
            case "tasks": request.Entries[0].Tasks = Enumerable.Repeat(new OvertimeReportTask("งาน", "1 งาน"), 21).ToList(); break;
            case "duplicates": request.Entries = [Entry(), Entry()]; break;
            case "limit": request.Entries = Enumerable.Range(0, 51).Select(_ => Entry()).ToList(); break;
            case "xml": request.ProjectName = "ข้อความ\u0001"; break;
        }
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, request, default));
        Assert.Empty(db.AuditLogs); Assert.Empty(db.DomainEvents);
    }
    [Fact]
    public async Task Calculates_overnight_hours_when_no_hours_are_submitted()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var request = Request(author, head);
        request.Entries = [new() { Date = new(2026, 12, 31), EndDate = new(2027, 1, 1), StartTime = "23:00", EndTime = "07:00", Tasks = [new("ปฏิบัติงานข้ามคืน", "จำนวน 1 งาน")] }];
        var report = await Service(db).ResolveAsync(author.Id, request, default);
        Assert.Equal(8, report.Entries[0].Hours); Assert.Equal(2027, report.Entries[0].EndDate.Year);
        head.IsActive = false; await db.SaveChangesAsync(); await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, request, default));
    }
    [Fact]
    public async Task Generates_five_column_docx_and_pdf_with_exact_tasks_quantities_and_two_signatures()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var report = await Service(db).ResolveAsync(author.Id, Request(author, head), default);
        var files = new OvertimeWorkReportRenderer(new Environment()).Render(report);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(files.PdfBase64)));
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64)));
        using var input = zip.GetEntry("word/document.xml")!.Open(); var xml = XDocument.Load(input);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var text = string.Concat(xml.Descendants(w + "t").Select(t => t.Value));
        Assert.Contains(OvertimeWorkReportRenderer.Title, text); Assert.Contains(author.FullName, text); Assert.Contains(head.FullName, text);
        Assert.Contains("18 ตุลาคม 2569", text); Assert.Contains("08.29 น.–16.34 น.", text); Assert.Contains("(8 ชม.)", text);
        Assert.Contains("ผู้รายงาน", text); Assert.Contains("ผู้ควบคุม", text); Assert.DoesNotContain("บันทึกข้อความ", text);
        foreach (var person in new[] { author, head })
        {
            foreach (var line in new[] { $"({person.FullName})", person.Position! })
            {
                var paragraph = xml.Descendants(w + "p").Single(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)) == line);
                Assert.Equal("60", paragraph.Element(w + "pPr")!.Element(w + "spacing")!.Attribute(w + "before")!.Value);
            }
        }
        Assert.Equal(5, xml.Descendants(w + "tblGrid").First().Elements().Count()); Assert.Single(xml.Descendants(w + "tblHeader"));
        Assert.Equal(2, xml.Descendants(w + "tbl").Last().Elements(w + "tr").Single().Elements(w + "tc").Count());
        var blankRows = xml.Descendants(w + "tr").Where(row => row.Descendants(w + "t").All(t => string.IsNullOrEmpty(t.Value))).ToList();
        Assert.Single(blankRows);
        Assert.Equal("320", blankRows[0].Descendants(w + "trHeight").Single().Attribute(w + "val")!.Value);
        Assert.Contains("ดึงลูกหนี้", string.Concat(xml.Descendants(w + "tbl").First().Elements(w + "tr").Last().Descendants(w + "t").Select(t => t.Value)));
        Assert.All(xml.Descendants(w + "rFonts"), font => Assert.Equal("TH SarabunPSK", font.Attribute(w + "ascii")!.Value));
        var workRows = xml.Descendants(w + "tr").Where(row => string.Concat(row.Descendants(w + "t").Select(t => t.Value)).Contains("ดึงลูกหนี้"));
        Assert.All(workRows, row => Assert.Contains("จำนวน 159 ราย", string.Concat(row.Descendants(w + "t").Select(t => t.Value))));
        Assert.Equal("overtime-report-v2", files.TemplateVersion); Assert.Empty(db.AuditLogs);
    }
    [Theory]
    [InlineData("2026-11-05", "03:00", "2026-11-05", "07:00", 4)]
    [InlineData("2026-11-05", "03:00", "2026-11-05", "07:59", 4)]
    [InlineData("2026-11-05", "08:59", "2026-11-05", "09:00", 0)]
    [InlineData("2026-12-31", "23:45", "2027-01-01", "00:44", 0)]
    [InlineData("2026-12-31", "23:45", "2027-01-01", "00:45", 1)]
    [InlineData("2026-12-31", "23:30", "2027-01-01", "07:29", 7)]
    [InlineData("2024-02-28", "23:59", "2024-03-01", "00:00", 24)]
    public async Task Derives_full_hours_ignoring_client_values_and_keeps_zero_visible(string date, string start, string endDate, string end, int expected)
    {
        await using var db = Db(); var (author, head) = await Seed(db); var request = Request(author, head); var entry = Entry();
        entry.Date = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture); entry.EndDate = DateOnly.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        entry.StartTime = start; entry.EndTime = end; entry.Hours = 999;
        request.Entries = [entry]; var report = await Service(db).ResolveAsync(author.Id, request, default);
        Assert.Equal((decimal)expected, report.Entries[0].Hours); Assert.Equal(999, entry.Hours);
        if (expected == 0)
        {
            var files = new OvertimeWorkReportRenderer(new Environment()).Render(report);
            using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
            Assert.Contains("(0 ชม.)", await reader.ReadToEndAsync());
        }
    }
    [Fact]
    public async Task Long_report_can_flow_across_pages_without_losing_rows()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var request = Request(author, head);
        request.Entries = Enumerable.Range(1, 12).Select(day => { var entry = Entry(day); entry.Tasks = Enumerable.Range(1, 10).Select(i => new OvertimeReportTask($"งาน {day}-{i} รายละเอียดการปฏิบัติงานที่ต้องแสดงครบถ้วนพร้อมปริมาณในแถวเดียวกัน", $"จำนวน {i} งาน")).ToList(); return entry; }).ToList();
        var files = new OvertimeWorkReportRenderer(new Environment()).Render(await Service(db).ResolveAsync(author.Id, request, default));
        Assert.True(Convert.FromBase64String(files.PdfBase64).Length > 10000);
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        Assert.Contains("งาน 12-10", await reader.ReadToEndAsync());
    }
    private sealed class Environment : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = FindRoot();
        private static string FindRoot() { var current = new DirectoryInfo(AppContext.BaseDirectory); while (current != null) { var path = Path.Combine(current.FullName, "backend", "Hop.Api"); if (Directory.Exists(path)) return path; current = current.Parent; } throw new InvalidOperationException("Repository root not found"); }
        public string WebRootPath { get; set; } = ""; public string EnvironmentName { get; set; } = "Tests"; public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
