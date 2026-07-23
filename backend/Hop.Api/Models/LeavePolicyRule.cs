namespace Hop.Api.Models;

public class LeavePolicyRule
{
    public Guid Id { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public Guid LeaveTypeId { get; set; }
    public int? FiscalYear { get; set; }
    public decimal EntitlementDays { get; set; }
    public decimal? AnnualEntitlementDays { get; set; }
    public decimal? MaxPaidDays { get; set; }
    public decimal? EmployerPaidLimitDays { get; set; }
    public bool AllowCarryOver { get; set; }
    public decimal? CarryOverMaxDays { get; set; }
    public decimal? CarryForwardLimitDays { get; set; }
    public decimal? MaxAccumulatedDays { get; set; }
    public decimal? MaximumTotalAvailableDays { get; set; }
    public int? MinServiceMonths { get; set; }
    public int? MinServiceYears { get; set; }
    public bool ProrateIfServiceLessThanYear { get; set; }
    public decimal? FirstYearEntitlementDays { get; set; }
    public decimal? ProbationEntitlementDays { get; set; }
    public decimal? FirstYearPaidDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowRequest { get; set; } = true;
    public decimal? MaxExtendedDays { get; set; }
    public decimal? MaximumLeaveDays { get; set; }
    public decimal? RequiresSpecialApprovalAfterDays { get; set; }
    public decimal? SocialSecurityMaxDays { get; set; }
    public bool UsesSocialSecurity { get; set; }
    public string PaymentRuleType { get; set; } = "EmployerPaid";
    public string DayCountingType { get; set; } = "BusinessDays";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public LeaveType? LeaveType { get; set; }
}
