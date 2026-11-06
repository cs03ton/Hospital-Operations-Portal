using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hop.Api.Services;

internal static class CorrespondenceFileNames
{
    // Reserve five UTF-8 bytes for the longest extension (.docx).
    private const int MaxStemBytes = 235;
    public static string Memo(string title, DateOnly month, string documentNumber, string name) =>
        Build("บันทึกข้อความ", title, month, documentNumber[(documentNumber.LastIndexOf('/') + 1)..], name);

    public static string Report(string kind, DateOnly month, string name) => Build(kind, null, month, null, name);
    public static string Cho11(DateOnly start, DateOnly end, string name) => Build("ฉ.11", null, start,
        start == end ? null : "ถึงเดือน" + end.ToDateTime(TimeOnly.MinValue).ToString("MMMM", CultureInfo.GetCultureInfo("th-TH")) + "-" + (end.Year + 543).ToString(CultureInfo.InvariantCulture), name);

    public static DateOnly ParseMonth(string value)
    {
        if (value.Length != 7 || !DateOnly.TryParseExact(value + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
            || month.Year is < 1900 or > 2200)
            throw new ArgumentException("เดือนงานสำหรับชื่อไฟล์ไม่ถูกต้อง");
        return month;
    }

    private static string Clean(string text) => Regex.Replace(text.Trim().Normalize(), """[\p{Cc}<>:"/\\|?*]""", "-").Trim(' ', '.', '-');
    private static string Shorten(string text)
    {
        var starts = StringInfo.ParseCombiningCharacters(text);
        if (starts.Length <= 1) return text;
        var end = starts[^1];
        // Keep preposed Thai vowels attached to the following consonant.
        while (end > 0 && text[end - 1] is >= '\u0e40' and <= '\u0e44') end--;
        return text[..end].TrimEnd(' ', '.', '-');
    }

    private static string Build(string kind, string? title, DateOnly month, string? outgoing, string name)
    {
        var shortTitle = title is null ? null : Clean(title);
        if (shortTitle is "") shortTitle = "เอกสาร";
        var person = Clean(name);
        var period = "เดือน" + month.ToDateTime(TimeOnly.MinValue).ToString("MMMM", CultureInfo.GetCultureInfo("th-TH"));
        var number = outgoing is null ? null : Clean(outgoing);
        string Compose() => string.Join("-", new[] { kind, shortTitle, period, (month.Year + 543).ToString(CultureInfo.InvariantCulture), number, person }.Where(x => !string.IsNullOrEmpty(x)));
        while (Encoding.UTF8.GetByteCount(Compose()) > MaxStemBytes && shortTitle is { Length: > 0 }) {
            var next = Shorten(shortTitle); if (next == shortTitle) break; shortTitle = next;
        }
        while (Encoding.UTF8.GetByteCount(Compose()) > MaxStemBytes && person.Length > 0) {
            var next = Shorten(person); if (next == person) break; person = next;
        }
        var result = Compose();
        if (Encoding.UTF8.GetByteCount(result) > MaxStemBytes) throw new ArgumentException("ข้อมูลสำหรับชื่อไฟล์ยาวเกินกำหนด กรุณาตรวจเลขที่หนังสือ");
        return result;
    }
}
