using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveRequestNumberTests
{
    [Fact]
    public async Task GenerateAsync_IncrementsWithinSameMonth()
    {
        await using var db = CreateDbContext();
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = "LV-202606-001",
            UserId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 20),
            EndDate = new DateOnly(2026, 6, 20),
            TotalDays = 1,
            Reason = "Existing",
            Status = "Draft"
        });
        await db.SaveChangesAsync();
        var service = new LeaveRequestNumberService(db);

        var result = await service.GenerateAsync(new DateTime(2026, 6, 22, 2, 0, 0, DateTimeKind.Utc));

        Assert.Equal("LV-202606-002", result);
    }

    [Fact]
    public async Task GenerateAsync_ResetsSequenceForNewMonth()
    {
        await using var db = CreateDbContext();
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = "LV-202606-009",
            UserId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 20),
            EndDate = new DateOnly(2026, 6, 20),
            TotalDays = 1,
            Reason = "Existing",
            Status = "Draft"
        });
        await db.SaveChangesAsync();
        var service = new LeaveRequestNumberService(db);

        var result = await service.GenerateAsync(new DateTime(2026, 7, 1, 2, 0, 0, DateTimeKind.Utc));

        Assert.Equal("LV-202607-001", result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
