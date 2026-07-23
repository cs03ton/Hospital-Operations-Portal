namespace Hop.Api.Models;

public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = AnnouncementStatuses.Draft;
    public string Priority { get; set; } = AnnouncementPriorities.Normal;
    public Guid? CategoryId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTime? PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool IsFeatured { get; set; }
    public bool ShowAsPopup { get; set; }
    public bool ShowAsBanner { get; set; }
    public bool RequiresAcknowledgement { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Tags { get; set; }
    public int ViewCount { get; set; }
    public bool NotifyInApp { get; set; } = true;
    public bool NotifyViaLine { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public DateTime? LineNotificationQueuedAt { get; set; }
    public string? NotificationDispatchStatus { get; set; }
    public string? NotificationDispatchError { get; set; }
    public int NotificationConfigVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public AnnouncementCategory? Category { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? PublishedByUser { get; set; }
    public ICollection<AnnouncementTarget> Targets { get; set; } = [];
    public ICollection<AnnouncementFile> Files { get; set; } = [];
    public ICollection<AnnouncementImage> Images { get; set; } = [];
    public ICollection<AnnouncementRead> Reads { get; set; } = [];
    public ICollection<AnnouncementNotificationDelivery> NotificationDeliveries { get; set; } = [];
}
