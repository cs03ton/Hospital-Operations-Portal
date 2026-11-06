using System.IO.Compression;
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

public sealed class Cho11Tests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<(User Author, User Head)> Seed(AppDbContext db)
    {
        var department = new Department { Name = "กลุ่มงานสุขภาพดิจิทัล" };
        var author = new User { Username = "cho11-author", NamePrefix = "นาย", FullName = "ทดสอบ นามสกุลดิจิ", Position = "นักวิชาการคอมพิวเตอร์", Department = department };
        var head = new User { Username = "cho11-head", NamePrefix = "นางสาว", FullName = "ผู้ตรวจสอบ ทดสอบ", Position = "นักเทคโนโลยีสารสนเทศปฏิบัติการ", Department = department, UserRoles = [new() { Role = new Role { Name = "DepartmentHead" } }] };
        db.AddRange(author, head); await db.SaveChangesAsync(); return (author, head);
    }
    private static Cho11Service Service(AppDbContext db) { var config = new ConfigurationBuilder().Build(); return new(new(db, config), config); }
    private static Cho11Request Request(User head) => new() { StartMonth = "2026-10", EndMonth = "2026-12", EmploymentMonth = "2025-11", Hospital = "โรงพยาบาลนาหมื่น", Province = "น่าน", ReviewerUserId = head.Id };
    [Fact]
    public async Task Uses_real_names_rechecks_reviewer_and_never_persists_document()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var service = Service(db);
        var options = await service.OptionsAsync(author.Id, default);
        Assert.Equal("นายทดสอบ นามสกุลดิจิ", options.Author.Name); Assert.Equal("โรงพยาบาลนาหมื่น", options.Hospital);
        var input = Request(head); var result = await service.ResolveAsync(author.Id, input, default);
        Assert.Equal(3, result.Months.Count); Assert.Equal((1, 0, 0), Cho11Service.Tenure(result.EmploymentDate, result.Months[0]));
        input.Hospital = "โรงพยาบาลทดสอบ"; input.Province = "เชียงใหม่";
        Assert.Equal("เชียงใหม่", (await service.ResolveAsync(author.Id, input, default)).Input.Province);
        Assert.Null(author.EmploymentStartDate); Assert.Empty(db.AuditLogs); Assert.Empty(db.DomainEvents);
        head.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(author.Id, input, default));
        author.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OptionsAsync(author.Id, default));
        Assert.Equal(CorrespondenceAccess.Permission, typeof(Cho11Controller).GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>().Single().PermissionCode);
    }
    [Theory]
    [InlineData("2026-12", "2026-10", "2025-01")]
    [InlineData("2026-01", "2027-01", "2025-01")]
    [InlineData("2026-10", "2026-12", "2026-11")]
    [InlineData("2569-10", "2026-12", "2025-01")]
    [InlineData("2026-13", "2026-12", "2025-01")]
    public async Task Rejects_invalid_ranges(string start, string end, string employment)
    {
        await using var db = Db(); var (author, head) = await Seed(db); var input = Request(head);
        input.StartMonth = start; input.EndMonth = end; input.EmploymentMonth = employment;
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, input, default));
    }
    [Fact]
    public async Task Validates_profile_fields_and_unlisted_reviewers()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var input = Request(head); var service = Service(db);
        input.ReviewerUserId = author.Id; await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(author.Id, input, default));
        input.ReviewerUserId = head.Id; input.Province = " "; await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(author.Id, input, default));
        input.Province = "น่าน"; author.Position = null; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(author.Id, input, default));
    }
    [Theory]
    [InlineData("2024-02", "2024-02", 0, 1, "๒๙ กุมภาพันธ์ ๒๕๖๗")]
    [InlineData("2023-03", "2024-02", 1, 0, "๒๙ กุมภาพันธ์ ๒๕๖๗")]
    [InlineData("2025-02", "2025-02", 0, 1, "๒๘ กุมภาพันธ์ ๒๕๖๘")]
    public async Task Inclusive_month_tenure_and_month_end(string employment, string month, int years, int months, string expected)
    {
        await using var db = Db(); var (author, head) = await Seed(db); var input = Request(head);
        input.StartMonth = input.EndMonth = month; input.EmploymentMonth = employment;
        var report = await Service(db).ResolveAsync(author.Id, input, default);
        Assert.Equal((years, months, 0), Cho11Service.Tenure(report.EmploymentDate, report.Months[0]));
        var files = new Cho11Renderer(new TestEnvironment()).Render(report);
        Assert.Contains(expected, Xml(files).ToString());
    }
    [Fact]
    public async Task Produces_one_page_per_month_and_preserves_form_and_signatures()
    {
        await using var db = Db(); var (author, head) = await Seed(db);
        var report = await Service(db).ResolveAsync(author.Id, Request(head), default);
        var files = new Cho11Renderer(new TestEnvironment()).Render(report); var xml = Xml(files);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        Assert.Equal(2, xml.Descendants(w + "pageBreakBefore").Count());
        var text = string.Concat(xml.Descendants(w + "t").Select(t => t.Value));
        Assert.Contains("ตุลาคม พ.ศ. ๒๕๖๙", text); Assert.Contains("ธันวาคม พ.ศ. ๒๕๖๙", text);
        Assert.Contains(report.Author.Name, text); Assert.Contains(report.Reviewer.Name, text); Assert.Contains("๖. ปฏิบัติงาน", text);
        Assert.Contains("...๑... ปี ...๐... เดือน ...๐... วัน", text);
        Assert.Contains("...๑... ปี ...๒... เดือน ...๐... วัน", text);
        Assert.Equal(3, xml.Descendants(w + "tcMar").Count());
        foreach (var cell in xml.Descendants(w + "tc").Where(c => c.Descendants(w + "tcMar").Any())) {
            var paragraphs = cell.Elements(w + "p").Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value))).ToArray();
            Assert.Equal("ผู้ตรวจสอบ", paragraphs[1]);
            Assert.Equal("(.....................................)", paragraphs[2]);
            Assert.Equal("(" + report.Reviewer.Name + ")", paragraphs[3]);
        }
        Assert.All(xml.Descendants(w + "tcMar"), margin => Assert.Equal("567", margin.Element(w + "right")!.Attribute(w + "w")!.Value));
        Assert.True(Convert.FromBase64String(files.PdfBase64).Length > 10000);
        // Optional development-only visual QA output, never used by the API.
        var qa = System.Environment.GetEnvironmentVariable("CHO11_QA_OUTPUT");
        if (!string.IsNullOrEmpty(qa)) { Directory.CreateDirectory(qa); await File.WriteAllBytesAsync(Path.Combine(qa, "cho11.pdf"), Convert.FromBase64String(files.PdfBase64)); await File.WriteAllBytesAsync(Path.Combine(qa, "cho11.docx"), Convert.FromBase64String(files.DocxBase64)); }
    }
    private static XDocument Xml(MemoFiles files) { using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var stream = zip.GetEntry("word/document.xml")!.Open(); return XDocument.Load(stream); }
    [Theory]
    [InlineData("2026-10-15", "2026-10-01", 0, 0, 17)]
    [InlineData("2025-10-15", "2026-10-01", 1, 0, 17)]
    [InlineData("2024-02-29", "2024-02-01", 0, 0, 1)]
    [InlineData("2026-10-01", "2026-10-01", 0, 1, 0)]
    public void Exact_start_date_uses_calendar_years_months_and_inclusive_days(string employment, string month, int years, int months, int days)
        => Assert.Equal((years, months, days), Cho11Service.Tenure(DateOnly.ParseExact(employment, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), DateOnly.ParseExact(month, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
    [Fact]
    public async Task Exact_date_is_required_when_sent_and_is_rendered_with_indented_declarations()
    {
        await using var db = Db(); var (author, head) = await Seed(db); var input = Request(head);
        input.EmploymentMonth = ""; input.EmploymentDate = "2026-10-15";
        var report = await Service(db).ResolveAsync(author.Id, input, default);
        var files = new Cho11Renderer(new TestEnvironment()).Render(report); var xml = Xml(files);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        Assert.Contains("๑๕ ตุลาคม ๒๕๖๙", xml.ToString());
        var declarations = xml.Descendants(w + "p").Where(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)).StartsWith("ข้าพเจ้าขอ")).ToArray();
        Assert.Equal(6, declarations.Length);
        Assert.All(declarations, p => { Assert.Equal("709", p.Element(w + "pPr")!.Element(w + "ind")!.Attribute(w + "firstLine")!.Value); Assert.Equal("both", p.Element(w + "pPr")!.Element(w + "jc")!.Attribute(w + "val")!.Value); });
        input.EmploymentDate = "2026-02-30"; await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, input, default));
        input.EmploymentDate = ""; await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(author.Id, input, default));
    }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Hop.Api"));
        public string WebRootPath { get; set; } = ""; public string EnvironmentName { get; set; } = "Tests"; public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}




