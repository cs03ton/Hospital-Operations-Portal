using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface IAnnouncementMediaStorageService
{
    Task<AnnouncementImage> SaveImageAsync(
        Guid announcementId,
        Guid uploadedByUserId,
        IFormFile file,
        bool isCover,
        int displayOrder,
        CancellationToken cancellationToken = default);

    Task<AnnouncementFile> SaveAttachmentAsync(
        Guid announcementId,
        Guid uploadedByUserId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<FileInfo> OpenImageAsync(AnnouncementImage image, string variant, CancellationToken cancellationToken = default);
    Task<FileInfo> OpenAttachmentAsync(AnnouncementFile file, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(AnnouncementImage image, CancellationToken cancellationToken = default);
    Task DeleteAttachmentAsync(AnnouncementFile file, CancellationToken cancellationToken = default);
    Task GenerateImageVariantsAsync(AnnouncementImage image, CancellationToken cancellationToken = default);
}
