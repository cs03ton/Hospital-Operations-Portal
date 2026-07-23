using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveEntitlementInitializationTests
{
    [Fact]
    public async Task InitializeAsync_CreatesBalancesFromEmploymentPolicy()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee, new DateOnly(2026, 1, 1));
        var vacation = await AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน", 10);
        var sick = await AddLeaveType(db, "SICK_LEAVE", "ลาป่วย", 30);
        AddPolicy(db, user.EmploymentType!, vacation.Id, 10, firstYear: 6);
        AddPolicy(db, user.EmploymentType!, sick.Id, 45);
        await db.SaveChangesAsync();
        var audit = new CaptureAuditLogService();
        var service = CreateService(db, audit);

        var result = await service.InitializeAsync(user.Id, 2027, new DateOnly(2026, 10, 1), Guid.NewGuid(), "test");

        Assert.Equal(2, result.Created);
        Assert.Empty(result.Errors);
        var balances = await db.LeaveBalances.OrderBy(item => item.LeaveTypeId).ToListAsync();
        Assert.Equal(2, balances.Count);
        Assert.Contains(balances, item => item.LeaveTypeId == vacation.Id && item.EntitledDays == 6);
        Assert.Contains(balances, item => item.LeaveTypeId == sick.Id && item.EntitledDays == 45);
        Assert.Equal(2, await db.LeaveBalanceTransactions.CountAsync(item => item.TransactionType == LeaveBalanceTransactionTypes.EntitlementGranted));
        Assert.Contains("LeaveEntitlement.Initialized", audit.Actions);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee, new DateOnly(2024, 10, 1));
        var vacation = await AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน", 10);
        AddPolicy(db, user.EmploymentType!, vacation.Id, 10);
        await db.SaveChangesAsync();
        var service = CreateService(db, new CaptureAuditLogService());

        var first = await service.InitializeAsync(user.Id, 2027, new DateOnly(2026, 10, 1), null, "first");
        var second = await service.InitializeAsync(user.Id, 2027, new DateOnly(2026, 10, 1), null, "second");

        Assert.Equal(1, first.Created);
        Assert.Equal(0, second.Created);
        Assert.Equal(1, second.Skipped);
        Assert.Equal(1, await db.LeaveBalances.CountAsync(item => item.UserId == user.Id && item.Year == 2027));
        Assert.Equal(1, await db.LeaveBalanceTransactions.CountAsync(item => item.TransactionType == LeaveBalanceTransactionTypes.EntitlementGranted));
    }

    [Fact]
    public async Task InitializeAsync_BlocksWhenStartDateIsMissing()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee, null);
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน", 10);
        AddPolicy(db, user.EmploymentType!, leaveType.Id, 10);
        await db.SaveChangesAsync();
        var service = CreateService(db, new CaptureAuditLogService());

        var result = await service.InitializeAsync(user.Id, 2027, new DateOnly(2026, 10, 1), null, "test");

        Assert.Equal(1, result.Blocked);
        Assert.Contains(result.Errors, item => item.Contains("วันที่เริ่มงาน"));
        Assert.Empty(await db.LeaveBalances.ToListAsync());
    }

    private static LeaveEntitlementService CreateService(AppDbContext db, IAuditLogService audit)
    {
        return new LeaveEntitlementService(db, new LeavePolicyService(db), audit);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<User> AddUser(AppDbContext db, string employmentType, DateOnly? startDate)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"user-{Guid.NewGuid():N}",
            FullName = "Test User",
            PasswordHash = "hash",
            EmploymentType = employmentType,
            EmploymentStartDate = startDate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<LeaveType> AddLeaveType(AppDbContext db, string code, string name, decimal defaultDays)
    {
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            DefaultDaysPerYear = defaultDays,
            RequiresBalance = true,
            IsActive = true,
            UseFiscalYear = true
        };
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();
        return leaveType;
    }

    private static void AddPolicy(AppDbContext db, string employmentType, Guid leaveTypeId, decimal entitlement, decimal? firstYear = null)
    {
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = employmentType,
            LeaveTypeId = leaveTypeId,
            EntitlementDays = entitlement,
            FirstYearEntitlementDays = firstYear,
            IsActive = true
        });
    }

    private sealed class CaptureAuditLogService : IAuditLogService
    {
        public List<string> Actions { get; } = [];

        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
