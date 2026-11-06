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

public sealed class OvertimePaymentTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static OvertimePaymentService Service(AppDbContext db, IConfiguration? config = null) => new(db, config ?? new ConfigurationBuilder().Build(), new(new CorrespondenceMemoService(db, new ConfigurationBuilder().Build())));
    private static async Task<(User Author, User Head)> Seed(AppDbContext db)
    {
        var department = new Department { Name = "กลุ่มงานสุขภาพดิจิทัล" };
        var author = new User { Username = "author", FullName = "นายทดสอบ รายงาน", Position = "นักวิชาการคอมพิวเตอร์", Department = department };
        var head = new User { Username = "head", FullName = "นางสาวผู้ควบคุม ทดสอบ", Position = "หัวหน้ากลุ่มงาน", Department = department, UserRoles = [new() { Role = new() { Name = "DepartmentHead" } }] };
        db.AddRange(author, head); await db.SaveChangesAsync(); return (author, head);
    }
    private static OvertimeReportEntry Entry(int day = 5, string start = "08:22", string end = "16:44", int? endDay = null) => new() { Date = new(2026, 11, day), EndDate = new(2026, 11, endDay ?? day), StartTime = start, EndTime = end, Tasks = [new("ตรวจสอบระบบและจัดทำรายงาน", "จำนวน 1 งาน")] };
    private static OvertimeBundleRequest Request(User a, User h, params OvertimeReportEntry[] entries) => new() { Report = new() { DepartmentId = a.DepartmentId!.Value, SupervisorUserId = h.Id, ProjectName = "กลุ่มงานสุขภาพดิจิทัล", Entries = entries.ToList() }, Payment = new() { Month = "2026-11", Days = entries.Select(e => new OvertimeClaimDay(e.Date, "weekday", 0)).DistinctBy(e => e.Date).ToList() } };
    [Fact]
    public async Task Options_uses_live_holidays_and_configuration_without_writes()
    {
        await using var db = Db(); var (a, _) = await Seed(db);
        db.LeaveHolidays.AddRange(new() { HolidayDate = new(2026, 11, 5), Name = "วันหยุดทดสอบ" }, new() { HolidayDate = new(2026, 11, 6), Name = "ปิด", IsActive = false }); await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Correspondence:OvertimePayment:WeekdayRate"] = "55.50", ["Correspondence:OvertimePayment:HolidayRate"] = "70", ["Correspondence:OvertimePayment:PayerName"] = "นางสาวรสริน แสนซ้าย" }).Build();
        var options = await Service(db, config).OptionsAsync(a.Id, "2026-11", default);
        Assert.Equal(55.50m, options.WeekdayRate); Assert.Equal(70m, options.HolidayRate); Assert.Contains(new DateOnly(2026, 11, 5), options.HolidayDates); Assert.Contains(new DateOnly(2026, 11, 7), options.HolidayDates); Assert.DoesNotContain(new DateOnly(2026, 11, 6), options.HolidayDates);
        Assert.Equal(new OvertimePaymentRules(), options.Rules);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(db).OptionsAsync(Guid.NewGuid(), "2026-11", default));
        Assert.Equal(CorrespondenceAccess.Permission, typeof(OvertimeWorkReportController).GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>().Single().PermissionCode);
        Assert.Empty(db.AuditLogs); Assert.Empty(db.OutboxMessages);
    }
    [Theory]
    [InlineData("weekday", "15:00", "19:00", 2)]
    [InlineData("weekday", "16:30", "20:30", 4)]
    [InlineData("weekday", "16:30", "22:00", 4)]
    [InlineData("weekday", "18:00", "22:00", 4)]
    [InlineData("weekday", "08:30", "16:30", 0)]
    [InlineData("weekday", "16:00", "17:29", 0)]
    [InlineData("weekday", "16:00", "17:30", 1)]
    [InlineData("holiday", "08:30", "16:30", 7)]
    [InlineData("holiday", "13:00", "20:00", 7)]
    [InlineData("holiday", "08:00", "20:00", 7)]
    [InlineData("holiday", "11:30", "13:30", 1)]
    [InlineData("holiday", "11:30", "12:30", 0)]
    [InlineData("holiday", "12:30", "14:00", 1)]
    [InlineData("holiday", "12:00", "13:00", 0)]
    [InlineData("holiday", "13:00", "13:59", 0)]
    public async Task Enforces_time_windows_break_overlap_flooring_and_daily_caps(string type, string start, string end, int expected)
    {
        await using var db = Db(); var (a, h) = await Seed(db); var request = Request(a, h, Entry(5, start, end)); request.Payment.Days = [new(new(2026, 11, 5), type, expected)];
        var result = await Service(db).ResolveAsync(a.Id, request, default);
        Assert.Equal(expected, result.Days[0].MaxClaimHours); Assert.Equal(expected * (type == "holiday" ? 60m : 50m), result.TotalAmount);
        request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = expected + 1 };
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(a.Id, request, default));
    }
    [Fact]
    public async Task Configuration_controls_the_rules_and_is_returned_to_the_client()
    {
        await using var db = Db(); var (a, h) = await Seed(db);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Correspondence:OvertimePayment:Rules:WeekdayStart"] = "17:00", ["Correspondence:OvertimePayment:Rules:WeekdayMaxHours"] = "2" }).Build();
        var service = Service(db, config); var options = await service.OptionsAsync(a.Id, "2026-11", default);
        Assert.Equal("17:00", options.Rules.WeekdayStart); Assert.Equal(2, options.Rules.WeekdayMaxHours); Assert.Equal("12:00", options.Rules.BreakStart);
        var request = Request(a, h, Entry(5, "16:30", "22:00")); request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = 2 };
        Assert.Equal(2, (await service.ResolveAsync(a.Id, request, default)).Days[0].MaxClaimHours);
    }
    [Theory]
    [InlineData("WeekdayStart", "25:00")][InlineData("WeekdayMaxHours", "25")][InlineData("HolidayMaxHours", "-1")][InlineData("BreakEnd", "11:00")]
    public async Task Rejects_invalid_configuration(string key, string value)
    {
        await using var db = Db(); var (a, _) = await Seed(db); var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [$"Correspondence:OvertimePayment:Rules:{key}"] = value }).Build();
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db, config).OptionsAsync(a.Id, "2026-11", default));
    }
    [Fact]
    public async Task Aggregates_daily_minutes_before_flooring_splits_midnight_and_preserves_report_hours()
    {
        await using var db = Db(); var (a, h) = await Seed(db);
        var request = Request(a, h, Entry(5, "17:00", "17:40"), Entry(5, "18:00", "18:40"), Entry(6, "23:30", "01:15", 7));
        request.Payment.Days = [new(new(2026, 11, 5), "weekday", 1), new(new(2026, 11, 6), "weekday", 0), new(new(2026, 11, 7), "holiday", 1)];
        var result = await Service(db).ResolveAsync(a.Id, request, default);
        Assert.Equal(new[] { 80, 30, 75 }, result.Days.Select(d => d.ActualMinutes)); Assert.Equal(new[] { 1, 0, 1 }, result.Days.Select(d => d.WholeHours));
        Assert.Equal(new decimal?[] { 0, 0, 1 }, result.Report.Entries.Select(e => e.Hours));
        Assert.Equal(110m, result.TotalAmount); Assert.Equal("หนึ่งร้อยสิบบาทถ้วน", result.AmountText);
        var files = new OvertimeWorkReportRenderer(new Environment()).Render(result.Report, result.Days);
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var stream = zip.GetEntry("word/document.xml")!.Open();
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"; var text = string.Concat(XDocument.Load(stream).Descendants(w + "t").Select(t => t.Value));
        foreach (var date in new[] { "05/11/2569", "06/11/2569", "07/11/2569" }) Assert.Equal(1, text.Split("รวมเบิก " + date).Length - 1);
        Assert.Empty(db.DomainEvents); Assert.Empty(db.AuditLogs);
    }
    [Fact]
    public async Task Both_reports_use_the_confirmed_claim_hours_while_the_source_keeps_actual_hours()
    {
        await using var db = Db(); var (a, h) = await Seed(db); var request = Request(a, h, Entry(7, "08:30", "16:30"));
        request.Payment.Days = [new(new(2026, 11, 7), "holiday", 7)]; var result = await Service(db).ResolveAsync(a.Id, request, default);
        Assert.Equal(8, result.Report.Entries[0].Hours); Assert.Equal(7, result.Days[0].MaxClaimHours);
        var renderer = new OvertimeWorkReportRenderer(new Environment());
        Assert.Equal($"รายงานผลการปฏิบัติงานนอกเวลา-เดือนพฤศจิกายน-2569-{result.Report.Reporter.Name}", renderer.Render(result.Report, result.Days, result.MonthStart).FileName);
        Assert.Equal($"หลักฐานการเบิกจ่าย-เดือนพฤศจิกายน-2569-{result.Report.Reporter.Name}", new OvertimePaymentRenderer(new Environment()).Render(result).FileName);
        string Text(MemoFiles files) { using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var input = zip.GetEntry("word/document.xml")!.Open(); XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"; return string.Concat(XDocument.Load(input).Descendants(w + "t").Select(t => t.Value)); }
        var work = Text(renderer.Render(result.Report, result.Days)); Assert.Contains("08.30 น.–16.30 น.", work); Assert.Contains("(7 ชม.)", work); Assert.DoesNotContain("(8 ชม.)", work);
        Assert.Contains("(8 ชม.)", Text(renderer.Render(result.Report)));
        Assert.Contains("420.00", Text(new OvertimePaymentRenderer(new Environment()).Render(result)));
        request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = 5 }; result = await Service(db).ResolveAsync(a.Id, request, default);
        Assert.Contains("(5 ชม.)", Text(renderer.Render(result.Report, result.Days))); Assert.Contains("300.00", Text(new OvertimePaymentRenderer(new Environment()).Render(result)));
    }
    [Fact]
    public async Task User_can_override_day_type_and_claim_less_or_zero_while_amounts_are_recomputed()
    {
        await using var db = Db(); var (a, h) = await Seed(db); var request = Request(a, h, Entry());
        request.Payment.Days = [new(new(2026, 11, 5), "holiday", 7)];
        var result = await Service(db).ResolveAsync(a.Id, request, default); Assert.Equal(420m, result.TotalAmount); Assert.Equal(8, result.Days[0].WholeHours);
        request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = 0 };
        Assert.Equal("ศูนย์บาทถ้วน", (await Service(db).ResolveAsync(a.Id, request, default)).AmountText);
    }
    [Fact]
    public async Task Long_signer_names_and_positions_wrap_without_losing_content()
    {
        await using var db = Db(); var (a, h) = await Seed(db);
        a.FullName = "นายผู้รายงาน " + string.Concat(Enumerable.Repeat("ชื่อทดสอบนามสกุลยาว", 10));
        h.FullName = "นางสาวผู้ควบคุม " + string.Concat(Enumerable.Repeat("ชื่อทดสอบนามสกุลยาว", 10));
        h.Position = string.Concat(Enumerable.Repeat("หัวหน้ากลุ่มงานและผู้รับผิดชอบระบบสารสนเทศ ", 5)); await db.SaveChangesAsync();
        var payment = await Service(db).ResolveAsync(a.Id, Request(a, h, Entry()), default);
        foreach (var files in new[] { new OvertimeWorkReportRenderer(new Environment()).Render(payment.Report), new OvertimePaymentRenderer(new Environment()).Render(payment) })
        {
            using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
            var xml = await reader.ReadToEndAsync(); Assert.Contains(h.FullName, xml); Assert.Contains(h.Position, xml); Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(files.PdfBase64)));
        }
    }
    [Theory]
    [InlineData("negative")][InlineData("fraction")][InlineData("excess")][InlineData("missing")][InlineData("duplicate")][InlineData("unworked")][InlineData("type")][InlineData("month")][InlineData("overlap")][InlineData("outside")][InlineData("reverse")]
    public async Task Rejects_invalid_claims_without_partial_writes(string invalid)
    {
        await using var db = Db(); var (a, h) = await Seed(db); var request = Request(a, h, Entry());
        switch (invalid)
        {
            case "negative": request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = -1 }; break;
            case "fraction": request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = 1.5m }; break;
            case "excess": request.Payment.Days[0] = request.Payment.Days[0] with { ClaimHours = 9 }; break;
            case "missing": request.Payment.Days.Clear(); break;
            case "duplicate": request.Payment.Days.Add(request.Payment.Days[0]); break;
            case "unworked": request.Payment.Days[0] = request.Payment.Days[0] with { Date = new(2026, 11, 6) }; break;
            case "type": request.Payment.Days[0] = request.Payment.Days[0] with { DayType = "anything" }; break;
            case "month": request.Payment.Month = "2026-13"; break;
            case "overlap": request.Report.Entries.Add(Entry(5, "16:00", "17:00")); break;
            case "outside": request.Payment.Month = "2026-10"; break;
            case "reverse": request.Report.Entries[0].EndTime = "07:00"; break;
        }
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(a.Id, request, default)); Assert.Empty(db.DomainEvents); Assert.Empty(db.AuditLogs);
    }
    [Theory]
    [InlineData(2027, 2, 28)][InlineData(2028, 2, 29)][InlineData(2026, 4, 30)][InlineData(2026, 10, 31)]
    public async Task Renders_landscape_evidence_with_correct_calendar_and_real_names(int year, int month, int days)
    {
        await using var db = Db(); var (a, h) = await Seed(db); var entry = Entry(); entry.Date = entry.EndDate = new(year, month, 1);
        var request = Request(a, h, entry); request.Payment.Month = $"{year:D4}-{month:D2}"; request.Payment.Days = [new(entry.Date, "holiday", 7)];
        var resolved = await Service(db).ResolveAsync(a.Id, request, default); var files = new OvertimePaymentRenderer(new Environment()).Render(resolved);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(files.PdfBase64)));
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var stream = zip.GetEntry("word/document.xml")!.Open(); var xml = XDocument.Load(stream); XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        Assert.Equal("landscape", xml.Descendants(w + "pgSz").Single().Attribute(w + "orient")!.Value);
        Assert.Equal(days + 9, xml.Descendants(w + "tblGrid").First().Elements().Count());
        Assert.NotNull(xml.Descendants(w + "tbl").First().Elements(w + "tr").Skip(1).First().Elements(w + "tc").Skip(3).First().Descendants(w + "shd").SingleOrDefault());
        var text = string.Concat(xml.Descendants(w + "t").Select(t => t.Value)); Assert.Contains(a.FullName, text); Assert.Contains(h.FullName, text); Assert.Contains("นางสาวรสริน แสนซ้าย", text); Assert.Contains("420.00", text); Assert.Contains("สี่ร้อยยี่สิบบาทถ้วน", text);
        Assert.All(xml.Descendants(w + "rFonts"), f => Assert.Equal("TH SarabunPSK", f.Attribute(w + "ascii")!.Value));
        var amountLine = xml.Descendants(w + "p").Single(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)).StartsWith("รวมจ่ายเงินทั้งสิ้น"));
        Assert.Equal("รวมจ่ายเงินทั้งสิ้น (ตัวอักษร)      (สี่ร้อยยี่สิบบาทถ้วน)", string.Concat(amountLine.Descendants(w + "t").Select(t => t.Value)));
        Assert.Equal("720", amountLine.Descendants(w + "ind").Single().Attribute(w + "left")!.Value);
        Assert.Equal("160", amountLine.Descendants(w + "spacing").Single().Attribute(w + "before")!.Value);
        Assert.DoesNotContain("Page 1", text); Assert.DoesNotContain("อนุมัติแล้ว", text);
    }
    [Theory]
    [InlineData(0, "ศูนย์บาทถ้วน")][InlineData(1260, "หนึ่งพันสองร้อยหกสิบบาทถ้วน")][InlineData(21.25, "ยี่สิบเอ็ดบาทยี่สิบห้าสตางค์")][InlineData(1000001, "หนึ่งล้านเอ็ดบาทถ้วน")]
    public void Formats_baht_from_the_server_total(decimal amount, string expected) => Assert.Equal(expected, OvertimePaymentService.ThaiBaht(amount));
    private sealed class Environment : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = FindRoot();
        private static string FindRoot() { var dir = new DirectoryInfo(AppContext.BaseDirectory); while (dir != null) { var root = Path.Combine(dir.FullName, "backend", "Hop.Api"); if (Directory.Exists(root)) return root; dir = dir.Parent; } throw new InvalidOperationException(); }
        public string WebRootPath { get; set; } = ""; public string EnvironmentName { get; set; } = "Tests"; public string ApplicationName { get; set; } = "Tests"; public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
