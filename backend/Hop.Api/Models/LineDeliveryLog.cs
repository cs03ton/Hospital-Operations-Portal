namespace Hop.Api.Models;

public class LineDeliveryLog
{
    public Guid Id { get; set; }
    public Guid? LeaveRequestId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string Payload { get; set; } = string.Empty;
    public string? ResponseDetail { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }
    public User? RecipientUser { get; set; }
}
