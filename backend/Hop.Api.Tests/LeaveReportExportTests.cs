using Hop.Api.Controllers;
using Hop.Api.DTOs;
using System.IO.Compression;
using System.Xml.Linq;
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

        Assert.StartsWith("'", result);
    }

    [Fact]
    public void BuildExcelWorkbook_CreatesOpenXmlWorkbookWithSafeContent()
    {
        var report = new LeaveReportResponse(
            [new LeaveReportItemResponse(Guid.NewGuid(), "<script>", "ER & ICU", "=Annual", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), 1, "Approved", null)],
            [],
            0);

        var bytes = LeaveReportsController.BuildExcelWorkbook(report);
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(worksheet);
        using var stream = worksheet.Open();
        var xml = XDocument.Load(stream).ToString(SaveOptions.DisableFormatting);

        Assert.StartsWith("PK", System.Text.Encoding.ASCII.GetString(bytes, 0, 2));
        Assert.Contains("&lt;script&gt;", xml);
        Assert.Contains("ER &amp; ICU", xml);
        Assert.Contains("'=Annual", xml);
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
