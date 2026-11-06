using System.Globalization;
using System.Xml;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record OvertimePaymentRules(string WeekdayStart = "16:30", int WeekdayMaxHours = 4, int HolidayMaxHours = 7, string BreakStart = "12:00", string BreakEnd = "13:00");
public sealed record OvertimePaymentOptions(string Month, decimal WeekdayRate, decimal HolidayRate, string PayerName, IReadOnlyList<DateOnly> HolidayDates, OvertimePaymentRules Rules);
public sealed record OvertimeDailyWork(int ActualMinutes, int WeekdayMinutes, int HolidayMinutes);
public sealed record OvertimeClaimDay(DateOnly Date, string DayType, decimal ClaimHours);
public sealed class OvertimePaymentInput
{
    public string Month { get; set; } = "";
    public List<OvertimeClaimDay> Days { get; set; } = [];
}
public sealed class OvertimeBundleRequest
{
    public OvertimeReportRequest Report { get; set; } = new();
    public OvertimePaymentInput Payment { get; set; } = new();
}
public sealed record OvertimeReportBundle(MemoFiles WorkReport, MemoFiles PaymentEvidence);
public sealed record OvertimePaymentDay(DateOnly Date, string DayType, int ActualMinutes, int WholeHours, int ClaimHours, decimal Rate, decimal Amount, int EligibleMinutes, int MaxClaimHours);
public sealed record ResolvedOvertimePayment(ResolvedOvertimeReport Report, DateOnly MonthStart, OvertimePaymentOptions Options, IReadOnlyList<OvertimePaymentDay> Days, decimal TotalAmount, string AmountText);

