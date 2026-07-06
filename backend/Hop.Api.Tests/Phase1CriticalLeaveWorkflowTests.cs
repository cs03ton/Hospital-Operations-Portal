using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hop.Api.Tests;

public class Phase1CriticalLeaveWorkflowTests
{
    [Fact]
    public async Task ApproveFlow_MovesCurrentApproverThenApprovesAndDeductsBalance()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedPendingLeaveAsync(db);
        var notifications = new CaptureNotificationPublisher();

        var headController = CreateController(db, fixture.Head.Id, notifications);
        var headResult = await headController.ApproveLeaveRequest(fixture.LeaveRequest.Id, new LeaveDecisionRequest("ผ่านการตรวจสอบ"));

        Assert.IsType<ApiResponse<LeaveRequestResponse>>(headResult.Value);
        var afterHead = await db.LeaveRequests.Include(item => item.Approvals).SingleAsync(item => item.Id == fixture.LeaveRequest.Id);
        Assert.Equal("Pending", afterHead.Status);
        Assert.Equal(fixture.Director.Id, afterHead.CurrentApproverId);
        Assert.Contains(afterHead.Approvals, item => item.ApproverId == fixture.Head.Id && item.Status == "Approved" && item.Remark == "ผ่านการตรวจสอบ");
        Assert.Contains(afterHead.Approvals, item => item.ApproverId == fixture.Director.Id && item.Status == "Pending");
        Assert.Contains(notifications.Events, item => item.EventName == "ApprovalStepActivated" && item.RecipientUserId == fixture.Director.Id);

        var directorController = CreateController(db, fixture.Director.Id, notifications);
        var directorResult = await directorController.ApproveLeaveRequest(fixture.LeaveRequest.Id, new LeaveDecisionRequest("อนุมัติ"));

