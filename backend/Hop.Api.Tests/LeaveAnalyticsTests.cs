using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveAnalyticsTests
{
    [Fact]
    public async Task GetAnalytics_DefaultsToApprovedCoreLeaveTypesAndCountsHalfDay()
    {
        await using var db = CreateDbContext();
        var department = AddDepartment(db, "OPD");
        var admin = AddUser(db, "admin", department.Id);
        var staff1 = AddUser(db, "staff01", department.Id);
        var staff2 = AddUser(db, "staff02", department.Id);
        GrantRole(db, admin.Id, "Admin");
        var sick = AddLeaveType(db, "SICK_LEAVE", "ลาป่วย");
        var vacation = AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน");
        var personal = AddLeaveType(db, "PERSONAL_LEAVE", "ลากิจส่วนตัว");
        var maternity = AddLeaveType(db, "MATERNITY_LEAVE", "ลาคลอดบุตร");

        AddLeaveRequest(db, staff1.Id, sick.Id, new DateOnly(2026, 7, 1), 0.5m, "Approved");
        AddLeaveRequest(db, staff2.Id, vacation.Id, new DateOnly(2026, 7, 2), 1m, "Approved");
        AddLeaveRequest(db, staff2.Id, personal.Id, new DateOnly(2026, 7, 3), 1m, "Rejected");
        AddLeaveRequest(db, staff2.Id, personal.Id, new DateOnly(2026, 7, 4), 1m, "Cancelled");
        AddLeaveRequest(db, staff1.Id, maternity.Id, new DateOnly(2026, 7, 5), 90m, "Approved");
        await db.SaveChangesAsync();

        var controller = CreateController(db, admin.Id);
        var result = await controller.GetAnalytics(fiscalYear: 2027, year: 2026, month: 7, departmentId: null, leaveTypeId: null, status: null, coreOnly: true);

        var response = Assert.IsType<ApiResponse<LeaveAnalyticsResponse>>(result.Value);
        Assert.Equal(2, response.Data!.Summary.TotalRequests);
        Assert.Equal(2, response.Data.Summary.UniqueUsers);
        Assert.Equal(1.5m, response.Data.Summary.TotalDays);
        Assert.Equal(0.5m, response.Data.Summary.SickDays);
        Assert.Equal(1m, response.Data.Summary.VacationDays);
        Assert.DoesNotContain(response.Data.Items, item => item.LeaveTypeCode == "MATERNITY_LEAVE");
        Assert.DoesNotContain(response.Data.Items, item => item.Status is "Rejected" or "Cancelled");
    }

    [Fact]
    public async Task GetAnalytics_FiscalYearUsesOctoberToSeptemberAndDepartmentStack()
    {
        await using var db = CreateDbContext();
        var opd = AddDepartment(db, "OPD");
        var er = AddDepartment(db, "ER");
        var admin = AddUser(db, "admin", opd.Id);
        var staff1 = AddUser(db, "staff01", opd.Id);
        var staff2 = AddUser(db, "staff02", er.Id);
        GrantRole(db, admin.Id, "Admin");
        var personal = AddLeaveType(db, "PERSONAL_LEAVE", "ลากิจส่วนตัว");

        AddLeaveRequest(db, staff1.Id, personal.Id, new DateOnly(2025, 9, 30), 1m, "Approved");
        AddLeaveRequest(db, staff1.Id, personal.Id, new DateOnly(2025, 10, 1), 2m, "Approved");
        AddLeaveRequest(db, staff2.Id, personal.Id, new DateOnly(2026, 9, 30), 3m, "Approved");
        AddLeaveRequest(db, staff2.Id, personal.Id, new DateOnly(2026, 10, 1), 4m, "Approved");
        await db.SaveChangesAsync();

        var controller = CreateController(db, admin.Id);
        var result = await controller.GetAnalytics(fiscalYear: 2026, year: null, month: null, departmentId: null, leaveTypeId: null, status: null, coreOnly: true);

        var response = Assert.IsType<ApiResponse<LeaveAnalyticsResponse>>(result.Value);
        Assert.Equal(new DateOnly(2025, 10, 1), response.Data!.Filters.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 30), response.Data.Filters.EndDate);
        Assert.Equal(2, response.Data.Summary.TotalRequests);
        Assert.Equal(5m, response.Data.Summary.TotalDays);
        Assert.Equal(12, response.Data.MonthlyTrend.Count);
        Assert.Contains(response.Data.DepartmentStacked, item => item.DepartmentName == "OPD" && item.PersonalDays == 2m);
        Assert.Contains(response.Data.DepartmentStacked, item => item.DepartmentName == "ER" && item.PersonalDays == 3m);
    }

    [Fact]
    public async Task GetAnalytics_StatusFilterCanIncludeRejectedAndStaffIsDenied()
    {
        await using var db = CreateDbContext();
        var department = AddDepartment(db, "OPD");
        var admin = AddUser(db, "admin", department.Id);
        var staff = AddUser(db, "staff01", department.Id);
        GrantRole(db, admin.Id, "Admin");
        GrantRole(db, staff.Id, "Staff");
        var sick = AddLeaveType(db, "SICK_LEAVE", "ลาป่วย");

        AddLeaveRequest(db, staff.Id, sick.Id, new DateOnly(2026, 7, 1), 1m, "Rejected");
        await db.SaveChangesAsync();

        var adminController = CreateController(db, admin.Id);
        var result = await adminController.GetAnalytics(fiscalYear: 2027, year: 2026, month: 7, departmentId: null, leaveTypeId: null, status: "Rejected", coreOnly: true);
        var response = Assert.IsType<ApiResponse<LeaveAnalyticsResponse>>(result.Value);
        Assert.Equal(1, response.Data!.Summary.TotalRequests);
        Assert.Equal("Rejected", response.Data.Filters.Status);

        var staffController = CreateController(db, staff.Id);
        var denied = await staffController.GetAnalytics(fiscalYear: 2027, year: 2026, month: 7, departmentId: null, leaveTypeId: null, status: null, coreOnly: true);
        Assert.IsType<ForbidResult>(denied.Result);
    }

    [Fact]
    public async Task GetOptions_UsesAnalyticsAccessInsteadOfDepartmentManagementPermission()
    {
        await using var db = CreateDbContext();
        var department = AddDepartment(db, "OPD");
        var director = AddUser(db, "director01", department.Id);
        GrantRole(db, director.Id, "Director");
        AddLeaveType(db, "SICK_LEAVE", "ลาป่วย");
        await db.SaveChangesAsync();

        var controller = CreateController(db, director.Id);
        var result = await controller.GetOptions(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<LeaveAnalyticsOptionsResponse>>(result.Value);
        Assert.Contains(response.Data!.Departments, item => item.Name == "OPD");
        Assert.Contains(response.Data.LeaveTypes, item => item.Code == "SICK_LEAVE");
    }

    private static Department AddDepartment(AppDbContext db, string name)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = name, IsActive = true };
        db.Departments.Add(department);
        return department;
    }

    private static User AddUser(AppDbContext db, string username, Guid departmentId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            FullName = username,
            PasswordHash = "hash",
            DepartmentId = departmentId,
            IsActive = true
        };
        db.Users.Add(user);
        return user;
    }

    private static LeaveType AddLeaveType(AppDbContext db, string code, string name)
    {
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = code, Name = name, IsActive = true };
        db.LeaveTypes.Add(leaveType);
        return leaveType;
    }

    private static void GrantRole(AppDbContext db, Guid userId, string roleName)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = roleName, IsActive = true };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id, Role = role });
    }

    private static void AddLeaveRequest(AppDbContext db, Guid userId, Guid leaveTypeId, DateOnly date, decimal totalDays, string status)
    {
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            StartDate = date,
            EndDate = date,
            DurationType = totalDays == 0.5m ? "HALF_DAY_AM" : "FULL_DAY",
            TotalDays = totalDays,
            Reason = "test",
            Status = status,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static LeaveAnalyticsController CreateController(AppDbContext db, Guid userId)
    {
        var controller = new LeaveAnalyticsController(db, new NoopAuditLogService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "Test"))
            }
        };
        return controller;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }
}
