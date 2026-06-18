namespace Hop.Api.Interfaces;

public interface ILoginRateLimiter
{
    bool IsLocked(string username, string? ipAddress, DateTime now);
    void RecordFailedAttempt(string username, string? ipAddress, DateTime now);
    void Reset(string username, string? ipAddress);
}
