using System.Security.Claims;
using Hop.Api.Authorization;
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

    [Fact]
    public async Task GetSummary_CountsReturnedForRevisionAsMyRequestButNotPending()
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
        var leaveType = AddLeaveType(db, "PERSONAL_LEAVE", "ลากิจส่วนตัว");
        db.Users.Add(user);
        AddLeaveRequest(db, user.Id, leaveType.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "Draft");
        AddLeaveRequest(db, user.Id, leaveType.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "Pending");
        AddLeaveRequest(db, user.Id, leaveType.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "ReturnedForRevision");
        AddLeaveRequest(db, user.Id, leaveType.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "Approved");
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, user.Id);

        var result = await controller.GetSummary();

        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(result.Value);
        var summary = response.Data!;
        Assert.Equal(4, summary.MyLeaveRequestsTotal);
        Assert.Equal(1, summary.MyLeaveRequestsDraft);
        Assert.Equal(1, summary.MyLeaveRequestsPending);
        Assert.Equal(1, summary.MyLeaveRequestsReturnedForRevision);
        Assert.Equal(1, summary.MyLeaveRequestsApproved);
    }

    [Fact]
    public async Task GetSummary_DepartmentHeadGetsOwnPendingAndDepartmentRequestsSeparately()
    {
        await using var db = CreateDbContext();
        var department = new Department { Id = Guid.NewGuid(), Name = "Information Technology", IsActive = true };
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Finance", IsActive = true };
        var head = new User { Id = Guid.NewGuid(), FullName = "หัวหน้าหน่วยงาน", Username = "head01", PasswordHash = "hash", IsActive = true, DepartmentId = department.Id };
        var staff = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 01", Username = "staff01", PasswordHash = "hash", IsActive = true, DepartmentId = department.Id };
        var otherStaff = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ต่างหน่วยงาน", Username = "staff.other", PasswordHash = "hash", IsActive = true, DepartmentId = otherDepartment.Id };
        var leaveType = AddLeaveType(db, "PERSONAL_LEAVE", "ลากิจส่วนตัว");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Departments.AddRange(department, otherDepartment);
        db.Users.AddRange(head, staff, otherStaff);
        GrantRoleWithPermissions(db, head.Id, "DepartmentHead", LeavePermissions.ViewOwn, LeavePermissions.ViewDepartment);
        AddLeaveRequest(db, head.Id, leaveType.Id, today, 1m, "Pending");
        AddLeaveRequest(db, head.Id, leaveType.Id, today, 1m, "ReturnedForRevision");
        AddLeaveRequest(db, staff.Id, leaveType.Id, today, 1m, "Pending");
        AddLeaveRequest(db, staff.Id, leaveType.Id, today, 1m, "Approved");
        AddLeaveRequest(db, staff.Id, leaveType.Id, today, 1m, "ReturnedForRevision");
        AddLeaveRequest(db, otherStaff.Id, leaveType.Id, today, 1m, "Pending");
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, head.Id);

        var result = await controller.GetSummary();

        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(result.Value);
        var summary = response.Data!;
        Assert.Equal(1, summary.MyPendingRequests?.Count);
        Assert.Single(summary.MyPendingRequests!.Items);
        Assert.Equal("หัวหน้าหน่วยงาน", summary.MyPendingRequests.Items[0].RequesterName);
        Assert.Equal(3, summary.DepartmentRequests?.Count);
        Assert.All(summary.DepartmentRequests!.Items, item => Assert.Equal("เจ้าหน้าที่ 01", item.RequesterName));
        Assert.DoesNotContain(summary.DepartmentRequests.Items, item => item.RequesterName == "หัวหน้าหน่วยงาน");
        Assert.DoesNotContain(summary.DepartmentRequests.Items, item => item.RequesterName == "เจ้าหน้าที่ต่างหน่วยงาน");
    }

    [Fact]
    public async Task GetSummary_StaffDoesNotReceiveDepartmentRequestGroup()
    {
        await using var db = CreateDbContext();
        var department = new Department { Id = Guid.NewGuid(), Name = "Information Technology", IsActive = true };
        var staff = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 01", Username = "staff01", PasswordHash = "hash", IsActive = true, DepartmentId = department.Id };
        var teammate = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 02", Username = "staff02", PasswordHash = "hash", IsActive = true, DepartmentId = department.Id };
        var leaveType = AddLeaveType(db, "PERSONAL_LEAVE", "ลากิจส่วนตัว");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Departments.Add(department);
        db.Users.AddRange(staff, teammate);
        GrantRoleWithPermissions(db, staff.Id, "Staff", LeavePermissions.ViewOwn);
        AddLeaveRequest(db, staff.Id, leaveType.Id, today, 1m, "Pending");
        AddLeaveRequest(db, teammate.Id, leaveType.Id, today, 1m, "Pending");
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, staff.Id);

        var result = await controller.GetSummary();

        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(result.Value);
        var summary = response.Data!;
        Assert.Equal(1, summary.MyPendingRequests?.Count);
        Assert.Equal(0, summary.DepartmentRequests?.Count);
        Assert.Empty(summary.DepartmentRequests!.Items);
    }

    [Fact]
    public async Task GetSummary_ReturnsLeaveCancellationSummaryForCurrentUser()
    {
        await using var db = CreateDbContext();
        var staff = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 01", Username = "staff01", PasswordHash = "hash", IsActive = true };
        var otherStaff = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 02", Username = "staff02", PasswordHash = "hash", IsActive = true };
        var leaveType = AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Users.AddRange(staff, otherStaff);
        GrantRoleWithPermissions(db, staff.Id, "Staff", LeavePermissions.CancellationViewOwn);
        var approvedLeave = AddLeaveRequest(db, staff.Id, leaveType.Id, today, 1m, "CancelledAfterApproval");
        var pendingLeave = AddLeaveRequest(db, staff.Id, leaveType.Id, today, 0.5m, "Approved");
        var otherLeave = AddLeaveRequest(db, otherStaff.Id, leaveType.Id, today, 1m, "Approved");
        AddCancellation(db, approvedLeave, staff.Id, leaveType.Id, LeaveCancellationStatuses.Approved, 1m);
        AddCancellation(db, pendingLeave, staff.Id, leaveType.Id, LeaveCancellationStatuses.Pending, 0.5m);
        AddCancellation(db, otherLeave, otherStaff.Id, leaveType.Id, LeaveCancellationStatuses.Pending, 1m);
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, staff.Id);

        var result = await controller.GetSummary();

        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(result.Value);
        var summary = response.Data!.LeaveCancellationSummary!;
        Assert.Equal(2, summary.Total);
        Assert.Equal(1, summary.Pending);
        Assert.Equal(1, summary.Approved);
        Assert.Equal(1m, summary.RestoredDaysThisYear);
        Assert.Equal(1m, summary.RestoredDaysTotal);
        Assert.Equal(100m, summary.ApprovalRate);
        Assert.Equal(2, summary.RecentRequests.Count);
        Assert.All(summary.RecentRequests.Items, item => Assert.Equal("LeaveCancellationRequest", item.SourceType));
    }

    [Fact]
    public async Task GetExecutiveDashboard_CountsUniqueApprovedLeaveUsersToday()
    {
        await using var db = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var department = new Department { Id = Guid.NewGuid(), Name = "OPD", IsActive = true };
        var user = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 01", Username = "staff01", PasswordHash = "hash", IsActive = true, DepartmentId = department.Id };
        var otherUser = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 02", Username = "staff02", PasswordHash = "hash", IsActive = true, DepartmentId = department.Id };
        var leaveType = AddLeaveType(db, "SICK_LEAVE", "ลาป่วย");
        db.Departments.Add(department);
        db.Users.AddRange(user, otherUser);
        GrantRole(db, user.Id, "SuperAdmin");
        AddLeaveRequest(db, user.Id, leaveType.Id, today, 0.5m, "Approved");
        AddLeaveRequest(db, user.Id, leaveType.Id, today, 0.5m, "Approved");
        AddLeaveRequest(db, otherUser.Id, leaveType.Id, today, 1m, "Rejected");
        AddLeaveRequest(db, otherUser.Id, leaveType.Id, today, 1m, "Cancelled");
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, user.Id);

        var result = await controller.GetExecutiveDashboard(null, null, null, CancellationToken.None);

        var response = Assert.IsType<ApiResponse<ExecutiveDashboardResponse>>(result.Value);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data!.Kpis.TotalActiveUsers);
        Assert.Equal(1, response.Data.Kpis.OnLeaveToday);
        Assert.Equal(1, response.Data.Kpis.PresentToday);
        Assert.Equal(50m, response.Data.Kpis.LeaveRate);
        Assert.Equal(1, response.Data.TodaySummary.SickLeaveToday);
        Assert.Equal("OPD", response.Data.TodaySummary.TopDepartmentToday);
        Assert.Equal(12, response.Data.MonthlyTrend.Count);
    }

    [Fact]
    public async Task GetExecutiveDashboard_YearlySummaryUsesFiscalYearAndHalfDayTotals()
    {
        await using var db = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalYear = FiscalYearHelper.GetFiscalYear(today);
        var user = new User { Id = Guid.NewGuid(), FullName = "เจ้าหน้าที่ 01", Username = "staff01", PasswordHash = "hash", IsActive = true };
        var vacation = AddLeaveType(db, "VACATION_LEAVE", "ลาพักผ่อน");
        db.Users.Add(user);
        GrantRole(db, user.Id, "SuperAdmin");
        AddLeaveRequest(db, user.Id, vacation.Id, today, 0.5m, "Approved");
        AddLeaveRequest(db, user.Id, vacation.Id, today, 1m, "Approved");
        AddLeaveRequest(db, user.Id, vacation.Id, today, 1m, "Rejected");
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        SetUserContext(controller, user.Id);

        var result = await controller.GetExecutiveDashboard(today.Month, today.Year, fiscalYear, CancellationToken.None);

        var response = Assert.IsType<ApiResponse<ExecutiveDashboardResponse>>(result.Value);
        var vacationSummary = Assert.Single(response.Data!.YearlySummary, item => item.LeaveTypeCode == "VACATION_LEAVE");
        Assert.Equal(fiscalYear, vacationSummary.FiscalYear);
        Assert.Equal(1.5m, vacationSummary.UsedDays);
        Assert.Single(response.Data.MonthlyTrend);

        var previousFiscalYearResult = await controller.GetExecutiveDashboard(today.Month, today.Year, fiscalYear - 1, CancellationToken.None);
        var previousFiscalYearResponse = Assert.IsType<ApiResponse<ExecutiveDashboardResponse>>(previousFiscalYearResult.Value);
        Assert.DoesNotContain(previousFiscalYearResponse.Data!.YearlySummary, item => item.LeaveTypeCode == "VACATION_LEAVE");
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

    private static void GrantRole(AppDbContext db, Guid userId, string roleName)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            IsActive = true
        };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            Role = role
        });
    }

    private static void GrantRoleWithPermissions(AppDbContext db, Guid userId, string roleName, params string[] permissions)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            IsActive = true
        };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            Role = role
        });

        foreach (var code in permissions)
        {
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = code,
                Group = "LeaveRequest",
                Action = code
            };
            db.Permissions.Add(permission);
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Role = role,
                Permission = permission
            });
        }
    }

    private static LeaveRequest AddLeaveRequest(AppDbContext db, Guid userId, Guid leaveTypeId, DateOnly date, decimal totalDays, string status)
    {
        var leaveRequest = new LeaveRequest
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
            SubmittedAt = DateTime.UtcNow.AddHours(-8),
            UpdatedAt = status is "Approved" or "Rejected" ? DateTime.UtcNow : null
        };
        db.LeaveRequests.Add(leaveRequest);
        return leaveRequest;
    }

    private static LeaveCancellationRequest AddCancellation(AppDbContext db, LeaveRequest originalLeave, Guid userId, Guid leaveTypeId, string status, decimal days)
    {
        var now = DateTime.UtcNow;
        var cancellation = new LeaveCancellationRequest
        {
            Id = Guid.NewGuid(),
            CancellationRequestNumber = $"LVC-{now:yyyyMM}-{db.LeaveCancellationRequests.Local.Count + 1:000}",
            OriginalLeaveRequestId = originalLeave.Id,
            OriginalLeaveRequest = originalLeave,
            RequesterUserId = userId,
            LeaveTypeId = leaveTypeId,
            OriginalLeaveDays = days,
            Reason = "test cancellation",
            Status = status,
            CurrentApproverId = status == LeaveCancellationStatuses.Pending ? Guid.NewGuid() : null,
            CreatedAt = now.AddDays(-1),
            SubmittedAt = now.AddHours(-6),
            UpdatedAt = status is LeaveCancellationStatuses.Approved or LeaveCancellationStatuses.Rejected ? now : null,
            ApprovedAt = status == LeaveCancellationStatuses.Approved ? now : null,
            RejectedAt = status == LeaveCancellationStatuses.Rejected ? now : null,
            BalanceRestoredAt = status == LeaveCancellationStatuses.Approved ? now : null
        };
        db.LeaveCancellationRequests.Add(cancellation);
        return cancellation;
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
