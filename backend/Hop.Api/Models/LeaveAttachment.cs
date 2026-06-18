namespace Hop.Api.Models;

public class LeaveAttachment
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LeaveRequest? LeaveRequest { get; set; }
    public User? UploadedByUser { get; set; }
}
