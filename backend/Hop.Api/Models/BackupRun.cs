namespace Hop.Api.Models;

public class BackupRun
{
    public Guid Id { get; set; }
    public string BackupType { get; set; } = BackupTypes.Database;
    public string Status { get; set; } = BackupStatuses.Success;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Checksum { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public User? VerifiedByUser { get; set; }
    public User? DeletedByUser { get; set; }
}

public static class BackupTypes
{
    public const string Database = "Database";
    public const string Storage = "Storage";
    public const string Full = "Full";
}

public static class BackupStatuses
{
    public const string Running = "Running";
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Verified = "Verified";
    public const string VerificationFailed = "VerificationFailed";
    public const string Deleted = "Deleted";
}
