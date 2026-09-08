using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hop.Api.Services;

public sealed class LeaveBalanceReconciliationService(AppDbContext db) : ILeaveBalanceReconciliationService
{
    public async Task<LeaveBalanceUsage> GetUsageAsync(Guid userId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default)
    {
        var leaveType = await db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken);
        if (leaveType is null) return new LeaveBalanceUsage(0, 0);
        var (from, to) = GetDateRange(year, leaveType.UseFiscalYear);
        var totals = await db.LeaveRequests.AsNoTracking()
            .Where(item => item.UserId == userId && item.LeaveTypeId == leaveTypeId && item.StartDate >= from && item.StartDate < to &&
                           (item.Status == "Approved" || item.Status == "Pending"))
            .GroupBy(_ => 1)
            .Select(group => new { Used = group.Sum(item => item.Status == "Approved" ? item.TotalDays : 0), Pending = group.Sum(item => item.Status == "Pending" ? item.TotalDays : 0) })
            .FirstOrDefaultAsync(cancellationToken);
        return new LeaveBalanceUsage(totals?.Used ?? 0, totals?.Pending ?? 0);
    }

    public Task<LeaveBalanceReconciliationResponse> PreviewAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default) =>
        ReconcileAsync(request, false, cancellationToken);
    public Task<LeaveBalanceReconciliationResponse> ConfirmAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default) =>
        ReconcileAsync(request, true, cancellationToken);

    private async Task<LeaveBalanceReconciliationResponse> ReconcileAsync(LeaveBalanceReconciliationRequest request, bool persist, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = persist && db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var balanceQuery = db.LeaveBalances.Include(item => item.User).Include(item => item.LeaveType).AsQueryable();
            if (request.UserId is not null) balanceQuery = balanceQuery.Where(item => item.UserId == request.UserId);
            if (request.DepartmentId is not null) balanceQuery = balanceQuery.Where(item => item.User != null && item.User.DepartmentId == request.DepartmentId);
            if (request.LeaveTypeId is not null) balanceQuery = balanceQuery.Where(item => item.LeaveTypeId == request.LeaveTypeId);
            if (request.Year is not null) balanceQuery = balanceQuery.Where(item => item.Year == request.Year);
            var balances = await balanceQuery.ToListAsync(cancellationToken);

            var leaveRequestQuery = db.LeaveRequests.AsNoTracking()
                .Where(item => item.Status == "Approved" || item.Status == "Pending")
                .AsQueryable();
            if (request.UserId is not null) leaveRequestQuery = leaveRequestQuery.Where(item => item.UserId == request.UserId);
            if (request.DepartmentId is not null) leaveRequestQuery = leaveRequestQuery.Where(item => item.User != null && item.User.DepartmentId == request.DepartmentId);
            if (request.LeaveTypeId is not null) leaveRequestQuery = leaveRequestQuery.Where(item => item.LeaveTypeId == request.LeaveTypeId);
            var source = await leaveRequestQuery.Select(item => new
            {
                item.UserId, item.LeaveTypeId, item.StartDate, item.Status, item.TotalDays,
                UseFiscalYear = item.LeaveType != null && item.LeaveType.UseFiscalYear
            }).ToListAsync(cancellationToken);

            var usageByKey = source
                .Select(item => new { Key = new BalanceKey(item.UserId, item.LeaveTypeId, ResolveYear(item.StartDate, item.UseFiscalYear)), item.Status, item.TotalDays })
                .Where(item => request.Year is null || item.Key.Year == request.Year)
                .GroupBy(item => item.Key)
                .ToDictionary(group => group.Key, group => new LeaveBalanceUsage(
                    group.Where(item => item.Status == "Approved").Sum(item => item.TotalDays),
                    group.Where(item => item.Status == "Pending").Sum(item => item.TotalDays)));

            var balanceByKey = balances.ToDictionary(item => new BalanceKey(item.UserId, item.LeaveTypeId, item.Year));
            var keys = balanceByKey.Keys.Union(usageByKey.Keys).OrderBy(item => item.UserId).ThenBy(item => item.LeaveTypeId).ThenBy(item => item.Year).ToList();
            var missingKeys = keys.Where(key => !balanceByKey.ContainsKey(key)).ToList();
            var missingUserIds = missingKeys.Select(key => key.UserId).Distinct().ToList();
            var missingTypeIds = missingKeys.Select(key => key.LeaveTypeId).Distinct().ToList();
            var users = await db.Users.AsNoTracking().Where(item => missingUserIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
            var leaveTypes = await db.LeaveTypes.AsNoTracking().Where(item => missingTypeIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
            var policies = await db.LeavePolicyRules.AsNoTracking().Where(item => item.IsActive && missingTypeIds.Contains(item.LeaveTypeId)).ToListAsync(cancellationToken);

            var changed = new List<LeaveBalanceReconciliationItemResponse>();
            foreach (var key in keys)
            {
                var usage = usageByKey.GetValueOrDefault(key) ?? new LeaveBalanceUsage(0, 0);
                if (balanceByKey.TryGetValue(key, out var balance))
                {
                    if (balance.UsedDays == usage.UsedDays && balance.PendingDays == usage.PendingDays) continue;
                    changed.Add(new(balance.Id, key.UserId, key.LeaveTypeId, key.Year, balance.UsedDays, usage.UsedDays, balance.PendingDays, usage.PendingDays));
                    if (persist) { balance.UsedDays = usage.UsedDays; balance.PendingDays = usage.PendingDays; balance.UpdatedAt = DateTime.UtcNow; }
                    continue;
                }

                changed.Add(new(null, key.UserId, key.LeaveTypeId, key.Year, 0, usage.UsedDays, 0, usage.PendingDays));
                if (!persist || !users.TryGetValue(key.UserId, out var user) || !leaveTypes.TryGetValue(key.LeaveTypeId, out var leaveType)) continue;
                var policy = policies.Where(item => item.LeaveTypeId == key.LeaveTypeId && item.EmploymentType == user.EmploymentType &&
                                                   (item.FiscalYear == null || item.FiscalYear == key.Year))
                    .OrderByDescending(item => item.FiscalYear == key.Year).FirstOrDefault();
                db.LeaveBalances.Add(new LeaveBalance
                {
                    Id = Guid.NewGuid(), UserId = key.UserId, LeaveTypeId = key.LeaveTypeId, Year = key.Year,
                    EntitledDays = policy is null ? 0 : LeavePolicyService.ResolvePolicyEntitlement(user, policy, GetDateRange(key.Year, leaveType.UseFiscalYear).From),
                    UsedDays = usage.UsedDays, PendingDays = usage.PendingDays,
                    Notes = "Created by leave balance reconciliation from leave request history."
                });
            }

            if (persist && changed.Count > 0) await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new LeaveBalanceReconciliationResponse(keys.Count, changed.Count, keys.Count - changed.Count, changed);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static int ResolveYear(DateOnly startDate, bool useFiscalYear) => useFiscalYear && startDate.Month >= 10 ? startDate.Year + 1 : startDate.Year;
    private static (DateOnly From, DateOnly To) GetDateRange(int year, bool useFiscalYear) => useFiscalYear
        ? (new DateOnly(year - 1, 10, 1), new DateOnly(year, 10, 1))
        : (new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1));
    private sealed record BalanceKey(Guid UserId, Guid LeaveTypeId, int Year);
}

internal sealed class CachedLeaveBalanceUsageService(AppDbContext db) : ILeaveBalanceReconciliationService
{
    public async Task<LeaveBalanceUsage> GetUsageAsync(Guid userId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default)
    {
        var value = await db.LeaveBalances.AsNoTracking().Where(item => item.UserId == userId && item.LeaveTypeId == leaveTypeId && item.Year == year)
            .Select(item => new { item.UsedDays, item.PendingDays }).FirstOrDefaultAsync(cancellationToken);
        return new LeaveBalanceUsage(value?.UsedDays ?? 0, value?.PendingDays ?? 0);
    }
    public Task<LeaveBalanceReconciliationResponse> PreviewAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new LeaveBalanceReconciliationResponse(0, 0, 0, []));
    public Task<LeaveBalanceReconciliationResponse> ConfirmAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new LeaveBalanceReconciliationResponse(0, 0, 0, []));
}
