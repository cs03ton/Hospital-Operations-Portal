using Hop.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed record FleetUtcRange(DateTime Start, DateTime End, DateOnly LocalStart, DateOnly LocalEndExclusive);
public sealed class FleetDateRangeService(IOptions<FleetOperationsOptions> options)
{
    private readonly TimeZoneInfo bangkok = Resolve();
    public FleetUtcRange Resolve(string? preset, DateOnly? startDate, DateOnly? endDate, bool calendar = false)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkok));
        DateOnly start;
        DateOnly endExclusive;
        switch ((preset ?? "month").ToLowerInvariant())
        {
            case "today": start = today; endExclusive = today.AddDays(1); break;
            case "week": start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); endExclusive = start.AddDays(7); break;
            case "fiscalyear": start = new(today.Month >= 10 ? today.Year : today.Year - 1, 10, 1); endExclusive = start.AddYears(1); break;
            case "custom": start = startDate ?? throw new ArgumentException("startDate is required."); endExclusive = (endDate ?? throw new ArgumentException("endDate is required.")).AddDays(1); break;
            default: start = new(today.Year, today.Month, 1); endExclusive = start.AddMonths(1); break;
        }
        if (endExclusive <= start) throw new ArgumentException("End date must be after start date.");
        var max = calendar ? options.Value.MaximumCalendarRangeDays : options.Value.MaximumReportRangeDays;
        if (endExclusive.DayNumber - start.DayNumber > max) throw new ArgumentException($"Date range cannot exceed {max} days.");
        return new(ToUtc(start), ToUtc(endExclusive), start, endExclusive);
    }
    private DateTime ToUtc(DateOnly date) => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), bangkok);
    private static TimeZoneInfo Resolve() { try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); } catch { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } }
}
