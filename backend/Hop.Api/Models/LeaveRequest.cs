namespace Hop.Api.Models;

public class LeaveRequest
{
    public Guid Id { get; set; }
    public string? RequestNumber { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string DurationType { get; set; } = "FULL_DAY";
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public Guid? CurrentApproverId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReturnedForRevisionAt { get; set; }
    public Guid? ReturnedForRevisionByUserId { get; set; }
    public string? RevisionReason { get; set; }
    public int RevisionCount { get; set; }
    public DateTime? LastResubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public User? CurrentApprover { get; set; }
    public User? ReturnedForRevisionByUser { get; set; }
    public LeaveType? LeaveType { get; set; }
    public ICollection<LeaveAttachment> Attachments { get; set; } = [];
    public ICollection<LeaveApproval> Approvals { get; set; } = [];
}
