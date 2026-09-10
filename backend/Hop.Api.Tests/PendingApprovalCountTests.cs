using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class PendingApprovalCountTests
{
    [Fact]
    public async Task Count_includes_only_leave_and_cancellation_items_at_the_current_approver_step()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pending-approval-count-{Guid.NewGuid():N}")
            .Options);
        var approverId = Guid.NewGuid();
        var otherApproverId = Guid.NewGuid();

        AddLeaveApproval(db, approverId, approverId, "Pending", "Pending");
        AddLeaveApproval(db, approverId, otherApproverId, "Pending", "Pending");
        AddLeaveApproval(db, approverId, approverId, "Approved", "Pending");
        AddLeaveApproval(db, approverId, approverId, "Pending", "Approved");

        AddCancellationApproval(db, approverId, approverId, "Pending", LeaveCancellationStatuses.Pending);
        AddCancellationApproval(db, approverId, otherApproverId, "Pending", LeaveCancellationStatuses.Pending);
        AddCancellationApproval(db, approverId, approverId, "Approved", LeaveCancellationStatuses.Pending);
        AddCancellationApproval(db, approverId, approverId, "Pending", LeaveCancellationStatuses.Cancelled);
        await db.SaveChangesAsync();

        var result = await new PendingApprovalNotificationService(db)
            .GetMyPendingApprovalCountAsync(approverId);

        Assert.Equal(1, result.LeaveRequests);
        Assert.Equal(1, result.LeaveCancellations);
        Assert.Equal(2, result.Total);
    }

    private static void AddLeaveApproval(
        AppDbContext db,
        Guid approverId,
        Guid currentApproverId,
        string approvalStatus,
        string requestStatus)
    {
        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalDays = 1,
            Reason = "test",
            Status = requestStatus,
            CurrentApproverId = currentApproverId
        };
        db.LeaveRequests.Add(request);
        db.LeaveApprovals.Add(new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = request.Id,
            LeaveRequest = request,
            ApproverId = approverId,
            Status = approvalStatus,
            StepOrder = 1
        });
    }

    private static void AddCancellationApproval(
        AppDbContext db,
        Guid approverId,
        Guid currentApproverId,
        string approvalStatus,
        string requestStatus)
    {
        var request = new LeaveCancellationRequest
        {
            Id = Guid.NewGuid(),
            CancellationRequestNumber = $"LC-{Guid.NewGuid():N}",
            OriginalLeaveRequestId = Guid.NewGuid(),
            RequesterUserId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            OriginalLeaveDays = 1,
            Reason = "test",
            Status = requestStatus,
            CurrentApproverId = currentApproverId
        };
        db.LeaveCancellationRequests.Add(request);
        db.LeaveCancellationApprovals.Add(new LeaveCancellationApproval
        {
            Id = Guid.NewGuid(),
            LeaveCancellationRequestId = request.Id,
            LeaveCancellationRequest = request,
            ApproverId = approverId,
            Status = approvalStatus,
            StepOrder = 1
        });
    }
}
