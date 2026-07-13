namespace Hop.Api.DTOs;

public record BackupRunResponse(
    Guid Id,
    string BackupType,
    string Status,
    string FileName,
    string RelativePath,
    long FileSizeBytes,
    string? Checksum,
    DateTime StartedAt,
    DateTime? CompletedAt,
    long? DurationMs,
    string? ErrorMessage,
    string? CreatedBy,
    DateTime? VerifiedAt,
    string? VerifiedBy,
    DateTime? DeletedAt);

public record BackupRunDetailResponse(
    BackupRunResponse Backup,
    string LogSummary,
    bool CanRestore,
    bool IsVerified,
    IReadOnlyList<string> RestoreWarnings,
    IReadOnlyList<string> RestoreErrors);

public record BackupOverviewResponse(
    BackupRunResponse? LastSuccessfulBackup,
    BackupRunResponse? LastFailedBackup,
    BackupRunResponse? LastVerifiedBackup,
    RestoreRunResponse? LastRestoreTest,
    long TotalBackupSizeBytes,
    string BackupRoot,
    BackupRetentionPolicyResponse RetentionPolicy);

public record BackupQuery(
    int Page = 1,
    int PageSize = 20,
    string? Type = null,
    string? Status = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null,
    string? Sort = null,
    string? Direction = null);

public record RestoreRunResponse(
    Guid Id,
    Guid BackupRunId,
    string BackupFileName,
    string RestoreType,
    string TargetEnvironment,
    string? TargetDatabase,
    string Status,
    string Reason,
    DateTime StartedAt,
    DateTime? CompletedAt,
    long? DurationMs,
    string? ErrorMessage,
    string? CreatedBy,
    string ConfirmationMethod,
    Guid? PreRestoreBackupRunId);

public record RestorePreviewResponse(
    bool CanRestore,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    BackupRunResponse BackupInfo,
    string CurrentEnvironment,
    string RecommendedMode,
    long? FreeDiskBytes);

public record RestoreRequest(
    string ConfirmationText,
    string Reason,
    bool RestoreDatabase = true,
    bool RestoreStorage = false,
    string RestoreMode = "TestDatabase",
    string? TargetDatabase = null);

public record BackupVerificationResponse(
    Guid BackupId,
    string Status,
    string Message,
    string? Checksum,
    DateTime VerifiedAt);

public record BackupRetentionPolicyResponse(
    int DailyDays,
    int WeeklyWeeks,
    int MonthlyMonths,
    bool KeepVerified,
    int KeepFailedDays);

public record RetentionPreviewItemResponse(
    Guid BackupId,
    string FileName,
    DateTime CreatedAt,
    string Type,
    string Status,
    string Action,
    string Reason,
    long FileSizeBytes);

public record RetentionPreviewResponse(
    int TotalFiles,
    int Keep,
    int Delete,
    long FreedBytes,
    IReadOnlyList<RetentionPreviewItemResponse> Items);

public record ApplyRetentionRequest(
    string Reason,
    string ConfirmationText);

public record ApplyRetentionResponse(
    int DeletedCount,
    long FreedBytes,
    IReadOnlyList<RetentionPreviewItemResponse> Items);
