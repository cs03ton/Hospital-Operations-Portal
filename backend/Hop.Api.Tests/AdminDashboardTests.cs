using System.Security.Claims;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class AdminDashboardTests
{
    [Fact]
    public async Task Get_AdminRole_ReturnsControlCenterSummary()
    {
        await using var db = CreateDbContext();
        var ids = SeedControlCenterData(db);
        await db.SaveChangesAsync();
        using var storage = new TempDirectory();
        var configuration = CreateConfiguration(storage.Path);
        var controller = CreateController(db, configuration, ids.AdminUserId, "Admin");

        var result = await controller.Get(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<AdminDashboardResponse>>(result.Value);
        var data = Assert.IsType<AdminDashboardResponse>(response.Data);
        Assert.Equal(5, data.Users.Total);
        Assert.Equal(4, data.Users.Active);
        Assert.Equal(1, data.Users.Inactive);
        Assert.Equal(1, data.Users.MissingLineBinding);
        Assert.Equal(1, data.Users.MissingEmploymentType);
        Assert.Equal(1, data.Users.MissingApprovalRule);
        Assert.Equal(2, data.Departments.Total);
        Assert.Equal(1, data.Departments.WithoutHead);
        Assert.Equal(1, data.Departments.WithoutUsers);
        Assert.Equal(4, data.Roles.Total);
        Assert.Equal(1, data.Roles.UnusedRoles);
        Assert.Equal(0, data.Roles.ImportantPermissionsUnassigned);
        Assert.True(data.Line.Enabled);
        Assert.Equal(3, data.Line.BoundUsers);
        Assert.Equal(1, data.Line.UnboundUsers);
        Assert.NotNull(data.Line.LastFailedDeliveryAt);
        Assert.Equal(1, data.Leave.PendingApprovals);
        Assert.Equal(1, data.Leave.TodayRequests);
        Assert.Equal(1, data.Leave.MissingBalances);
        Assert.Equal(1, data.Leave.MissingApprovalRules);
        Assert.Equal("Healthy", data.Health.Api.Status);
        Assert.Equal(1, data.Audit.RecentFailedLogins);
        Assert.Equal(1, data.Audit.RecentPermissionDenied);
        Assert.NotEmpty(data.Audit.RecentAdminActions);
    }

    [Fact]
    public async Task Get_StaffWithoutPermission_ReturnsForbid()
    {
        await using var db = CreateDbContext();
        var staffId = Guid.NewGuid();
        db.Users.Add(new User { Id = staffId, Username = "staff", FullName = "Staff", PasswordHash = "hash", IsActive = true });
        await db.SaveChangesAsync();
        using var storage = new TempDirectory();
        var controller = CreateController(db, CreateConfiguration(storage.Path), staffId, "Staff");

        var result = await controller.Get(CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Get_ResponseDoesNotContainLineSecrets()
    {
        await using var db = CreateDbContext();
        var ids = SeedControlCenterData(db);
        await db.SaveChangesAsync();
        using var storage = new TempDirectory();
        var accessToken = $"line-token-{Guid.NewGuid():N}";
        var secret = $"line-secret-{Guid.NewGuid():N}";
        var configuration = CreateConfiguration(storage.Path, accessToken, secret);
        var controller = CreateController(db, configuration, ids.AdminUserId, "Admin");

        var result = await controller.Get(CancellationToken.None);

        var response = Assert.IsType<ApiResponse<AdminDashboardResponse>>(result.Value);
        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain(accessToken, json);
        Assert.DoesNotContain(secret, json);
    }

    private static SeedIds SeedControlCenterData(AppDbContext db)
    {
        var adminUserId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var emptyDepartmentId = Guid.NewGuid();
        var headId = Guid.NewGuid();
        var staffMissingId = Guid.NewGuid();
        var staffReadyId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var approvalRuleId = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();
        var leaveRequestId = Guid.NewGuid();

        db.Departments.AddRange(
            new Department { Id = departmentId, Name = "IT", IsActive = true },
            new Department { Id = emptyDepartmentId, Name = "Empty Department", IsActive = true });
        db.ApprovalChains.Add(new ApprovalChain { Id = approvalRuleId, Name = "IT Rule", IsActive = true });
        db.LeaveTypes.Add(new LeaveType { Id = leaveTypeId, Code = "VACATION_LEAVE", Name = "ลาพักผ่อน", IsActive = true });

        db.Users.AddRange(
            new User { Id = adminUserId, Username = "admin", FullName = "Admin", PasswordHash = "hash", IsActive = true, DepartmentId = departmentId, EmploymentType = "CIVIL_SERVANT", LeaveApprovalRuleId = approvalRuleId, LineUserId = "Uadmin" },
            new User { Id = headId, Username = "head", FullName = "Head", PasswordHash = "hash", IsActive = true, DepartmentId = departmentId, EmploymentType = "CIVIL_SERVANT", LeaveApprovalRuleId = approvalRuleId, LineUserId = "Uhead" },
            new User { Id = staffMissingId, Username = "staff1", FullName = "Staff Missing", PasswordHash = "hash", IsActive = true, DepartmentId = departmentId },
            new User { Id = staffReadyId, Username = "staff2", FullName = "Staff Ready", PasswordHash = "hash", IsActive = true, DepartmentId = departmentId, EmploymentType = "CIVIL_SERVANT", LeaveApprovalRuleId = approvalRuleId, LineUserId = "Ustaff2" },
            new User { Id = inactiveId, Username = "inactive", FullName = "Inactive", PasswordHash = "hash", IsActive = false, DepartmentId = departmentId });

        var adminRole = AddRole(db, "Admin");
        var headRole = AddRole(db, "DepartmentHead");
        var staffRole = AddRole(db, "Staff");
        AddRole(db, "UnusedRole");
        AddUserRole(db, adminUserId, adminRole);
        AddUserRole(db, headId, headRole);
        AddUserRole(db, staffMissingId, staffRole);
        AddUserRole(db, staffReadyId, staffRole);

        foreach (var code in new[]
        {
            "AdminDashboard.View",
            "System.Health.View",
            "System.Line.TestSend",
            "UserManagement.View",
            "DepartmentManagement.View",
            "RoleManagement.View",
            "LeaveAdmin.ManageBalances",
            "LeaveAdmin.ManageApprovalChains",
            "LeaveApproval.ApproveCurrentStep"
        })
        {
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = code,
                Group = code.Split('.')[0],
                Action = code.Split('.')[1],
                IsActive = true
            };
            db.Permissions.Add(permission);
            db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = permission.Id, Role = adminRole, Permission = permission });
        }

        db.LeaveBalances.AddRange(
            new LeaveBalance { Id = Guid.NewGuid(), UserId = headId, LeaveTypeId = leaveTypeId, Year = DateTime.UtcNow.Year, EntitledDays = 10 },
            new LeaveBalance { Id = Guid.NewGuid(), UserId = staffReadyId, LeaveTypeId = leaveTypeId, Year = DateTime.UtcNow.Year, EntitledDays = 10 });
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = leaveRequestId,
            UserId = staffReadyId,
            LeaveTypeId = leaveTypeId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalDays = 1,
            Reason = "test",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        db.LeaveApprovals.Add(new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequestId,
            ApproverId = headId,
            StepOrder = 1,
            Status = "Pending"
        });
        db.LineDeliveryLogs.Add(new LineDeliveryLog
        {
            Id = Guid.NewGuid(),
            EventName = "Line.TestMessageFailed",
            Status = "Failed",
            Payload = "{}",
            ResponseDetail = "HTTP 400",
            UpdatedAt = DateTime.UtcNow
        });
        db.AuditLogs.AddRange(
            new AuditLog { Id = Guid.NewGuid(), UserId = adminUserId, Action = "Auth.LoginFailed", EntityName = "Auth", Result = "Failed", CreatedAt = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), UserId = staffMissingId, Action = "PermissionDenied", EntityName = "LeaveRequest", Result = "Denied", CreatedAt = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), UserId = adminUserId, Action = "User.Created", EntityName = "User", Result = "Success", CreatedAt = DateTime.UtcNow });

        return new SeedIds(adminUserId);
    }

    private static Role AddRole(AppDbContext db, string name)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = name, IsActive = true };
        db.Roles.Add(role);
        return role;
    }

    private static void AddUserRole(AppDbContext db, Guid userId, Role role)
    {
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id, Role = role });
    }

    private static AdminDashboardController CreateController(AppDbContext db, IConfiguration configuration, Guid userId, string role)
    {
        var controller = new AdminDashboardController(
            db,
            new HealthCenterService(
                db,
                configuration,
                new TestWebHostEnvironment("Production"),
                new LineConfigurationResolver(Options.Create(new LineOptions()), configuration)),
            new LineConfigurationResolver(Options.Create(new LineOptions()), configuration));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role)
                    ],
                    "Test"))
            }
        };
        return controller;
    }

    private static IConfiguration CreateConfiguration(string storagePath, string accessToken = "test-access-token", string channelSecret = "test-channel-secret")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = storagePath,
                ["Line:Enabled"] = "true",
                ["Line:AccessToken"] = accessToken,
                ["Line:ChannelSecret"] = channelSecret
            })
            .Build();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed record SeedIds(Guid AdminUserId);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"hop-admin-dashboard-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class TestWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Hop.Api.Tests";
        public string WebRootPath { get; set; } = System.IO.Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = System.IO.Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
