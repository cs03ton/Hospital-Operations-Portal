namespace Hop.Api.DTOs;

public sealed record FleetHealthMetric(string Key, string Label, long Value, string Severity, string? ActionUrl = null);
public sealed record FleetHealthIssue(string Code, string Severity, string Message, string EntityType, string? EntityId, DateTime DetectedAt, string? ActionUrl);
public sealed record FleetHealthResponse(string Status, DateTime GeneratedAt, IReadOnlyList<FleetHealthMetric> Metrics, int WarningCount, int CriticalCount);
public sealed record FleetOutboxHealthItem(Guid Id, Guid EventId, string EventType, string Status, int AttemptCount, string? LastError, DateTime CreatedAt, DateTime AvailableAt, DateTime? ProcessedAt, int InAppFailed, int LineFailed, int InAppRetry, int LineRetry);
public sealed record FleetPermissionDiagnostic(string Status, IReadOnlyList<string> MissingDefinitions, IReadOnlyList<string> DuplicateDefinitions, IReadOnlyList<FleetRolePermissionDiagnostic> Roles, bool DirectUserPermissionsSupported);
public sealed record FleetRolePermissionDiagnostic(string RoleName, IReadOnlyList<string> FleetPermissions, IReadOnlyList<string> UnexpectedPermissions);
public sealed record FleetDiagnosticsSummary(string RolloutMode, bool CurrentUserAllowed, string HealthStatus, long PendingOutbox, long FailedOutbox, long FailedDeliveries, long StuckWorkflows, DateTime GeneratedAt);
