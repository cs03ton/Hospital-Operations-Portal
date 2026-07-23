using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/announcements")]
[Authorize]
[RequirePermission(AnnouncementPermissions.View)]
public class AnnouncementsController(
    AppDbContext db,
    IAuditLogService auditLogService,
    IAnnouncementMediaStorageService mediaStorage) : ControllerBase
{
    [HttpGet("feed")]
    public async Task<ActionResult<ApiResponse<PagedResponse<AnnouncementSummaryResponse>>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var visible = await GetVisiblePublishedAnnouncementsAsync(access, cancellationToken);

        var query = visible.AsEnumerable();
        if (categoryId is not null)
        {
            query = query.Where(item => item.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(item => string.Equals(item.Priority, priority, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item =>
                item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.Body.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query
            .OrderBy(item => item.Priority == AnnouncementPriorities.Critical ? 0 : item.Priority == AnnouncementPriorities.Important ? 1 : 2)
            .ThenByDescending(item => item.PublishedAt ?? item.PublishAt ?? item.CreatedAt)
            .ToList();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var totalItems = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => ToSummary(item, access.UserId, Url))
            .ToList();

        return ApiResponse<PagedResponse<AnnouncementSummaryResponse>>.Ok(new PagedResponse<AnnouncementSummaryResponse>(
            items,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)
        ));
    }

    [HttpGet("featured")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementSummaryResponse>>>> GetFeatured(CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var items = (await GetVisiblePublishedAnnouncementsAsync(access, cancellationToken))
            .Where(item => item.IsFeatured)
            .OrderByDescending(item => item.PublishedAt ?? item.PublishAt ?? item.CreatedAt)
            .Take(5)
            .Select(item => ToSummary(item, access.UserId, Url))
            .ToList();

        return ApiResponse<IReadOnlyList<AnnouncementSummaryResponse>>.Ok(items);
    }

    [HttpGet("popup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementSummaryResponse>>>> GetPopup(CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var items = (await GetVisiblePublishedAnnouncementsAsync(access, cancellationToken))
            .Where(item => item.ShowAsPopup)
            .Where(item => item.Reads.All(read => read.UserId != access.UserId))
            .OrderByDescending(item => item.Priority == AnnouncementPriorities.Critical)
            .ThenByDescending(item => item.PublishedAt ?? item.PublishAt ?? item.CreatedAt)
            .Take(3)
            .Select(item => ToSummary(item, access.UserId, Url))
            .ToList();

        return ApiResponse<IReadOnlyList<AnnouncementSummaryResponse>>.Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var item = (await GetVisiblePublishedAnnouncementsAsync(access, cancellationToken)).FirstOrDefault(item => item.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<AnnouncementDetailResponse>.Fail("ไม่พบประกาศ หรือคุณไม่มีสิทธิ์เข้าถึงประกาศนี้"));
        }

        await MarkReadAsync(item, access.UserId, cancellationToken);
        await auditLogService.WriteAsync(access.UserId, "Announcement.Read", "Announcement", id.ToString(), item.Title, httpContext: HttpContext);

        return ApiResponse<AnnouncementDetailResponse>.Ok(ToDetail(item, access.UserId, Url));
    }

    [HttpPost("{id:guid}/acknowledge")]
    [RequirePermission(AnnouncementPermissions.Acknowledge)]
    public async Task<ActionResult<ApiResponse<AnnouncementAcknowledgeResponse>>> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var item = (await GetVisiblePublishedAnnouncementsAsync(access, cancellationToken)).FirstOrDefault(item => item.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<AnnouncementAcknowledgeResponse>.Fail("ไม่พบประกาศ หรือคุณไม่มีสิทธิ์เข้าถึงประกาศนี้"));
        }

        var read = await EnsureReadAsync(item.Id, access.UserId, cancellationToken);
        read.AcknowledgedAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(access.UserId, "Announcement.Acknowledged", "Announcement", id.ToString(), item.Title, httpContext: HttpContext);

        return ApiResponse<AnnouncementAcknowledgeResponse>.Ok(new AnnouncementAcknowledgeResponse(
            item.Id,
            true,
            true,
            read.ReadAt,
            read.AcknowledgedAt
        ), "รับทราบประกาศเรียบร้อยแล้ว");
    }

    [HttpGet("{announcementId:guid}/images")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementImageResponse>>>> GetImages(Guid announcementId, CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var item = (await GetVisiblePublishedAnnouncementsAsync(access, cancellationToken)).FirstOrDefault(item => item.Id == announcementId);
        if (item is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<AnnouncementImageResponse>>.Fail("ไม่พบประกาศ หรือคุณไม่มีสิทธิ์เข้าถึงประกาศนี้"));
        }

        return ApiResponse<IReadOnlyList<AnnouncementImageResponse>>.Ok(ToImageResponses(item.Images, Url));
    }

    [HttpGet("images/{imageId:guid}/{variant}")]
    public async Task<IActionResult> GetImage(Guid imageId, string variant, CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var image = await db.AnnouncementImages
            .Include(item => item.Announcement)
                .ThenInclude(item => item!.Targets)
            .FirstOrDefaultAsync(item => item.Id == imageId, cancellationToken);
        if (image?.Announcement is null || !await CanReadAnnouncementAsync(image.Announcement, access, cancellationToken))
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบรูปภาพ หรือคุณไม่มีสิทธิ์เข้าถึงรูปภาพนี้"));
        }

        if (variant is not ("thumbnail" or "medium" or "large" or "original"))
        {
            return BadRequest(ApiResponse<string>.Fail("ขนาดรูปภาพไม่ถูกต้อง"));
        }

        var fileInfo = await mediaStorage.OpenImageAsync(image, variant, cancellationToken);
        if (!fileInfo.Exists)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบไฟล์รูปภาพ"));
        }

        Response.Headers.CacheControl = "private, max-age=3600";
        return PhysicalFile(fileInfo.FullName, image.MimeType, enableRangeProcessing: true);
    }

    [HttpGet("files/{fileId:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken cancellationToken)
    {
        var access = await BuildAccessAsync(cancellationToken);
        var file = await db.AnnouncementFiles
            .Include(item => item.Announcement)
                .ThenInclude(item => item!.Targets)
            .FirstOrDefaultAsync(item => item.Id == fileId, cancellationToken);
        if (file?.Announcement is null || !await CanReadAnnouncementAsync(file.Announcement, access, cancellationToken))
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบไฟล์ หรือคุณไม่มีสิทธิ์ดาวน์โหลดไฟล์นี้"));
        }

        var fileInfo = await mediaStorage.OpenAttachmentAsync(file, cancellationToken);
        if (!fileInfo.Exists)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบไฟล์แนบ"));
        }

        await auditLogService.WriteAsync(access.UserId, "Announcement.FileDownloaded", "AnnouncementFile", fileId.ToString(), file.OriginalFileName, httpContext: HttpContext);
        return PhysicalFile(fileInfo.FullName, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
    }

    private async Task<IReadOnlyList<Announcement>> GetVisiblePublishedAnnouncementsAsync(AnnouncementAccess access, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = await db.Announcements
            .AsSplitQuery()
            .Include(item => item.Category)
            .Include(item => item.Targets)
            .Include(item => item.Files)
            .Include(item => item.Images)
            .Include(item => item.Reads)
            .Include(item => item.NotificationDeliveries)
                .ThenInclude(item => item.LineQueue)
            .Include(item => item.CreatedByUser)
            .Include(item => item.PublishedByUser)
            .Where(item => item.Status == AnnouncementStatuses.Published)
            .Where(item => item.PublishAt == null || item.PublishAt <= now)
            .Where(item => item.ExpiresAt == null || item.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        return items.Where(item => IsTargetedToUser(item, access)).ToList();
    }

    private async Task MarkReadAsync(Announcement item, Guid userId, CancellationToken cancellationToken)
    {
        item.ViewCount += 1;
        await EnsureReadAsync(item.Id, userId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AnnouncementRead> EnsureReadAsync(Guid announcementId, Guid userId, CancellationToken cancellationToken)
    {
        var read = await db.AnnouncementReads.FirstOrDefaultAsync(item => item.AnnouncementId == announcementId && item.UserId == userId, cancellationToken);
        if (read is not null)
        {
            return read;
        }

        read = new AnnouncementRead
        {
            AnnouncementId = announcementId,
            UserId = userId
        };
        db.AnnouncementReads.Add(read);
        return read;
    }

    private async Task<AnnouncementAccess> BuildAccessAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? throw new InvalidOperationException("Invalid access token.");
        var user = await db.Users.AsNoTracking().FirstAsync(item => item.Id == userId, cancellationToken);
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(item => item.Value)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .Where(item => item.Permission != null && item.Permission.IsActive)
            .Select(item => item.Permission!.Code)
            .ToListAsync(cancellationToken);

        return new AnnouncementAccess(userId, user.DepartmentId, roles, permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    internal static bool IsTargetedToUser(Announcement item, AnnouncementAccess access)
    {
        if (item.Targets.Count == 0)
        {
            return true;
        }

        return item.Targets.Any(target =>
            target.TargetType == AnnouncementTargetTypes.Everyone ||
            (target.TargetType == AnnouncementTargetTypes.User && string.Equals(target.TargetValue, access.UserId.ToString(), StringComparison.OrdinalIgnoreCase)) ||
            (target.TargetType == AnnouncementTargetTypes.Department && access.DepartmentId is not null && string.Equals(target.TargetValue, access.DepartmentId.Value.ToString(), StringComparison.OrdinalIgnoreCase)) ||
            (target.TargetType == AnnouncementTargetTypes.Role && target.TargetValue is not null && access.Roles.Contains(target.TargetValue)) ||
            (target.TargetType == AnnouncementTargetTypes.Permission && target.TargetValue is not null && access.Permissions.Contains(target.TargetValue)));
    }

    internal static AnnouncementSummaryResponse ToSummary(Announcement item, Guid userId, IUrlHelper? url = null)
    {
        var read = item.Reads.FirstOrDefault(read => read.UserId == userId);
        return new AnnouncementSummaryResponse(
            item.Id,
            item.Title,
            item.Summary,
            item.Status,
            item.Priority,
            ToCategory(item.Category),
            item.IsFeatured,
            item.ShowAsPopup,
            item.ShowAsBanner,
            item.RequiresAcknowledgement,
            read is not null,
            read?.AcknowledgedAt is not null,
            item.PublishAt,
            item.ExpiresAt,
            item.CreatedAt,
            item.PublishedAt,
            item.CreatedByUser?.FullName,
            ToCoverImage(item, url),
            item.CoverImageUrl,
            item.Tags,
            item.ViewCount,
            item.Reads.Count(read => read.AcknowledgedAt is not null),
            item.NotifyInApp,
            item.NotifyViaLine,
            item.NotificationDispatchStatus,
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.InApp),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line && (delivery.LineQueue == null || delivery.LineQueue.Status == "Queued")),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line && delivery.LineQueue != null && delivery.LineQueue.Status == "Sent"),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line && delivery.LineQueue != null && delivery.LineQueue.Status == "Failed")
        );
    }

    internal static AnnouncementDetailResponse ToDetail(Announcement item, Guid userId, IUrlHelper? url = null)
    {
        var read = item.Reads.FirstOrDefault(read => read.UserId == userId);
        return new AnnouncementDetailResponse(
            item.Id,
            item.Title,
            item.Summary,
            item.Body,
            item.Status,
            item.Priority,
            ToCategory(item.Category),
            item.Targets.Select(target => new AnnouncementTargetResponse(target.Id, target.TargetType, target.TargetValue)).ToList(),
            item.Files.Select(file => ToFileResponse(file, url)).ToList(),
            ToCoverImage(item, url),
            ToImageResponses(item.Images, url),
            item.IsFeatured,
            item.ShowAsPopup,
            item.ShowAsBanner,
            item.RequiresAcknowledgement,
            read is not null,
            read?.AcknowledgedAt is not null,
            read?.ReadAt,
            read?.AcknowledgedAt,
            item.PublishAt,
            item.ExpiresAt,
            item.CreatedAt,
            item.PublishedAt,
            item.UpdatedAt,
            item.CreatedByUser?.FullName,
            item.PublishedByUser?.FullName,
            item.CoverImageUrl,
            item.Tags,
            item.ViewCount,
            item.Reads.Count(read => read.AcknowledgedAt is not null),
            item.NotifyInApp,
            item.NotifyViaLine,
            item.NotificationDispatchStatus,
            item.NotificationDispatchError,
            item.NotificationSentAt,
            item.LineNotificationQueuedAt,
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.InApp),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line && (delivery.LineQueue == null || delivery.LineQueue.Status == "Queued")),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line && delivery.LineQueue != null && delivery.LineQueue.Status == "Sent"),
            item.NotificationDeliveries.Count(delivery => delivery.Channel == AnnouncementNotificationChannels.Line && delivery.LineQueue != null && delivery.LineQueue.Status == "Failed")
        );
    }

    internal static AnnouncementImageResponse? ToCoverImage(Announcement item, IUrlHelper? url)
    {
        var cover = item.Images
            .OrderByDescending(image => image.IsCover)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.CreatedAt)
            .FirstOrDefault(image => image.IsCover);
        return cover is null ? null : ToImageResponse(cover, url);
    }

    internal static IReadOnlyList<AnnouncementImageResponse> ToImageResponses(IEnumerable<AnnouncementImage> images, IUrlHelper? url)
    {
        return images
            .OrderByDescending(image => image.IsCover)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.CreatedAt)
            .Select(image => ToImageResponse(image, url))
            .ToList();
    }

    internal static AnnouncementImageResponse ToImageResponse(AnnouncementImage image, IUrlHelper? url)
    {
        return new AnnouncementImageResponse(
            image.Id,
            BuildImageUrl(url, image.Id, "thumbnail"),
            BuildImageUrl(url, image.Id, "medium"),
            BuildImageUrl(url, image.Id, "large"),
            BuildImageUrl(url, image.Id, "original"),
            image.DisplayOrder,
            image.IsCover,
            image.Width,
            image.Height,
            image.FileSize
        );
    }

    internal static AnnouncementFileResponse ToFileResponse(AnnouncementFile file, IUrlHelper? url)
    {
        return new AnnouncementFileResponse(
            file.Id,
            file.FileName,
            file.OriginalFileName,
            file.ContentType,
            file.FileSize,
            file.FileRole,
            BuildFileUrl(url, file.Id)
        );
    }

    private static string BuildImageUrl(IUrlHelper? url, Guid imageId, string variant)
    {
        return url?.Action(nameof(GetImage), "Announcements", new { imageId, variant }) ?? $"/api/announcements/images/{imageId}/{variant}";
    }

    private static string BuildFileUrl(IUrlHelper? url, Guid fileId)
    {
        return url?.Action(nameof(DownloadFile), "Announcements", new { fileId }) ?? $"/api/announcements/files/{fileId}/download";
    }

    internal static AnnouncementCategoryResponse? ToCategory(AnnouncementCategory? category)
    {
        return category is null
            ? null
            : new AnnouncementCategoryResponse(category.Id, category.Name, category.Description, category.Color, category.IsActive, category.DisplayOrder);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private async Task<bool> CanReadAnnouncementAsync(Announcement announcement, AnnouncementAccess access, CancellationToken cancellationToken)
    {
        if (await HasAnyAdminAnnouncementPermissionAsync(access.UserId, cancellationToken))
        {
            return true;
        }

        var now = DateTime.UtcNow;
        return announcement.Status == AnnouncementStatuses.Published &&
            (announcement.PublishAt == null || announcement.PublishAt <= now) &&
            (announcement.ExpiresAt == null || announcement.ExpiresAt > now) &&
            IsTargetedToUser(announcement, access);
    }

    private async Task<bool> HasAnyAdminAnnouncementPermissionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var permissions = await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .Where(item => item.Permission != null && item.Permission.IsActive)
            .Select(item => item.Permission!.Code)
            .ToListAsync(cancellationToken);

        return permissions.Any(permission => permission is
            AnnouncementPermissions.Manage or
            AnnouncementPermissions.Create or
            AnnouncementPermissions.EditOwn or
            AnnouncementPermissions.EditAll or
            AnnouncementPermissions.Publish or
            AnnouncementPermissions.Archive or
            AnnouncementPermissions.Cancel);
    }
}

