using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveDateNormalizerTests
{
    [Theory]
    [InlineData(2026, 9, 18, 2026)]
    [InlineData(2569, 9, 18, 2026)]
    public void NormalizesGregorianAndBuddhistYears(int year, int month, int day, int expectedYear)
    {
        Assert.True(LeaveDateNormalizer.TryNormalize(new DateOnly(year, month, day), out var result));
        Assert.Equal(new DateOnly(expectedYear, month, day), result);
    }

    [Fact]
    public void RejectsDayInvalidAfterBuddhistConversion()
    {
        Assert.False(LeaveDateNormalizer.TryNormalize(new DateOnly(2568, 2, 29), out _));
    }

    [Fact]
    public void RejectsReversedRange()
    {
        Assert.False(LeaveDateNormalizer.TryNormalizeRange(
            new DateOnly(2569, 9, 21), new DateOnly(2026, 9, 18), out _, out _));
    }
}
