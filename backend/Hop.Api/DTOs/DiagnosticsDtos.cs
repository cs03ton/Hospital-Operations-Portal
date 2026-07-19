namespace Hop.Api.DTOs;

public record DiagnosticsSummaryResponse(
    DateTimeOffset CheckedAt,
    string Environment,
    string Version,
    string? GitCommit,
    IReadOnlyDictionary<string, DiagnosticServiceStatusResponse> Services,
    IReadOnlyList<RecentErrorResponse> RecentErrors,
    DiagnosticInfoResponse LastDeploy,
    DiagnosticInfoResponse LastMigration);

public record DiagnosticServiceStatusResponse(
    string Key,
    string Label,
    string Status,
    string? Message = null,
    long? LatencyMs = null,
    IReadOnlyDictionary<string, string?>? Details = null);

public record DiagnosticInfoResponse(
    string Status,
    string? Message = null,
    DateTime? Timestamp = null,
    string? Reference = null);

public record DiagnosticTestResultResponse(
    Guid RunId,
    string DiagnosticType,
    string Status,
    string Message,
    string ReferenceId,
    long DurationMs);

public record DiagnosticRunResponse(
    Guid Id,
    string DiagnosticType,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    long? DurationMs,
    string? ResultSummary,
    string? ReferenceId,
    string? ErrorMessage,
    string? CreatedBy);

public record RecentErrorResponse(
    DateTime Timestamp,
    string Module,
    string Message,
    string? ReferenceId,
    string? Actor,
    string? RequestPath,
    string? StatusCode);

public record DiagnosticsLogQuery(
    string? Source = null,
    string? Severity = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 100);

public record DiagnosticsLogResponse(
    string Source,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<DiagnosticsLogLineResponse> Items);

public record DiagnosticsLogLineResponse(
    DateTime? Timestamp,
    string Severity,
    string Message);

public record SupportBundleRequest(
    bool IncludeAppLogs = true,
    bool IncludeNginxLogs = true,
    bool IncludePostgresLogs = false,
    bool IncludeHealth = true,
    bool IncludeDeployInfo = true,
    bool IncludeMigrationInfo = true,
    bool IncludeLineSummary = true,
    bool IncludeBackupSummary = true,
    int TimeRangeHours = 24,
    string Reason = "");

public record SupportBundleResponse(
    Guid Id,
    string FileName,
    long FileSizeBytes,
    string? Checksum,
    DateTime ExpiresAt,
    string Status,
    DateTime CreatedAt,
    string DownloadUrl);

public record SupportBundleHistoryResponse(
    Guid Id,
    string FileName,
    long FileSizeBytes,
    string? Checksum,
    DateTime ExpiresAt,
    string Reason,
    string Status,
    string? CreatedBy,
    DateTime CreatedAt,
    DateTime? DownloadedAt,
    DateTime? DeletedAt);
