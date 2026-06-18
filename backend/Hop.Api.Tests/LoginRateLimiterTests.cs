using Hop.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public class LoginRateLimiterTests
{
    [Fact]
    public void RecordFailedAttempt_LocksAfterConfiguredLimit()
    {
        var limiter = new InMemoryLoginRateLimiter(CreateConfiguration());
        var now = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc);

        limiter.RecordFailedAttempt("admin", "127.0.0.1", now);
        limiter.RecordFailedAttempt("admin", "127.0.0.1", now.AddMinutes(1));
        limiter.RecordFailedAttempt("admin", "127.0.0.1", now.AddMinutes(2));

        Assert.True(limiter.IsLocked("admin", "127.0.0.1", now.AddMinutes(3)));
    }

    [Fact]
    public void Reset_ClearsLockoutState()
    {
        var limiter = new InMemoryLoginRateLimiter(CreateConfiguration());
        var now = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc);

        limiter.RecordFailedAttempt("admin", "127.0.0.1", now);
        limiter.RecordFailedAttempt("admin", "127.0.0.1", now.AddMinutes(1));
        limiter.RecordFailedAttempt("admin", "127.0.0.1", now.AddMinutes(2));
        limiter.Reset("admin", "127.0.0.1");

        Assert.False(limiter.IsLocked("admin", "127.0.0.1", now.AddMinutes(3)));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoginRateLimit:Enabled"] = "true",
                ["LoginRateLimit:MaxFailedAttempts"] = "3",
                ["LoginRateLimit:WindowMinutes"] = "10",
                ["LoginRateLimit:LockoutMinutes"] = "15"
            })
            .Build();
    }
}
