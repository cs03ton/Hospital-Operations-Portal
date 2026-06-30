using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveSecurityHardeningTests
{
    [Fact]
    public async Task ApprovalPlan_AppliesDirectorFallback_WhenDirectorWouldApproveOwnRequest()
    {
        await using var db = CreateDbContext();
        var auditLog = new CaptureAuditLogService();
        var director = CreateUser("director01");
        var fallback = CreateUser("deputy01");
        var directorRole = CreateRole("Director");
        var approverRole = CreateRole("DepartmentHead");
        var permission = CreatePermission("LeaveApproval.ApproveCurrentStep");
        var chain = new ApprovalChain { Id = Guid.NewGuid(), Name = "Director leave", IsActive = true };
        director.LeaveApprovalRuleId = chain.Id;
        var step = new ApprovalChainStep
        {
            Id = Guid.NewGuid(),
            ApprovalChainId = chain.Id,
            StepOrder = 1,
            Name = "ผู้อำนวยการ",
            ApproverUserId = director.Id,
            RequiredPermissionCode = permission.Code,
            IsActive = true
        };
        chain.Steps.Add(step);
        db.Users.AddRange(director, fallback);
        db.Roles.AddRange(directorRole, approverRole);
        db.Permissions.Add(permission);
        db.UserRoles.AddRange(
            new UserRole { UserId = director.Id, RoleId = directorRole.Id, User = director, Role = directorRole },
            new UserRole { UserId = fallback.Id, RoleId = approverRole.Id, User = fallback, Role = approverRole });
        db.RolePermissions.Add(new RolePermission { RoleId = approverRole.Id, PermissionId = permission.Id, Role = approverRole, Permission = permission });
        db.ApprovalChains.Add(chain);
        await db.SaveChangesAsync();
        var service = new ApprovalChainService(db, CreateConfiguration(("LeaveApproval:DirectorFallbackApproverUsername", fallback.Username)), auditLog);

        var plan = await service.BuildApprovalPlanAsync(CreateLeaveRequest(director.Id));

        var stepPlan = Assert.Single(plan);
        Assert.Equal(fallback.Id, stepPlan.ApproverId);
        Assert.Contains(auditLog.Events, item => item.Action == "DirectorLeaveFallbackApplied" && item.Result == "Success");
    }

    [Fact]
    public async Task ApprovalPlan_BlocksSelfApproval_WhenFallbackIsNotConfigured()
    {
        await using var db = CreateDbContext();
        var auditLog = new CaptureAuditLogService();
        var director = CreateUser("director01");
        var directorRole = CreateRole("Director");
        var permission = CreatePermission("LeaveApproval.ApproveCurrentStep");
        var chain = new ApprovalChain { Id = Guid.NewGuid(), Name = "Director leave", IsActive = true };
        director.LeaveApprovalRuleId = chain.Id;
        chain.Steps.Add(new ApprovalChainStep
        {
            Id = Guid.NewGuid(),
            ApprovalChainId = chain.Id,
            StepOrder = 1,
            Name = "ผู้อำนวยการ",
            ApproverUserId = director.Id,
            RequiredPermissionCode = permission.Code,
            IsActive = true
        });
        db.Users.Add(director);
        db.Roles.Add(directorRole);
        db.Permissions.Add(permission);
        db.UserRoles.Add(new UserRole { UserId = director.Id, RoleId = directorRole.Id, User = director, Role = directorRole });
        db.RolePermissions.Add(new RolePermission { RoleId = directorRole.Id, PermissionId = permission.Id, Role = directorRole, Permission = permission });
        db.ApprovalChains.Add(chain);
        await db.SaveChangesAsync();
        var service = new ApprovalChainService(db, CreateConfiguration(), auditLog);

        var plan = await service.BuildApprovalPlanAsync(CreateLeaveRequest(director.Id));

        Assert.Empty(plan);
        Assert.Contains(auditLog.Events, item => item.Action == "SelfApprovalBlocked" && item.Result == "Denied");
    }

    [Fact]
    public async Task ApprovalPlan_ReturnsEmpty_WhenRequesterHasNoApprovalRule()
    {
        await using var db = CreateDbContext();
        var requester = CreateUser("staff01");
        db.Users.Add(requester);
        await db.SaveChangesAsync();
        var service = new ApprovalChainService(db, CreateConfiguration(), new CaptureAuditLogService());

        var plan = await service.BuildApprovalPlanAsync(CreateLeaveRequest(requester.Id));

        Assert.Empty(plan);
    }

    [Fact]
    public async Task LeaveRequestAccess_UsesGranularVisibilityRules()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var currentApproverId = Guid.NewGuid();
        var previousApproverId = Guid.NewGuid();
        var approveOnlyUserId = Guid.NewGuid();
        var manageUserId = Guid.NewGuid();
        var departmentViewerId = Guid.NewGuid();
        var otherDepartmentStaffId = Guid.NewGuid();
        var directorId = Guid.NewGuid();
        var supportViewerId = Guid.NewGuid();
        var adminRoleOnlyId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var otherDepartmentId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId);
        leaveRequest.CurrentApproverId = currentApproverId;
        leaveRequest.Approvals.Add(new LeaveApproval
        {
            LeaveRequestId = leaveRequest.Id,
            ApproverId = previousApproverId,
            StepOrder = 1,
            Status = "Approved"
        });
        var otherDepartmentLeaveRequest = CreateLeaveRequest(otherDepartmentStaffId);
        var directorOwnRequest = CreateLeaveRequest(directorId);
        var directorCurrentApprovalRequest = CreateLeaveRequest(otherDepartmentStaffId);
        directorCurrentApprovalRequest.CurrentApproverId = directorId;
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveRequests.Add(otherDepartmentLeaveRequest);
        db.LeaveRequests.Add(directorOwnRequest);
        db.LeaveRequests.Add(directorCurrentApprovalRequest);
        db.Departments.AddRange(
            new Department { Id = departmentId, Name = "IT" },
            new Department { Id = otherDepartmentId, Name = "Other" });
        SeedPermissionUser(db, ownerId, "Staff", "LeaveRequest.ViewOwn", departmentId);
        SeedPermissionUser(db, otherDepartmentStaffId, "OtherStaff", "LeaveRequest.ViewOwn", otherDepartmentId, "Staff");
        SeedPermissionUser(db, currentApproverId, "CurrentApprover", "LeaveRequest.ViewPendingApproval");
        SeedPermissionUser(db, previousApproverId, "PreviousApprover", "LeaveRequest.ViewPendingApproval");
        SeedPermissionUser(db, approveOnlyUserId, "Approver", "LeaveManagement.Approve");
        SeedPermissionUser(db, manageUserId, "SuperAdmin", "LeaveRequest.ViewAll");
        SeedPermissionUser(db, departmentViewerId, "DepartmentHead", "LeaveRequest.ViewPendingApproval", departmentId);
        SeedPermissionUserWithPermissions(db, directorId, "Director", ["LeaveRequest.ViewOwn", "LeaveRequest.ViewPendingApproval"]);
        SeedPermissionUser(db, supportViewerId, "LeaveSupport", "LeaveSupport.ViewAll");
        SeedPermissionUser(db, adminRoleOnlyId, "Admin", "LeaveRequest.ViewOwn");
        await db.SaveChangesAsync();
        var service = new LeaveRequestAccessService(db);

        Assert.True(await service.CanAccessLeaveRequestAsync(leaveRequest, ownerId));
        Assert.True(await service.CanAccessLeaveRequestAsync(leaveRequest, currentApproverId));
        Assert.False(await service.CanAccessLeaveRequestAsync(leaveRequest, previousApproverId));
        Assert.False(await service.CanAccessLeaveRequestAsync(leaveRequest, approveOnlyUserId));
        Assert.True(await service.CanAccessLeaveRequestAsync(leaveRequest, departmentViewerId));
        Assert.False(await service.CanAccessLeaveRequestAsync(otherDepartmentLeaveRequest, departmentViewerId));
        Assert.True(await service.CanAccessLeaveRequestAsync(directorOwnRequest, directorId));
        Assert.True(await service.CanAccessLeaveRequestAsync(directorCurrentApprovalRequest, directorId));
        Assert.False(await service.CanAccessLeaveRequestAsync(leaveRequest, directorId));
        Assert.True(await service.CanAccessLeaveRequestAsync(leaveRequest, manageUserId));
        Assert.True(await service.CanAccessLeaveRequestAsync(leaveRequest, supportViewerId));
        Assert.False(await service.CanAccessLeaveRequestAsync(leaveRequest, adminRoleOnlyId));

        db.ChangeTracker.Clear();
        var requestLoadedWithoutUserRoles = await db.LeaveRequests
            .Include(item => item.User)
            .SingleAsync(item => item.Id == leaveRequest.Id);

        Assert.True(await service.CanAccessLeaveRequestAsync(requestLoadedWithoutUserRoles, departmentViewerId));
    }

    [Fact]
    public async Task PendingApprovalNotification_RemovesItem_WhenRequestIsRejected()
    {
        await using var db = CreateDbContext();
        var requester = CreateUser("staff01");
        var approver = CreateUser("head01");
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = "AnnualLeave", Name = "ลาพักผ่อน", IsActive = true };
        var leaveRequest = CreateLeaveRequest(requester.Id);
        leaveRequest.LeaveTypeId = leaveType.Id;
        leaveRequest.Status = "Pending";
        leaveRequest.CurrentApproverId = approver.Id;
        leaveRequest.User = requester;
        leaveRequest.LeaveType = leaveType;
        var approval = new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            LeaveRequest = leaveRequest,
            ApproverId = approver.Id,
            StepOrder = 1,
            Status = "Pending"
        };

        db.Users.AddRange(requester, approver);
        db.LeaveTypes.Add(leaveType);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveApprovals.Add(approval);
        await db.SaveChangesAsync();
        var service = new PendingApprovalNotificationService(db);

        Assert.Single(await service.GetMyPendingApprovalsAsync(approver.Id));

        leaveRequest.Status = "Rejected";
        leaveRequest.CurrentApproverId = null;
        approval.Status = "Rejected";
        await db.SaveChangesAsync();

        Assert.Empty(await service.GetMyPendingApprovalsAsync(approver.Id));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
    }

    private static User CreateUser(string username)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            FullName = username,
            PasswordHash = "hash",
            IsActive = true
        };
    }

    private static Role CreateRole(string name)
    {
        return new Role { Id = Guid.NewGuid(), Name = name, IsActive = true };
    }

    private static Permission CreatePermission(string code)
    {
        return new Permission { Id = Guid.NewGuid(), Code = code, Name = code, IsActive = true };
    }

    private static LeaveRequest CreateLeaveRequest(Guid userId)
    {
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 7, 20),
            EndDate = new DateOnly(2026, 7, 20),
            TotalDays = 1,
            Reason = "Security test",
            Status = "Pending"
        };
    }

    private static void SeedPermissionUser(AppDbContext db, Guid userId, string roleName, string permissionCode, Guid? departmentId = null, string? persistedRoleName = null)
    {
        SeedPermissionUserWithPermissions(db, userId, roleName, [permissionCode], departmentId, persistedRoleName);
    }

    private static void SeedPermissionUserWithPermissions(AppDbContext db, Guid userId, string roleName, IReadOnlyList<string> permissionCodes, Guid? departmentId = null, string? persistedRoleName = null)
    {
        var user = new User
        {
            Id = userId,
            Username = roleName,
            FullName = roleName,
            PasswordHash = "hash",
            DepartmentId = departmentId,
            IsActive = true
        };
        var role = CreateRole(persistedRoleName ?? roleName);
        db.Users.Add(user);
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id, User = user, Role = role });
        foreach (var permissionCode in permissionCodes)
        {
            var permission = CreatePermission(permissionCode);
            db.Permissions.Add(permission);
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission });
        }
    }

    private sealed class CaptureAuditLogService : IAuditLogService
    {
        public List<(Guid? UserId, string Action, string Resource, string? ResourceId, string? Detail, string Result)> Events { get; } = [];

        public Task WriteAsync(
            Guid? userId,
            string action,
            string resource,
            string? resourceId,
            string? detail,
            string result = "Success",
            HttpContext? httpContext = null)
        {
            Events.Add((userId, action, resource, resourceId, detail, result));
            return Task.CompletedTask;
        }
    }
}
