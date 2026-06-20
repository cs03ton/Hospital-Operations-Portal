using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveAttachmentAccessTests
{
    [Fact]
    public async Task DownloadAttachment_AllowsUserWithLeaveApprovePermission()
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
            UploadedByUserId = ownerId,
            ContentType = "application/pdf"
        };
        db.LeaveRequests.Add(leaveRequest);
        db.LeaveAttachments.Add(attachment);
        db.Roles.Add(new Role { Id = roleId, Name = "Approver", IsActive = true });
        db.Permissions.Add(new Permission { Id = permissionId, Code = "LeaveManagement.Approve", Name = "LeaveManagement.Approve", IsActive = true });
        db.UserRoles.Add(new UserRole { UserId = approverId, RoleId = roleId });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await db.SaveChangesAsync();
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService());
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
        var controller = new LeaveAttachmentsController(db, new NoopAuditLogService(), new FakeAttachmentStorageService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(otherUserId)
        };

        var result = await controller.DownloadAttachment(attachment.Id);

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
