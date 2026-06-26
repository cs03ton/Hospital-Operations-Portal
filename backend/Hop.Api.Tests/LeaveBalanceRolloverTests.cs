using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveBalanceRolloverTests
{
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

    private static LeaveBalancesController CreateController(AppDbContext db, IAuditLogService auditLogService)
    {
        var controller = new LeaveBalancesController(db, auditLogService);
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
