using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public class PendingApprovalNotificationService(AppDbContext db) : IPendingApprovalNotificationService
{
    public async Task<IReadOnlyList<PendingApprovalNotificationResponse>> GetMyPendingApprovalsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var leaveItems = await db.LeaveApprovals
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

        var leaveApprovals = leaveItems
            .Select(item => new PendingApprovalNotificationResponse(
                item.LeaveRequestId,
                item.RequestNumber,
                item.EmployeeName,
                item.LeaveType,
                item.StartDate,
                item.EndDate,
                item.SubmittedAt,
                item.StepOrder,
                GetPriority(item.StartDate),
                "LeaveRequest",
                $"/leave/{item.LeaveRequestId}"))
            .ToList();

        var cancellationItems = await db.LeaveCancellationApprovals
            .AsNoTracking()
            .Include(item => item.LeaveCancellationRequest)
                .ThenInclude(request => request!.RequesterUser)
            .Include(item => item.LeaveCancellationRequest)
                .ThenInclude(request => request!.LeaveType)
            .Include(item => item.LeaveCancellationRequest)
                .ThenInclude(request => request!.OriginalLeaveRequest)
            .Where(item =>
                item.ApproverId == userId &&
                item.Status == "Pending" &&
                item.LeaveCancellationRequest != null &&
                item.LeaveCancellationRequest.Status == LeaveCancellationStatuses.Pending &&
                item.LeaveCancellationRequest.CurrentApproverId == userId)
            .OrderBy(item => item.LeaveCancellationRequest!.SubmittedAt ?? item.LeaveCancellationRequest.CreatedAt)
            .Select(item => new
            {
                item.LeaveCancellationRequestId,
                RequestNumber = item.LeaveCancellationRequest!.CancellationRequestNumber,
                EmployeeName = item.LeaveCancellationRequest.RequesterUser != null ? item.LeaveCancellationRequest.RequesterUser.FullName : null,
                LeaveType = item.LeaveCancellationRequest.LeaveType != null ? item.LeaveCancellationRequest.LeaveType.Name : null,
                OriginalStartDate = item.LeaveCancellationRequest.OriginalLeaveRequest != null ? item.LeaveCancellationRequest.OriginalLeaveRequest.StartDate : (DateOnly?)null,
                OriginalEndDate = item.LeaveCancellationRequest.OriginalLeaveRequest != null ? item.LeaveCancellationRequest.OriginalLeaveRequest.EndDate : (DateOnly?)null,
                item.LeaveCancellationRequest.CreatedAt,
                item.LeaveCancellationRequest.SubmittedAt,
                item.StepOrder
            })
            .ToListAsync(cancellationToken);

        var cancellationApprovals = cancellationItems
            .Select(item =>
            {
                var startDate = item.OriginalStartDate ?? DateOnly.FromDateTime(item.CreatedAt);
                var endDate = item.OriginalEndDate ?? startDate;
                return new PendingApprovalNotificationResponse(
                    item.LeaveCancellationRequestId,
                    item.RequestNumber,
                    item.EmployeeName,
                    item.LeaveType,
                    startDate,
                    endDate,
                    item.SubmittedAt,
                    item.StepOrder,
                    GetPriority(startDate),
                    "LeaveCancellationRequest",
                    $"/leave/cancellations/{item.LeaveCancellationRequestId}");
            })
            .ToList();

        return leaveApprovals
            .Concat(cancellationApprovals)
            .OrderBy(item => item.SubmittedAt ?? DateTime.UtcNow)
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
