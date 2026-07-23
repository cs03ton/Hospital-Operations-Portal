namespace Hop.Api.Models;

public class LeaveBalanceTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int FiscalYear { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal AmountDays { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public User? User { get; set; }
    public LeaveType? LeaveType { get; set; }
    public User? CreatedByUser { get; set; }
}

public static class LeaveBalanceTransactionTypes
{
    public const string EntitlementGranted = "EntitlementGranted";
    public const string LeaveCancellationRestore = "LeaveCancellationRestore";
}
