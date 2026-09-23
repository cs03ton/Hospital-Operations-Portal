namespace Hop.Api.Services;

public static class LeaveDateNormalizer
{
    public static bool TryNormalize(DateOnly value, out DateOnly normalized)
    {
        normalized = value;
        if (value.Year is >= 2443 and <= 2643)
        {
            try
            {
                normalized = new DateOnly(value.Year - 543, value.Month, value.Day);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        return normalized.Year is >= 1900 and <= 2100;
    }

    public static bool TryNormalizeRange(DateOnly start, DateOnly end, out DateOnly normalizedStart, out DateOnly normalizedEnd)
    {
        normalizedStart = default;
        normalizedEnd = default;
        return TryNormalize(start, out normalizedStart)
            && TryNormalize(end, out normalizedEnd)
            && normalizedEnd >= normalizedStart;
    }
}
