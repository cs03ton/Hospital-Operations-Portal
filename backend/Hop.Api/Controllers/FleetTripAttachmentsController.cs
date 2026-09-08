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

[ApiController,Authorize,Route("api/fleet/trips")]
public sealed class FleetTripAttachmentsController(AppDbContext db,FleetTripAttachmentStorage storage,IDomainEventPublisher events,IConfiguration config):ControllerBase
{
    [HttpGet("{requestId:guid}/attachments"),RequirePermission(FleetPermissions.DriverViewOwnJobs)]
    public async Task<ActionResult<ApiResponse<object>>> List(Guid requestId,CancellationToken ct)
    {
        var actor=Actor();
        var assignedToDriver=await db.FleetAssignments.AsNoTracking().AnyAsync(x=>x.FleetRequestId==requestId&&x.DriverUserId==actor&&x.IsActive,ct);
        if(!assignedToDriver)return Forbid();
        var trip=await db.FleetTripRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.FleetRequestId==requestId&&x.DriverUserId==actor,ct);
        if(trip is null)return ApiResponse<object>.Ok(Array.Empty<object>());
        var rows=await db.FleetTripAttachments.AsNoTracking().Where(x=>x.TripId==trip.Id&&!x.IsDeleted).OrderBy(x=>x.CreatedAt).Select(x=>new{x.Id,x.OriginalFileName,x.ContentType,x.FileSize,x.CreatedAt}).ToListAsync(ct);
        return ApiResponse<object>.Ok(rows);
    }

    [HttpGet("attachments/{id:guid}/download"),RequirePermission(FleetPermissions.DriverViewOwnJobs)]
    public async Task<IActionResult> Download(Guid id,CancellationToken ct){var actor=Actor();var item=await db.FleetTripAttachments.AsNoTracking().Include(x=>x.Trip).SingleOrDefaultAsync(x=>x.Id==id&&!x.IsDeleted,ct);if(item is null)return NotFound();if(item.Trip?.DriverUserId!=actor)return Forbid();var root=Path.GetFullPath(config["Storage:RootPath"]??config["STORAGE_ROOT_PATH"]??string.Empty);var path=Path.GetFullPath(Path.Combine(root,item.FilePath));if(root.Length==0||!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!System.IO.File.Exists(path))return NotFound();return PhysicalFile(path,item.ContentType,item.OriginalFileName);}

    [HttpDelete("attachments/{id:guid}"),RequirePermission(FleetPermissions.TripUploadAttachment)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id,CancellationToken ct){var actor=Actor();var item=await db.FleetTripAttachments.Include(x=>x.Trip).SingleOrDefaultAsync(x=>x.Id==id&&!x.IsDeleted,ct);if(item is null)return NotFound(ApiResponse<object>.Fail("Attachment not found."));if(item.Trip?.DriverUserId!=actor)return Forbid();item.IsDeleted=true;item.DeletedAt=DateTime.UtcNow;item.DeletedByUserId=actor;await db.SaveChangesAsync(ct);return ApiResponse<object>.Ok(new{item.Id,item.DeletedAt});}

    [HttpPost("{requestId:guid}/attachments"),RequirePermission(FleetPermissions.TripUploadAttachment),RequestSizeLimit(15_000_000)]
    public async Task<ActionResult<ApiResponse<object>>> Upload(Guid requestId,IFormFile file,CancellationToken ct)
    {
        var actor=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var key=Request.Headers["Idempotency-Key"].ToString();
        if(!string.IsNullOrWhiteSpace(key)){var existing=await db.FleetTripAttachments.AsNoTracking().SingleOrDefaultAsync(x=>x.IdempotencyKey==key,ct);if(existing is not null)return ApiResponse<object>.Ok(new{existing.Id,existing.OriginalFileName,existing.FileSize,IdempotentReplay=true});}
        var trip=await db.FleetTripRecords.Include(x=>x.Assignment).SingleOrDefaultAsync(x=>x.FleetRequestId==requestId,ct);if(trip is null)return NotFound(ApiResponse<object>.Fail("Trip not found."));if(trip.DriverUserId!=actor||trip.Assignment?.IsActive!=true)return Forbid();
        FleetTripAttachment item;try{item=await storage.Save(trip.Id,requestId,actor,key,file,ct);}catch(ArgumentException ex){return BadRequest(ApiResponse<object>.Fail(ex.Message));}db.Add(item);await events.PublishAsync(new("Fleet.TripAttachmentUploaded","FLEET","FleetRequest",requestId,actor,HttpContext.TraceIdentifier,new{RequestId=requestId,TripId=trip.Id,AttachmentId=item.Id,item.ContentType,item.FileSize,IdempotencyKey=key},[]),ct);await db.SaveChangesAsync(ct);return ApiResponse<object>.Ok(new{item.Id,item.OriginalFileName,item.FileSize});
    }
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<FleetTripRecord?> OwnedTrip(Guid requestId,Guid actor,CancellationToken ct)=>await db.FleetTripRecords.Include(x=>x.Assignment).SingleOrDefaultAsync(x=>x.FleetRequestId==requestId&&x.DriverUserId==actor&&x.Assignment!=null&&x.Assignment.IsActive,ct);
}
