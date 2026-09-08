using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class LeaveBalanceReconciliationTests
{
    [Fact]
    public async Task Usage_counts_only_approved_and_pending_requests()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();
        db.LeaveTypes.Add(new LeaveType { Id = leaveTypeId, Code = "VACATION", Name = "Vacation", UseFiscalYear = true });
        db.LeaveRequests.AddRange(
            Request(userId, leaveTypeId, "Approved", 2, new DateOnly(2025, 10, 1)),
            Request(userId, leaveTypeId, "Pending", 1.5m, new DateOnly(2026, 9, 30)),
            Request(userId, leaveTypeId, "Rejected", 9, new DateOnly(2026, 1, 1)),
            Request(userId, leaveTypeId, "Cancelled", 9, new DateOnly(2026, 1, 2)),
            Request(userId, leaveTypeId, "ReturnedForRevision", 9, new DateOnly(2026, 1, 3)),
            Request(userId, leaveTypeId, "Draft", 9, new DateOnly(2026, 1, 4)),
            Request(userId, leaveTypeId, "Approved", 9, new DateOnly(2026, 10, 1)));
        await db.SaveChangesAsync();

        var usage = await new LeaveBalanceReconciliationService(db).GetUsageAsync(userId, leaveTypeId, 2026);

        Assert.Equal(2, usage.UsedDays);
        Assert.Equal(1.5m, usage.PendingDays);
    }

    [Fact]
    public async Task Confirm_repairs_cache_and_is_idempotent()
    {
        await using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), Username = "reconcile-user", FullName = "Reconcile User", IsActive = true };
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = "SICK", Name = "Sick", UseFiscalYear = false };
        var balance = new LeaveBalance { Id = Guid.NewGuid(), UserId = user.Id, LeaveTypeId = leaveType.Id, Year = 2026, EntitledDays = 30 };
        db.AddRange(user, leaveType, balance, Request(user.Id, leaveType.Id, "Approved", 3, new DateOnly(2026, 5, 1)));
        await db.SaveChangesAsync();
        var service = new LeaveBalanceReconciliationService(db);

        var preview = await service.PreviewAsync(new LeaveBalanceReconciliationRequest(UserId: user.Id));
        Assert.Equal(1, preview.Updated);
        Assert.Equal(0, balance.UsedDays);

        var confirmed = await service.ConfirmAsync(new LeaveBalanceReconciliationRequest(UserId: user.Id));
        Assert.Equal(1, confirmed.Updated);
        Assert.Equal(3, balance.UsedDays);

        var repeated = await service.ConfirmAsync(new LeaveBalanceReconciliationRequest(UserId: user.Id));
        Assert.Equal(0, repeated.Updated);
        Assert.Equal(1, repeated.Skipped);
    }

    [Fact]
    public async Task Confirm_creates_missing_balance_from_policy_and_request_history()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "missing-balance", FullName = "Missing Balance",
            EmploymentType = "CIVIL_SERVANT", EmploymentStartDate = new DateOnly(2020, 1, 1), IsActive = true
        };
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = "SICK", Name = "Sick", UseFiscalYear = false, RequiresBalance = true };
        db.AddRange(
            user,
            leaveType,
            new LeavePolicyRule { Id = Guid.NewGuid(), LeaveTypeId = leaveType.Id, EmploymentType = user.EmploymentType, EntitlementDays = 30, IsActive = true },
            Request(user.Id, leaveType.Id, "Approved", 2, new DateOnly(2026, 3, 1)),
            Request(user.Id, leaveType.Id, "Pending", 1, new DateOnly(2026, 4, 1)));
        await db.SaveChangesAsync();
        var service = new LeaveBalanceReconciliationService(db);

        var preview = await service.PreviewAsync(new LeaveBalanceReconciliationRequest(UserId: user.Id));
        Assert.Single(preview.Items);
        Assert.Null(preview.Items[0].BalanceId);
        Assert.Empty(db.LeaveBalances);

        var confirmed = await service.ConfirmAsync(new LeaveBalanceReconciliationRequest(UserId: user.Id));
        var created = Assert.Single(db.LeaveBalances);
        Assert.Equal(1, confirmed.Updated);
        Assert.Equal(30, created.EntitledDays);
        Assert.Equal(2, created.UsedDays);
        Assert.Equal(1, created.PendingDays);

        var repeated = await service.ConfirmAsync(new LeaveBalanceReconciliationRequest(UserId: user.Id));
        Assert.Equal(0, repeated.Updated);
        Assert.Equal(1, repeated.Skipped);
    }

    private static LeaveRequest Request(Guid userId, Guid leaveTypeId, string status, decimal days, DateOnly startDate) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, LeaveTypeId = leaveTypeId, Status = status,
        StartDate = startDate, EndDate = startDate, TotalDays = days, Reason = "test"
    };

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
