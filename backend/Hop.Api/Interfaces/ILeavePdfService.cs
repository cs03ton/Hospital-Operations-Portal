using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface ILeavePdfService
{
    byte[] GenerateLeaveRequestPdf(LeaveRequest leaveRequest, LeavePdfRenderContext context);
}

public sealed record LeavePdfRenderContext(
    string HospitalName,
    string ApplicationVersion,
    LeaveBalance? LeaveBalance,
    IReadOnlyList<LeaveHoliday> Holidays);
