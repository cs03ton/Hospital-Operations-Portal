namespace Hop.Api.Models;

public class AnnouncementImage
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string? LargePath { get; set; }
    public string? MediumPath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCover { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public Announcement? Announcement { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}
