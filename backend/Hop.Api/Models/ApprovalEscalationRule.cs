namespace Hop.Api.Models;

public class ApprovalEscalationRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public int EscalateAfterHours { get; set; } = 24;
    public Guid? EscalateToUserId { get; set; }
    public Guid? EscalateToRoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }
    public LeaveType? LeaveType { get; set; }
    public User? EscalateToUser { get; set; }
    public Role? EscalateToRole { get; set; }
}
