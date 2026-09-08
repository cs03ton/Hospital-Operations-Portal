namespace Hop.Api.Configuration;

public sealed class FleetRolloutOptions
{
    public const string SectionName = "Fleet";
    public string RolloutMode { get; set; } = "Disabled";
    public Guid[] UatUserIds { get; set; } = [];
    public string[] UatRoleCodes { get; set; } = [];
    public int WarningStuckHours { get; set; } = 24;
    public int CriticalStuckHours { get; set; } = 72;
    public int DeliveryRetryWarningCount { get; set; } = 3;
    public int DeliveryFailedCriticalCount { get; set; } = 1;
}
