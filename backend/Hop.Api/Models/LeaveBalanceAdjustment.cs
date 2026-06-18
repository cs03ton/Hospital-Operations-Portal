namespace Hop.Api.Models;

public class LeaveBalanceAdjustment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal AdjustmentDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid AdjustedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public LeaveType? LeaveType { get; set; }
    public User? AdjustedByUser { get; set; }
}
