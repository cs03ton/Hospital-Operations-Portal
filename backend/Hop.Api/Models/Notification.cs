namespace Hop.Api.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
