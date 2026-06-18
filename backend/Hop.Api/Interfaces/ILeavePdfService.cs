using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface ILeavePdfService
{
    byte[] GenerateLeaveRequestPdf(LeaveRequest leaveRequest, string hospitalName);
}