[ApiController]
[Route("api/admin/announcements")]
[Authorize]
[RequireAnyPermission(
    AnnouncementPermissions.Manage,
    AnnouncementPermissions.Create,
    AnnouncementPermissions.EditOwn,
    AnnouncementPermissions.EditAll,
    AnnouncementPermissions.Publish,
    AnnouncementPermissions.Schedule,
    AnnouncementPermissions.Archive,
    AnnouncementPermissions.Cancel,
    AnnouncementPermissions.DeleteDraft,
    AnnouncementPermissions.ManageCategories,
    AnnouncementPermissions.ManageTargets,
    AnnouncementPermissions.AnalyticsView,
    AnnouncementPermissions.AnalyticsViewUsers,
    AnnouncementPermissions.NotificationConfigure,
    AnnouncementPermissions.NotificationPreview,
    AnnouncementPermissions.NotificationViewDelivery)]
public class AdminAnnouncementsController(
    AppDbContext db,
    IAuditLogService auditLogService,
    IAnnouncementMediaStorageService mediaStorage,
    IAnnouncementNotificationDispatcher notificationDispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AnnouncementSummaryResponse>>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Announcements
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Reads)
            .Include(item => item.Images)
            .Include(item => item.NotificationDeliveries)
                .ThenInclude(item => item.LineQueue)
            .Include(item => item.CreatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(item => item.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item => item.Title.Contains(keyword) || item.Summary.Contains(keyword));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<AnnouncementSummaryResponse>>.Ok(new PagedResponse<AnnouncementSummaryResponse>(
            items.Select(item => AnnouncementsController.ToSummary(item, GetCurrentUserId() ?? Guid.Empty, Url)).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var item = await LoadAnnouncementAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<AnnouncementDetailResponse>.Fail("ไม่พบประกาศ"));
        }

        return ApiResponse<AnnouncementDetailResponse>.Ok(AnnouncementsController.ToDetail(item, GetCurrentUserId() ?? Guid.Empty, Url));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementCategoryResponse>>>> GetCategories(CancellationToken cancellationToken)
    {
        var items = await db.AnnouncementCategories
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new AnnouncementCategoryResponse(item.Id, item.Name, item.Description, item.Color, item.IsActive, item.DisplayOrder))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<AnnouncementCategoryResponse>>.Ok(items);
    }

    [HttpPost("{announcementId:guid}/images")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AnnouncementImageResponse>>> UploadImage(
        Guid announcementId,
        IFormFile file,
        [FromForm] bool isCover = false,
        [FromForm] int? displayOrder = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var announcement = await db.Announcements
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == announcementId, cancellationToken);
        if (announcement is null || userId is null)
        {
            return NotFound(ApiResponse<AnnouncementImageResponse>.Fail("ไม่พบประกาศ"));
        }

        AnnouncementImage? image = null;
        try
        {
            var order = displayOrder ?? (announcement.Images.Count == 0 ? 1 : announcement.Images.Max(image => image.DisplayOrder) + 1);
            image = await mediaStorage.SaveImageAsync(announcementId, userId.Value, file, isCover, order, cancellationToken);
            if (image.IsCover)
            {
                foreach (var existing in announcement.Images.Where(item => item.IsCover))
                {
                    existing.IsCover = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedByUserId = userId;
                }
            }

            db.AnnouncementImages.Add(image);
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(userId, "Announcement.ImageUploaded", "AnnouncementImage", image.Id.ToString(), $"{announcementId}; {image.OriginalFileName}; {image.MimeType}; {image.FileSize}; Cover={image.IsCover}", httpContext: HttpContext);
            if (image.IsCover)
            {
                await auditLogService.WriteAsync(userId, "Announcement.ImageCoverChanged", "Announcement", announcementId.ToString(), image.Id.ToString(), httpContext: HttpContext);
            }

            return ApiResponse<AnnouncementImageResponse>.Ok(AnnouncementsController.ToImageResponse(image, Url), "อัปโหลดรูปภาพเรียบร้อยแล้ว");
        }
        catch (InvalidOperationException ex)
        {
            if (image is not null)
            {
                await mediaStorage.DeleteImageAsync(image, cancellationToken);
            }

            await auditLogService.WriteAsync(userId, "Announcement.ImageUploaded", "Announcement", announcementId.ToString(), ex.Message, "Failed", HttpContext);
            return BadRequest(ApiResponse<AnnouncementImageResponse>.Fail(ex.Message));
        }
        catch
        {
            if (image is not null)
            {
                await mediaStorage.DeleteImageAsync(image, cancellationToken);
            }

            throw;
        }
    }

    [HttpGet("{announcementId:guid}/images")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementImageResponse>>>> GetAdminImages(Guid announcementId, CancellationToken cancellationToken)
    {
        var announcement = await db.Announcements
            .AsNoTracking()
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == announcementId, cancellationToken);
        if (announcement is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<AnnouncementImageResponse>>.Fail("ไม่พบประกาศ"));
        }

        return ApiResponse<IReadOnlyList<AnnouncementImageResponse>>.Ok(AnnouncementsController.ToImageResponses(announcement.Images, Url));
    }

    [HttpPut("{announcementId:guid}/images/order")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementImageResponse>>>> ReorderImages(
        Guid announcementId,
        AnnouncementImageOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var images = await db.AnnouncementImages
            .Where(item => item.AnnouncementId == announcementId)
            .ToListAsync(cancellationToken);
        var requestedIds = request.Items.Select(item => item.ImageId).ToHashSet();
        if (request.Items.Count == 0 || requestedIds.Count != request.Items.Count || requestedIds.Any(id => images.All(image => image.Id != id)))
        {
            return BadRequest(ApiResponse<IReadOnlyList<AnnouncementImageResponse>>.Fail("รายการรูปภาพไม่ถูกต้อง"));
        }

        foreach (var item in request.Items)
        {
            var image = images.First(image => image.Id == item.ImageId);
            image.DisplayOrder = item.DisplayOrder;
            image.UpdatedAt = DateTime.UtcNow;
            image.UpdatedByUserId = userId;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Announcement.ImageOrderChanged", "Announcement", announcementId.ToString(), $"{request.Items.Count} images", httpContext: HttpContext);
        return ApiResponse<IReadOnlyList<AnnouncementImageResponse>>.Ok(AnnouncementsController.ToImageResponses(images, Url), "จัดลำดับรูปภาพเรียบร้อยแล้ว");
    }

    [HttpDelete("images/{imageId:guid}")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteImage(Guid imageId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var image = await db.AnnouncementImages.FirstOrDefaultAsync(item => item.Id == imageId, cancellationToken);
        if (image is null)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบรูปภาพ"));
        }

        await mediaStorage.DeleteImageAsync(image, cancellationToken);
        db.AnnouncementImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Announcement.ImageDeleted", "AnnouncementImage", imageId.ToString(), $"{image.AnnouncementId}; {image.OriginalFileName}", httpContext: HttpContext);
        return ApiResponse<string>.Ok(imageId.ToString(), "ลบรูปภาพเรียบร้อยแล้ว");
    }

    [HttpPost("{announcementId:guid}/attachments")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnnouncementFileResponse>>>> UploadAttachments(
        Guid announcementId,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var announcementExists = await db.Announcements.AnyAsync(item => item.Id == announcementId, cancellationToken);
        if (!announcementExists || userId is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<AnnouncementFileResponse>>.Fail("ไม่พบประกาศ"));
        }

        if (files.Count == 0)
        {
            return BadRequest(ApiResponse<IReadOnlyList<AnnouncementFileResponse>>.Fail("กรุณาเลือกไฟล์แนบ"));
        }

        var savedFiles = new List<AnnouncementFile>();
        try
        {
            foreach (var file in files)
            {
                var saved = await mediaStorage.SaveAttachmentAsync(announcementId, userId.Value, file, cancellationToken);
                savedFiles.Add(saved);
                db.AnnouncementFiles.Add(saved);
            }

            await db.SaveChangesAsync(cancellationToken);
            foreach (var saved in savedFiles)
            {
                await auditLogService.WriteAsync(userId, "Announcement.FileUploaded", "AnnouncementFile", saved.Id.ToString(), $"{announcementId}; {saved.OriginalFileName}; {saved.ContentType}; {saved.FileSize}", httpContext: HttpContext);
            }

            return ApiResponse<IReadOnlyList<AnnouncementFileResponse>>.Ok(savedFiles.Select(file => AnnouncementsController.ToFileResponse(file, Url)).ToList(), "อัปโหลดไฟล์แนบเรียบร้อยแล้ว");
        }
        catch (InvalidOperationException ex)
        {
            foreach (var saved in savedFiles)
            {
                await mediaStorage.DeleteAttachmentAsync(saved, cancellationToken);
            }

            await auditLogService.WriteAsync(userId, "Announcement.FileUploaded", "Announcement", announcementId.ToString(), ex.Message, "Failed", HttpContext);
            return BadRequest(ApiResponse<IReadOnlyList<AnnouncementFileResponse>>.Fail(ex.Message));
        }
        catch
        {
            foreach (var saved in savedFiles)
            {
                await mediaStorage.DeleteAttachmentAsync(saved, cancellationToken);
            }

            throw;
        }
    }

    [HttpDelete("files/{fileId:guid}")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteFile(Guid fileId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var file = await db.AnnouncementFiles.FirstOrDefaultAsync(item => item.Id == fileId, cancellationToken);
        if (file is null)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบไฟล์แนบ"));
        }

        await mediaStorage.DeleteAttachmentAsync(file, cancellationToken);
        db.AnnouncementFiles.Remove(file);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Announcement.FileDeleted", "AnnouncementFile", fileId.ToString(), $"{file.AnnouncementId}; {file.OriginalFileName}", httpContext: HttpContext);
        return ApiResponse<string>.Ok(fileId.ToString(), "ลบไฟล์แนบเรียบร้อยแล้ว");
    }

    [HttpPost]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.Create)]
    public async Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Create(CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request.Title, request.Body, request.Priority);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<AnnouncementDetailResponse>.Fail(validation));
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<AnnouncementDetailResponse>.Fail("Invalid access token."));
        }

        var channelValidation = await ValidateNotificationChannelsAsync(request.NotifyInApp ?? true, request.NotifyViaLine ?? false, userId.Value, cancellationToken);
        if (channelValidation is not null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AnnouncementDetailResponse>.Fail(channelValidation));
        }

        var item = new Announcement
        {
            Title = request.Title.Trim(),
            Summary = NormalizeSummary(request.Summary, request.Body),
            Body = request.Body.Trim(),
            Priority = request.Priority,
            CategoryId = request.CategoryId,
            CreatedByUserId = userId.Value,
            PublishAt = request.PublishAt,
            ExpiresAt = request.ExpiresAt,
            IsFeatured = request.IsFeatured,
            ShowAsPopup = request.ShowAsPopup,
            ShowAsBanner = request.ShowAsBanner,
            RequiresAcknowledgement = request.RequiresAcknowledgement,
            Tags = NormalizeTags(request.Tags),
            NotifyInApp = request.NotifyInApp ?? true,
            NotifyViaLine = request.NotifyViaLine ?? false,
            NotificationDispatchStatus = "Pending",
            Status = request.PublishAt is not null && request.PublishAt > DateTime.UtcNow ? AnnouncementStatuses.Scheduled : AnnouncementStatuses.Draft
        };
        ReplaceTargets(item, request.Targets);
        db.Announcements.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Announcement.Created", "Announcement", item.Id.ToString(), item.Title, httpContext: HttpContext);

        var created = await LoadAnnouncementAsync(item.Id, cancellationToken);
        return CreatedAtAction(nameof(GetDetail), new { id = item.Id }, ApiResponse<AnnouncementDetailResponse>.Ok(AnnouncementsController.ToDetail(created!, userId.Value, Url), "บันทึกประกาศเรียบร้อยแล้ว"));
    }

    [HttpPut("{id:guid}")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.EditOwn, AnnouncementPermissions.EditAll)]
    public async Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Update(Guid id, UpdateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request.Title, request.Body, request.Priority);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<AnnouncementDetailResponse>.Fail(validation));
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<AnnouncementDetailResponse>.Fail("Invalid access token."));
        }

        var item = await db.Announcements
            .Include(item => item.Targets)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<AnnouncementDetailResponse>.Fail("ไม่พบประกาศ"));
        }

        if (item.Status is AnnouncementStatuses.Archived or AnnouncementStatuses.Cancelled)
        {
            return BadRequest(ApiResponse<AnnouncementDetailResponse>.Fail("ไม่สามารถแก้ไขประกาศที่ปิดรายการแล้ว"));
        }

        var channelValidation = await ValidateNotificationChannelsAsync(request.NotifyInApp ?? true, request.NotifyViaLine ?? false, userId.Value, cancellationToken);
        if (channelValidation is not null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AnnouncementDetailResponse>.Fail(channelValidation));
        }

        item.Title = request.Title.Trim();
        item.Summary = NormalizeSummary(request.Summary, request.Body);
        item.Body = request.Body.Trim();
        item.Priority = request.Priority;
        item.CategoryId = request.CategoryId;
        item.PublishAt = request.PublishAt;
        item.ExpiresAt = request.ExpiresAt;
        item.IsFeatured = request.IsFeatured;
        item.ShowAsPopup = request.ShowAsPopup;
        item.ShowAsBanner = request.ShowAsBanner;
        item.RequiresAcknowledgement = request.RequiresAcknowledgement;
        item.Tags = NormalizeTags(request.Tags);
        var previousNotifyInApp = item.NotifyInApp;
        var previousNotifyViaLine = item.NotifyViaLine;
        item.NotifyInApp = request.NotifyInApp ?? true;
        item.NotifyViaLine = request.NotifyViaLine ?? false;
        if (previousNotifyInApp != item.NotifyInApp || previousNotifyViaLine != item.NotifyViaLine)
        {
            item.NotificationConfigVersion += 1;
            item.NotificationDispatchStatus = item.Status == AnnouncementStatuses.Published ? "Pending" : item.NotificationDispatchStatus;
            item.NotificationDispatchError = null;
        }
        item.UpdatedByUserId = userId;
        item.UpdatedAt = DateTime.UtcNow;
        ReplaceTargets(item, request.Targets);

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Announcement.Updated", "Announcement", item.Id.ToString(), item.Title, httpContext: HttpContext);

        var updated = await LoadAnnouncementAsync(item.Id, cancellationToken);
        return ApiResponse<AnnouncementDetailResponse>.Ok(AnnouncementsController.ToDetail(updated!, userId.Value, Url), "บันทึกประกาศเรียบร้อยแล้ว");
    }

    [HttpPost("{id:guid}/publish")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.Publish)]
    public Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Publish(Guid id, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(id, AnnouncementStatuses.Published, "Announcement.Published", cancellationToken);
    }

    [HttpPost("{id:guid}/notification-preview")]
    [RequireAnyPermission(AnnouncementPermissions.NotificationPreview, AnnouncementPermissions.Manage, AnnouncementPermissions.Publish)]
    public async Task<ActionResult<ApiResponse<AnnouncementNotificationPreviewResponse>>> PreviewNotification(
        Guid id,
        AnnouncementNotificationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var item = await LoadAnnouncementAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<AnnouncementNotificationPreviewResponse>.Fail("ไม่พบประกาศ"));
        }

        var preview = await notificationDispatcher.PreviewAsync(item, request.NotifyInApp, request.NotifyViaLine, cancellationToken);
        await auditLogService.WriteAsync(GetCurrentUserId(), "Announcement.NotificationPreviewed", "Announcement", id.ToString(), $"InApp={preview.InAppRecipientCount}; Line={preview.LineBoundRecipientCount}", httpContext: HttpContext);
        return ApiResponse<AnnouncementNotificationPreviewResponse>.Ok(preview);
    }

    [HttpPost("{id:guid}/unpublish")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.Publish)]
    public Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(id, AnnouncementStatuses.Draft, "Announcement.Unpublished", cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.Archive)]
    public Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Archive(Guid id, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(id, AnnouncementStatuses.Archived, "Announcement.Archived", cancellationToken);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.Cancel)]
    public Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(id, AnnouncementStatuses.Cancelled, "Announcement.Cancelled", cancellationToken);
    }

    [HttpPost("{id:guid}/duplicate")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.Create)]
    public async Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var source = await LoadAnnouncementAsync(id, cancellationToken);
        if (source is null || userId is null)
        {
            return NotFound(ApiResponse<AnnouncementDetailResponse>.Fail("ไม่พบประกาศ"));
        }

        var item = new Announcement
        {
            Title = $"{source.Title} (สำเนา)",
            Summary = source.Summary,
            Body = source.Body,
            Priority = source.Priority,
            CategoryId = source.CategoryId,
            CreatedByUserId = userId.Value,
            IsFeatured = source.IsFeatured,
            ShowAsPopup = source.ShowAsPopup,
            ShowAsBanner = source.ShowAsBanner,
            RequiresAcknowledgement = source.RequiresAcknowledgement,
            Tags = source.Tags,
            Status = AnnouncementStatuses.Draft,
            Targets = source.Targets.Select(target => new AnnouncementTarget
            {
                TargetType = target.TargetType,
                TargetValue = target.TargetValue
            }).ToList()
        };
        db.Announcements.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Announcement.Duplicated", "Announcement", item.Id.ToString(), $"Duplicated from {id}", httpContext: HttpContext);

        var duplicated = await LoadAnnouncementAsync(item.Id, cancellationToken);
        return ApiResponse<AnnouncementDetailResponse>.Ok(AnnouncementsController.ToDetail(duplicated!, userId.Value, Url), "คัดลอกประกาศเรียบร้อยแล้ว");
    }

    [HttpDelete("{id:guid}")]
    [RequireAnyPermission(AnnouncementPermissions.Manage, AnnouncementPermissions.DeleteDraft)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteDraft(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Announcements
            .Include(item => item.Images)
            .Include(item => item.Files)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบประกาศ"));
        }

        foreach (var image in item.Images.ToList())
        {
            await mediaStorage.DeleteImageAsync(image, cancellationToken);
        }

        foreach (var file in item.Files.ToList())
        {
            await mediaStorage.DeleteAttachmentAsync(file, cancellationToken);
        }

        db.Announcements.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(GetCurrentUserId(), "Announcement.Deleted", "Announcement", id.ToString(), $"{item.Status}; {item.Title}", httpContext: HttpContext);
        return ApiResponse<string>.Ok(id.ToString(), "ลบประกาศเรียบร้อยแล้ว");
    }

    private async Task<ActionResult<ApiResponse<AnnouncementDetailResponse>>> ChangeStatusAsync(Guid id, string status, string auditAction, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var item = await db.Announcements.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<AnnouncementDetailResponse>.Fail("ไม่พบประกาศ"));
        }

        var wasPublished = item.Status == AnnouncementStatuses.Published;
        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByUserId = userId;
        if (status == AnnouncementStatuses.Published)
        {
            item.PublishedAt ??= DateTime.UtcNow;
            item.PublishedByUserId = userId;
            item.PublishAt ??= DateTime.UtcNow;
        }
        else if (status == AnnouncementStatuses.Archived)
        {
            item.ArchivedAt = DateTime.UtcNow;
        }
        else if (status == AnnouncementStatuses.Cancelled)
        {
            item.CancelledAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, auditAction, "Announcement", id.ToString(), item.Title, httpContext: HttpContext);

        var updated = await LoadAnnouncementAsync(item.Id, cancellationToken);
        if (status == AnnouncementStatuses.Published && updated is not null && (!wasPublished || updated.NotificationDispatchStatus is null or "Pending"))
        {
            try
            {
                await notificationDispatcher.DispatchAsync(updated, userId, cancellationToken);
                updated = await LoadAnnouncementAsync(item.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                item.NotificationDispatchStatus = "Failed";
                item.NotificationDispatchError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                await db.SaveChangesAsync(cancellationToken);
                await auditLogService.WriteAsync(userId, "Announcement.NotificationDispatchFailed", "Announcement", id.ToString(), item.NotificationDispatchError, "Failed", HttpContext);
            }
        }

        return ApiResponse<AnnouncementDetailResponse>.Ok(AnnouncementsController.ToDetail(updated!, userId ?? Guid.Empty, Url), "อัปเดตสถานะประกาศเรียบร้อยแล้ว");
    }

    private async Task<Announcement?> LoadAnnouncementAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Announcements
            .AsSplitQuery()
            .Include(item => item.Category)
            .Include(item => item.Targets)
            .Include(item => item.Files)
            .Include(item => item.Images)
            .Include(item => item.Reads)
            .Include(item => item.NotificationDeliveries)
                .ThenInclude(item => item.LineQueue)
            .Include(item => item.CreatedByUser)
            .Include(item => item.PublishedByUser)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    private static string? Validate(string title, string body, string priority)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "กรุณาระบุหัวข้อประกาศ";
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return "กรุณาระบุเนื้อหาประกาศ";
        }

        if (priority is not (AnnouncementPriorities.Normal or AnnouncementPriorities.Important or AnnouncementPriorities.Critical))
        {
            return "ระดับความสำคัญไม่ถูกต้อง";
        }

        return null;
    }

    private async Task<string?> ValidateNotificationChannelsAsync(bool notifyInApp, bool notifyViaLine, Guid userId, CancellationToken cancellationToken)
    {
        if (!notifyInApp && !notifyViaLine)
        {
            return null;
        }

        if (notifyInApp && !await HasPermissionAsync(userId, AnnouncementPermissions.NotificationSendInApp, cancellationToken))
        {
            return "คุณไม่มีสิทธิ์ส่ง Notification Bell สำหรับประกาศ";
        }

        if (notifyViaLine && !await HasPermissionAsync(userId, AnnouncementPermissions.NotificationSendLine, cancellationToken))
        {
            return "คุณไม่มีสิทธิ์ส่ง LINE สำหรับประกาศ";
        }

        return null;
    }

    private async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken)
    {
        return await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode, cancellationToken);
    }

    private static string NormalizeSummary(string? summary, string body)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary.Trim();
        }

        var normalized = body.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return normalized.Length <= 180 ? normalized : $"{normalized[..180]}...";
    }

    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        var normalized = string.Join(", ",
            tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.StartsWith('#') ? tag[1..] : tag)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20));

        return normalized.Length <= 1000 ? normalized : normalized[..1000];
    }

    private static void ReplaceTargets(Announcement item, IReadOnlyList<AnnouncementTargetRequest>? targets)
    {
        item.Targets.Clear();
        if (targets is null || targets.Count == 0)
        {
            item.Targets.Add(new AnnouncementTarget { TargetType = AnnouncementTargetTypes.Everyone });
            return;
        }

        foreach (var target in targets)
        {
            item.Targets.Add(new AnnouncementTarget
            {
                TargetType = string.IsNullOrWhiteSpace(target.TargetType) ? AnnouncementTargetTypes.Everyone : target.TargetType.Trim(),
                TargetValue = string.IsNullOrWhiteSpace(target.TargetValue) ? null : target.TargetValue.Trim()
            });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

public sealed record AnnouncementAccess(
    Guid UserId,
    Guid? DepartmentId,
    HashSet<string> Roles,
    HashSet<string> Permissions
);
