using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveBalanceValidationService(AppDbContext db) : ILeaveBalanceValidationService
{
    public async Task<LeaveBalanceValidationResult> ValidateAvailableBalanceAsync(
        LeaveRequest leaveRequest,
        LeaveType leaveType,
        decimal requestedDays)
    {
        if (!leaveType.RequiresBalance)
        {
            return new LeaveBalanceValidationResult(true, null, 0, 0, 0, decimal.MaxValue, false);
        }

        var year = leaveRequest.StartDate.Year;
        var balance = await db.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.UserId == leaveRequest.UserId &&
                item.LeaveTypeId == leaveRequest.LeaveTypeId &&
                item.Year == year);

        var entitled = balance?.EntitledDays ?? leaveType.DefaultDaysPerYear;
        var used = balance?.UsedDays ?? 0;
        var pending = balance?.PendingDays ?? 0;
        var available = entitled - used - pending;

        if (available < requestedDays)
        {
            var message = pending > 0
                ? $"วันลาคงเหลือไม่เพียงพอ คงเหลือ {entitled - used:0.##} วัน มีคำขอรออนุมัติ {pending:0.##} วัน เหลือใช้ได้ {available:0.##} วัน แต่ขอลา {requestedDays:0.##} วัน"
                : $"วันลาคงเหลือไม่เพียงพอ คงเหลือ {available:0.##} วัน แต่ขอลา {requestedDays:0.##} วัน";

            return new LeaveBalanceValidationResult(false, message, entitled, used, pending, available, true);
        }

        return new LeaveBalanceValidationResult(true, null, entitled, used, pending, available, true);
    }
}
