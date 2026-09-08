using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveBalanceReconciliationService(AppDbContext db) : ILeaveBalanceReconciliationService
{
    public async Task<LeaveBalanceUsage> GetUsageAsync(
        Guid userId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var leaveType = await db.LeaveTypes.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken);
        if (leaveType is null)
        {
            return new LeaveBalanceUsage(0, 0);
        }

        var (from, to) = GetDateRange(year, leaveType.UseFiscalYear);
        var totals = await db.LeaveRequests.AsNoTracking()
            .Where(item => item.UserId == userId &&
                           item.LeaveTypeId == leaveTypeId &&
                           item.StartDate >= from && item.StartDate < to &&
                           (item.Status == "Approved" || item.Status == "Pending"))
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Used = group.Sum(item => item.Status == "Approved" ? item.TotalDays : 0),
                Pending = group.Sum(item => item.Status == "Pending" ? item.TotalDays : 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new LeaveBalanceUsage(totals?.Used ?? 0, totals?.Pending ?? 0);
    }

    public Task<LeaveBalanceReconciliationResponse> PreviewAsync(
        LeaveBalanceReconciliationRequest request,
        CancellationToken cancellationToken = default) =>
        ReconcileAsync(request, persist: false, cancellationToken);

    public Task<LeaveBalanceReconciliationResponse> ConfirmAsync(
        LeaveBalanceReconciliationRequest request,
        CancellationToken cancellationToken = default) =>
        ReconcileAsync(request, persist: true, cancellationToken);

    private async Task<LeaveBalanceReconciliationResponse> ReconcileAsync(
        LeaveBalanceReconciliationRequest request,
        bool persist,
        CancellationToken cancellationToken)
    {
        var query = db.LeaveBalances.Include(item => item.User).Include(item => item.LeaveType).AsQueryable();
        if (request.UserId is not null) query = query.Where(item => item.UserId == request.UserId);
        if (request.DepartmentId is not null) query = query.Where(item => item.User != null && item.User.DepartmentId == request.DepartmentId);
        if (request.LeaveTypeId is not null) query = query.Where(item => item.LeaveTypeId == request.LeaveTypeId);
        if (request.Year is not null) query = query.Where(item => item.Year == request.Year);

        var balances = await query.OrderBy(item => item.UserId).ThenBy(item => item.LeaveTypeId).ThenBy(item => item.Year)
            .ToListAsync(cancellationToken);
        var items = new List<LeaveBalanceReconciliationItemResponse>();
        foreach (var balance in balances)
        {
            var usage = await GetUsageAsync(balance.UserId, balance.LeaveTypeId, balance.Year, cancellationToken);
            if (usage.UsedDays == balance.UsedDays && usage.PendingDays == balance.PendingDays) continue;

            items.Add(new LeaveBalanceReconciliationItemResponse(
                balance.Id, balance.UserId, balance.LeaveTypeId, balance.Year,
                balance.UsedDays, usage.UsedDays, balance.PendingDays, usage.PendingDays));
            if (persist)
            {
                balance.UsedDays = usage.UsedDays;
                balance.PendingDays = usage.PendingDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (persist && items.Count > 0)
        {
            if (db.Database.IsRelational())
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return new LeaveBalanceReconciliationResponse(balances.Count, items.Count, balances.Count - items.Count, items);
    }

    private static (DateOnly From, DateOnly To) GetDateRange(int year, bool useFiscalYear) => useFiscalYear
        ? (new DateOnly(year - 1, 10, 1), new DateOnly(year, 10, 1))
        : (new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1));
}

// Keeps older manually-constructed unit-test services compatible. Production DI always resolves
// LeaveBalanceReconciliationService and therefore treats leave_requests as the source of truth.
internal sealed class CachedLeaveBalanceUsageService(AppDbContext db) : ILeaveBalanceReconciliationService
{
    public async Task<LeaveBalanceUsage> GetUsageAsync(Guid userId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default)
    {
        var value = await db.LeaveBalances.AsNoTracking()
            .Where(item => item.UserId == userId && item.LeaveTypeId == leaveTypeId && item.Year == year)
            .Select(item => new { item.UsedDays, item.PendingDays })
            .FirstOrDefaultAsync(cancellationToken);
        return new LeaveBalanceUsage(value?.UsedDays ?? 0, value?.PendingDays ?? 0);
    }

    public Task<LeaveBalanceReconciliationResponse> PreviewAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LeaveBalanceReconciliationResponse(0, 0, 0, []));

    public Task<LeaveBalanceReconciliationResponse> ConfirmAsync(LeaveBalanceReconciliationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LeaveBalanceReconciliationResponse(0, 0, 0, []));
}
