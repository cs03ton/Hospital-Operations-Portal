using System.Text.Json;
using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class CalendarDateInputTests
{
    private static JsonSerializerOptions Options() => new() { Converters = { new CalendarDateJsonConverter(), new CalendarInstantJsonConverter() } };

    [Theory]
    [InlineData("2569-09-24", "2026-09-24")]
    [InlineData("2026-09-24", "2026-09-24")]
    [InlineData("2567-02-29", "2024-02-29")]
    [InlineData("2443-01-01", "1900-01-01")]
    [InlineData("2643-12-31", "2100-12-31")]
    public void JsonReadsYearBeforeCheckingGregorianDay(string input, string expected)
    {
        var date = JsonSerializer.Deserialize<DateOnly>($"\"{input}\"", Options());
        Assert.Equal(expected, date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal($"\"{expected}\"", JsonSerializer.Serialize(date, Options()));
    }
    [Theory]
    [InlineData("2568-02-29")]
    [InlineData("2026-02-30")]
    [InlineData("69-09-24")]
    [InlineData("2200-09-24")]
    public void InvalidInputFailsBeforeBusinessLogic(string input) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateOnly>($"\"{input}\"", Options()));

    [Theory]
    [InlineData("2567-02-29T08:00:00+07:00", "2024-02-29T01:00:00Z")]
    [InlineData("2026-09-24T00:30:00+07:00", "2026-09-23T17:30:00Z")]
    public void InstantIsNormalizedAndKeptInUtc(string input, string expected)
    {
        var date = JsonSerializer.Deserialize<DateTime>($"\"{input}\"", Options());
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        Assert.Equal(expected, date.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
    }
    [Fact]
    public void InstantWithoutTimezoneIsRejected() => Assert.Throws<JsonException>(() =>
        JsonSerializer.Deserialize<DateTime>("\"2026-09-24T08:00:00\"", Options()));

    [Fact]
    public void FiscalYearChangesAtMidnightInThailand()
    {
        var before = HospitalTime.FromUtc(new DateTime(2026, 9, 30, 16, 59, 0, DateTimeKind.Utc));
        var after = HospitalTime.FromUtc(new DateTime(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc));
        Assert.Equal(2026, FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(before)));
        Assert.Equal(2027, FiscalYearHelper.GetFiscalYear(DateOnly.FromDateTime(after)));
    }
}
