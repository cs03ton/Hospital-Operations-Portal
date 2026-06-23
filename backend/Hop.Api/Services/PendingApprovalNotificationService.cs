using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public class PendingApprovalNotificationService(AppDbContext db) : IPendingApprovalNotificationService
{
    public async Task<IReadOnlyList<PendingApprovalNotificationResponse>> GetMyPendingApprovalsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await db.LeaveApprovals
            .AsNoTracking()
            .Include(item => item.LeaveRequest)
                .ThenInclude(request => request!.User)
            .Include(item => item.LeaveRequest)
                .ThenInclude(request => request!.LeaveType)
            .Where(item =>
                item.ApproverId == userId &&
                item.Status == "Pending" &&
                item.LeaveRequest != null &&
                item.LeaveRequest.Status == "Pending" &&
                item.LeaveRequest.CurrentApproverId == userId)
            .OrderBy(item => item.LeaveRequest!.SubmittedAt ?? item.LeaveRequest.CreatedAt)
            .Select(item => new
            {
                item.LeaveRequestId,
                RequestNumber = item.LeaveRequest!.RequestNumber,
                EmployeeName = item.LeaveRequest!.User != null ? item.LeaveRequest.User.FullName : null,
                LeaveType = item.LeaveRequest.LeaveType != null ? item.LeaveRequest.LeaveType.Name : null,
                item.LeaveRequest.StartDate,
                item.LeaveRequest.EndDate,
                item.LeaveRequest.SubmittedAt,
                item.StepOrder
            })
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new PendingApprovalNotificationResponse(
                item.LeaveRequestId,
                item.RequestNumber,
                item.EmployeeName,
                item.LeaveType,
                item.StartDate,
                item.EndDate,
                item.SubmittedAt,
                item.StepOrder,
                GetPriority(item.StartDate)))
            .ToList();
    }

    private static string GetPriority(DateOnly startDate)
    {
        var daysUntilLeave = startDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        return daysUntilLeave switch
        {
            <= 1 => "High",
            <= 3 => "Medium",
            _ => "Normal"
        };
    }
}
