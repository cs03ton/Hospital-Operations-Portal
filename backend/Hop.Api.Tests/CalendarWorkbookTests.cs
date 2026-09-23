using System.IO.Compression;
using System.Xml.Linq;
using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class CalendarWorkbookTests
{
    [Theory]
    [InlineData(2026, 9, 24)]
    [InlineData(1900, 1, 1)]
    public void ExcelStoresGregorianSerialWithExplicitBuddhistDisplay(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        var bytes = SimpleXlsxWriter.CreateWorkbook([new[] { "date" }, new[] { "placeholder" }], [20],
            new Dictionary<(int, int), DateOnly> { [(2, 1)] = date });
        using var archive = new ZipArchive(new MemoryStream(bytes));
        using var sheetStream = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var sheet = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cell = sheet.Descendants(ns + "c").Single(c => (string?)c.Attribute("r") == "A2");
        Assert.Equal("n", (string?)cell.Attribute("t"));
        Assert.Equal("1", (string?)cell.Attribute("s"));
        var serial = double.Parse(cell.Element(ns + "v")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(year == 1900 ? 1 : date.ToDateTime(TimeOnly.MinValue).ToOADate(), serial);
        using var stylesStream = archive.GetEntry("xl/styles.xml")!.Open();
        Assert.Contains("[$-107041E]dd/mm/yyyy", XDocument.Load(stylesStream).ToString());
    }
}
