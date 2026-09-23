namespace Hop.Api.Interfaces;

public interface ILeaveCalendarService
{
    Task<decimal> CalculateLeaveDaysAsync(DateOnly startDate, DateOnly endDate, bool isHalfDay, bool countCalendarDays);
    Task<decimal> CalculateBusinessDaysAsync(DateOnly startDate, DateOnly endDate, bool isHalfDay);
    Task<bool> IsHolidayAsync(DateOnly date);
}
