using System.IO.Compression;
using System.Xml.Linq;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Hop.Api.Tests;

public sealed class CorrespondenceMemoTests
{
    [Theory]
    [InlineData("MEMO_WORK_CERT", "รับรองการขึ้นปฏิบัติงาน")]
    [InlineData("MEMO_OT_REQUEST", "ขอปฏิบัติงานนอกเวลา")]
    [InlineData("MEMO_OT_CLAIM", "ขอเบิกค่าตอบแทนนอกเวลา")]
    [InlineData("MEMO_PURCHASE_IT", "ขอจัดซื้อวัสดุคอมพิวเตอร์")]
    public async Task Resolves_template_filename_from_config_work_month_and_real_proposer(string code, string title)
    {
        await using var db = Db(); var (a, h, d) = await Seed(db);
        a.FullName = "กฤษฎา แสงวันเพ็ญ"; a.NamePrefix = "นาย"; await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(new TestEnvironment().ContentRootPath, "appsettings.json")).Build();
        var service = new CorrespondenceMemoService(db, config);
        var request = Request(a, h, d); request.ContentTemplateCode = code; request.WorkMonth = "2026-10"; request.DocumentNumber = "นน 0033.312/28.1";
        var resolved = await service.ResolveAsync(a.Id, request, default);
        var expected = $"บันทึกข้อความ-{title}-เดือนตุลาคม-2569-28.1-นายกฤษฎา แสงวันเพ็ญ";
        Assert.Equal(expected, resolved.DownloadName);
        Assert.Equal(expected, new CorrespondenceMemoRenderer(new TestEnvironment()).Render(resolved).FileName);
        request.WorkMonth = null;
        Assert.Contains("-เดือนพฤศจิกายน-2569-", (await service.ResolveAsync(a.Id, request, default)).DownloadName);
        request.ContentTemplateCode = null;
        Assert.Contains("-" + request.Subject + "-", (await service.ResolveAsync(a.Id, request, default)).DownloadName);
        request.WorkMonth = "2026-10";
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        request.ContentTemplateCode = "UNKNOWN";
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        request.ContentTemplateCode = code; request.WorkMonth = "2569-10";
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
    }
    [Theory]
    [InlineData("นาย", "ทดสอบ ชื่อ", "นายทดสอบ ชื่อ")]
    [InlineData(" นางสาว ", " ทดสอบ ชื่อ ", "นางสาวทดสอบ ชื่อ")]
    [InlineData("พญ.", "ทดสอบ ชื่อ", "พญ.ทดสอบ ชื่อ")]
    [InlineData("พล.ต.", "ทดสอบ ชื่อ", "พล.ต.ทดสอบ ชื่อ")]
    [InlineData(null, "นายก ชื่อ", "นายก ชื่อ")]
    [InlineData(" ", "ทดสอบ ชื่อ", "ทดสอบ ชื่อ")]
    public void Person_uses_explicit_database_prefix_without_guessing(string? prefix, string fullname, string expected)
    {
        var person = MemoPerson.From(Guid.NewGuid(), fullname, prefix, "ตำแหน่ง", null);
        Assert.Equal(expected, person.Name); Assert.Equal(fullname.Trim(), person.Fullname);
        Assert.Equal(string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim(), person.NamePrefix);
    }

    [Fact]
    public async Task All_people_and_three_documents_use_current_database_names()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db);
        a.FullName = "ทดสอบ ผู้เสนอ"; a.NamePrefix = " นาย ";
        h.FullName = "หัวหน้า ทดสอบ"; h.NamePrefix = "นางสาว";
        d.FullName = "ผู้อำนวยการ ทดสอบ"; d.NamePrefix = "นพ.";
        var other = new User { Username = "foreign", FullName = "ต่างหน่วยงาน ทดสอบ", NamePrefix = "พญ.", Department = new Department { Name = "อื่น" } };
        db.Users.Add(other); await db.SaveChangesAsync();
        var service = Service(db); var options = await service.OptionsAsync(a.Id, default);
        Assert.Equal("นายทดสอบ ผู้เสนอ", options.Proposer.Name);
        Assert.Equal("ทดสอบ ผู้เสนอ", options.Proposer.Fullname);
        Assert.Equal("นางสาวหัวหน้า ทดสอบ", Assert.Single(options.Heads).Name);
        Assert.Equal("นพ.ผู้อำนวยการ ทดสอบ", Assert.Single(options.Directors).Name);
        Assert.Contains(options.Approvers!, p => p.Id == h.Id && p.Name == "นางสาวหัวหน้า ทดสอบ");
        Assert.Contains(await service.DepartmentPeopleAsync(a.Id, a.DepartmentId!.Value, default, true), p => p.Id == other.Id && p.Name == "พญ.ต่างหน่วยงาน ทดสอบ");
        // A stale browser cannot keep the old signatory name when rendering.
        h.NamePrefix = "ดร."; await db.SaveChangesAsync();
        var memoRequest = Request(a, h, d);
        memoRequest.DocumentNumber = "นน 0033.312/29.1";
        var memo = await service.ResolveAsync(a.Id, memoRequest, default);
        Assert.Equal("ดร.หัวหน้า ทดสอบ", memo.Head!.Name);
        var reportService = new OvertimeWorkReportService(service);
        var request = new OvertimeReportRequest { DepartmentId = a.DepartmentId.Value, ProjectName = "กลุ่มงานสุขภาพดิจิทัล", SupervisorUserId = h.Id,
            Entries = [new() { Date = new(2026, 11, 7), EndDate = new(2026, 11, 7), StartTime = "08:30", EndTime = "16:30", Tasks = [new("ตรวจสอบและจัดทำรายงาน", "1 งาน")] }] };
        var payment = await new OvertimePaymentService(db, new ConfigurationBuilder().Build(), reportService).ResolveAsync(a.Id,
            new() { Report = request, Payment = new() { Month = "2026-11", Days = [new(new(2026, 11, 7), "holiday", 7)] } }, default);
        var env = new TestEnvironment();
        var documents = new Dictionary<string, MemoFiles> {
            ["memo"] = new CorrespondenceMemoRenderer(env).Render(memo),
            ["report"] = new OvertimeWorkReportRenderer(env).Render(payment.Report, payment.Days),
            ["payment"] = new OvertimePaymentRenderer(env).Render(payment)
        };
        foreach (var (key, files) in documents)
        {
            using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64)));
            using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
            var xml = await reader.ReadToEndAsync();
            Assert.Contains("นายทดสอบ ผู้เสนอ", xml); Assert.Contains("ดร.หัวหน้า ทดสอบ", xml);
            if (key == "memo") { Assert.Contains("นพ.ผู้อำนวยการ ทดสอบ", xml); Assert.Contains("นน 0033.312/29.1", xml); }
            if (Environment.GetEnvironmentVariable("HOP_PREFIX_QA_DIR") is { Length: > 0 } qa)
            {
                Directory.CreateDirectory(qa);
                File.WriteAllBytes(Path.Combine(qa, key + ".docx"), Convert.FromBase64String(files.DocxBase64));
                File.WriteAllBytes(Path.Combine(qa, key + ".pdf"), Convert.FromBase64String(files.PdfBase64));
            }
        }
        Assert.Equal("ทดสอบ ผู้เสนอ", a.FullName);
    }
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static CorrespondenceMemoService Service(AppDbContext db) => new(db, new ConfigurationBuilder().Build());
    private static async Task<(User Author, User Head, User Director)> Seed(AppDbContext db)
    {
        var department = new Department { Name = "กลุ่มงานสุขภาพดิจิทัล" };
        var author = new User { Username = "memo-author", FullName = "นายตัวอย่าง ผู้เสนอ", Position = "นักวิชาการคอมพิวเตอร์", Department = department };
        var head = new User { Username = "memo-head", FullName = "นางสาวตัวอย่าง หัวหน้า", Position = "นักเทคโนโลยีสารสนเทศปฏิบัติการ", Department = department,
            UserRoles = [new() { Role = new() { Name = "DepartmentHead", IsActive = true } }] };
        var director = new User { Username = "memo-director", FullName = "นายตัวอย่าง ผู้อำนวยการ", UserRoles = [new() { Role = new() { Name = "Director", IsActive = true } }] };
        db.AddRange(author, head, director); await db.SaveChangesAsync(); return (author, head, director);
    }
    [Fact]
    public async Task Department_roster_only_returns_active_members_of_the_selected_active_department()
    {
        await using var db = Db(); var (author, head, director) = await Seed(db);
        var other = new Department { Name = "กลุ่มงานอื่น" };
        db.AddRange(other,
            new User { Username = "inactive", FullName = "ปิดใช้งาน", DepartmentId = author.DepartmentId, IsActive = false },
            new User { Username = "outside", FullName = "สมาชิกกลุ่มงานอื่น", Department = other });
        await db.SaveChangesAsync(); var service = Service(db);
        var roster = await service.DepartmentPeopleAsync(author.Id, author.DepartmentId!.Value, default);
        Assert.Equal(2, roster.Count);
        Assert.Contains(roster, person => person.Id == author.Id && person.Name == author.FullName && person.Position == author.Position);
        Assert.Contains(roster, person => person.Id == head.Id && person.Position == head.Position);
        Assert.All(roster, person => Assert.Equal(author.DepartmentId, person.DepartmentId));
        Assert.DoesNotContain(roster, person => person.Id == director.Id);
        var empty = new Department { Name = "กลุ่มงานว่าง" }; db.Add(empty); await db.SaveChangesAsync();
        Assert.Empty(await service.DepartmentPeopleAsync(author.Id, empty.Id, default));
        empty.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.DepartmentPeopleAsync(author.Id, empty.Id, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DepartmentPeopleAsync(author.Id, Guid.NewGuid(), default));
        author.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DepartmentPeopleAsync(author.Id, head.DepartmentId!.Value, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DepartmentPeopleAsync(Guid.NewGuid(), head.DepartmentId!.Value, default));
    }

    private static MemoRenderRequest Request(User a, User h, User d) => new() { DepartmentId = a.DepartmentId!.Value, Extension = "103", DocumentNumber = "นน 0033.312/29", DocumentDate = new(2026, 11, 4), Subject = "รับรองการขึ้นปฏิบัติงาน", Recipient = "ผู้อำนวยการโรงพยาบาลนาหมื่น",
        Body = [new([new("เนื่องด้วย ข้าพเจ้า นายตัวอย่าง ผู้เสนอ ตำแหน่งนักวิชาการคอมพิวเตอร์ ได้ออกปฏิบัติงานหน่วยเอกซเรย์เคลื่อนที่ในพื้นที่อำเภอนาหมื่น แต่ไม่ได้สแกนลงเวลาเข้า–ออกการปฏิบัติงานในช่วงเวลาดังกล่าว")]),
            new([new("ดังนั้น ข้าพเจ้าขอรับรองว่าได้ขึ้นปฏิบัติงานตามวันและเวลาดังกล่าวจริง", true)])], Closing = "จึงเรียนมาเพื่อโปรดทราบ", HeadUserId = h.Id, DirectorUserId = d.Id, HeadComment = "เห็นสมควรอนุมัติ", DirectorComment = "อนุมัติ" };

    [Fact]
    public async Task Resolves_real_caller_and_validates_active_approver_and_positions_without_saving()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var service = Service(db); var request = Request(a, h, d);
        var memo = await service.ResolveAsync(a.Id, request, default); Assert.Equal(a.FullName, memo.Proposer.Name); Assert.Equal(h.Id, memo.Head!.Id);
        Assert.Single((await service.OptionsAsync(a.Id, default)).Heads);
        request.HeadUserId = a.Id; await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default)); request.HeadUserId = h.Id;
        h.Department = new Department { Name = "หน่วยงานอื่น" }; await db.SaveChangesAsync();
        Assert.Equal(h.Id, (await service.ResolveAsync(a.Id, request, default)).Head!.Id);
        h.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default)); request.IncludeHead = false;
        d.IsActive = false; await db.SaveChangesAsync(); await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default)); request.IncludeDirector = false;
        Assert.Null((await service.ResolveAsync(a.Id, request, default)).Head);
        a.Position = ""; await db.SaveChangesAsync(); await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OptionsAsync(Guid.NewGuid(), default));
        Assert.Empty(db.DomainEvents); Assert.Empty(db.AuditLogs); Assert.Empty(db.OutboxMessages);
    }
    [Fact]
    public async Task Accepts_custom_approvers_across_departments_but_rejects_revoked_roles_and_missing_positions()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var service = Service(db);
        var role = new Role { Name = "ApprovesVehicles", RolePermissions = [new() { Permission = new() { Code = Hop.Api.Authorization.FleetPermissions.AdminReviewApprove, Name = "อนุมัติรถ" } }] };
        var person = new User { Username = "other-approver", FullName = "นายผู้อนุมัติ ต่างหน่วยงาน", Position = "หัวหน้าฝ่าย", Department = new() { Name = "ฝ่ายอื่น" }, UserRoles = [new() { Role = role }] };
        db.Add(person); await db.SaveChangesAsync(); var request = Request(a, h, d); request.HeadUserId = person.Id;
        Assert.Equal(person.Id, (await service.ResolveAsync(a.Id, request, default)).Head!.Id);
        person.Position = ""; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        person.Position = "หัวหน้าฝ่าย"; role.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        d.Position = "แพทย์"; await db.SaveChangesAsync(); request.HeadUserId = d.Id;
        Assert.Equal(d.Id, (await service.ResolveAsync(a.Id, request, default)).Head!.Id);
        Assert.Empty(db.AuditLogs); Assert.Empty(db.DomainEvents);
    }
    [Fact]
    public async Task Cross_department_roster_excludes_admin_accounts_even_when_they_also_hold_staff_roles()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var other = new Department { Name = "กลุ่มงานอื่น" };
        var member = new User { Username = "other-member", FullName = "บุคลากรต่างหน่วยงาน", Position = "เจ้าหน้าที่", Department = other };
        var admin = new User { Username = "admin-member", FullName = "ผู้ดูแล", DepartmentId = a.DepartmentId, UserRoles = [new() { Role = new() { Name = "Admin" } }, new() { Role = new() { Name = "Staff" } }] };
        var super = new User { Username = "super-member", FullName = "ผู้ดูแลสูงสุด", Department = other, UserRoles = [new() { Role = new() { Name = "SuperAdmin" } }] };
        var inactive = new User { Username = "disabled-member", FullName = "บัญชีปิด", Department = other, IsActive = false };
        db.AddRange(member, admin, super, inactive); await db.SaveChangesAsync(); var service = Service(db);
        var people = await service.DepartmentPeopleAsync(a.Id, a.DepartmentId!.Value, default, true);
        Assert.Equal(new[] { a.Id, h.Id, d.Id, member.Id }.Order(), people.Select(p => p.Id).Order());
        Assert.DoesNotContain(await service.DepartmentPeopleAsync(a.Id, a.DepartmentId.Value, default), p => p.Id == member.Id || p.Id == admin.Id);
        await Assert.ThrowsAsync<ArgumentException>(() => service.DepartmentPeopleAsync(a.Id, Guid.NewGuid(), default, true));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DepartmentPeopleAsync(Guid.NewGuid(), a.DepartmentId.Value, default, true));
    }

    [Theory]
    [InlineData("align")] [InlineData("indent")] [InlineData("control")] [InlineData("length")] [InlineData("empty")]
    public async Task Rejects_unsupported_or_excessive_structured_content(string invalid)
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var request = Request(a, h, d);
        request.Body = invalid switch { "align" => [new([new("text")], "html")], "indent" => [new([new("text")], Indent: 100)], "control" => [new([new("\u0001")])], "length" => [new([new(new string('a', 30001))])], _ => [new([new(" ")])] };
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(a.Id, request, default));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Renders_real_docx_and_pdf_in_memory_with_rich_text_and_multi_page_signatures(bool longDocument)
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var request = Request(a, h, d);
        request.Subject = "ขออนุมัติเบิกเงินค่าตอบแทนการปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ ประจำเดือน พฤศจิกายน 2569";
        request.Body.Add(new([new("รายการที่มี ", Italic: true), new("ขีดเส้นใต้", Underline: true), new("\nขึ้นบรรทัดใหม่")], "left", 0, true));
        if (longDocument) for (var i = 0; i < 35; i++) request.Body.Add(new([new($"ย่อหน้าที่ {i + 1} เพื่อทดสอบเอกสารหลายหน้า สระและวรรณยุกต์ไทย ต้องอ่านชัดเจน รักษาขนาดตัวอักษรและระยะขอบตามต้นฉบับ โดยกลุ่มชื่อผู้ลงนามและตำแหน่งต้องไม่แยกออกจากกัน")], "justify", 1));
        var memo = await Service(db).ResolveAsync(a.Id, request, default); var env = new TestEnvironment(); var renderer = new CorrespondenceMemoRenderer(env);
        var before = Directory.GetFiles(env.ContentRootPath, "*", SearchOption.AllDirectories).Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")).ToHashSet();
        var files = renderer.Render(memo); var docx = Convert.FromBase64String(files.DocxBase64); var pdf = Convert.FromBase64String(files.PdfBase64);
        if (Environment.GetEnvironmentVariable("HOP_MEMO_QA_DIR") is { Length: > 0 } qa) { Directory.CreateDirectory(qa); File.WriteAllBytes(Path.Combine(qa, longDocument ? "long.docx" : "sample.docx"), docx); File.WriteAllBytes(Path.Combine(qa, longDocument ? "long.pdf" : "sample.pdf"), pdf); }
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf.Take(5).ToArray()));
        using var zip = new ZipArchive(new MemoryStream(docx)); using var input = zip.GetEntry("word/document.xml")!.Open(); var xml = XDocument.Load(input); XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var text = string.Concat(xml.Descendants(w + "t").Select(x => x.Value)); Assert.Contains(a.FullName, text); Assert.Contains("4 พฤศจิกายน 2569", text); Assert.DoesNotContain("{{", text, StringComparison.Ordinal); Assert.DoesNotContain("กฤษฎา", text, StringComparison.Ordinal); Assert.Contains(h.FullName, text); Assert.Contains(d.FullName, text);
        var title = xml.Descendants(w + "body").Single().Elements(w + "p").First();
        Assert.Equal("center", title.Element(w + "pPr")!.Element(w + "jc")!.Attribute(w + "val")!.Value);
        Assert.Equal("0", title.Element(w + "pPr")!.Element(w + "ind")!.Attribute(w + "right")!.Value);
        var bodyParagraph = xml.Descendants(w + "p").First(p => p.Descendants(w + "t").Any(t => t.Value.Contains("เนื่องด้วย")));
        Assert.Equal("thaiDistribute", bodyParagraph.Element(w + "pPr")!.Element(w + "jc")!.Attribute(w + "val")!.Value);
        Assert.All(bodyParagraph.Descendants(w + "lang"), language => Assert.Equal("th-TH", language.Attribute(w + "val")!.Value));
        var paragraphs = xml.Descendants(w + "p").ToList();
        XElement SignParagraph(string text) => paragraphs.Single(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)) == text);
        void AssertSide(string text, bool right) {
            var ind = SignParagraph(text).Element(w + "pPr")!.Element(w + "ind")!;
            Assert.Equal("4535", ind.Attribute(w + (right ? "left" : "right"))!.Value);
            Assert.Null(ind.Attribute(w + (right ? "right" : "left")));
        }
        AssertSide($"({a.FullName})", true); AssertSide($"({h.FullName})", false); AssertSide($"({d.FullName})", true);
        AssertSide("- " + request.HeadComment, false); AssertSide(request.DirectorComment, true);
        Assert.NotNull(SignParagraph("- " + request.HeadComment).Element(w + "pPr")!.Element(w + "keepNext"));
        Assert.NotNull(SignParagraph(request.DirectorComment).Element(w + "pPr")!.Element(w + "keepNext"));
        Assert.NotEmpty(SignParagraph(request.DirectorComment).Descendants(w + "b"));
        Assert.True(paragraphs.IndexOf(SignParagraph($"({a.FullName})")) < paragraphs.IndexOf(SignParagraph($"({h.FullName})")));
        Assert.True(paragraphs.IndexOf(SignParagraph($"({h.FullName})")) < paragraphs.IndexOf(SignParagraph($"({d.FullName})")));
        Assert.Equal(3, xml.Descendants(w + "tcBorders").Elements(w + "bottom").Count());
        var dateLabel = xml.Descendants(w + "tc").Single(cell => string.Concat(cell.Descendants(w + "t").Select(t => t.Value)) == "วันที่");
        Assert.Equal("right", dateLabel.Element(w + "p")!.Element(w + "pPr")!.Element(w + "jc")!.Attribute(w + "val")!.Value);
        var subjectCell = xml.Descendants(w + "tbl").Last().Descendants(w + "tc").Last();
        Assert.True(subjectCell.Elements(w + "p").Count() >= 2);
        Assert.All(subjectCell.Elements(w + "p"), p => { Assert.NotNull(p.Element(w + "pPr")!.Element(w + "pBdr")!.Element(w + "bottom")); Assert.NotNull(p.Element(w + "pPr")!.Element(w + "pBdr")!.Element(w + "between")); });
        Assert.Equal(3, xml.Descendants(w + "tbl").Count());
        Assert.NotEmpty(xml.Descendants(w + "b")); Assert.NotEmpty(xml.Descendants(w + "i")); Assert.NotEmpty(xml.Descendants(w + "u")); Assert.NotEmpty(xml.Descendants(w + "keepNext"));
        Assert.Equal("851", xml.Descendants(w + "pgMar").Single().Attribute(w + "bottom")!.Value);
        Assert.True(before.SetEquals(Directory.GetFiles(env.ContentRootPath, "*", SearchOption.AllDirectories).Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))));
        Assert.Empty(db.AuditLogs); Assert.Empty(db.DomainEvents);
        // Optional developer QA export is confined to this test process, never the application.

    }

    [Fact]
    public async Task Thai_wrapping_example_preserves_source_and_formats_in_docx_and_pdf()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var request = Request(a, h, d);
        request.Subject = "ขออนุมัติปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ ประจำเดือนตุลาคม 2569";
        request.Body = [
            new([new("ด้วย กลุ่มงานสุขภาพดิจิทัล โรงพยาบาลนาหมื่น มีความจำเป็นที่ต้องปฏิบัติงานนอกเวลาราช"), new("การและวันหยุดราชการ", Bold: true), new(" เนื่องจากมีปริมาณงาน และมีเจ้าหน้าที่รับผิดชอบงานไม่เพียงพอ ทำให้มีภาระงานเพิ่มมากขึ้น มีงานคั่งค้าง ไม่สามารถทำเสร็จได้ทันเวลาในวันทำการ")]),
            new([new("กลุ่มงานสุขภาพดิจิทัล โรงพยาบาลนาหมื่น จึงขออนุมัติให้เจ้าหน้าที่ปฏิบัติงาน ดังนี้")]),
            new([new("นายทดสอบ ผู้เสนอ ตำแหน่ง นักวิชาการคอมพิวเตอร์")], "left", 1, true),
            new([new("ปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ ตั้งแต่วันที่ 1 ตุลาคม 2569 ถึงวันที่ 31 ตุลาคม 2569 โดยมี หัวหน้ากลุ่มงาน เป็นผู้ควบคุมการปฏิบัติงานดังกล่าว ในการเบิกเงินตอบแทนให้เป็นไปตามหลักเกณฑ์และอัตรา ดังนี้")]),
            new([new("7.1 การปฏิบัติงานนอกเวลาราชการในวันทำการ ให้มีสิทธิเบิกเงินตอบแทนได้ไม่เกินวันละสี่ชั่วโมง ในอัตราชั่วโมงละห้าสิบบาท", Italic: true)]),
            new([new("7.2 การปฏิบัติงานในวันหยุดราชการ ให้มีสิทธิเบิกเงินตอบแทนได้ไม่เกินวันละเจ็ดชั่วโมง ในอัตราชั่วโมงละหกสิบบาท", Underline: true)])
        ];
        var files = new CorrespondenceMemoRenderer(new TestEnvironment()).Render(await Service(db).ResolveAsync(a.Id, request, default));
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64)));
        using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        var xml = XDocument.Parse(await reader.ReadToEndAsync());
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var text = string.Concat(xml.Descendants(w + "t").Select(t => t.Value));
        foreach (var paragraph in request.Body) Assert.Contains(string.Concat(paragraph.Runs.Select(r => r.Text)), text);
        Assert.DoesNotContain('\u200b', text); Assert.DoesNotContain('\u00ad', text);
        if (Environment.GetEnvironmentVariable("HOP_MEMO_QA_DIR") is { Length: > 0 } qa) {
            Directory.CreateDirectory(qa);
            File.WriteAllBytes(Path.Combine(qa, "wrapping.docx"), Convert.FromBase64String(files.DocxBase64));
            File.WriteAllBytes(Path.Combine(qa, "wrapping.pdf"), Convert.FromBase64String(files.PdfBase64));
        }
    }
    [Fact]
    public async Task Disabled_signature_sections_do_not_leak_names_into_exports()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var request = Request(a, h, d); request.IncludeHead = request.IncludeDirector = false;
        var files = new CorrespondenceMemoRenderer(new TestEnvironment()).Render(await Service(db).ResolveAsync(a.Id, request, default));
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(files.DocxBase64))); using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open()); var xml = reader.ReadToEnd(); Assert.DoesNotContain(h.FullName, xml); Assert.DoesNotContain(d.FullName, xml);
    }
    [Fact]
    public async Task Unfilled_template_variables_cannot_be_exported_even_when_split_across_runs()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var request = Request(a, h, d);
        request.Body = [new([new("{work_", true), new("dates}")])];
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(a.Id, request, default));
        request.Body = [new([new("ข้อความครบ")])]; request.Subject = "เรื่อง {month}";
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveAsync(a.Id, request, default));
    }
    [Fact]
    public async Task Options_expose_only_active_content_templates_from_configuration()
    {
        await using var db = Db(); var (a, _, _) = await Seed(db);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Correspondence:ContentTemplates:0:Code"] = "ON", ["Correspondence:ContentTemplates:0:Name"] = "เปิด", ["Correspondence:ContentTemplates:0:IsActive"] = "true", ["Correspondence:ContentTemplates:0:SubjectTemplate"] = "เรื่อง", ["Correspondence:ContentTemplates:0:BodyTemplate:0"] = "เนื้อหา", ["Correspondence:ContentTemplates:0:ClosingTemplate"] = "ลงท้าย", ["Correspondence:ContentTemplates:0:Variables:0"] = "month",
            ["Correspondence:ContentTemplates:1:Code"] = "OFF", ["Correspondence:ContentTemplates:1:Name"] = "ปิด", ["Correspondence:ContentTemplates:1:IsActive"] = "false", ["Correspondence:ContentTemplates:1:SubjectTemplate"] = "เรื่อง", ["Correspondence:ContentTemplates:1:BodyTemplate:0"] = "เนื้อหา", ["Correspondence:ContentTemplates:1:ClosingTemplate"] = "ลงท้าย", ["Correspondence:ContentTemplates:1:Variables:0"] = "month"
        }).Build();
        var options = await new CorrespondenceMemoService(db, config).OptionsAsync(a.Id, default);
        Assert.Equal("ON", Assert.Single(options.ContentTemplates!).Code);
    }
    [Fact]
    public async Task Director_is_resolved_at_render_time_ignoring_client_selection_and_rejecting_ambiguity()
    {
        await using var db = Db(); var (a, h, d) = await Seed(db); var service = Service(db); var request = Request(a, h, d);
        request.DirectorUserId = a.Id;
        Assert.Equal(d.Id, (await service.ResolveAsync(a.Id, request, default)).Director!.Id);
        request.DirectorUserId = null;
        Assert.Equal(d.Id, (await service.ResolveAsync(a.Id, request, default)).Director!.Id);
        var next = new User { Username = "next-director", FullName = "ผู้อำนวยการใหม่", UserRoles = [new() { Role = d.UserRoles.Single().Role }] };
        db.Users.Add(next); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        d.IsActive = false; await db.SaveChangesAsync();
        Assert.Equal(next.Id, (await service.ResolveAsync(a.Id, request, default)).Director!.Id);
        next.UserRoles.Single().Role!.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveAsync(a.Id, request, default));
        request.IncludeDirector = false;
        Assert.Null((await service.ResolveAsync(a.Id, request, default)).Director);
    }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = FindRoot();
        private static string FindRoot() { var current = new DirectoryInfo(AppContext.BaseDirectory); while (current != null) { var path = Path.Combine(current.FullName, "backend", "Hop.Api"); if (Directory.Exists(path)) return path; current = current.Parent; } throw new InvalidOperationException("Repository content root not found"); }
        public string WebRootPath { get; set; } = ""; public string EnvironmentName { get; set; } = "Tests"; public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
