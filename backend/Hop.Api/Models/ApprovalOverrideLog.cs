namespace Hop.Api.Models;

public class ApprovalOverrideLog
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public Guid? OriginalApproverId { get; set; }
    public Guid OverrideByUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LeaveRequest? LeaveRequest { get; set; }
    public User? OriginalApprover { get; set; }
    public User? OverrideByUser { get; set; }
}
