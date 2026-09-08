namespace Hop.Api.Models;

public static class FleetMaintenanceStatuses
{
    public const string Active = "ACTIVE";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
}

public sealed class FleetMaintenanceType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Other";
    public bool IsDateBased { get; set; }
    public bool IsMileageBased { get; set; }
    public bool BlocksAvailabilityWhenOverdue { get; set; }
    public int? DefaultReminderDays { get; set; }
    public decimal? DefaultReminderMileage { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public sealed class FleetVehicleMaintenanceSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Guid MaintenanceTypeId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? DueMileage { get; set; }
    public int? ReminderDays { get; set; }
    public decimal? ReminderMileage { get; set; }
    public int? RecurrenceDays { get; set; }
    public decimal? RecurrenceMileage { get; set; }
    public string Status { get; set; } = FleetMaintenanceStatuses.Active;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public Guid? LastCompletedRecordId { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public FleetVehicle? Vehicle { get; set; }
    public FleetMaintenanceType? MaintenanceType { get; set; }
    public ICollection<FleetVehicleMaintenanceRecord> Records { get; set; } = [];
}

public sealed class FleetVehicleMaintenanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Guid MaintenanceScheduleId { get; set; }
    public Guid MaintenanceTypeId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? StartMileage { get; set; }
    public decimal? CompletedMileage { get; set; }
    public decimal? Cost { get; set; }
    public string? Vendor { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Description { get; set; }
    public string? Result { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? NextDueDate { get; set; }
    public decimal? NextDueMileage { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public FleetVehicleMaintenanceSchedule? Schedule { get; set; }
    public ICollection<FleetMaintenanceAttachment> Attachments { get; set; } = [];
}

public sealed class FleetMaintenanceAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaintenanceRecordId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public FleetVehicleMaintenanceRecord? MaintenanceRecord { get; set; }
}

public sealed class FleetVehicleDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Provider { get; set; }
    public string? Notes { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public FleetVehicle? Vehicle { get; set; }
}
