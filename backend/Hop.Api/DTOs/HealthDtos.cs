namespace Hop.Api.DTOs;

public record AdminHealthResponse(
    string OverallStatus,
    DateTimeOffset CheckedAt,
    HealthComponentResponse Api,
    HealthComponentResponse Database,
    StorageHealthResponse Storage,
    LineHealthResponse Line,
    QueueHealthResponse Queue,
    DiskHealthResponse Disk,
    MemoryHealthResponse Memory,
    CpuHealthResponse Cpu,
    BackupHealthResponse Backup,
    string Version,
    string Environment,
    DateTime CurrentTimeServer,
    string? GitCommit = null,
    string? Timezone = null
);

public record HealthComponentResponse(
    string Status,
    string? Message = null,
    long? LatencyMs = null,
    long? UptimeSeconds = null,
    string? Provider = null
);

public record StorageHealthResponse(
    string Status,
    bool Writable,
    string? Message = null,
    string? Path = null
);

public record LineHealthResponse(
    string Status,
    bool Enabled,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    string? Message = null,
    bool HasAccessToken = false,
    bool HasChannelSecret = false,
    string? LastError = null
);

public record QueueHealthResponse(
    string Status,
    bool LineRetryEnabled,
    bool ApprovalEscalationEnabled,
    int PendingLineDeliveries,
    int FailedLineDeliveries,
    int PendingRetries,
    DateTime? LastLineSuccessAt,
    DateTime? LastLineFailureAt,
    string? Message = null
);

public record DiskHealthResponse(
    string Status,
    double? UsedPercent,
    string? Message = null,
    double? TotalGb = null,
    double? UsedGb = null,
    double? FreeGb = null
);

public record MemoryHealthResponse(
    string Status,
    double? TotalMb,
    double? UsedMb,
    double? AvailableMb,
    double? UsedPercent,
    string? Message = null
);

public record CpuHealthResponse(
    string Status,
    int ProcessorCount,
    string? LoadAverage,
    string? Message = null
);

public record BackupHealthResponse(
    string Status,
    DateTime? LastBackupAt,
    string? Message = null,
    DateTime? LastRestoreTestAt = null,
    string? BackupDirectory = null,
    long? LatestBackupSizeBytes = null,
    string? LatestBackupFile = null
);

public record SafeErrorResponse(
    string Message,
    string ReferenceId
);
