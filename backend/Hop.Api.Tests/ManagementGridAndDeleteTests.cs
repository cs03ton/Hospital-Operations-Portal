using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment(), CreateConfiguration(), new NoopLeaveEntitlementService());

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
        user.EmployeeCode = "DEL001";
        user.Position = "เจ้าหน้าที่";
        user.Email = "staff.delete@example.local";
        user.PhoneNumber = "0812345678";
        user.LineUserId = "Udeletedtest";
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Staff", IsActive = true };
        db.Roles.Add(staffRole);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = staffRole.Id, User = user, Role = staffRole });
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        db.LineUserBindings.Add(new LineUserBinding
        {
            Id = Guid.NewGuid(),
            LineUserId = user.LineUserId,
            UserId = user.Id,
            Status = "Bound",
            BoundAt = DateTime.UtcNow
        });
        db.LineConnectTokens.Add(new LineConnectToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "connect-token",
            ShortCode = "HOP-123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
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

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment(), CreateConfiguration(), new NoopLeaveEntitlementService());
        SetUserContext(controller, adminId);

        var result = await controller.DeleteUser(user.Id);

        var response = Assert.IsType<ApiResponse<DeleteResultResponse>>(result.Value);
        Assert.Equal("SoftDeleted", response.Data?.Action);
        var savedUser = await db.Users.SingleAsync(item => item.Id == user.Id);
        Assert.False(savedUser.IsActive);
        Assert.StartsWith("deleted-", savedUser.Username);
        Assert.Equal("ผู้ใช้ที่ถูกลบ", savedUser.FullName);
        Assert.Null(savedUser.EmployeeCode);
        Assert.Null(savedUser.Email);
        Assert.Null(savedUser.PhoneNumber);
        Assert.Null(savedUser.LineUserId);
        Assert.Null(savedUser.DepartmentId);
        Assert.Empty(await db.UserRoles.Where(item => item.UserId == user.Id).ToListAsync());
        Assert.NotNull((await db.RefreshTokens.SingleAsync(item => item.UserId == user.Id)).RevokedAt);
        Assert.False(await db.LineConnectTokens.AnyAsync(item => item.UserId == user.Id));
        var binding = await db.LineUserBindings.SingleAsync(item => item.LineUserId == "Udeletedtest");
        Assert.Null(binding.UserId);
        Assert.Equal("Unbound", binding.Status);
    }

    [Fact]
    public async Task DeleteUser_WithOnlyAuditAndAccessData_HardDeletesAndCleansReferences()
    {
        await using var db = CreateDbContext();
        var adminId = Guid.NewGuid();
        var user = AddUser(db, "uat.delete", "UAT Delete", null);
        var role = new Role { Id = Guid.NewGuid(), Name = "Staff", IsActive = true };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "uat-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EntityName = "User",
            EntityId = user.Id.ToString(),
            Action = "User.Create"
        });
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "test",
            Message = "test"
        });
        db.LineUserBindings.Add(new LineUserBinding
        {
            Id = Guid.NewGuid(),
            LineUserId = "Uuatdelete",
            UserId = user.Id,
            Status = "Bound"
        });
        await db.SaveChangesAsync();

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment(), CreateConfiguration(), new NoopLeaveEntitlementService());
        SetUserContext(controller, adminId);

        var result = await controller.DeleteUser(user.Id);

        var response = Assert.IsType<ApiResponse<DeleteResultResponse>>(result.Value);
        Assert.Equal("Deleted", response.Data?.Action);
        Assert.False(await db.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await db.AuditLogs.AnyAsync(item => item.UserId == user.Id || item.EntityId == user.Id.ToString()));
        Assert.False(await db.RefreshTokens.AnyAsync(item => item.UserId == user.Id));
        Assert.False(await db.UserRoles.AnyAsync(item => item.UserId == user.Id));
        Assert.False(await db.Notifications.AnyAsync(item => item.UserId == user.Id));
        var binding = await db.LineUserBindings.SingleAsync(item => item.LineUserId == "Uuatdelete");
        Assert.Null(binding.UserId);
        Assert.Equal("Unbound", binding.Status);
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

        var controller = new UsersController(db, new NoopAuditLogService(), new FakeHostEnvironment(), CreateConfiguration(), new NoopLeaveEntitlementService());
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

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = Path.Combine(Path.GetTempPath(), "hop-tests-storage")
            })
            .Build();
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

    private sealed class NoopLeaveEntitlementService : ILeaveEntitlementService
    {
        public Task<LeaveEntitlementInitializationResult> InitializeAsync(Guid userId, int fiscalYear, DateOnly effectiveDate, Guid? initiatedByUserId, string reason, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LeaveEntitlementInitializationResult(userId, fiscalYear, 0, 0, 0, [], []));
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
