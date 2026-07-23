namespace Hop.Api.Models;

public class AnnouncementFile
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileRole { get; set; } = "Attachment";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public Announcement? Announcement { get; set; }
    public User? CreatedByUser { get; set; }
}
