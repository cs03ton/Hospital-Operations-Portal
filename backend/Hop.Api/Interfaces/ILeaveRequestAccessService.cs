using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public sealed record LeaveRequestVisibility(
    bool ViewOwn,
    bool ViewPendingApproval,
    bool ViewDepartment,
    bool ViewAll,
    Guid? DepartmentId,
    bool DepartmentStaffOnly = false);

public interface ILeaveRequestAccessService
{
    Task<LeaveRequestVisibility> GetVisibilityAsync(Guid? userId);

    IQueryable<LeaveRequest> ApplyVisibility(IQueryable<LeaveRequest> query, Guid? userId, LeaveRequestVisibility visibility);

    Task<bool> CanAccessLeaveRequestAsync(LeaveRequest leaveRequest, Guid? userId);
}
