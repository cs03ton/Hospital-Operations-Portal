namespace Hop.Api.Models;

public class AnnouncementNotificationDelivery
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public Guid UserId { get; set; }
    public string Channel { get; set; } = AnnouncementNotificationChannels.InApp;
    public string Status { get; set; } = AnnouncementNotificationDeliveryStatuses.Queued;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessageSanitized { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid? LineQueueId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Announcement? Announcement { get; set; }
    public User? User { get; set; }
    public Notification? Notification { get; set; }
    public LineDeliveryLog? LineQueue { get; set; }
}

public static class AnnouncementNotificationChannels
{
    public const string InApp = "InApp";
    public const string Line = "Line";
}

public static class AnnouncementNotificationDeliveryStatuses
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
}
