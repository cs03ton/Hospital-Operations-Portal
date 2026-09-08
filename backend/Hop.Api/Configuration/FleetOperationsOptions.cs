namespace Hop.Api.Configuration;

public sealed class FleetOperationsOptions
{
    public const string SectionName = "Fleet";
    public int MaximumReportRangeDays { get; set; } = 366;
    public int MaximumCalendarRangeDays { get; set; } = 93;
    public int MaximumExportRows { get; set; } = 10_000;
    public int FeedbackWindowDays { get; set; } = 7;
    public int FeedbackAttentionThreshold { get; set; } = 2;
    public MaintenanceOptions Maintenance { get; set; } = new();
}

public sealed class MaintenanceOptions
{
    public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromHours(6);
    public int DefaultReminderDays { get; set; } = 30;
    public decimal DefaultReminderMileage { get; set; } = 500m;
    public bool BlockOnOverdue { get; set; } = true;
    public bool BlockOnExpiredRequiredDocument { get; set; } = true;
    public int MaximumAttachmentSizeMb { get; set; } = 10;
}
