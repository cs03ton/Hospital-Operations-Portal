using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveHolidayPagingTests
{
    [Fact]
    public async Task GetHolidays_ReturnsPagedResult_WhenPagingIsRequested()
    {
        await using var db = CreateDbContext();
        await SeedHolidays(db);
        var controller = new LeaveHolidaysController(db, new NoopAuditLogService());

        var result = await controller.GetHolidays(year: 2027, page: 1, pageSize: 2, search: null);

        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        var paged = Assert.IsType<PagedResponse<LeaveHolidayResponse>>(response.Data);
        Assert.Equal(1, paged.Page);
        Assert.Equal(2, paged.PageSize);
        Assert.Equal(3, paged.TotalItems);
        Assert.Equal(2, paged.TotalPages);
        Assert.Equal(2, paged.Items.Count);
    }

    [Fact]
    public async Task GetHolidays_FiltersByYearAndSearch()
    {
        await using var db = CreateDbContext();
        await SeedHolidays(db);
        var controller = new LeaveHolidaysController(db, new NoopAuditLogService());

        var result = await controller.GetHolidays(year: 2027, page: 1, pageSize: 20, search: "สงกรานต์");

        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        var paged = Assert.IsType<PagedResponse<LeaveHolidayResponse>>(response.Data);
        var item = Assert.Single(paged.Items);
        Assert.Equal("วันสงกรานต์", item.Name);
        Assert.Equal(2027, item.HolidayDate.Year);
    }

    [Fact]
    public async Task GetHolidays_ReturnsList_ForBackwardCompatibleCalls()
    {
        await using var db = CreateDbContext();
        await SeedHolidays(db);
        var controller = new LeaveHolidaysController(db, new NoopAuditLogService());

        var result = await controller.GetHolidays(year: 2026);

        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        var items = Assert.IsAssignableFrom<IReadOnlyList<LeaveHolidayResponse>>(response.Data);
        var item = Assert.Single(items);
        Assert.Equal("วันขึ้นปีใหม่", item.Name);
        Assert.Equal(2026, item.HolidayDate.Year);
    }

    [Fact]
    public async Task ConfirmImport_ThenPagedQuery_FindsImportedYear()
    {
        await using var db = CreateDbContext();
        var controller = new LeaveHolidaysController(db, new NoopAuditLogService());
        SetUserContext(controller);

        var importResult = await controller.ConfirmImport(new LeaveHolidayImportConfirmRequest([
            new LeaveHolidayImportRowRequest(new DateOnly(2027, 12, 31), "วันหยุดสิ้นปีทดสอบ", "National")
        ]));

        var importResponse = Assert.IsType<ApiResponse<LeaveHolidayImportConfirmResponse>>(importResult.Value);
        Assert.Equal(1, importResponse.Data?.AddedCount);
        Assert.Equal(1, await db.LeaveHolidays.CountAsync());
        var savedHoliday = await db.LeaveHolidays.SingleAsync();
        Assert.Equal(2027, savedHoliday.HolidayDate.Year);

        var result = await controller.GetHolidays(year: 2027, page: 1, pageSize: 20, search: null);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        var paged = Assert.IsType<PagedResponse<LeaveHolidayResponse>>(response.Data);
        var item = Assert.Single(paged.Items);
        Assert.Equal("วันหยุดสิ้นปีทดสอบ", item.Name);
    }

    private static async Task SeedHolidays(AppDbContext db)
    {
        db.LeaveHolidays.AddRange(
            new LeaveHoliday { HolidayDate = new DateOnly(2026, 1, 1), Name = "วันขึ้นปีใหม่", IsActive = true },
            new LeaveHoliday { HolidayDate = new DateOnly(2027, 1, 1), Name = "วันขึ้นปีใหม่", IsActive = true },
            new LeaveHoliday { HolidayDate = new DateOnly(2027, 4, 6), Name = "วันจักรี", IsActive = true },
            new LeaveHoliday { HolidayDate = new DateOnly(2027, 4, 13), Name = "วันสงกรานต์", IsActive = true });
        await db.SaveChangesAsync();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SetUserContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                    "Test"))
            }
        };
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }
}
