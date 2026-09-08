namespace Hop.Api.Models;

public class DomainEventRecord
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? ActorUserId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = "PENDING";
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DomainEventRecord? DomainEvent { get; set; }
    public ICollection<NotificationDelivery> Deliveries { get; set; } = [];
}

public class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid OutboxMessageId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string Channel { get; set; } = "IN_APP";
    public string Status { get; set; } = "PENDING";
    public string IdempotencyKey { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public OutboxMessage? OutboxMessage { get; set; }
    public User? RecipientUser { get; set; }
}
