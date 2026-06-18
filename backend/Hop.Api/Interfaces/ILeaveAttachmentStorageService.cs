using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface ILeaveAttachmentStorageService
{
    Task<LeaveAttachment> SaveAsync(Guid leaveRequestId, Guid uploadedByUserId, IFormFile file);
    Task DeleteAsync(LeaveAttachment attachment);
    FileInfo GetFileInfo(LeaveAttachment attachment);
}
