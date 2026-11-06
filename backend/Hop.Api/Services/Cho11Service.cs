namespace Hop.Api.Services;

public sealed class Cho11Request
{
    public string StartMonth { get; set; } = "";
    public string EndMonth { get; set; } = "";
    public string EmploymentMonth { get; set; } = "";
    public string? EmploymentDate { get; set; }
    public string Hospital { get; set; } = "";
    public string Province { get; set; } = "";
    public Guid ReviewerUserId { get; set; }
}
public sealed record Cho11Options(MemoPerson Author, IReadOnlyList<MemoPerson> Reviewers, IReadOnlyList<MemoPerson> Heads, string Hospital, string Province, string AreaLevel);
public sealed record ResolvedCho11(Cho11Request Input, MemoPerson Author, MemoPerson Reviewer, string AreaLevel, DateOnly EmploymentDate, IReadOnlyList<DateOnly> Months);

public sealed class Cho11Service(CorrespondenceMemoService memos, IConfiguration config)
{
    public async Task<Cho11Options> OptionsAsync(Guid actor, CancellationToken ct)
    {
        var options = await memos.OptionsAsync(actor, ct);
        return new(options.Proposer, options.Approvers ?? [], options.Heads,
            config["Correspondence:Cho11:Hospital"] ?? "โรงพยาบาลนาหมื่น",
            config["Correspondence:Cho11:Province"] ?? "น่าน",
            config["Correspondence:Cho11:AreaLevel"] ?? "พื้นที่เฉพาะระดับ 1");
    }
    public async Task<ResolvedCho11> ResolveAsync(Guid actor, Cho11Request input, CancellationToken ct)
    {
        var options = await OptionsAsync(actor, ct);
        var start = CorrespondenceFileNames.ParseMonth(input.StartMonth ?? "");
        var end = CorrespondenceFileNames.ParseMonth(input.EndMonth ?? "");
        var employment = input.EmploymentDate is null ? CorrespondenceFileNames.ParseMonth(input.EmploymentMonth ?? "") : default;
        if (input.EmploymentDate is not null) {
            if (!DateOnly.TryParseExact(input.EmploymentDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out employment) || employment.Year is < 1900 or > 2200)
                throw new ArgumentException("กรุณาระบุวันเริ่มงานให้ถูกต้อง");
        }
        var count = (end.Year - start.Year) * 12 + end.Month - start.Month + 1;
        if (count is < 1 or > 12) throw new ArgumentException("กรุณาเลือกช่วงเอกสารตามลำดับ ไม่เกิน 12 เดือน");
        if (employment >= start.AddMonths(1)) throw new ArgumentException("เดือนเอกสารต้องไม่อยู่ก่อนวันเริ่มงาน");
        Check(input.Hospital, "โรงพยาบาล", 120); Check(input.Province, "จังหวัด", 80);
        Check(options.Author.Name, "ชื่อผู้ทำเอกสาร กรุณาแก้ Profile", 300);
        Check(options.Author.Position, "ตำแหน่งผู้ทำเอกสาร กรุณาแก้ Profile", 300);
        var reviewer = options.Reviewers.SingleOrDefault(p => p.Id == input.ReviewerUserId)
            ?? throw new ArgumentException("กรุณาเลือกผู้ตรวจสอบจากผู้อนุมัติที่เปิดใช้งาน");
        Check(reviewer.Position, "ตำแหน่งผู้ตรวจสอบ กรุณาติดต่อผู้ดูแล", 300);
        // Copy the validated input; nothing is persisted or added to delivery/audit queues.
        return new(new() { StartMonth = start.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), EndMonth = end.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), EmploymentMonth = employment.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
            EmploymentDate = employment.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Hospital = input.Hospital.Trim(), Province = input.Province.Trim(), ReviewerUserId = reviewer.Id },
            options.Author, reviewer, options.AreaLevel, employment, Enumerable.Range(0, count).Select(start.AddMonths).ToArray());
    }
    private static void Check(string? value, string label, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(char.IsControl))
            throw new ArgumentException($"กรุณาระบุ{label} ไม่เกิน {max} ตัวอักษร");
    }
    public static (int Years, int Months, int Days) Tenure(DateOnly employment, DateOnly month)
    {
        var exclusiveEnd = month.AddMonths(1);
        var months = (exclusiveEnd.Year - employment.Year) * 12 + exclusiveEnd.Month - employment.Month;
        if (employment.AddMonths(months) > exclusiveEnd) months--;
        return (months / 12, months % 12, exclusiveEnd.DayNumber - employment.AddMonths(months).DayNumber);
    }
}

