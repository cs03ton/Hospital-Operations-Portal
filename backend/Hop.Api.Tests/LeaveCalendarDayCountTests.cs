using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveCalendarDayCountTests
{
    [Fact]
    public async Task September24To25CountsTwoWorkingDaysAndOneWhenHolidayIsActive()
    {
        await using var db = CreateDbContext();
        var calendar = new LeaveCalendarService(db);
        var start = new DateOnly(2026, 9, 24);
        var end = new DateOnly(2026, 9, 25);

        Assert.Equal(2m, await calendar.CalculateBusinessDaysAsync(start, end, false));

        db.LeaveHolidays.Add(new LeaveHoliday
        {
            Id = Guid.NewGuid(), HolidayDate = start, Name = "Test holiday", IsActive = true
        });
        await db.SaveChangesAsync();

        Assert.Equal(1m, await calendar.CalculateBusinessDaysAsync(start, end, false));
    }

    [Fact]
    public async Task WeekendAndHalfDayFollowExistingCalendarRules()
    {
        await using var db = CreateDbContext();
        var calendar = new LeaveCalendarService(db);

        Assert.Equal(2m, await calendar.CalculateBusinessDaysAsync(
            new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 28), false));
        Assert.Equal(0.5m, await calendar.CalculateBusinessDaysAsync(
            new DateOnly(2026, 9, 24), new DateOnly(2026, 9, 24), true));
    }

    [Fact]
    public async Task GeneralStaffExcludeWeekendAndHolidayWhileNursingCountsEveryCalendarDay()
    {
        await using var db = CreateDbContext();
        db.LeaveHolidays.Add(new LeaveHoliday
        {
            Id = Guid.NewGuid(), HolidayDate = new DateOnly(2026, 10, 13), Name = "วันหยุดทดสอบ", IsActive = true
        });
        await db.SaveChangesAsync();
        var calendar = new LeaveCalendarService(db);
        var start = new DateOnly(2026, 10, 9);
        var end = new DateOnly(2026, 10, 14);

        Assert.Equal(3m, await calendar.CalculateLeaveDaysAsync(start, end, false, false));
        Assert.Equal(6m, await calendar.CalculateLeaveDaysAsync(start, end, false, true));
        Assert.Equal(0m, await calendar.CalculateLeaveDaysAsync(new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 13), false, false));
        Assert.Equal(1m, await calendar.CalculateLeaveDaysAsync(new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 13), false, true));
        Assert.Equal(0.5m, await calendar.CalculateLeaveDaysAsync(new DateOnly(2026, 10, 11), new DateOnly(2026, 10, 11), true, true));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }
}
