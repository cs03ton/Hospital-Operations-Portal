namespace Hop.Api.Models;

public class FleetRolloutSetting
{
    public Guid Id { get; set; }
    public string Mode { get; set; } = FleetRolloutModes.Disabled;
    public Guid[] UatUserIds { get; set; } = [];
    public string[] UatRoleCodes { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public User? UpdatedByUser { get; set; }
}

public static class FleetRolloutModes
{
    public const string Disabled = "Disabled";
    public const string UatOnly = "UATOnly";
    public const string Enabled = "Enabled";
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Disabled, UatOnly, Enabled };
}
