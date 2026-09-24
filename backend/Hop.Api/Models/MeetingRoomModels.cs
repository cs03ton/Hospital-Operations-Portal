namespace Hop.Api.Models;

public class MeetingRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PhotoPath { get; set; }
    public string? PhotoContentType { get; set; }
    public DateTime? PhotoUpdatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MeetingRoomBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Number { get; set; }
    public Guid RoomId { get; set; }
    public Guid BookerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Subject { get; set; } = "";
    public string Purpose { get; set; } = "";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int AttendeeCount { get; set; }
    public string? MeetingLink { get; set; }
    public string? AdditionalRequest { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string? CancellationReason { get; set; }
    public Guid? CancelledById { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MeetingRoomBookingHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public string FromStatus { get; set; } = "";
    public string ToStatus { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MeetingRoomBookingAttendee
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public bool IsBooker { get; set; }
}

public class MeetingRoomAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid UploadedById { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
