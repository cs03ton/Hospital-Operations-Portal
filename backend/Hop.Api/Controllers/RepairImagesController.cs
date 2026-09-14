using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Hop.Api.Controllers;

[ApiController, Route("api/repairs"), Authorize, RepairActiveUser]
[RequireAnyPermission(RepairPermissions.ViewOwn, RepairPermissions.WorkIT, RepairPermissions.WorkGeneral, RepairPermissions.ViewAll)]
public sealed class RepairImagesController(AppDbContext db, IConfiguration config, IFileScanningService scanner,
    IFileTypeValidationService fileTypes) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string PathFor(Guid requestId, Guid imageId)
    {
        var root = config["Storage:RootPath"] ?? throw new InvalidOperationException("Storage root is required.");
        return Path.Combine(Path.GetFullPath(root), "repairs", requestId.ToString("N"), imageId.ToString("N") + ".webp");
    }

    [HttpPost("{id:guid}/images"), RequestSizeLimit(53_000_000)]
    public async Task<IActionResult> Upload(Guid id, [FromForm] Guid concurrencyToken, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        var r = await db.Set<RepairRequest>().SingleOrDefaultAsync(x => x.Id == id, ct);
        var access = await RepairWorkflow.Access(db, Actor, ct);
        if (r is null || !access.View(r)) return NotFound();
        if (!(access.Work(r.TeamCode) || (r.RequesterId == Actor && access.Permissions.Contains(RepairPermissions.ViewOwn)))) return Forbid();
        if (r.Status is "Closed" or "Cancelled") return BadRequest(ApiResponse<object>.Fail("ใบงานปิดแล้ว"));
        if (r.ConcurrencyToken != concurrencyToken) return Conflict(ApiResponse<object>.Fail("กรุณาโหลดข้อมูลใหม่"));
        if (files.Count is < 1 or > 5) return BadRequest(ApiResponse<object>.Fail("แนบรูปได้ครั้งละ 1–5 รูป"));
        var paths = new List<string>();
        var committed = false;
        try
        {
            var e = new RepairEvent { RequestId = id, ActorId = Actor, Round = r.CurrentRound,
                Action = "images", FromStatus = r.Status, ToStatus = r.Status, Note = $"แนบรูป {files.Count} รูป" };
            db.Add(e);
            foreach (var file in files)
            {
                if (file.Length is <= 0 or > 10_485_760) return BadRequest(ApiResponse<object>.Fail("รูปต้องไม่เกิน 10 MB"));
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var expected = ext switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => "" };
                if (expected == "" || file.ContentType != expected) return BadRequest(ApiResponse<object>.Fail("รองรับ JPG PNG WebP เท่านั้น"));
                var valid = await fileTypes.ValidateAsync(file, new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" }, ct);
                if (!valid.IsValid || !(await scanner.ScanAsync(file, ct)).IsClean) return BadRequest(ApiResponse<object>.Fail("ไฟล์ไม่ผ่านการตรวจสอบ"));
                await using var input = file.OpenReadStream();
                using var memory = new MemoryStream();
                await input.CopyToAsync(memory, ct);
                using var data = SKData.CreateCopy(memory.ToArray());
                using var codec = SKCodec.Create(data);
                if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0 ||
                    codec.Info.Width > 10000 || codec.Info.Height > 10000 ||
                    (long)codec.Info.Width * codec.Info.Height > 25_000_000)
                    return BadRequest(ApiResponse<object>.Fail("ขนาดภาพไม่ถูกต้องหรือใหญ่เกินไป"));
                using var bitmap = SKBitmap.Decode(data);
                if (bitmap is null) return BadRequest(ApiResponse<object>.Fail("ไม่สามารถอ่านภาพ"));
                using var image = SKImage.FromBitmap(bitmap);
                using var encoded = image.Encode(SKEncodedImageFormat.Webp, 85);
                var row = new RepairImage { RequestId = id, EventId = e.Id, UploadedById = Actor };
                var path = PathFor(id, row.Id);
                row.StoredPath = $"repairs/{id:N}/{row.Id:N}.webp";
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                paths.Add(path);
                await System.IO.File.WriteAllBytesAsync(path, encoded.ToArray(), ct);
                db.Add(row);
            }
            r.UpdatedAt = DateTime.UtcNow; r.ConcurrencyToken = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
            committed = true;
            return Ok(ApiResponse<object>.Ok(new { r.ConcurrencyToken }));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.Fail("มีการแก้ไขพร้อมกัน กรุณาโหลดใหม่")); }
        finally
        {
            if (!committed) foreach (var path in paths) System.IO.File.Delete(path);
        }
    }

    [HttpGet("images/{imageId:guid}")]
    public async Task<IActionResult> Download(Guid imageId, CancellationToken ct)
    {
        var image = await db.Set<RepairImage>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == imageId, ct);
        if (image is null) return NotFound();
        var r = await db.Set<RepairRequest>().AsNoTracking().SingleAsync(x => x.Id == image.RequestId, ct);
        if (!(await RepairWorkflow.Access(db, Actor, ct)).View(r)) return NotFound();
        var path = PathFor(r.Id, image.Id);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(path, "image/webp");
    }
}
