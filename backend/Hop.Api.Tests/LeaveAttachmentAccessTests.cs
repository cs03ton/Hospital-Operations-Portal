using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveAttachmentAccessTests
{
    [Fact]
    public async Task DownloadAttachment_AllowsCurrentApprover()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 18),
            EndDate = new DateOnly(2026, 6, 18),
            TotalDays = 1,
            Reason = "Leave",
            Status = "Pending",
            CurrentApproverId = approverId
        };
        var attachment = new LeaveAttachment
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            FileName = "sample.pdf",
            FilePath = "leave-attachments/sample.pdf",
            UploadedByUserId = ownerId,
            ContentType = "application/pdf"
        };
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, approverId, "Approver", "LeaveRequest.ViewPendingApproval");
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(approverId)
        };

        var result = await controller.DownloadAttachment(attachment.Id);

        Assert.IsType<PhysicalFileResult>(result);
    }

    [Fact]
    public async Task DownloadAttachment_ForbidsUnrelatedUser()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 18),
            EndDate = new DateOnly(2026, 6, 18),
            TotalDays = 1,
            Reason = "Leave",
            Status = "Draft"
        };
        var attachment = new LeaveAttachment
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            FileName = "sample.pdf",
            FilePath = "leave-attachments/sample.pdf",
            UploadedByUserId = ownerId
        };
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(otherUserId)
        };

        var result = await controller.DownloadAttachment(attachment.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DownloadAttachment_ForbidsApprovePermissionWhenUserIsNotInWorkflow()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 18),
            EndDate = new DateOnly(2026, 6, 18),
            TotalDays = 1,
            Reason = "Leave",
            Status = "Draft"
        };
        var attachment = new LeaveAttachment
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            FileName = "sample.pdf",
            FilePath = "leave-attachments/sample.pdf",
            UploadedByUserId = ownerId
        };
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        db.Roles.Add(new Role { Id = roleId, Name = "Approver", IsActive = true });
        db.Permissions.Add(new Permission { Id = permissionId, Code = "LeaveManagement.Approve", Name = "LeaveManagement.Approve", IsActive = true });
        db.UserRoles.Add(new UserRole { UserId = approverId, RoleId = roleId });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(approverId)
        };

        var result = await controller.DownloadAttachment(attachment.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PreviewAttachment_AllowsCurrentApproverAndStreamsInline()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "Pending");
        leaveRequest.CurrentApproverId = approverId;
        leaveRequest.Approvals.Add(new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            ApproverId = approverId,
            Status = "Pending",
            StepOrder = 1,
            RequiredPermissionCode = LeavePermissions.ApproveCurrentStep
        });
        var attachment = CreateAttachment(leaveRequest, ownerId);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, approverId, "Approver", LeavePermissions.ViewPendingApproval);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(approverId) };

        var result = await controller.PreviewAttachment(leaveRequest.Id, attachment.Id);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.StartsWith("inline;", controller.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task PreviewAttachment_AllowsSupportedImage()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "Draft");
        var attachment = CreateAttachment(leaveRequest, ownerId);
        attachment.FileName = "medical.png";
        attachment.ContentType = "image/png";
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, ownerId, "Staff", LeavePermissions.ViewOwn);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(ownerId) };

        var result = await controller.PreviewAttachment(leaveRequest.Id, attachment.Id);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
    }

    [Fact]
    public async Task PreviewAttachment_BlocksUnsupportedFileType()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "Draft");
        var attachment = CreateAttachment(leaveRequest, ownerId);
        attachment.FileName = "archive.zip";
        attachment.ContentType = "application/zip";
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, ownerId, "Staff", LeavePermissions.ViewOwn);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(ownerId) };

        var result = await controller.PreviewAttachment(leaveRequest.Id, attachment.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.Equal("ไม่รองรับการแสดงตัวอย่างไฟล์ประเภทนี้", response.Message);
    }

    [Fact]
    public async Task PreviewAttachment_ForbidsUnrelatedUser()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "Draft");
        var attachment = CreateAttachment(leaveRequest, ownerId);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(otherUserId) };

        var result = await controller.PreviewAttachment(leaveRequest.Id, attachment.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteAttachment_AllowsRequesterInReturnedForRevision()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "ReturnedForRevision");
        var attachment = CreateAttachment(leaveRequest, ownerId);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, ownerId, "Staff", LeavePermissions.EditOwn);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(ownerId) };

        var result = await controller.DeleteAttachment(attachment.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.LeaveAttachments);
    }

    [Fact]
    public async Task DeleteAttachment_BlocksRequesterWhenRequestIsPending()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "Pending");
        var attachment = CreateAttachment(leaveRequest, ownerId);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, ownerId, "Staff", LeavePermissions.EditOwn);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(ownerId) };

        var result = await controller.DeleteAttachment(attachment.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.Contains("ลบไฟล์แนบได้เฉพาะ", response.Message);
    }

    [Fact]
    public async Task DeleteAttachment_ForbidsCurrentApprover()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(ownerId, "Pending");
        leaveRequest.CurrentApproverId = approverId;
        var attachment = CreateAttachment(leaveRequest, ownerId);
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        SeedPermissionUser(db, ownerId, "Staff", LeavePermissions.EditOwn);
        SeedPermissionUser(db, approverId, "Approver", LeavePermissions.ViewPendingApproval);
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService(), new LeaveRequestAccessService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(approverId) };

        var result = await controller.DeleteAttachment(attachment.Id);

        Assert.IsType<ForbidResult>(result);
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test"));
        return context;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedPermissionUser(AppDbContext db, Guid userId, string roleName, string permissionCode)
    {
        var user = new User
        {
            Id = userId,
            Username = roleName,
            FullName = roleName,
            PasswordHash = "hash",
            IsActive = true
        };
        var role = new Role { Id = Guid.NewGuid(), Name = roleName, IsActive = true };
        var permission = new Permission { Id = Guid.NewGuid(), Code = permissionCode, Name = permissionCode, IsActive = true };
        db.Users.Add(user);
        db.Roles.Add(role);
        db.Permissions.Add(permission);
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id, User = user, Role = role });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission });
    }

    private static LeaveRequest CreateLeaveRequest(Guid ownerId, string status)
    {
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            LeaveTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 18),
            EndDate = new DateOnly(2026, 6, 18),
            TotalDays = 1,
            Reason = "Leave",
            Status = status
        };
    }

    private static LeaveAttachment CreateAttachment(LeaveRequest leaveRequest, Guid ownerId)
    {
        return new LeaveAttachment
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            LeaveRequest = leaveRequest,
            FileName = "sample.pdf",
            FilePath = "leave-attachments/sample.pdf",
            UploadedByUserId = ownerId,
            ContentType = "application/pdf"
        };
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
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
}
