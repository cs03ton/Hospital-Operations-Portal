namespace Hop.Api.Models;

public static class LeaveDurationTypes
{
    public const string FullDay = "FULL_DAY";
    public const string HalfDayAm = "HALF_DAY_AM";
    public const string HalfDayPm = "HALF_DAY_PM";

    public static bool IsHalfDay(string? durationType)
    {
        return string.Equals(durationType, HalfDayAm, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(durationType, HalfDayPm, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? durationType)
    {
        return durationType?.Trim().ToUpperInvariant() switch
        {
            HalfDayAm => HalfDayAm,
            HalfDayPm => HalfDayPm,
            FullDay or null or "" => FullDay,
            _ => string.Empty
        };
    }
}
