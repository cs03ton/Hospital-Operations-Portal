namespace Hop.Api.DTOs;

public record AdminHealthResponse(
    HealthComponentResponse Api,
    HealthComponentResponse Database,
    StorageHealthResponse Storage,
    LineHealthResponse Line,
    DiskHealthResponse Disk,
    BackupHealthResponse Backup,
    string Version,
    string Environment,
    DateTime CurrentTimeServer
);

public record HealthComponentResponse(
    string Status,
    string? Message = null,
    long? LatencyMs = null
);

public record StorageHealthResponse(
    string Status,
    bool Writable,
    string? Message = null
);

public record LineHealthResponse(
    string Status,
    bool Enabled,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    string? Message = null
);

public record DiskHealthResponse(
    string Status,
    double? UsedPercent,
    string? Message = null
);

public record BackupHealthResponse(
    string Status,
    DateTime? LastBackupAt,
    string? Message = null
);

public record SafeErrorResponse(
    string Message,
    string ReferenceId
);
