using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class DashboardSummaryTests
{
    [Fact]
    public async Task GetSummary_ReturnsCoreLeaveBalancesWithoutCombiningSpecialLeaveTypes()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "เจ้าหน้าที่ทดสอบ",
            Username = "staff.dashboard",
            PasswordHash = "hash",
            IsActive = true
        };
        db.Users.Add(user);

        var fiscalYear = FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(DateTime.UtcNow));
        var vacation = AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน");
        var personal = AddLeaveType(db, "PERSONAL_LEAVE", "ลากิจส่วนตัว");
        var sick = AddLeaveType(db, "SICK_LEAVE", "ลาป่วย");
        var maternity = AddLeaveType(db, "MATERNITY_LEAVE", "ลาคลอดบุตร");
        var ordination = AddLeaveType(db, "ORDINATION_LEAVE", "ลาบวช");

        AddBalance(db, user.Id, vacation.Id, fiscalYear, entitled: 10, used: 2, pending: 1);
        AddBalance(db, user.Id, personal.Id, fiscalYear, entitled: 5, used: 1, pending: 0);
        AddBalance(db, user.Id, sick.Id, fiscalYear, entitled: 30, used: 3, pending: 0.5m);
        AddBalance(db, user.Id, maternity.Id, fiscalYear, entitled: 90, used: 0, pending: 0);
        AddBalance(db, user.Id, ordination.Id, fiscalYear, entitled: 120, used: 0, pending: 0);
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, user.Id);

        var result = await controller.GetSummary();

        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(result.Value);
        Assert.NotNull(response.Data);
        var summary = response.Data!;
        Assert.Equal(0, summary.MyRemainingLeaveDays);
        Assert.NotNull(summary.MyCoreLeaveBalances);
        var balances = summary.MyCoreLeaveBalances!;
        Assert.Equal(["VACATION_LEAVE", "PERSONAL_LEAVE", "SICK_LEAVE"], balances.Select(item => item.LeaveTypeCode).ToArray());
        Assert.DoesNotContain(balances, item => item.LeaveTypeCode is "MATERNITY_LEAVE" or "ORDINATION_LEAVE");
        Assert.Equal(7, balances.Single(item => item.LeaveTypeCode == "VACATION_LEAVE").AvailableDays);
        Assert.Equal(4, balances.Single(item => item.LeaveTypeCode == "PERSONAL_LEAVE").AvailableDays);
        Assert.Equal(26.5m, balances.Single(item => item.LeaveTypeCode == "SICK_LEAVE").AvailableDays);
    }

    private static LeaveType AddLeaveType(AppDbContext db, string code, string name)
    {
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IsActive = true,
            RequiresBalance = true
        };
        db.LeaveTypes.Add(leaveType);
        return leaveType;
    }

    private static void AddBalance(AppDbContext db, Guid userId, Guid leaveTypeId, int year, decimal entitled, decimal used, decimal pending)
    {
        db.LeaveBalances.Add(new LeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            EntitledDays = entitled,
            UsedDays = used,
            PendingDays = pending
        });
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SetUserContext(ControllerBase controller, Guid userId)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "Test"))
            }
        };
    }
}
