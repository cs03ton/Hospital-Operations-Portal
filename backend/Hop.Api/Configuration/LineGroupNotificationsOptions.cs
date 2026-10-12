namespace Hop.Api.Configuration;

public sealed class LineGroupNotificationsOptions
{
    public bool Enabled { get; set; }
    public bool WorkerEnabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 15;
    public int MaxAttempts { get; set; } = 5;
    public int BatchSize { get; set; } = 20;
    public bool TripOverdueEnabled { get; set; }
    public DateTimeOffset? TripOverdueEnabledFromUtc { get; set; }
}
