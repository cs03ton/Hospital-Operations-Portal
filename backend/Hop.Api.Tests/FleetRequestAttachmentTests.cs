using System.Security.Claims;
using System.IO.Compression;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetRequestAttachmentTests
{
    [Fact]
    public async Task Storage_RejectsWrongSignature_Oversize_AndFailedScan()
    {
        var root = Path.Combine(Path.GetTempPath(), "hop-fleet-request-test-" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = root }).Build();
        try
        {
            var clean = new FleetRequestAttachmentStorage(config, new Scanner(true), new FileTypeValidationService());
            await Assert.ThrowsAsync<ArgumentException>(() => clean.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), File("invite.pdf", "application/pdf", [1, 2, 3]), default));
            await Assert.ThrowsAsync<ArgumentException>(() => clean.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), File("invite.pdf", "application/pdf", new byte[5 * 1024 * 1024 + 1]), default));
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) archive.CreateEntry("not-a-word-document.txt");
                await Assert.ThrowsAsync<ArgumentException>(() => clean.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), File("invite.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", stream.ToArray()), default));
            }
            var infected = new FleetRequestAttachmentStorage(config, new Scanner(false), new FileTypeValidationService());
            await Assert.ThrowsAsync<ArgumentException>(() => infected.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), File("invite.pdf", "application/pdf", "%PDF-"u8.ToArray()), default));
            var saved = await clean.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), File("invite.pdf", "application/pdf", "%PDF-"u8.ToArray()), default);
            Assert.True(clean.Get(saved).Exists);
            Assert.Equal("application/pdf", saved.ContentType);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task PassengerCannotListDocuments_ReviewerCan_AndRequesterCanDeleteOnlyDraft()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var requester = Guid.NewGuid(); var passenger = Guid.NewGuid(); var reviewer = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = "Reviewer" };
        var permission = new Permission { Id = Guid.NewGuid(), Code = "FleetAdminReview.Approve", Name = "Approve", Group = "Fleet", Action = "Approve" };
        db.Roles.Add(role); db.Permissions.Add(permission);
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.UserRoles.Add(new UserRole { UserId = reviewer, RoleId = role.Id });
        var request = new FleetRequest { Id = Guid.NewGuid(), RequesterUserId = requester, Status = FleetRequestStatuses.Draft };
        db.FleetRequests.Add(request);
        var attachment = new FleetRequestAttachment { FleetRequestId = request.Id, UploadedByUserId = requester, OriginalFileName = "invite.pdf", StoredPath = "x", ContentType = "application/pdf", FileSize = 10 };
        db.FleetRequestAttachments.Add(attachment);
        await db.SaveChangesAsync();

        var controller = Controller(db, passenger);
        Assert.IsType<ForbidResult>(await controller.List(request.Id, default));
        controller = Controller(db, reviewer);
        Assert.IsType<OkObjectResult>(await controller.List(request.Id, default));
        Assert.IsType<ForbidResult>(await controller.Delete(attachment.Id, default));
        controller = Controller(db, requester);
        Assert.IsType<OkObjectResult>(await controller.List(request.Id, default));
        request.Status = FleetRequestStatuses.PendingAdminReview;
        await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(await controller.Delete(attachment.Id, default));
        request.Status = FleetRequestStatuses.Returned; request.ReturnTarget = FleetReturnTargets.Requester;
        await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await controller.Delete(attachment.Id, default));
    }

    private static FleetRequestAttachmentsController Controller(AppDbContext db, Guid actor)
    {
        var controller = new FleetRequestAttachmentsController(db, null!);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.ToString())], "test")) } };
        return controller;
    }

    private static IFormFile File(string name, string type, byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = type };
    private sealed class Scanner(bool clean) : IFileScanningService
    {
        public Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default) => Task.FromResult(new FileScanResult(clean, "test", clean ? "Clean" : "Rejected"));
    }
}
