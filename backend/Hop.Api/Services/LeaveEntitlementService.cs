using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveEntitlementService(
    AppDbContext db,
    ILeavePolicyService leavePolicyService,
    IAuditLogService auditLogService) : ILeaveEntitlementService
{
    public async Task<LeaveEntitlementInitializationResult> InitializeAsync(
        Guid userId,
        int fiscalYear,
        DateOnly effectiveDate,
        Guid? initiatedByUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return new LeaveEntitlementInitializationResult(userId, fiscalYear, 0, 0, 1, warnings, ["ไม่พบข้อมูลผู้ใช้งาน"]);
        }

        if (!user.IsActive)
        {
            return new LeaveEntitlementInitializationResult(userId, fiscalYear, 0, 0, 1, warnings, ["บัญชีผู้ใช้งานไม่ได้เปิดใช้งาน"]);
        }

        if (string.IsNullOrWhiteSpace(user.EmploymentType))
        {
            return new LeaveEntitlementInitializationResult(userId, fiscalYear, 0, 0, 1, warnings, ["ไม่พบประเภทพนักงาน"]);
        }

        if (user.EmploymentStartDate is null)
        {
            return new LeaveEntitlementInitializationResult(userId, fiscalYear, 0, 0, 1, warnings, ["ไม่พบวันที่เริ่มงาน"]);
        }

        var leaveTypes = await db.LeaveTypes
            .AsNoTracking()
            .Where(item => item.IsActive && item.RequiresBalance)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var created = 0;
        var skipped = 0;
        foreach (var leaveType in leaveTypes)
        {
            var existing = await db.LeaveBalances.AnyAsync(item =>
                item.UserId == userId &&
                item.LeaveTypeId == leaveType.Id &&
                item.Year == fiscalYear,
                cancellationToken);

            if (existing)
            {
                skipped++;
                continue;
            }

            var policy = await leavePolicyService.GetPolicyAsync(userId, leaveType.Id, fiscalYear, cancellationToken);
            if (policy is null)
            {
                warnings.Add($"ยังไม่ได้กำหนด policy สำหรับ {leaveType.Name} ({EmploymentTypes.GetThaiLabel(user.EmploymentType)})");
                skipped++;
                continue;
            }

            var entitlementDays = await leavePolicyService.CalculateEntitlementAsync(userId, leaveType.Id, fiscalYear, cancellationToken);
            var balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveType.Id,
                Year = fiscalYear,
                EntitledDays = entitlementDays,
                CarriedOverDays = 0,
                AdjustedDays = 0,
                UsedDays = 0,
                PendingDays = 0,
                Notes = $"Initialized from {user.EmploymentType} policy on {effectiveDate:yyyy-MM-dd}. {reason}"
            };

            db.LeaveBalances.Add(balance);
            db.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
            {
                UserId = userId,
                LeaveTypeId = leaveType.Id,
                FiscalYear = fiscalYear,
                TransactionType = LeaveBalanceTransactionTypes.EntitlementGranted,
                AmountDays = entitlementDays,
                ReferenceType = "LeaveBalance",
                ReferenceId = balance.Id,
                Reason = reason,
                CreatedByUserId = initiatedByUserId
            });

            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        await auditLogService.WriteAsync(
            initiatedByUserId,
            "LeaveEntitlement.Initialized",
            "User",
            userId.ToString(),
            $"Initialized leave entitlement. fiscalYear={fiscalYear}, employmentType={user.EmploymentType}, effectiveDate={effectiveDate:yyyy-MM-dd}, created={created}, skipped={skipped}, warnings={string.Join(" | ", warnings)}.",
            errors.Count == 0 ? "Success" : "Failed");

        return new LeaveEntitlementInitializationResult(userId, fiscalYear, created, skipped, 0, warnings, errors);
    }
}
