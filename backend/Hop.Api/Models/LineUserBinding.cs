namespace Hop.Api.Models;

public class LineUserBinding
{
    public Guid Id { get; set; }
    public string LineUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? PictureUrl { get; set; }
    public Guid? UserId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? LastEventType { get; set; }
    public DateTime? LastEventAt { get; set; }
    public DateTime? BoundAt { get; set; }
    public DateTime? UnboundAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
}
