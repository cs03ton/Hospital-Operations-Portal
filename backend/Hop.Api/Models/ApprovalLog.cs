namespace Hop.Api.Models;

public class ApprovalLog
{
    public Guid Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public Guid? ApproverId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
