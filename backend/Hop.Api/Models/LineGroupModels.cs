namespace Hop.Api.Models;

public static class LineGroupDestinationStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Disabled = "Disabled";
}

public sealed class LineGroupDestination
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LineGroupId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "กลุ่ม LINE ที่ตรวจพบ";
    public string Status { get; set; } = LineGroupDestinationStatuses.Pending;
    public string Module { get; set; } = "FLEET";
    public string DeliveryProvider { get; set; } = "LINE_MESSAGING_API";
    public string? EndpointUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecretProtected { get; set; }
    public bool AttentionRequired { get; set; }
    public string? AttentionReason { get; set; }
    public DateTime FirstDetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastDetectedAt { get; set; } = DateTime.UtcNow;
    public Guid? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? DisabledByUserId { get; set; }
    public DateTime? DisabledAt { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public ICollection<LineGroupEventSubscription> EventSubscriptions { get; set; } = [];
}

public sealed class LineGroupEventSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DestinationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public LineGroupDestination? Destination { get; set; }
}

public sealed class LineWebhookInbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WebhookEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? SourceGroupId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LineGroupDeliveryLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid DestinationId { get; set; }
    public string CanonicalEventType { get; set; } = string.Empty;
    public string SourceEventType { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public string Status { get; set; } = "Pending";
    public string DeduplicationKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? MessageText { get; set; }
    public int AttemptCount { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public LineGroupDestination? Destination { get; set; }
}

public static class FleetLineGroupEvents
{
    public static readonly IReadOnlyDictionary<string, bool> Defaults = new Dictionary<string, bool>
    {
        ["Fleet.RequestSubmitted"] = true,
        ["Fleet.AssignmentCreated"] = true,
        ["Fleet.AdminReviewed"] = true,
        ["Fleet.Returned"] = true,
        ["Fleet.DirectorApproved"] = true,
        ["Fleet.Rejected"] = true,
        ["Fleet.Cancelled"] = true,
        ["Fleet.AssignmentChanged"] = true,
        ["Fleet.DriverAcknowledged"] = true,
        ["Fleet.TripCompleted"] = true,
        ["Fleet.TripOverdue"] = true
    };
}
