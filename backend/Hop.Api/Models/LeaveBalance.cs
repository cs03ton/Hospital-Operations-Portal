namespace Hop.Api.Models;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal EntitledDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public LeaveType? LeaveType { get; set; }
}
