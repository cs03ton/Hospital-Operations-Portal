using System.Globalization;

namespace Hop.Api.Services;

public static class ThaiDateDisplay
{
    public static string Date(DateOnly date) => FormattableString.Invariant($"{date.Day:00}/{date.Month:00}/{date.Year + 543}");
    public static string Year(int gregorianYear) => (gregorianYear + 543).ToString(CultureInfo.InvariantCulture);
    public static string FiscalYear(int gregorianFiscalYear) => Year(gregorianFiscalYear);
    public static string Instant(DateTime value)
    {
        var thai = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"));
        return Date(DateOnly.FromDateTime(thai)) + " " + thai.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    public static string InstantWithSuffix(DateTime value) => Instant(value) + " น.";
}
