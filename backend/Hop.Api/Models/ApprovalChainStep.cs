namespace Hop.Api.Models;

public class ApprovalChainStep
{
    public Guid Id { get; set; }
    public Guid ApprovalChainId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverUserId { get; set; }
    public string RequiredPermissionCode { get; set; } = "LeaveApproval.ApproveCurrentStep";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ApprovalChain? ApprovalChain { get; set; }
    public Role? ApproverRole { get; set; }
    public User? ApproverUser { get; set; }
}
