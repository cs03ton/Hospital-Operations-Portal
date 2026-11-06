using System.Xml;
using System.Text.RegularExpressions;
using Hop.Api.Data;
using Hop.Api.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record MemoRun(string Text, bool Bold = false, bool Italic = false, bool Underline = false);
public sealed record MemoParagraph(List<MemoRun> Runs, string Align = "justify", int Indent = 1, bool Ordered = false);
public sealed class MemoRenderRequest
{
    public Guid DepartmentId { get; set; }
    public string Extension { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public DateOnly DocumentDate { get; set; }
    public string Subject { get; set; } = "";
    public string Recipient { get; set; } = "";
    public List<MemoParagraph> Body { get; set; } = [];
    public string Closing { get; set; } = "";
    public bool IncludeHead { get; set; } = true;
    public Guid? HeadUserId { get; set; }
    public string HeadComment { get; set; } = "";
    public bool IncludeDirector { get; set; } = true;
    // Compatibility only: the current Director is always resolved from active database roles.
    public Guid? DirectorUserId { get; set; }
    public string DirectorComment { get; set; } = "";
    public string? ContentTemplateCode { get; set; }
    public string? WorkMonth { get; set; }
}
public sealed record MemoPerson(Guid Id, string Name, string? Position, Guid? DepartmentId, string? NamePrefix = null, string? Fullname = null)
{
    public static MemoPerson From(Guid id, string fullname, string? prefix, string? position, Guid? departmentId)
    {
        var name = fullname.Trim();
        var title = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim();
        return new(id, (title ?? "") + name, position, departmentId, title, name);
    }
}
public sealed record MemoDepartment(Guid Id, string Name);
public sealed record MemoContentTemplate(string Code, string Name, string SubjectTemplate, string[] BodyTemplate, string ClosingTemplate, string[] Variables, bool IsActive = true, string? FileNameTitle = null);
public sealed record MemoOptions(MemoPerson Proposer, IReadOnlyList<MemoDepartment> Departments, IReadOnlyList<MemoPerson> Heads,
    IReadOnlyList<MemoPerson> Directors, string[] Closings, string[] HeadComments, string[] DirectorComments, IReadOnlyList<MemoContentTemplate>? ContentTemplates = null,
    IReadOnlyList<MemoPerson>? Approvers = null);
public sealed record ResolvedMemo(MemoRenderRequest Input, string Department, MemoPerson Proposer, MemoPerson? Head, MemoPerson? Director, string? DownloadName = null);

public sealed class CorrespondenceMemoService(AppDbContext db, IConfiguration configuration)
{
    public const string Telephone = "054-719361";
    public const string DirectorPosition = "นายแพทย์ปฏิบัติการ รักษาการในตำแหน่ง ผู้อำนวยการโรงพยาบาลนาหมื่น";
    public async Task<MemoOptions> OptionsAsync(Guid actor, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actor && u.IsActive, ct) ?? throw new UnauthorizedAccessException();
        var departments = await db.Departments.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).Select(d => new MemoDepartment(d.Id, d.Name)).ToListAsync(ct);
        var people = await db.Users.AsNoTracking().Where(u => u.IsActive && u.UserRoles.Any(r => r.Role != null && r.Role.IsActive &&
            (r.Role.Name == "DepartmentHead" || r.Role.Name == "หัวหน้าหน่วยงานสูงสุด" || r.Role.Name == "Director")))
            .Select(u => new { Person = MemoPerson.From(u.Id, u.FullName, u.NamePrefix, u.Position, u.DepartmentId), Roles = u.UserRoles.Where(r => r.Role!.IsActive).Select(r => r.Role!.Name).ToList() }).ToListAsync(ct);
        var approvalPermissions = new[] { LeavePermissions.ApproveCurrentStep, LeavePermissions.CancellationApproveCurrentStep,
            FleetPermissions.AdminReviewApprove, FleetPermissions.DirectorApprove };
        var approvers = await db.Users.AsNoTracking().Where(u => u.IsActive && u.UserRoles.Any(r => r.Role != null && r.Role.IsActive &&
            (r.Role.Name == "DepartmentHead" || r.Role.Name == "หัวหน้าหน่วยงานสูงสุด" || r.Role.Name == "Director" ||
             r.Role.RolePermissions.Any(p => p.Permission != null && p.Permission.IsActive && approvalPermissions.Contains(p.Permission.Code)))))
            .OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(u => MemoPerson.From(u.Id, u.FullName, u.NamePrefix, u.Position, u.DepartmentId)).ToListAsync(ct);
        return new(MemoPerson.From(user.Id, user.FullName, user.NamePrefix, user.Position, user.DepartmentId), departments,
            people.Where(u => u.Roles.Any(r => r is "DepartmentHead" or "หัวหน้าหน่วยงานสูงสุด")).Select(u => u.Person).OrderBy(u => u.Name).ToArray(),
            people.Where(u => u.Roles.Any(r => r == "Director")).Select(u => u.Person).OrderBy(u => u.Name).ToArray(),
            List("Closings", ["จึงเรียนมาเพื่อโปรดทราบ"]), List("HeadComments", ["- เห็นสมควรอนุมัติ"]), List("DirectorComments", ["อนุมัติ", "ไม่อนุมัติ"]),
            (configuration.GetSection("Correspondence:ContentTemplates").Get<MemoContentTemplate[]>() ?? []).Where(t => t.IsActive).ToArray(), approvers);
    }
    private string[] List(string key, string[] fallback) => configuration.GetSection($"Correspondence:{key}").Get<string[]>() is { Length: > 0 } values ? values : fallback;
    public async Task<IReadOnlyList<MemoPerson>> DepartmentPeopleAsync(Guid actor, Guid departmentId, CancellationToken ct, bool includeOtherDepartments = false)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == actor && u.IsActive, ct)) throw new UnauthorizedAccessException();
        if (!await db.Departments.AsNoTracking().AnyAsync(d => d.Id == departmentId && d.IsActive, ct)) throw new ArgumentException("กรุณาเลือกหน่วยงานที่เปิดใช้งาน");
        return await db.Users.AsNoTracking().Where(u => u.IsActive && (includeOtherDepartments || u.DepartmentId == departmentId) &&
            !u.UserRoles.Any(r => r.Role != null && (r.Role.Name == "Admin" || r.Role.Name == "SuperAdmin")))
            .OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(u => MemoPerson.From(u.Id, u.FullName, u.NamePrefix, u.Position, u.DepartmentId)).ToListAsync(ct);
    }
    public async Task<ResolvedMemo> ResolveAsync(Guid actor, MemoRenderRequest request, CancellationToken ct)
    {
        var options = await OptionsAsync(actor, ct);
        Check(options.Proposer.Name, 300, "ชื่อผู้เสนอ"); Check(options.Proposer.Position, 300, "ตำแหน่งผู้เสนอ กรุณาแก้ Profile ผ่านผู้ดูแลระบบ");
        var department = options.Departments.SingleOrDefault(d => d.Id == request.DepartmentId) ?? throw new ArgumentException("กรุณาเลือกหน่วยงานที่เปิดใช้งาน");
        Check(request.Extension, 12, "เบอร์ต่อ");
        if (request.Extension.Any(c => !char.IsAsciiDigit(c))) throw new ArgumentException("เบอร์ต่อต้องเป็นตัวเลข");
        Check(request.DocumentNumber, 80, "เลขที่หนังสือ"); Check(request.Subject, 300, "เรื่อง"); Check(request.Recipient, 300, "เรียน"); Check(request.Closing, 500, "ข้อความลงท้าย");
        if (request.DocumentDate.Year is < 1900 or > 2200) throw new ArgumentException("วันที่เอกสารไม่ถูกต้อง");
        if (request.Body == null || request.Body.Count is < 1 or > 300) throw new ArgumentException("เนื้อหาต้องมี 1–300 ย่อหน้า");
        var total = 0; var runs = 0;
        foreach (var paragraph in request.Body)
        {
            if (paragraph == null || paragraph.Align is not ("left" or "center" or "right" or "justify") || paragraph.Indent is < 0 or > 3 || paragraph.Runs == null)
                throw new ArgumentException("รูปแบบย่อหน้าไม่ถูกต้อง");
            foreach (var run in paragraph.Runs)
            {
                if (run == null || run.Text == null) throw new ArgumentException("เนื้อหาไม่ถูกต้อง");
                Check(run.Text, 30000, "เนื้อหา", false); total += run.Text.Length; runs++;
            }
        }
        if (total > 30000 || runs > 3000 || !request.Body.Any(p => p.Runs.Any(r => !string.IsNullOrWhiteSpace(r.Text)))) throw new ArgumentException("กรุณาระบุเนื้อหาไม่เกิน 30,000 ตัวอักษร");
        var documentText = string.Join("\n", new[] { request.Subject, request.Closing }.Concat(request.Body.Select(p => string.Concat(p.Runs.Select(r => r.Text)))));
        if (Regex.IsMatch(documentText, @"\{[A-Za-z_][A-Za-z0-9_]*\}")) throw new ArgumentException("ยังมีตัวแปรในเทมเพลตที่ไม่ได้กรอก กรุณาเติมข้อมูลให้ครบก่อนสร้างไฟล์");
        Check(request.HeadComment, 1000, "ความเห็นหัวหน้า", false); Check(request.DirectorComment, 1000, "ความเห็นผู้อำนวยการ", false);
        MemoPerson? head = null; MemoPerson? director = null;
        if (request.IncludeHead)
        {
            head = options.Approvers?.SingleOrDefault(p => p.Id == request.HeadUserId) ?? throw new ArgumentException("กรุณาเลือกผู้ลงนามจากผู้อนุมัติที่เปิดใช้งาน");
            Check(head.Name, 300, "ชื่อหัวหน้า"); Check(head.Position, 300, "ตำแหน่งหัวหน้า กรุณาติดต่อผู้ดูแลระบบ");
        }
        if (request.IncludeDirector)
        {
            if (options.Directors.Count == 0) throw new ArgumentException("ไม่พบผู้ดำรงบทบาท Director ที่เปิดใช้งาน กรุณาติดต่อผู้ดูแลระบบ");
            if (options.Directors.Count != 1) throw new ArgumentException("พบผู้ดำรงบทบาท Director มากกว่าหนึ่งบัญชี กรุณาให้ผู้ดูแลตรวจสอบผู้ดำรงตำแหน่งปัจจุบัน");
            director = options.Directors.Single();
            Check(director.Name, 300, "ชื่อผู้อำนวยการ");
        }
        var code = request.ContentTemplateCode?.Trim();
        var template = string.IsNullOrEmpty(code) ? null : options.ContentTemplates?.SingleOrDefault(t => t.Code == code && t.IsActive)
            ?? throw new ArgumentException("เทมเพลตสำหรับชื่อไฟล์ไม่ถูกต้องหรือปิดใช้งาน");
        if (!string.IsNullOrEmpty(request.WorkMonth) && template is null) throw new ArgumentException("กรุณาระบุเทมเพลตของเดือนงานสำหรับชื่อไฟล์");
        var month = string.IsNullOrEmpty(request.WorkMonth) ? request.DocumentDate : CorrespondenceFileNames.ParseMonth(request.WorkMonth);
        var title = template is null ? request.Subject : string.IsNullOrWhiteSpace(template.FileNameTitle) ? template.Name : template.FileNameTitle;
        return new(request, department.Name, options.Proposer, head, director,
            CorrespondenceFileNames.Memo(title, month, request.DocumentNumber, options.Proposer.Name));
    }
    private static void Check(string? value, int limit, string label, bool required = true)
    {
        if (value == null || value.Length > limit || required && string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{label}: ข้อมูลไม่ครบหรือยาวเกินกำหนด");
        try { XmlConvert.VerifyXmlChars(value); } catch (XmlException) { throw new ArgumentException($"{label}: มีอักขระที่ไม่รองรับ"); }
    }
}
