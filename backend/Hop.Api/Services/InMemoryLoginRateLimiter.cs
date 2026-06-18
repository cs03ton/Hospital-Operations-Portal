using System.Collections.Concurrent;
using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class InMemoryLoginRateLimiter(IConfiguration configuration) : ILoginRateLimiter
{
    private readonly ConcurrentDictionary<string, LoginAttemptState> attempts = new();

    public bool IsLocked(string username, string? ipAddress, DateTime now)
    {
        if (!IsEnabled())
        {
            return false;
        }

        var key = BuildKey(username, ipAddress);
        return attempts.TryGetValue(key, out var state) && state.LockedUntil > now;
    }

    public void RecordFailedAttempt(string username, string? ipAddress, DateTime now)
    {
        if (!IsEnabled())
        {
            return;
        }

        var maxAttempts = Math.Max(1, configuration.GetValue("LoginRateLimit:MaxFailedAttempts", 5));
        var windowMinutes = Math.Max(1, configuration.GetValue("LoginRateLimit:WindowMinutes", 15));
        var lockoutMinutes = Math.Max(1, configuration.GetValue("LoginRateLimit:LockoutMinutes", 15));
        var key = BuildKey(username, ipAddress);

        attempts.AddOrUpdate(
            key,
            _ => new LoginAttemptState(1, now, null),
            (_, state) =>
            {
                var windowStart = now.AddMinutes(-windowMinutes);
                var failedCount = state.FirstFailedAt < windowStart ? 1 : state.FailedCount + 1;
                var firstFailedAt = state.FirstFailedAt < windowStart ? now : state.FirstFailedAt;
                var lockedUntil = failedCount >= maxAttempts ? now.AddMinutes(lockoutMinutes) : state.LockedUntil;
                return new LoginAttemptState(failedCount, firstFailedAt, lockedUntil);
            });
    }

    public void Reset(string username, string? ipAddress)
    {
        attempts.TryRemove(BuildKey(username, ipAddress), out _);
    }

    private bool IsEnabled()
    {
        return configuration.GetValue("LoginRateLimit:Enabled", true);
    }

    private static string BuildKey(string username, string? ipAddress)
    {
        return $"{username.Trim().ToLowerInvariant()}|{ipAddress ?? "unknown"}";
    }

    private sealed record LoginAttemptState(int FailedCount, DateTime FirstFailedAt, DateTime? LockedUntil);
}
