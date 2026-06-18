using Hop.Api.Data;
using Hop.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveCalendarService(AppDbContext db) : ILeaveCalendarService
{
    public async Task<decimal> CalculateBusinessDaysAsync(DateOnly startDate, DateOnly endDate, bool isHalfDay)
    {
        if (endDate < startDate)
        {
            return 0;
        }

        if (isHalfDay && startDate == endDate)
        {
            return await IsWorkingDayAsync(startDate) ? 0.5m : 0;
        }

        var days = 0m;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (await IsWorkingDayAsync(date))
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
