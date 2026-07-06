namespace Hop.Api.Models;

public class LeaveBalanceSnapshot
{
    public Guid Id { get; set; }
    public Guid RolloverRunId { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int FiscalYear { get; set; }
    public decimal EntitlementDays { get; set; }
    public decimal CarriedOverDays { get; set; }
    public decimal AdjustedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal AvailableDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public LeaveBalanceRolloverRun? RolloverRun { get; set; }
    public User? User { get; set; }
    public LeaveType? LeaveType { get; set; }
    public User? CreatedByUser { get; set; }
}
