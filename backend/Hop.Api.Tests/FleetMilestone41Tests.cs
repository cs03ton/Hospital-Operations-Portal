using Hop.Api.Configuration;
using Hop.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetMilestone41Tests
{
    [Fact]
    public void FiscalYear_StartsOctoberFirst_AndUsesHalfOpenUtcRange()
    {
        var service = new FleetDateRangeService(Options.Create(new FleetOperationsOptions { MaximumReportRangeDays = 400 }));
        var range = service.Resolve("fiscalyear", null, null);
        Assert.Equal(10, range.LocalStart.Month);
        Assert.Equal(1, range.LocalStart.Day);
        Assert.Equal(range.LocalStart.AddYears(1), range.LocalEndExclusive);
        Assert.InRange((range.End - range.Start).TotalDays, 365, 366);
    }

    [Theory]
    [InlineData("=SUM(A1:A2)")]
    [InlineData("+cmd")]
    [InlineData("-1")]
    [InlineData("@formula")]
    [InlineData("\tformula")]
    [InlineData("\rformula")]
    public void Csv_Escape_PreventsFormulaInjection(string value)
    {
        Assert.StartsWith("\"'", FleetReportExportService.Escape(value));
    }

    [Fact]
    public void CustomRange_RejectsConfiguredMaximum()
    {
        var service = new FleetDateRangeService(Options.Create(new FleetOperationsOptions { MaximumReportRangeDays = 30 }));
        Assert.Throws<ArgumentException>(() => service.Resolve("custom", new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)));
    }
}
