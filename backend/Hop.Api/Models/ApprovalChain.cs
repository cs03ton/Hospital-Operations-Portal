namespace Hop.Api.Models;

public class ApprovalChain
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public decimal MinimumDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }
    public LeaveType? LeaveType { get; set; }
    public ICollection<ApprovalChainStep> Steps { get; set; } = [];
}
