using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hop.Api.Tests;

public class ManagementGridAndDeleteTests
{
    [Fact]
    public async Task UsersGrid_ReturnsPagedSearchResult()
    {
        await using var db = CreateDbContext();
        var department = new Department { Id = Guid.NewGuid(), Name = "Information Technology" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Staff", IsActive = true };
        db.Departments.Add(department);
        db.Roles.Add(role);
        var user = AddUser(db, "staff01", "เจ้าหน้าที่ 01", department.Id);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
        AddUser(db, "head01", "หัวหน้าหน่วยงาน", department.Id);
        await db.SaveChangesAsync();

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment());

        var result = await controller.GetUsers(page: 1, pageSize: 10, search: "staff01");

        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        var paged = Assert.IsType<PagedResponse<UserResponse>>(response.Data);
        var item = Assert.Single(paged.Items);
        Assert.Equal("staff01", item.Username);
        Assert.Equal(1, paged.TotalItems);
    }

    [Fact]
    public async Task DeleteUser_WithLeaveRequest_SoftDeletesInsteadOfHardDelete()
    {
        await using var db = CreateDbContext();
        var adminId = Guid.NewGuid();
        var user = AddUser(db, "staff.delete", "เจ้าหน้าที่ลบ", null);
        db.LeaveTypes.Add(new LeaveType { Id = Guid.NewGuid(), Code = "VACATION_LEAVE", Name = "ลาพักผ่อน", IsActive = true });
        var leaveTypeId = db.LeaveTypes.Local.Single().Id;
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            LeaveTypeId = leaveTypeId,
            StartDate = new DateOnly(2026, 7, 20),
            EndDate = new DateOnly(2026, 7, 20),
            TotalDays = 1,
            Reason = "test",
            Status = "Approved"
        });
        await db.SaveChangesAsync();

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment());
        SetUserContext(controller, adminId);

        var result = await controller.DeleteUser(user.Id);

        var response = Assert.IsType<ApiResponse<DeleteResultResponse>>(result.Value);
        Assert.Equal("SoftDeleted", response.Data?.Action);
        var savedUser = await db.Users.SingleAsync(item => item.Id == user.Id);
        Assert.False(savedUser.IsActive);
    }

    [Fact]
    public async Task DeleteUser_BlocksLastSuperAdmin()
    {
        await using var db = CreateDbContext();
        var admin = AddUser(db, "superadmin", "SuperAdmin", null);
        var role = new Role { Id = Guid.NewGuid(), Name = "SuperAdmin", IsSystemRole = true, IsActive = true };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = role.Id, User = admin, Role = role });
        await db.SaveChangesAsync();

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment());
        SetUserContext(controller, Guid.NewGuid());

        var result = await controller.DeleteUser(admin.Id);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.True(await db.Users.AnyAsync(item => item.Id == admin.Id && item.IsActive));
    }

    [Fact]
    public async Task DeleteDepartment_WithUsers_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var department = new Department { Id = Guid.NewGuid(), Name = "IT" };
        db.Departments.Add(department);
        AddUser(db, "staff.it", "Staff IT", department.Id);
        await db.SaveChangesAsync();

        var controller = new DepartmentsController(db, new NoopAuditLogService());

        var result = await controller.DeleteDepartment(department.Id);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DeleteResultResponse>>(conflict.Value);
        Assert.Equal("Blocked", response.Data?.Action);
    }

    [Fact]
    public async Task DeleteRole_WithUsers_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "CustomRole", IsActive = true };
        var user = AddUser(db, "custom.user", "Custom User", null);
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user });
        await db.SaveChangesAsync();

        var controller = new RolesController(db, new NoopAuditLogService());

        var result = await controller.DeleteRole(role.Id);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DeleteResultResponse>>(conflict.Value);
        Assert.Equal("Blocked", response.Data?.Action);
    }

    [Fact]
    public async Task DeletePermission_WithRoleAssignment_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "CustomRole", IsActive = true };
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "Custom.View",
            Name = "Custom View",
            Group = "Custom",
            Action = "View",
            IsActive = true
        };
        db.Roles.Add(role);
        db.Permissions.Add(permission);
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission });
        await db.SaveChangesAsync();

        var controller = new PermissionsController(db, new NoopAuditLogService());

        var result = await controller.DeletePermission(permission.Id);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DeleteResultResponse>>(conflict.Value);
        Assert.Equal("Blocked", response.Data?.Action);
    }

    private static User AddUser(AppDbContext db, string username, string fullname, Guid? departmentId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            FullName = fullname,
            PasswordHash = "hash",
            DepartmentId = departmentId,
            IsActive = true
        };
        db.Users.Add(user);
        return user;
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

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Hop.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
