namespace Hop.Api.Models;

public class ApprovalDelegation
{
    public Guid Id { get; set; }
    public Guid ApproverUserId { get; set; }
    public Guid DelegateUserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? ApproverUser { get; set; }
    public User? DelegateUser { get; set; }
}