        Assert.IsType<ApiResponse<LeaveRequestResponse>>(directorResult.Value);
        var approved = await db.LeaveRequests.SingleAsync(item => item.Id == fixture.LeaveRequest.Id);
        var balance = await db.LeaveBalances.SingleAsync(item => item.UserId == fixture.Staff.Id && item.LeaveTypeId == fixture.LeaveType.Id);
        Assert.Equal("Approved", approved.Status);
        Assert.Null(approved.CurrentApproverId);
        Assert.Equal(0, balance.PendingDays);
        Assert.Equal(2, balance.UsedDays);
        Assert.Contains(notifications.Events, item => item.EventName == "LeaveApproved" && item.RecipientUserId == fixture.Staff.Id);
    }

    [Fact]
    public async Task RejectFlow_ClearsPendingApprovalAndDoesNotDeductUsedBalance()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedPendingLeaveAsync(db);
        var notifications = new CaptureNotificationPublisher();
        var controller = CreateController(db, fixture.Head.Id, notifications);

        var result = await controller.RejectLeaveRequest(fixture.LeaveRequest.Id, new LeaveDecisionRequest("ข้อมูลไม่ครบ"));

        Assert.IsType<ApiResponse<LeaveRequestResponse>>(result.Value);
        var rejected = await db.LeaveRequests.Include(item => item.Approvals).SingleAsync(item => item.Id == fixture.LeaveRequest.Id);
        var balance = await db.LeaveBalances.SingleAsync(item => item.UserId == fixture.Staff.Id && item.LeaveTypeId == fixture.LeaveType.Id);
        Assert.Equal("Rejected", rejected.Status);
        Assert.Null(rejected.CurrentApproverId);
        Assert.Equal(0, balance.PendingDays);
        Assert.Equal(0, balance.UsedDays);
        Assert.Contains(rejected.Approvals, item => item.ApproverId == fixture.Head.Id && item.Status == "Rejected");
        Assert.Contains(rejected.Approvals, item => item.ApproverId == fixture.Director.Id && item.Status == "Skipped");
        Assert.Contains(notifications.Events, item => item.EventName == "LeaveRejected" && item.RecipientUserId == fixture.Staff.Id);
    }

    [Fact]
    public async Task CancelFlow_ClearsCurrentApproverNotificationAndKeepsUsedBalance()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedPendingLeaveAsync(db);
        var notifications = new CaptureNotificationPublisher();
        var controller = CreateController(db, fixture.Staff.Id, notifications);

        var result = await controller.CancelLeaveRequest(fixture.LeaveRequest.Id);

        Assert.IsType<ApiResponse<LeaveRequestResponse>>(result.Value);
        var cancelled = await db.LeaveRequests.SingleAsync(item => item.Id == fixture.LeaveRequest.Id);
        var balance = await db.LeaveBalances.SingleAsync(item => item.UserId == fixture.Staff.Id && item.LeaveTypeId == fixture.LeaveType.Id);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Null(cancelled.CurrentApproverId);
        Assert.Equal(0, balance.PendingDays);
        Assert.Equal(0, balance.UsedDays);
        Assert.Contains(notifications.Events, item => item.EventName == "LeaveCancelled" && item.RecipientUserId == fixture.Head.Id);
    }

    [Fact]
    public async Task ApproveFlow_ForbidsApproverWhoIsNotCurrentStep()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedPendingLeaveAsync(db);
        var controller = CreateController(db, fixture.Director.Id, new CaptureNotificationPublisher());

        var result = await controller.ApproveLeaveRequest(fixture.LeaveRequest.Id, new LeaveDecisionRequest("พยายามอนุมัติก่อนถึงคิว"));

        Assert.IsType<ForbidResult>(result.Result);
        var unchanged = await db.LeaveRequests.SingleAsync(item => item.Id == fixture.LeaveRequest.Id);
        Assert.Equal(fixture.Head.Id, unchanged.CurrentApproverId);
        Assert.Equal("Pending", unchanged.Status);
    }

    [Fact]
    public async Task LeaveDetail_ForbidsDirectorWhenRequestIsNotVisibleToDirector()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedPendingLeaveAsync(db);
        var controller = CreateController(db, fixture.Director.Id, new CaptureNotificationPublisher());

        var result = await controller.GetLeaveRequest(fixture.LeaveRequest.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<WorkflowFixture> SeedPendingLeaveAsync(AppDbContext db)
    {
        var staff = CreateUser("staff01", "เจ้าหน้าที่ 01");
        var head = CreateUser("head01", "หัวหน้าหน่วยงาน");
        var director = CreateUser("director01", "ผู้อำนวยการ");
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = "annual",
            Name = "ลาพักผ่อน",
            DefaultDaysPerYear = 10,
            RequiresBalance = true,
            IsActive = true
        };
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = "LV-202606-001",
            UserId = staff.Id,
            User = staff,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 2),
            DurationType = "FULL_DAY",
            TotalDays = 2,
            Reason = "ทดสอบ Phase 1",
            Status = "Pending",
            CurrentApproverId = head.Id,
            SubmittedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        leaveRequest.Approvals.Add(new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            LeaveRequest = leaveRequest,
            ApproverId = head.Id,
            Approver = head,
            StepOrder = 1,
            StepName = "หัวหน้าหน่วยงาน",
            RequiredPermissionCode = "LeaveApproval.ApproveCurrentStep",
            Status = "Pending"
        });
        leaveRequest.Approvals.Add(new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            LeaveRequest = leaveRequest,
            ApproverId = director.Id,
            Approver = director,
            StepOrder = 2,
            StepName = "ผู้อำนวยการ",
            RequiredPermissionCode = "LeaveApproval.ApproveCurrentStep",
            Status = "Waiting"
        });

        db.Users.AddRange(staff, head, director);
        db.LeaveTypes.Add(leaveType);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = staff.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2026,
            EntitledDays = 10,
            PendingDays = 2,
            UsedDays = 0
        });
        SeedRoleWithPermission(db, staff, "Staff", "LeaveRequest.CancelOwn");
        SeedRoleWithPermission(db, head, "DepartmentHead", "LeaveApproval.ApproveCurrentStep");
        SeedRoleWithPermission(db, director, "Director", "LeaveApproval.ApproveCurrentStep");
        await db.SaveChangesAsync();
        return new WorkflowFixture(staff, head, director, leaveType, leaveRequest);
    }

    private static User CreateUser(string username, string fullName)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            FullName = fullName,
            PasswordHash = "hash",
            IsActive = true
        };
    }

    private static void SeedRoleWithPermission(AppDbContext db, User user, string roleName, string permissionCode)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = roleName, IsActive = true };
        var permission = new Permission { Id = Guid.NewGuid(), Code = permissionCode, Name = permissionCode, IsActive = true };
        db.Roles.Add(role);
        db.Permissions.Add(permission);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission });
    }

    private static LeaveRequestsController CreateController(
        AppDbContext db,
        Guid currentUserId,
        ILeaveNotificationEventPublisher notificationPublisher)
    {
        var controller = new LeaveRequestsController(
            db,
            new NoopAuditLogService(),
            new ValidLeaveValidationService(),
            new LeavePolicyService(db),
            new LeaveCalendarService(db),
            new EmptyApprovalChainService(),
            new FakeAttachmentStorageService(),
            new FakeLeavePdfService(),
            new CleanFileScanningService(),
            notificationPublisher,
            new LeaveRequestAccessService(db),
            new StaticRequestNumberService(),
            new ConfigurationBuilder().Build(),
            NullLogger<LeaveRequestsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())],
                    "Test"))
            }
        };
        return controller;
    }

    private sealed record WorkflowFixture(
        User Staff,
        User Head,
        User Director,
        LeaveType LeaveType,
        LeaveRequest LeaveRequest);

    private sealed class CaptureNotificationPublisher : ILeaveNotificationEventPublisher
    {
        public List<(string EventName, Guid LeaveRequestId, Guid? RecipientUserId)> Events { get; } = [];

        public Task PublishAsync(string eventName, Guid leaveRequestId, Guid? recipientUserId, CancellationToken cancellationToken = default)
        {
            Events.Add((eventName, leaveRequestId, recipientUserId));
            return Task.CompletedTask;
        }
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ValidLeaveValidationService : ILeaveValidationService
    {
        public Task<LeaveValidationResult> ValidateDraftAsync(LeaveRequest leaveRequest, Guid? excludeLeaveRequestId = null)
        {
            return Task.FromResult(new LeaveValidationResult(true, null, leaveRequest.TotalDays));
        }

        public Task<LeaveValidationResult> ValidateSubmitAsync(LeaveRequest leaveRequest)
        {
            return Task.FromResult(new LeaveValidationResult(true, null, leaveRequest.TotalDays));
        }
    }

    private sealed class EmptyApprovalChainService : IApprovalChainService
    {
        public Task<IReadOnlyList<ApprovalStepPlan>> BuildApprovalPlanAsync(LeaveRequest leaveRequest)
        {
            return Task.FromResult<IReadOnlyList<ApprovalStepPlan>>([]);
        }
    }

    private sealed class FakeAttachmentStorageService : ILeaveAttachmentStorageService
    {
        public Task<LeaveAttachment> SaveAsync(Guid leaveRequestId, Guid uploadedByUserId, IFormFile file)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(LeaveAttachment attachment)
        {
            return Task.CompletedTask;
        }

        public FileInfo GetFileInfo(LeaveAttachment attachment)
        {
            return new FileInfo(Path.GetTempFileName());
        }
    }

    private sealed class FakeLeavePdfService : ILeavePdfService
    {
        public byte[] GenerateLeaveRequestPdf(LeaveRequest leaveRequest, LeavePdfRenderContext context)
        {
            return "%PDF-1.4"u8.ToArray();
        }
    }

    private sealed class CleanFileScanningService : IFileScanningService
    {
        public Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FileScanResult(true, "Test", "Clean"));
        }
    }

    private sealed class StaticRequestNumberService : ILeaveRequestNumberService
    {
        public Task<string> GenerateAsync(DateTime createdAtUtc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("LV-202606-999");
        }
    }
}
