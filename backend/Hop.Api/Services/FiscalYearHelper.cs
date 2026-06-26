using Hop.Api.Models;

namespace Hop.Api.Services;

public static class FiscalYearHelper
{
    public const int StartMonth = 10;
    public const int StartDay = 1;
    public const int CarryOverDefaultMaxDays = 30;

    public static int GetFiscalYear(DateOnly date)
    {
        return date.Month >= StartMonth ? date.Year + 1 : date.Year;
    }

    public static int ResolveBalanceYear(DateOnly date, LeaveType leaveType)
    {
        return leaveType.UseFiscalYear ? GetFiscalYear(date) : date.Year;
    }

    public static decimal CalculateAvailableDays(
        decimal entitledDays,
        decimal carriedOverDays,
        decimal usedDays,
        decimal pendingDays,
        decimal adjustedDays = 0)
    {
        return entitledDays + carriedOverDays + adjustedDays - usedDays - pendingDays;
    }

    public static decimal CalculateCarryOver(decimal previousAvailableDays, LeaveType leaveType)
    {
        if (!leaveType.AllowCarryOver)
        {
            return 0;
        }

        var maxDays = leaveType.CarryOverMaxDays > 0 ? leaveType.CarryOverMaxDays : CarryOverDefaultMaxDays;
        return Math.Min(Math.Max(previousAvailableDays, 0), maxDays);
    }
}
