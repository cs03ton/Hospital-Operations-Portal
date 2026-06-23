namespace Hop.Api.Models;

public class LeaveApproval
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public Guid ApproverId { get; set; }
    public Guid? ApprovalChainId { get; set; }
    public Guid? ApprovalChainStepId { get; set; }
    public int StepOrder { get; set; }
    public string Status { get; set; } = "Pending";
    public string? StepName { get; set; }
    public string RequiredPermissionCode { get; set; } = "LeaveApproval.ApproveCurrentStep";
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActionAt { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }
    public User? Approver { get; set; }
    public ApprovalChain? ApprovalChain { get; set; }
    public ApprovalChainStep? ApprovalChainStep { get; set; }
}
