namespace Hop.Api.Models;

public class LeaveBalanceRolloverRun
{
    public Guid Id { get; set; }
    public int FromFiscalYear { get; set; }
    public int ToFiscalYear { get; set; }
    public string Status { get; set; } = "Previewed";
    public string? FiltersJson { get; set; }
    public int Total { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int BlockedCount { get; set; }
    public string? Reason { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }
}
