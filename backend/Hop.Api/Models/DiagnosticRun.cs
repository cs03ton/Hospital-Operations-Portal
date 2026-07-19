namespace Hop.Api.Models;

public class DiagnosticRun
{
    public Guid Id { get; set; }
    public string DiagnosticType { get; set; } = string.Empty;
    public string Status { get; set; } = DiagnosticStatuses.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ResultSummary { get; set; }
    public string? ReferenceId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? CreatedByUser { get; set; }
}

public static class DiagnosticStatuses
{
    public const string Running = "Running";
    public const string Healthy = "Healthy";
    public const string Warning = "Warning";
    public const string Unhealthy = "Unhealthy";
    public const string Failed = "Failed";
}
