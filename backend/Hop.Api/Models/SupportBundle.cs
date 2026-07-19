namespace Hop.Api.Models;

public class SupportBundle
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Checksum { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = SupportBundleStatuses.Available;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DownloadedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User? CreatedByUser { get; set; }
}

public static class SupportBundleStatuses
{
    public const string Available = "Available";
    public const string Expired = "Expired";
    public const string Deleted = "Deleted";
}
