using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveValidationTests
{
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
        var service = new LeaveValidationService(db, new LeaveCalendarService(db));

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
        var service = new LeaveValidationService(db, new LeaveCalendarService(db));

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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