public sealed class OvertimePaymentService(AppDbContext db, IConfiguration configuration, OvertimeWorkReportService reports)
{
    public static DateOnly ParseMonth(string? month)
    {
        if (month?.Length != 7 || !DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date.Year is < 1900 or > 2100)
            throw new ArgumentException("กรุณาเลือกเดือนและปีของเอกสารให้ถูกต้อง");
        return date;
    }
    public async Task<OvertimePaymentOptions> OptionsAsync(Guid actor, string month, CancellationToken ct)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == actor && u.IsActive, ct)) throw new UnauthorizedAccessException();
        var first = ParseMonth(month); var last = first.AddMonths(1).AddDays(-1);
        var weekday = configuration.GetValue<decimal?>("Correspondence:OvertimePayment:WeekdayRate") ?? 50m;
        var holiday = configuration.GetValue<decimal?>("Correspondence:OvertimePayment:HolidayRate") ?? 60m;
        var payer = configuration["Correspondence:OvertimePayment:PayerName"] ?? "นางสาวรสริน แสนซ้าย";
        if (weekday < 0 || holiday < 0 || weekday > 100000 || holiday > 100000 || decimal.Round(weekday, 2) != weekday || decimal.Round(holiday, 2) != holiday || string.IsNullOrWhiteSpace(payer) || payer.Length > 300)
            throw new ArgumentException("การตั้งค่าอัตราค่าตอบแทนหรือผู้จ่ายเงินไม่ถูกต้อง กรุณาติดต่อผู้ดูแล");
        try { XmlConvert.VerifyXmlChars(payer); } catch (XmlException) { throw new ArgumentException("ชื่อผู้จ่ายเงินมีอักขระที่ไม่รองรับ"); }
        var rules = configuration.GetSection("Correspondence:OvertimePayment:Rules").Get<OvertimePaymentRules>() ?? new();
        ValidateRules(rules);
        var holidays = await db.LeaveHolidays.AsNoTracking().Where(h => h.IsActive && h.HolidayDate >= first && h.HolidayDate <= last).Select(h => h.HolidayDate).ToListAsync(ct);
        return new(month, weekday, holiday, payer, Enumerable.Range(0, last.Day).Select(first.AddDays)
            .Where(day => day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(day)).ToArray(), rules);
    }
    private static int Minute(string time) => (int)TimeOnly.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture).ToTimeSpan().TotalMinutes;
    private static void ValidateRules(OvertimePaymentRules rules)
    {
        bool ValidTime(string value) => TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        if (!ValidTime(rules.WeekdayStart) || !ValidTime(rules.BreakStart) || !ValidTime(rules.BreakEnd) || rules.WeekdayMaxHours is < 0 or > 24 || rules.HolidayMaxHours is < 0 or > 24 || Minute(rules.BreakEnd) <= Minute(rules.BreakStart))
            throw new ArgumentException("การตั้งค่ากฎชั่วโมงเบิกไม่ถูกต้อง กรุณาติดต่อผู้ดูแล");
    }
    // Half-open local-time intervals: midnight belongs to the following day only when work continues after it.
    public static IReadOnlyDictionary<DateOnly, int> DailyMinutes(IReadOnlyList<OvertimeReportEntry> entries, DateOnly month) => DailyWork(entries, month, new()).ToDictionary(d => d.Key, d => d.Value.ActualMinutes);
    public static IReadOnlyDictionary<DateOnly, OvertimeDailyWork> DailyWork(IReadOnlyList<OvertimeReportEntry> entries, DateOnly month, OvertimePaymentRules rules)
    {
        ValidateRules(rules);
        var from = month.ToDateTime(TimeOnly.MinValue); var until = month.AddMonths(1).ToDateTime(TimeOnly.MinValue);
        var intervals = entries.Select(e => (Start: e.Date.ToDateTime(TimeOnly.ParseExact(e.StartTime, "HH:mm", CultureInfo.InvariantCulture)), End: e.EndDate.ToDateTime(TimeOnly.ParseExact(e.EndTime, "HH:mm", CultureInfo.InvariantCulture)))).OrderBy(e => e.Start).ToArray();
        var minutes = new SortedDictionary<DateOnly, OvertimeDailyWork>(); DateTime? previousEnd = null;
        foreach (var interval in intervals)
        {
            if (interval.Start < from || interval.End > until || interval.End <= interval.Start) throw new ArgumentException("วันปฏิบัติงานต้องอยู่ภายในเดือนเอกสารที่เลือก กรุณาแยกเอกสารรายเดือน");
            if (previousEnd > interval.Start) throw new ArgumentException("ช่วงเวลาปฏิบัติงานซ้อนกัน กรุณาตรวจสอบก่อนยืนยันชั่วโมงเบิก");
            previousEnd = interval.End; var start = interval.Start;
            while (start < interval.End)
            {
                var end = interval.End < start.Date.AddDays(1) ? interval.End : start.Date.AddDays(1);
                var day = DateOnly.FromDateTime(start);
                var actual = (int)(end - start).TotalMinutes;
                var startMinute = (int)start.TimeOfDay.TotalMinutes;
                var endMinute = startMinute + actual;
                var weekday = Math.Max(0, endMinute - Math.Max(startMinute, Minute(rules.WeekdayStart)));
                var lunch = Math.Max(0, Math.Min(endMinute, Minute(rules.BreakEnd)) - Math.Max(startMinute, Minute(rules.BreakStart)));
                var previous = minutes.GetValueOrDefault(day) ?? new(0, 0, 0);
                minutes[day] = new(previous.ActualMinutes + actual, previous.WeekdayMinutes + weekday, previous.HolidayMinutes + actual - lunch); start = end;
            }
        }
        return minutes;
    }
    public async Task<ResolvedOvertimePayment> ResolveAsync(Guid actor, OvertimeBundleRequest input, CancellationToken ct)
    {
        if (input.Report == null || input.Payment == null) throw new ArgumentException("ข้อมูลรายงานและการเบิกจ่ายไม่ครบ");
        var report = await reports.ResolveAsync(actor, input.Report, ct);
        var options = await OptionsAsync(actor, input.Payment.Month, ct); var first = ParseMonth(options.Month);
        var minutes = DailyWork(report.Entries, first, options.Rules); var claims = input.Payment.Days;
        if (claims == null || claims.Count != minutes.Count || claims.Any(d => d == null) || claims.Select(d => d.Date).Distinct().Count() != claims.Count || claims.Any(d => !minutes.ContainsKey(d.Date)))
            throw new ArgumentException("กรุณายืนยันชั่วโมงเบิกให้ครบทุกวันที่ปฏิบัติงาน โดยไม่เพิ่มวันที่ซ้ำหรือวันที่ไม่มีงาน");
        var days = new List<OvertimePaymentDay>();
        foreach (var claim in claims.OrderBy(d => d.Date))
        {
            var work = minutes[claim.Date];
            if (claim.DayType is not ("weekday" or "holiday")) throw new ArgumentException("กรุณาเลือกประเภทวันให้ถูกต้อง");
            var eligible = claim.DayType == "holiday" ? work.HolidayMinutes : work.WeekdayMinutes;
            var maximum = Math.Min(eligible / 60, claim.DayType == "holiday" ? options.Rules.HolidayMaxHours : options.Rules.WeekdayMaxHours);
            if (claim.ClaimHours < 0 || claim.ClaimHours != decimal.Floor(claim.ClaimHours) || claim.ClaimHours > maximum)
                throw new ArgumentException($"วันที่ {claim.Date:dd/MM/yyyy}: ชั่วโมงเบิกต้องเป็นจำนวนเต็มตั้งแต่ 0 ถึง {maximum} ชั่วโมง ตามกฎของประเภทวันที่เลือก");
            var rate = claim.DayType == "holiday" ? options.HolidayRate : options.WeekdayRate;
            days.Add(new(claim.Date, claim.DayType, work.ActualMinutes, work.ActualMinutes / 60, (int)claim.ClaimHours, rate, claim.ClaimHours * rate, eligible, maximum));
        }
        var total = days.Sum(d => d.Amount);
        return new(report, first, options, days, total, ThaiBaht(total));
    }
    public static string ThaiBaht(decimal amount)
    {
        var value = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (value < 0 || value > 999999999999.99m) throw new ArgumentException("จำนวนเงินเกินขอบเขตที่รองรับ");
        var whole = (long)decimal.Floor(value); var cents = (int)((value - whole) * 100);
        return IntegerText(whole) + "บาท" + (cents == 0 ? "ถ้วน" : IntegerText(cents) + "สตางค์");
    }
    private static string IntegerText(long number)
    {
        if (number == 0) return "ศูนย์";
        if (number >= 1000000) { var rest = number % 1000000; return IntegerText(number / 1000000) + "ล้าน" + (rest == 1 ? "เอ็ด" : rest == 0 ? "" : IntegerText(rest)); }
        string[] digits = ["", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า"], units = ["", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน"];
        var text = ""; var source = number.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < source.Length; i++)
        {
            var digit = source[i] - '0'; var place = source.Length - i - 1; if (digit == 0) continue;
            text += place == 1 ? (digit == 1 ? "" : digit == 2 ? "ยี่" : digits[digit]) + "สิบ" : place == 0 && digit == 1 && number > 1 ? "เอ็ด" : digits[digit] + units[place];
        }
        return text;
    }
}
