using Hop.Api.Controllers;
using Hop.Api.DTOs;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveReportExportTests
{
    [Theory]
    [InlineData("=cmd")]
    [InlineData("+sum")]
    [InlineData("-10")]
    [InlineData("@link")]
    public void SafeExcelCell_PrefixesFormulaLikeValues(string value)
    {
        var result = LeaveReportsController.SafeExcelCell(value);

        Assert.StartsWith("&#39;", result);
    }

    [Fact]
    public void BuildExcelHtml_EncodesHtmlValues()
    {
        var report = new LeaveReportResponse(
            [new LeaveReportItemResponse(Guid.NewGuid(), "<script>", "ER & ICU", "=Annual", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), 1, "Approved", null)],
            [],
            0);

        var html = LeaveReportsController.BuildExcelHtml(report);

        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("ER &amp; ICU", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&#39;=Annual", html);
    }

    [Fact]
    public void BuildPdfPages_PaginatesAllLeaveRows()
    {
        var rows = Enumerable.Range(1, 65)
            .Select(index => new LeaveReportItemResponse(Guid.NewGuid(), $"User {index}", "Dept", "Annual", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), 1, "Approved", null))
            .ToList();
        var report = new LeaveReportResponse(rows, [], 0);

        var pages = LeaveReportsController.BuildPdfPages(report);

        Assert.Equal(3, pages.Count);
        Assert.Contains(pages.SelectMany(page => page), line => line.Text.Contains("User 65", StringComparison.Ordinal));
    }
}
