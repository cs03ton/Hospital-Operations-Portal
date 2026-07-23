namespace Hop.Api.Configuration;

public sealed class AnnouncementStorageOptions
{
    public const string SectionName = "Storage:Announcements";

    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;
    public long MaxAttachmentSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedImageExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
    public string[] AllowedAttachmentExtensions { get; set; } = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip"];
    public int ThumbnailMaxWidth { get; set; } = 400;
    public int ThumbnailMaxHeight { get; set; } = 225;
    public int MediumMaxSize { get; set; } = 800;
    public int LargeMaxSize { get; set; } = 1600;
    public int MaximumWidth { get; set; } = 8000;
    public int MaximumHeight { get; set; } = 8000;
    public long MaximumPixelCount { get; set; } = 40_000_000;
}
