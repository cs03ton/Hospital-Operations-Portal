namespace Hop.Api.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Category { get; set; } = "Leave";
    public string NotificationType { get; set; } = "Information";
    public string Priority { get; set; } = "Information";
    public string? TargetRole { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ReferenceEntity { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
