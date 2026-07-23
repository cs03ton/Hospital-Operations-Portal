using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveBalanceRolloverTests
{
    [Fact]
    public async Task PreviewBatchRollover_DoesNotWriteDatabase()
    {
        await using var db = CreateDbContext();
        var audit = new FakeAuditLogService();
        var (user, leaveType, _) = await SeedPolicyBalance(db, EmploymentTypes.MophEmployee, new DateOnly(2020, 10, 1), allowCarryOver: true, carryOverMaxDays: 15, used: 2);
        var service = new LeaveBalanceRolloverService(db, new LeavePolicyService(db), audit);

        var result = await service.PreviewAsync(new LeaveBalanceRolloverFilterRequest(2026, 2027, UserId: user.Id), Guid.NewGuid());

        Assert.Single(result.Items);
        Assert.Equal(0, await db.LeaveBalances.CountAsync(item => item.UserId == user.Id && item.LeaveTypeId == leaveType.Id && item.Year == 2027));
        Assert.Contains(audit.Actions, action => action == "LeaveBalance.RolloverPreviewed");
    }

    [Fact]
    public async Task ConfirmBatchRollover_CreatesTargetBalanceAndIsIdempotent()
    {
        await using var db = CreateDbContext();
        var audit = new FakeAuditLogService();
        var (user, leaveType, _) = await SeedPolicyBalance(db, EmploymentTypes.MophEmployee, new DateOnly(2020, 10, 1), allowCarryOver: true, carryOverMaxDays: 15, used: 2);
        var service = new LeaveBalanceRolloverService(db, new LeavePolicyService(db), audit);
        var request = new LeaveBalanceRolloverConfirmBatchRequest(2026, 2027, null, null, leaveType.Id, user.Id, "Rollover");

        var first = await service.ConfirmAsync(request, Guid.NewGuid());
        var second = await service.ConfirmAsync(request, Guid.NewGuid());

        Assert.Equal(1, first.Created);
        Assert.Equal(0, second.Created);
        Assert.Equal(1, second.Skipped);
        Assert.Equal(1, await db.LeaveBalances.CountAsync(item => item.UserId == user.Id && item.LeaveTypeId == leaveType.Id && item.Year == 2027));
        Assert.Single(await db.LeaveBalanceSnapshots.ToListAsync());
        Assert.Contains(audit.Actions, action => action == "LeaveBalance.RolloverCompleted");
    }

    [Fact]
    public async Task ConfirmBatchRollover_RequiresReason()
    {
        await using var db = CreateDbContext();
        var service = new LeaveBalanceRolloverService(db, new LeavePolicyService(db), new FakeAuditLogService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync(
            new LeaveBalanceRolloverConfirmBatchRequest(2026, 2027, null, null, null, null, ""),
            Guid.NewGuid()));
    }

    [Fact]
    public async Task PreviewBatchRollover_RejectsBuddhistFiscalYear()
    {
        await using var db = CreateDbContext();
        var service = new LeaveBalanceRolloverService(db, new LeavePolicyService(db), new FakeAuditLogService());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(
            new LeaveBalanceRolloverFilterRequest(2569, 2570),
            Guid.NewGuid()));

        Assert.Contains("ปีงบประมาณไม่ถูกต้อง", ex.Message);
    }

    [Theory]
    [InlineData(EmploymentTypes.CivilServant, "2018-10-01", 20)]
    [InlineData(EmploymentTypes.CivilServant, "2010-10-01", 30)]
    [InlineData(EmploymentTypes.GovernmentEmployee, "2018-10-01", 15)]
    [InlineData(EmploymentTypes.MophEmployee, "2018-10-01", 15)]
    [InlineData(EmploymentTypes.TemporaryEmployeeMonthly, "2018-10-01", 0)]
    public async Task PreviewBatchRollover_UsesPolicyCarryOverCapByEmploymentType(string employmentType, string startDateText, decimal expectedCap)
    {
        await using var db = CreateDbContext();
        var startDate = DateOnly.ParseExact(startDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var (user, _, _) = await SeedPolicyBalance(db, employmentType, startDate, allowCarryOver: true, carryOverMaxDays: 30, entitlement: 40);
        var service = new LeaveBalanceRolloverService(db, new LeavePolicyService(db), new FakeAuditLogService());

        var result = await service.PreviewAsync(new LeaveBalanceRolloverFilterRequest(2026, 2027, UserId: user.Id), Guid.NewGuid());

        var item = Assert.Single(result.Items);
        Assert.Equal(expectedCap, item.CarryOverCap);
        Assert.Equal(expectedCap, item.CarryOverDays);
    }

    [Fact]
    public async Task PreviewBatchRollover_DeductsPendingDays()
    {
        await using var db = CreateDbContext();
        var (user, _, _) = await SeedPolicyBalance(db, EmploymentTypes.MophEmployee, new DateOnly(2020, 10, 1), allowCarryOver: true, carryOverMaxDays: 15, entitlement: 10, used: 1, pending: 3);
        var service = new LeaveBalanceRolloverService(db, new LeavePolicyService(db), new FakeAuditLogService());

        var result = await service.PreviewAsync(new LeaveBalanceRolloverFilterRequest(2026, 2027, UserId: user.Id), Guid.NewGuid());

        var item = Assert.Single(result.Items);
        Assert.Equal(6, item.EndYearRemaining);
        Assert.Equal(6, item.CarryOverDays);
    }

    [Fact]
    public async Task PreviewIndividualRollover_CalculatesCarryOverAndForfeitedDays()
    {
        await using var db = CreateDbContext();
        var (_, leaveType, balance) = await SeedBalance(db, allowCarryOver: true, carryOverMaxDays: 30, entitlement: 40, carriedOver: 5, adjusted: 2, used: 7, pending: 3);
        var controller = CreateController(db, new FakeAuditLogService());

        var result = await controller.PreviewIndividualRollover(balance.Id);

        var response = Assert.IsType<ApiResponse<LeaveBalanceRolloverPreviewResponse>>(result.Value);
        Assert.NotNull(response.Data);
        Assert.Equal(37, response.Data.EndYearRemaining);
        Assert.Equal(30, response.Data.CarryOverDays);
        Assert.Equal(7, response.Data.ForfeitedDays);
        Assert.Equal(leaveType.DefaultDaysPerYear + 30, response.Data.NewAvailableDays);
    }

    [Fact]
    public async Task PreviewIndividualRollover_BlocksLeaveTypeThatDoesNotAllowCarryOver()
    {
        await using var db = CreateDbContext();
        var (_, _, balance) = await SeedBalance(db, allowCarryOver: false, carryOverMaxDays: 30);
        var audit = new FakeAuditLogService();
        var controller = CreateController(db, audit);

        var result = await controller.PreviewIndividualRollover(balance.Id);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains(audit.Actions, action => action == "LeaveBalance.RolloverBlocked");
    }

    [Fact]
    public async Task PreviewIndividualRollover_WarnsWhenTargetBalanceAlreadyExists()
    {
        await using var db = CreateDbContext();
        var (user, leaveType, balance) = await SeedBalance(db, allowCarryOver: true, carryOverMaxDays: 30);
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = balance.Year + 1,
            EntitledDays = 10,
            CarriedOverDays = 0
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new FakeAuditLogService());

        var result = await controller.PreviewIndividualRollover(balance.Id);

        var response = Assert.IsType<ApiResponse<LeaveBalanceRolloverPreviewResponse>>(result.Value);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.TargetBalanceExists);
        Assert.Contains(response.Data.Warnings, warning => warning.Contains("ปลายทาง"));
    }

    [Fact]
    public async Task ConfirmIndividualRollover_CreatesNextFiscalYearBalance()
    {
        await using var db = CreateDbContext();
        var (user, leaveType, balance) = await SeedBalance(db, allowCarryOver: true, carryOverMaxDays: 30, entitlement: 10, carriedOver: 5, adjusted: 0, used: 2, pending: 1);
        var audit = new FakeAuditLogService();
        var controller = CreateController(db, audit);

        var result = await controller.ConfirmIndividualRollover(
            balance.Id,
            new LeaveBalanceRolloverConfirmRequest(balance.Year + 1, 10, "Rollover for test"));

        var response = Assert.IsType<ApiResponse<LeaveBalanceResponse>>(result.Value);
        Assert.NotNull(response.Data);
        Assert.Equal(balance.Year + 1, response.Data.Year);
        Assert.Equal(12, response.Data.CarriedOverDays);
        Assert.Equal(10, response.Data.EntitledDays);
        Assert.Equal(22, response.Data.AvailableDays);
        Assert.Contains(audit.Actions, action => action == "LeaveBalance.RolloverConfirmed");

        var target = await db.LeaveBalances.SingleAsync(item => item.UserId == user.Id && item.LeaveTypeId == leaveType.Id && item.Year == balance.Year + 1);
        Assert.Equal("Rollover for test", target.Notes);
    }

    [Fact]
    public async Task ConfirmIndividualRollover_RequiresReason()
    {
        await using var db = CreateDbContext();
        var (_, _, balance) = await SeedBalance(db, allowCarryOver: true, carryOverMaxDays: 30);
        var controller = CreateController(db, new FakeAuditLogService());

        var result = await controller.ConfirmIndividualRollover(
            balance.Id,
            new LeaveBalanceRolloverConfirmRequest(balance.Year + 1, 10, ""));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(User User, LeaveType LeaveType, LeaveBalance Balance)> SeedBalance(
        AppDbContext db,
        bool allowCarryOver,
        decimal carryOverMaxDays,
        decimal entitlement = 10,
        decimal carriedOver = 0,
        decimal adjusted = 0,
        decimal used = 0,
        decimal pending = 0)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"user-{Guid.NewGuid():N}",
            FullName = "Test User",
            PasswordHash = "hash",
            IsActive = true
        };
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = $"LT-{Guid.NewGuid():N}",
            Name = "Annual Leave",
            DefaultDaysPerYear = entitlement,
            RequiresBalance = true,
            UseFiscalYear = true,
            AllowCarryOver = allowCarryOver,
            CarryOverMaxDays = carryOverMaxDays,
            IsActive = true
        };
        var balance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            Year = 2026,
            EntitledDays = entitlement,
            CarriedOverDays = carriedOver,
            AdjustedDays = adjusted,
            UsedDays = used,
            PendingDays = pending
        };
        db.Users.Add(user);
        db.LeaveTypes.Add(leaveType);
        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();
        return (user, leaveType, balance);
    }

    private static async Task<(User User, LeaveType LeaveType, LeaveBalance Balance)> SeedPolicyBalance(
        AppDbContext db,
        string employmentType,
        DateOnly employmentStartDate,
        bool allowCarryOver,
        decimal carryOverMaxDays,
        decimal entitlement = 40,
        decimal carriedOver = 0,
        decimal adjusted = 0,
        decimal used = 0,
        decimal pending = 0)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"user-{Guid.NewGuid():N}",
            FullName = "Test User",
            PasswordHash = "hash",
            EmploymentType = employmentType,
            EmploymentStartDate = employmentStartDate,
            IsActive = true
        };
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = "VACATION_LEAVE",
            Name = "ลาพักผ่อน",
            DefaultDaysPerYear = 10,
            RequiresBalance = true,
            UseFiscalYear = true,
            AllowCarryOver = allowCarryOver,
            CarryOverMaxDays = carryOverMaxDays,
            IsActive = true
        };
        var policy = new LeavePolicyRule
        {
            EmploymentType = employmentType,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            EntitlementDays = 10,
            AllowCarryOver = allowCarryOver,
            CarryOverMaxDays = carryOverMaxDays,
            MaxAccumulatedDays = carryOverMaxDays,
            IsActive = true
        };
        var balance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2026,
            EntitledDays = entitlement,
            CarriedOverDays = carriedOver,
            AdjustedDays = adjusted,
            UsedDays = used,
            PendingDays = pending
        };
        db.Users.Add(user);
        db.LeaveTypes.Add(leaveType);
        db.LeavePolicyRules.Add(policy);
        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();
        return (user, leaveType, balance);
    }

    private static LeaveBalancesController CreateController(AppDbContext db, IAuditLogService auditLogService)
    {
        var controller = new LeaveBalancesController(
            db,
            auditLogService,
            new LeaveBalanceRolloverService(db, new LeavePolicyService(db), auditLogService),
            new LeavePolicyService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                ], "Test"))
            }
        };
        return controller;
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<string> Actions { get; } = [];

        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
