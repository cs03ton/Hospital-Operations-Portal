using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveValidationTests
{
    [Theory]
    [InlineData(2026, 9, 30, 2026)]
    [InlineData(2026, 10, 1, 2027)]
    public void FiscalYearHelper_ReturnsExpectedFiscalYear(int year, int month, int day, int expectedFiscalYear)
    {
        var fiscalYear = FiscalYearHelper.GetFiscalYear(new DateOnly(year, month, day));

        Assert.Equal(expectedFiscalYear, fiscalYear);
    }

    [Fact]
    public void FiscalYearHelper_CapsCarryOverAtLeaveTypeMaximum()
    {
        var leaveType = new LeaveType
        {
            AllowCarryOver = true,
            CarryOverMaxDays = 30
        };

        var carriedOver = FiscalYearHelper.CalculateCarryOver(42, leaveType);

        Assert.Equal(30, carriedOver);
    }

    [Fact]
    public void FiscalYearHelper_DeductsUsedAndPendingDaysFromAvailableDays()
    {
        var available = FiscalYearHelper.CalculateAvailableDays(
            entitledDays: 10,
            carriedOverDays: 5,
            usedDays: 3,
            pendingDays: 2);

        Assert.Equal(10, available);
    }

    [Fact]
    public async Task ValidateDraftAsync_CalculatesFullDayBusinessDays()
    {
        await using var db = CreateDbContext();
        var leaveType = await AddLeaveType(db);
        var service = CreateService(db);

        var result = await service.ValidateDraftAsync(new LeaveRequest
        {
            UserId = Guid.NewGuid(),
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 22),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 1,
            Reason = "Full day leave"
        });

        Assert.True(result.IsValid);
        Assert.Equal(1m, result.CalculatedDays);
    }

    [Theory]
    [InlineData(LeaveDurationTypes.HalfDayAm)]
    [InlineData(LeaveDurationTypes.HalfDayPm)]
    public async Task ValidateDraftAsync_CalculatesHalfDayAsPointFive(string durationType)
    {
        await using var db = CreateDbContext();
        var leaveType = await AddLeaveType(db);
        var service = CreateService(db);

        var result = await service.ValidateDraftAsync(new LeaveRequest
        {
            UserId = Guid.NewGuid(),
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 22),
            DurationType = durationType,
            TotalDays = 1,
            Reason = "Half day leave"
        });

        Assert.True(result.IsValid);
        Assert.Equal(0.5m, result.CalculatedDays);
    }

    [Fact]
    public async Task ValidateDraftAsync_RejectsHalfDayAcrossMultipleDates()
    {
        await using var db = CreateDbContext();
        var leaveType = await AddLeaveType(db);
        var service = CreateService(db);

        var result = await service.ValidateDraftAsync(new LeaveRequest
        {
            UserId = Guid.NewGuid(),
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 23),
            DurationType = LeaveDurationTypes.HalfDayAm,
            TotalDays = 0.5m,
            Reason = "Invalid half day leave"
        });

        Assert.False(result.IsValid);
        Assert.Contains("ครึ่งวัน", result.Message);
    }

    [Fact]
    public async Task ValidateDraftAsync_RejectsLeaveOnActiveHoliday()
    {
        await using var db = CreateDbContext();
        var leaveType = await AddLeaveType(db);
        db.LeaveHolidays.Add(new LeaveHoliday
        {
            Id = Guid.NewGuid(),
            HolidayDate = new DateOnly(2026, 6, 22),
            Name = "วันหยุดทดสอบ",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateDraftAsync(new LeaveRequest
        {
            UserId = Guid.NewGuid(),
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 22),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 1,
            Reason = "Holiday leave"
        });

        Assert.False(result.IsValid);
        Assert.Contains("วันหยุด", result.Message);
        Assert.Contains("วันหยุดทดสอบ", result.Message);
    }

    [Fact]
    public async Task ValidateSubmitAsync_AllowsHalfDayWhenRemainingBalanceIsPointFive()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = "PersonalLeave",
            Name = "Personal Leave",
            DefaultDaysPerYear = 1,
            IsActive = true
        };
        db.LeaveTypes.Add(leaveType);
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            Year = 2026,
            EntitledDays = 1,
            UsedDays = 0.5m,
            PendingDays = 0
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateSubmitAsync(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 22),
            DurationType = LeaveDurationTypes.HalfDayPm,
            TotalDays = 0.5m,
            Reason = "Half day leave",
            Status = "Draft"
        });

        Assert.True(result.IsValid);
        Assert.Equal(0.5m, result.CalculatedDays);
    }

    [Fact]
    public async Task ValidateDraftAsync_RejectsOverlappingApprovedLeave()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 23),
            TotalDays = 2,
            Reason = "Existing leave",
            Status = "Approved"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateDraftAsync(new LeaveRequest
        {
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            StartDate = new DateOnly(2026, 6, 23),
            EndDate = new DateOnly(2026, 6, 24),
            TotalDays = 2,
            Reason = "Overlap"
        });

        Assert.False(result.IsValid);
        Assert.Contains("ทับซ้อน", result.Message);
    }

    [Fact]
    public async Task ValidateSubmitAsync_RejectsWhenRemainingBalanceIsNotEnough()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = "AnnualLeave",
            Name = "Annual Leave",
            DefaultDaysPerYear = 2,
            IsActive = true
        };
        db.LeaveTypes.Add(leaveType);
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            Year = 2026,
            EntitledDays = 2,
            UsedDays = 1,
            PendingDays = 0
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateSubmitAsync(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 23),
            TotalDays = 2,
            Reason = "Need leave",
            Status = "Draft"
        });

        Assert.False(result.IsValid);
        Assert.Contains("คงเหลือ", result.Message);
    }

    [Fact]
    public async Task ValidateSubmitAsync_RejectsWhenPendingBalanceLeavesAvailableDaysTooLow()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveType = await AddLeaveType(db, defaultDays: 5);
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            Year = 2026,
            EntitledDays = 5,
            UsedDays = 0,
            PendingDays = 3
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateSubmitAsync(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 24),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 3,
            Reason = "Pending should reserve balance",
            Status = "Draft"
        });

        Assert.False(result.IsValid);
        Assert.Contains("รออนุมัติ 3", result.Message);
        Assert.Contains("เหลือใช้ได้ 2", result.Message);
    }

    [Fact]
    public async Task ValidateSubmitAsync_UsesFiscalYearBalanceForOctoberLeave()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveType = await AddLeaveType(db, defaultDays: 10);
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 10,
            CarriedOverDays = 2,
            UsedDays = 8,
            PendingDays = 3
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateSubmitAsync(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 2),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 2,
            Reason = "Fiscal year leave",
            Status = "Draft"
        });

        Assert.False(result.IsValid);
        Assert.Contains("รออนุมัติ 3", result.Message);
        Assert.Contains("เหลือใช้ได้ 1", result.Message);
    }


    [Fact]
    public async Task ValidateDraftAsync_IgnoresRejectedAndCancelledRequestsForOverlap()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveType = await AddLeaveType(db);
        db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveType.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 22),
                DurationType = LeaveDurationTypes.FullDay,
                TotalDays = 1,
                Reason = "Rejected leave",
                Status = "Rejected"
            },
            new LeaveRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveType.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 22),
                DurationType = LeaveDurationTypes.FullDay,
                TotalDays = 1,
                Reason = "Cancelled leave",
                Status = "Cancelled"
            });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ValidateDraftAsync(new LeaveRequest
        {
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 22),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 1,
            Reason = "Allowed leave"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateSubmitAsync_AllowsLeaveTypeThatDoesNotRequireBalance()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var leaveType = await AddLeaveType(db, defaultDays: 0, requiresBalance: false);
        var service = CreateService(db);

        var result = await service.ValidateSubmitAsync(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 22),
            EndDate = new DateOnly(2026, 6, 26),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 5,
            Reason = "No quota leave",
            Status = "Draft"
        });

        Assert.True(result.IsValid);
        Assert.Equal(5m, result.CalculatedDays);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static LeaveValidationService CreateService(AppDbContext db)
    {
        return new LeaveValidationService(db, new LeaveCalendarService(db), new LeaveBalanceValidationService(db, new LeavePolicyService(db)));
    }

    private static async Task<LeaveType> AddLeaveType(AppDbContext db, decimal defaultDays = 30, bool requiresBalance = true)
    {
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = $"LT-{Guid.NewGuid():N}",
            Name = "Test Leave",
            DefaultDaysPerYear = defaultDays,
            RequiresBalance = requiresBalance,
            IsActive = true
        };
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();
        return leaveType;
    }
}
