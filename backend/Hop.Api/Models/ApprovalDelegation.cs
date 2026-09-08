namespace Hop.Api.Models;

public class ApprovalDelegation
{
    public Guid Id { get; set; }
    public Guid ApproverUserId { get; set; }
    public Guid DelegateUserId { get; set; }
    public string? Scope { get; set; }
    public string? RequiredPermissionCode { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public User? ApproverUser { get; set; }
    public User? DelegateUser { get; set; }
    public User? CreatedByUser { get; set; }
}
