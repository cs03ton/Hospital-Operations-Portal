using System.Globalization;
using System.Xml;

namespace Hop.Api.Services;

public sealed record OvertimeReportTask(string Description, string Quantity);
public sealed class OvertimeReportEntry
{
    public DateOnly Date { get; set; }
    public DateOnly EndDate { get; set; }
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public decimal? Hours { get; set; }
    public List<OvertimeReportTask> Tasks { get; set; } = [];
    public string Remarks { get; set; } = "";
}
public sealed class OvertimeReportRequest
{
    public Guid DepartmentId { get; set; }
    public string ProjectName { get; set; } = "";
    public Guid SupervisorUserId { get; set; }
    public List<OvertimeReportEntry> Entries { get; set; } = [];
}
public sealed record ResolvedOvertimeReport(string ProjectName, string Department, MemoPerson Reporter, MemoPerson Supervisor, IReadOnlyList<OvertimeReportEntry> Entries);

public sealed class OvertimeWorkReportService(CorrespondenceMemoService memos)
{
    public async Task<ResolvedOvertimeReport> ResolveAsync(Guid actor, OvertimeReportRequest request, CancellationToken ct)
    {
        var options = await memos.OptionsAsync(actor, ct);
        Check(options.Proposer.Name, 300, "ชื่อผู้รายงาน กรุณาแก้ Profile");
        Check(options.Proposer.Position, 300, "ตำแหน่งผู้รายงาน กรุณาแก้ Profile");
        var department = options.Departments.SingleOrDefault(d => d.Id == request.DepartmentId) ?? throw new ArgumentException("กรุณาเลือกหน่วยงานที่เปิดใช้งาน");
        var supervisor = options.Approvers?.SingleOrDefault(p => p.Id == request.SupervisorUserId) ?? throw new ArgumentException("กรุณาเลือกผู้ควบคุมจากผู้อนุมัติที่เปิดใช้งาน");
        Check(supervisor.Name, 300, "ชื่อผู้ควบคุม"); Check(supervisor.Position, 300, "ตำแหน่งผู้ควบคุม กรุณาแก้ Profile");
        Check(request.ProjectName, 500, "โครงการ/กิจกรรมปฏิบัติงาน");
        if (request.Entries == null || request.Entries.Count is < 1 or > 50) throw new ArgumentException("เพิ่มวันปฏิบัติงานได้ 1–50 รายการ");
        var total = request.ProjectName.Length; var tasks = 0; var entries = new List<OvertimeReportEntry>();
        foreach (var entry in request.Entries)
        {
            if (entry == null) throw new ArgumentException("ข้อมูลวันปฏิบัติงานไม่ถูกต้อง");
            if (entry.Date.Year is < 1900 or > 2100 || entry.EndDate.Year is < 1900 or > 2100 ||
                !TimeOnly.TryParseExact(entry.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !TimeOnly.TryParseExact(entry.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                throw new ArgumentException("กรุณาระบุวันที่และเวลาแบบ 24 ชั่วโมงให้ถูกต้อง");
            var duration = entry.EndDate.ToDateTime(end) - entry.Date.ToDateTime(start);
            if (duration <= TimeSpan.Zero) throw new ArgumentException("วัน–เวลาสิ้นสุดต้องอยู่หลังวัน–เวลาเริ่ม หากข้ามคืนให้เลือกวันสิ้นสุดจริง");
            if (entry.Tasks == null || entry.Tasks.Count is < 1 or > 20) throw new ArgumentException("ระบุลักษณะงานได้ 1–20 งานต่อรายการ");
            Check(entry.Remarks, 1000, "หมายเหตุ", false); total += entry.Remarks.Length;
            foreach (var task in entry.Tasks)
            {
                if (task == null) throw new ArgumentException("ข้อมูลลักษณะงานไม่ถูกต้อง");
                Check(task.Description, 2000, "ลักษณะงานที่ปฏิบัติ"); Check(task.Quantity, 200, "ปริมาณ/หน่วยนับ");
                total += task.Description.Length + task.Quantity.Length; tasks++;
            }
            // Hours sent by older clients are compatibility data only. Always derive them from the actual interval.
            entries.Add(new() { Date = entry.Date, EndDate = entry.EndDate, StartTime = entry.StartTime, EndTime = entry.EndTime,
                Hours = decimal.Floor(duration.Ticks / (decimal)TimeSpan.TicksPerHour), Tasks = entry.Tasks.ToList(), Remarks = entry.Remarks });
        }
        if (tasks > 500 || total > 60000) throw new ArgumentException("รายงานต้องไม่เกิน 500 งาน และ 60,000 ตัวอักษร");
        if (request.Entries.GroupBy(e => (e.Date, e.EndDate, e.StartTime, e.EndTime)).Any(g => g.Count() > 1)) throw new ArgumentException("มีวัน–เวลาปฏิบัติงานซ้ำกัน กรุณารวมงานไว้ในรายการเดียวกัน");
        return new(request.ProjectName.Trim(), department.Name, options.Proposer, supervisor,
            entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime, StringComparer.Ordinal).ToArray());
    }
    private static void Check(string? text, int limit, string label, bool required = true)
    {
        if (text == null || text.Length > limit || required && string.IsNullOrWhiteSpace(text)) throw new ArgumentException($"{label}: ข้อมูลไม่ครบหรือยาวเกินกำหนด");
        try { XmlConvert.VerifyXmlChars(text); } catch (XmlException) { throw new ArgumentException($"{label}: มีอักขระที่ไม่รองรับ"); }
    }
}
