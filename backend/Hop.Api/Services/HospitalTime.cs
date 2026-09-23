namespace Hop.Api.Services;

public static class HospitalTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    public static DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
    public static DateOnly Today => DateOnly.FromDateTime(FromUtc(DateTime.UtcNow));
    public static DateTime StartOfDayUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }

    public static DateTime EndExclusiveUtc(DateOnly date) => StartOfDayUtc(date.AddDays(1));
}
