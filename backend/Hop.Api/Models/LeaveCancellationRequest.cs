namespace Hop.Api.Models;

public class LeaveCancellationRequest
{
    public Guid Id { get; set; }
    public string CancellationRequestNumber { get; set; } = string.Empty;
    public Guid OriginalLeaveRequestId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public decimal OriginalLeaveDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = LeaveCancellationStatuses.Draft;
    public Guid? ApprovalChainId { get; set; }
    public Guid? CurrentApproverId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ReturnedForRevisionAt { get; set; }
    public Guid? ReturnedForRevisionByUserId { get; set; }
    public string? RevisionReason { get; set; }
    public int RevisionCount { get; set; }
    public DateTime? LastResubmittedAt { get; set; }
    public DateTime? BalanceRestoredAt { get; set; }

    public LeaveRequest? OriginalLeaveRequest { get; set; }
    public User? RequesterUser { get; set; }
    public LeaveType? LeaveType { get; set; }
    public ApprovalChain? ApprovalChain { get; set; }
    public User? CurrentApprover { get; set; }
    public User? CreatedByUser { get; set; }
    public User? ReturnedForRevisionByUser { get; set; }
    public ICollection<LeaveCancellationApproval> Approvals { get; set; } = [];
}

public static class LeaveCancellationStatuses
{
    public const string Draft = "Draft";
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string ReturnedForRevision = "ReturnedForRevision";

    public static readonly string[] ActiveStatuses = [Draft, Pending, ReturnedForRevision];
}
