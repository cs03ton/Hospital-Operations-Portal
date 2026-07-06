namespace Hop.Api.Models;

public class LineConnectToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public string? LineUserId { get; set; }
    public string? Metadata { get; set; }

    public User? User { get; set; }
}
