using System.Data;
using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/fleet/requests")]
public sealed class FleetRequestAttachmentsController(AppDbContext db, FleetRequestAttachmentStorage storage) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("{requestId:guid}/attachments")]
    public async Task<IActionResult> List(Guid requestId, CancellationToken ct)
    {
        var request = await db.FleetRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestId, ct);
        if (request is null) return NotFound();
        if (!await CanRead(request, ct)) return Forbid();
        var rows = await db.FleetRequestAttachments.AsNoTracking().Where(x => x.FleetRequestId == requestId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.FileSize, x.CreatedAt }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(rows));
    }

    [HttpPost("{requestId:guid}/attachments"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid requestId, [FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file is null) return BadRequest(ApiResponse<object>.Fail("กรุณาเลือกไฟล์เอกสาร"));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.FleetRequests.SingleOrDefaultAsync(x => x.Id == requestId, ct);
        if (request is null) return NotFound();
        if (!CanEdit(request)) return Forbid();
        if (await db.FleetRequestAttachments.CountAsync(x => x.FleetRequestId == requestId && !x.IsDeleted, ct) >= 2)
            return BadRequest(ApiResponse<object>.Fail("แนบเอกสารได้สูงสุด 2 ไฟล์"));
        FleetRequestAttachment attachment;
        try { attachment = await storage.SaveAsync(requestId, Actor!.Value, file, ct); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        try
        {
            db.FleetRequestAttachments.Add(attachment);
            Audit("Fleet.RequestAttachmentUploaded", attachment.Id);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch { storage.DeleteStoredFile(attachment); throw; }
        return Ok(ApiResponse<object>.Ok(new { attachment.Id, attachment.OriginalFileName, attachment.ContentType, attachment.FileSize, attachment.CreatedAt }));
    }

    [HttpGet("attachments/{id:guid}"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Open(Guid id, CancellationToken ct)
    {
        var item = await db.FleetRequestAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (item is null) return NotFound();
        var request = await db.FleetRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.FleetRequestId, ct);
        if (request is null) return NotFound();
        if (!await CanRead(request, ct)) return Forbid();
        var file = storage.Get(item);
        if (!file.Exists) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return item.ContentType is "application/pdf" or "image/jpeg" or "image/png"
            ? PhysicalFile(file.FullName, item.ContentType, enableRangeProcessing: true)
            : PhysicalFile(file.FullName, item.ContentType, item.OriginalFileName);
    }

    [HttpDelete("attachments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.FleetRequestAttachments.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (item is null) return NotFound();
        var request = await db.FleetRequests.SingleOrDefaultAsync(x => x.Id == item.FleetRequestId, ct);
        if (request is null) return NotFound();
        if (!CanEdit(request)) return Forbid();
        item.IsDeleted = true; item.DeletedAt = DateTime.UtcNow; item.DeletedByUserId = Actor;
        Audit("Fleet.RequestAttachmentDeleted", id);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { item.Id }));
    }

    private bool CanEdit(FleetRequest request) => Actor is { } actor && request.RequesterUserId == actor &&
        (request.Status == FleetRequestStatuses.Draft || request.Status == FleetRequestStatuses.Returned && request.ReturnTarget == FleetReturnTargets.Requester);

    private async Task<bool> CanRead(FleetRequest request, CancellationToken ct)
    {
        if (Actor is not { } actor) return false;
        if (request.RequesterUserId == actor) return true;
        var allowed = new[] {
            FleetPermissions.RequestViewAll, FleetPermissions.DispatchView,
            FleetPermissions.AdminReviewApprove, FleetPermissions.AdminReviewReturn, FleetPermissions.AdminReviewReject,
            FleetPermissions.DirectorApprove, FleetPermissions.DirectorReturn, FleetPermissions.DirectorReject
        };
        return await db.UserRoles.Where(x => x.UserId == actor && x.Role != null && x.Role.IsActive)
            .SelectMany(x => x.Role!.RolePermissions)
            .AnyAsync(x => x.Permission != null && x.Permission.IsActive && allowed.Contains(x.Permission.Code), ct);
    }

    private void Audit(string action, Guid attachmentId) => db.AuditLogs.Add(new AuditLog
    {
        UserId = Actor, EffectiveActorUserId = Actor, Action = action, EntityName = nameof(FleetRequestAttachment),
        EntityId = attachmentId.ToString(), CorrelationId = HttpContext.TraceIdentifier
    });
}
