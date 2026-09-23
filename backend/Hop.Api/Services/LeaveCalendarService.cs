using Hop.Api.Data;
using Hop.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveCalendarService(AppDbContext db) : ILeaveCalendarService
{
    public Task<decimal> CalculateBusinessDaysAsync(DateOnly startDate, DateOnly endDate, bool isHalfDay) =>
        CalculateLeaveDaysAsync(startDate, endDate, isHalfDay, false);

    public async Task<decimal> CalculateLeaveDaysAsync(DateOnly startDate, DateOnly endDate, bool isHalfDay, bool countCalendarDays)
    {
        if (endDate < startDate)
        {
            return 0;
        }

        if (isHalfDay && startDate == endDate)
        {
            return countCalendarDays || await IsWorkingDayAsync(startDate) ? 0.5m : 0;
        }

        if (countCalendarDays)
        {
            return endDate.DayNumber - startDate.DayNumber + 1;
        }

        var holidayDates = await db.LeaveHolidays.AsNoTracking()
            .Where(item => item.IsActive && item.HolidayDate >= startDate && item.HolidayDate <= endDate)
            .Select(item => item.HolidayDate)
            .ToHashSetAsync();
        var days = 0m;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidayDates.Contains(date))
            {
                days += 1;
            }
        }

        return days;
    }

    public Task<bool> IsHolidayAsync(DateOnly date)
    {
        return db.LeaveHolidays
            .AsNoTracking()
            .AnyAsync(item => item.HolidayDate == date && item.IsActive);
    }

    private async Task<bool> IsWorkingDayAsync(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        return !await IsHolidayAsync(date);
    }
}
