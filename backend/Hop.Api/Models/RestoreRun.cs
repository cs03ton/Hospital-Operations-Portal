namespace Hop.Api.Models;

public class RestoreRun
{
    public Guid Id { get; set; }
    public Guid BackupRunId { get; set; }
    public string RestoreType { get; set; } = RestoreTypes.Database;
    public string TargetEnvironment { get; set; } = string.Empty;
    public string? TargetDatabase { get; set; }
    public string Status { get; set; } = RestoreStatuses.Previewed;
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string ConfirmationMethod { get; set; } = string.Empty;
    public Guid? PreRestoreBackupRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public BackupRun? BackupRun { get; set; }
    public BackupRun? PreRestoreBackupRun { get; set; }
    public User? CreatedByUser { get; set; }
}

public static class RestoreTypes
{
    public const string Database = "Database";
    public const string Storage = "Storage";
    public const string Full = "Full";
    public const string TestDatabase = "TestDatabase";
}

public static class RestoreStatuses
{
    public const string Previewed = "Previewed";
    public const string Running = "Running";
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}
