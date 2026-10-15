using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetDailyDriverJobsDigestTests
{
    [Fact]
    public void Empty_day_produces_no_message()
    {
        Assert.Empty(FleetDailyDriverJobsDigest.BuildMessages(new DateOnly(2026, 10, 15), []));
    }

    [Fact]
    public void Long_schedule_is_split_without_losing_a_job()
    {
        var lines = Enumerable.Range(1, 150)
            .Select(index => $"{index}. 08:00 FL-{index:0000} → จุดหมายเลขที่ {index} รถ กข-{index} · คนขับ ทดสอบ {index}")
            .ToArray();
        var parts = FleetDailyDriverJobsDigest.BuildMessages(new DateOnly(2026, 10, 15), lines);
        Assert.True(parts.Count > 1);
        Assert.All(parts, part => Assert.True(part.Length <= 4000));
        foreach (var line in lines) Assert.Contains(parts, part => part.Contains(line));
        Assert.Contains("15/10/2569", parts[0]);
    }

    [Fact]
    public void Event_ids_are_stable_per_day_and_part()
    {
        var day = new DateOnly(2026, 10, 15);
        Assert.Equal(FleetDailyDriverJobsDigest.StableId(day, 0), FleetDailyDriverJobsDigest.StableId(day, 0));
        Assert.NotEqual(FleetDailyDriverJobsDigest.StableId(day, -1), FleetDailyDriverJobsDigest.StableId(day, 0));
        Assert.NotEqual(FleetDailyDriverJobsDigest.StableId(day, 0), FleetDailyDriverJobsDigest.StableId(day.AddDays(1), 0));
    }
}
