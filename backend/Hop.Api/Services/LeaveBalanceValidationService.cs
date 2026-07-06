using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveBalanceValidationService(AppDbContext db, ILeavePolicyService leavePolicyService) : ILeaveBalanceValidationService
{
    public async Task<LeaveBalanceValidationResult> ValidateAvailableBalanceAsync(
        LeaveRequest leaveRequest,
        LeaveType leaveType,
        decimal requestedDays)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(item => item.Id == leaveRequest.UserId))
        {
            return await ValidateLegacyFallbackAsync(leaveRequest, leaveType, requestedDays);
        }

        var preview = await leavePolicyService.ValidateLeaveRequestAsync(
            leaveRequest.UserId,
            leaveRequest.LeaveTypeId,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.DurationType,
            requestedDays,
            CancellationToken.None);

        return new LeaveBalanceValidationResult(
            preview.CanSubmit,
            preview.Errors.FirstOrDefault(),
            preview.EntitlementDays,
            preview.UsedDays,
            preview.PendingDays,
            preview.AvailableDays,
            preview.RequiresBalance);
    }

    private async Task<LeaveBalanceValidationResult> ValidateLegacyFallbackAsync(
        LeaveRequest leaveRequest,
        LeaveType leaveType,
        decimal requestedDays)
    {
        if (!leaveType.RequiresBalance)
        {
            return new LeaveBalanceValidationResult(true, null, leaveType.DefaultDaysPerYear, 0, 0, decimal.MaxValue, false);
        }

        var year = FiscalYearHelper.ResolveBalanceYear(leaveRequest.StartDate, leaveType);
        var balance = await db.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.UserId == leaveRequest.UserId &&
                item.LeaveTypeId == leaveRequest.LeaveTypeId &&
                item.Year == year);
        var entitled = balance?.EntitledDays ?? leaveType.DefaultDaysPerYear;
        var carriedOver = balance?.CarriedOverDays ?? 0;
        var adjusted = balance?.AdjustedDays ?? 0;
        var used = balance?.UsedDays ?? 0;
        var pending = balance?.PendingDays ?? 0;
        var available = FiscalYearHelper.CalculateAvailableDays(entitled, carriedOver, used, pending, adjusted);
        if (available < requestedDays)
        {
            var message = pending > 0
                ? $"วันลาคงเหลือไม่เพียงพอ คงเหลือ {entitled + carriedOver + adjusted - used:0.##} วัน มีคำขอรออนุมัติ {pending:0.##} วัน เหลือใช้ได้ {available:0.##} วัน แต่ขอลา {requestedDays:0.##} วัน"
                : $"วันลาคงเหลือไม่เพียงพอ คงเหลือ {available:0.##} วัน แต่ขอลา {requestedDays:0.##} วัน";
            return new LeaveBalanceValidationResult(false, message, entitled, used, pending, available, true);
        }

        return new LeaveBalanceValidationResult(true, null, entitled, used, pending, available, true);
    }
}
