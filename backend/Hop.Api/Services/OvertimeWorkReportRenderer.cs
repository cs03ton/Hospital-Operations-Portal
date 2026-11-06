using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

public sealed class OvertimeWorkReportRenderer(IWebHostEnvironment environment)
{
    public const string Title = "รายงานผลการปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ";
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly int[] Widths = [1800, 3900, 1800, 1700, 1000];
    private static readonly string[] Headers = ["วัน/เดือน/ปี", "ลักษณะงานที่ปฏิบัติ", "ปริมาณ\n(หน่วยนับ)", "เวลา", "หมายเหตุ"];
    private static string DateText(DateOnly date) => date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
    private static string EntryDate(OvertimeReportEntry entry) => entry.Date == entry.EndDate ? DateText(entry.Date) : $"{DateText(entry.Date)}\nถึง {DateText(entry.EndDate)}";
    private static string EntryTime(OvertimeReportEntry entry) => $"{entry.StartTime.Replace(':', '.')} น.–{entry.EndTime.Replace(':', '.')} น." + (entry.Hours is { } hours ? $"\n({hours.ToString("0.##", CultureInfo.InvariantCulture)} ชม.)" : "");
    private static string TaskText(string text) => "- " + text.Trim().TrimStart('-', ' ');
    private const float DayGapHeight = 16 * 1.05f;
    public MemoFiles Render(ResolvedOvertimeReport report, IReadOnlyList<OvertimePaymentDay>? claimDays = null, DateOnly? documentMonth = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        CorrespondenceFonts.Register(environment.ContentRootPath);
        var times = EntryTimes(report, claimDays);
        var pdf = Pdf(report, times); var docx = Word(report, times);
        return new(Convert.ToBase64String(docx), Convert.ToBase64String(pdf), CorrespondenceFileNames.Report("รายงานผลการปฏิบัติงานนอกเวลา", documentMonth ?? report.Entries.Min(e => e.Date), report.Reporter.Name), claimDays == null ? "overtime-report-v2" : "overtime-report-v3");
    }
    private static string[] EntryTimes(ResolvedOvertimeReport report, IReadOnlyList<OvertimePaymentDay>? claimDays)
    {
        if (claimDays == null) return report.Entries.Select(EntryTime).ToArray();
        var ranges = report.Entries.Select(entry => (Start: entry.Date.ToDateTime(TimeOnly.ParseExact(entry.StartTime, "HH:mm", CultureInfo.InvariantCulture)), End: entry.EndDate.ToDateTime(TimeOnly.ParseExact(entry.EndTime, "HH:mm", CultureInfo.InvariantCulture)))).ToArray();
        bool Touches(int index, DateOnly date) => ranges[index].Start < date.AddDays(1).ToDateTime(TimeOnly.MinValue) && ranges[index].End > date.ToDateTime(TimeOnly.MinValue);
        var seen = new HashSet<DateOnly>(); var result = new List<string>();
        for (var index = 0; index < report.Entries.Count; index++)
        {
            var entry = report.Entries[index];
            var text = $"{entry.StartTime.Replace(':', '.')} น.–{entry.EndTime.Replace(':', '.')} น.";
            foreach (var day in claimDays.OrderBy(d => d.Date).Where(d => Touches(index, d.Date)))
            {
                if (!seen.Add(day.Date)) continue;
                var onlyEntry = entry.Date == entry.EndDate && Enumerable.Range(0, ranges.Length).Count(i => Touches(i, day.Date)) == 1;
                text += onlyEntry ? $"\n({day.ClaimHours} ชม.)" : $"\nรวมเบิก {day.Date.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("th-TH"))}: {day.ClaimHours} ชม.";
            }
            result.Add(text);
        }
        return result.ToArray();
    }
    private static string[] Heading(ResolvedOvertimeReport report) => [Title, $"โครงการ/กิจกรรมปฏิบัติงาน {report.ProjectName}", $"ของ{report.Reporter.Name}", "สังกัดหน่วยงาน โรงพยาบาลนาหมื่น"];
    private static byte[] Pdf(ResolvedOvertimeReport report, IReadOnlyList<string> times) => Document.Create(document => document.Page(page =>
    {
        page.Size(PageSizes.A4); page.Margin(15, Unit.Millimetre);
        page.DefaultTextStyle(s => s.FontFamily("HOPMemo").FontSize(16).LineHeight(1.05f).FontColor(Colors.Black));
        page.Header().PaddingBottom(4).Column(c => { foreach (var heading in Heading(report)) c.Item().AlignCenter().Text(heading).Bold(); });
        page.Content().Column(column =>
        {
            column.Item().Border(0.5f).Table(table =>
            {
                table.ColumnsDefinition(columns => { foreach (var width in Widths) columns.RelativeColumn(width); });
                table.Header(header => { foreach (var label in Headers) header.Cell().Border(0.5f).Padding(3).AlignMiddle().AlignCenter().Text(label).Bold(); });
                for (var entryIndex = 0; entryIndex < report.Entries.Count; entryIndex++)
                {
                    var entry = report.Entries[entryIndex];
                    var hasNextDay = entryIndex < report.Entries.Count - 1;
                    var span = (uint)(entry.Tasks.Count + (hasNextDay ? 1 : 0));
                    Cell(table.Cell().RowSpan(span)).AlignCenter().Text(EntryDate(entry));
                    for (var i = 0; i < entry.Tasks.Count; i++)
                    {
                        Cell(table.Cell()).Text(TaskText(entry.Tasks[i].Description));
                        Cell(table.Cell()).AlignCenter().Text(entry.Tasks[i].Quantity);
                        if (i == 0)
                        {
                            Cell(table.Cell().RowSpan(span)).AlignCenter().Text(times[entryIndex]).FontSize(14);
                            Cell(table.Cell().RowSpan(span)).Text(entry.Remarks);
                        }
                    }
                    if (hasNextDay)
                    {
                        Cell(table.Cell()).Height(DayGapHeight);
                        Cell(table.Cell()).Height(DayGapHeight);
                    }
                }
            });
            column.Item().PaddingTop(30, Unit.Millimetre).ShowEntire().Row(row =>
            {
                row.RelativeItem().PaddingRight(10).Column(c => PdfSign(c, report.Reporter, "ผู้รายงาน"));
                row.RelativeItem().PaddingLeft(10).Column(c => PdfSign(c, report.Supervisor, "ผู้ควบคุม"));
            });
        });
    })).GeneratePdf();
    private static IContainer Cell(IContainer container) => container.BorderVertical(0.5f).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingTop(2);
    private static void PdfSign(ColumnDescriptor c, MemoPerson person, string label)
    {
        c.Item().AlignCenter().Text($"ลงชื่อ.......................................{label}");
        c.Item().PaddingTop(3).AlignCenter().Text($"({person.Name})");
        c.Item().PaddingTop(3).AlignCenter().Text(person.Position!);
    }
    private static byte[] Word(ResolvedOvertimeReport report, IReadOnlyList<string> times)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            var body = new XElement(W + "body");
            foreach (var heading in Heading(report)) body.Add(P(heading, "center", bold: true, keepNext: true));
            var border = new[] { "top", "left", "bottom", "right", "insideV" }.Select(side => new XElement(W + side, new XAttribute(W + "val", "single"), new XAttribute(W + "sz", "4"), new XAttribute(W + "color", "000000")));
            var table = new XElement(W + "tbl", new XElement(W + "tblPr",
                new XElement(W + "tblW", new XAttribute(W + "w", Widths.Sum()), new XAttribute(W + "type", "dxa")),
                new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed")), new XElement(W + "tblBorders", border),
                new XElement(W + "tblCellMar", new[] { "top", "bottom", "left", "right" }.Select(side => new XElement(W + side, new XAttribute(W + "w", "60"), new XAttribute(W + "type", "dxa"))))),
                new XElement(W + "tblGrid", Widths.Select(width => new XElement(W + "gridCol", new XAttribute(W + "w", width)))));
            var header = new XElement(W + "tr", new XElement(W + "trPr", new XElement(W + "tblHeader"), new XElement(W + "cantSplit")));
            for (var i = 0; i < Headers.Length; i++)
            {
                var cell = WordCell(Headers[i], i, center: true, bold: true);
                cell.Element(W + "tcPr")!.Add(new XElement(W + "tcBorders", new XElement(W + "bottom", new XAttribute(W + "val", "single"), new XAttribute(W + "sz", "4"))));
                header.Add(cell);
            }
            table.Add(header);
            for (var entryIndex = 0; entryIndex < report.Entries.Count; entryIndex++)
            {
                var entry = report.Entries[entryIndex];
                var rowCount = entry.Tasks.Count + (entryIndex < report.Entries.Count - 1 ? 1 : 0);
                for (var i = 0; i < rowCount; i++)
                {
                    var row = new XElement(W + "tr", new XElement(W + "trPr", new XElement(W + "cantSplit")));
                    if (i == entry.Tasks.Count) row.Element(W + "trPr")!.Add(new XElement(W + "trHeight", new XAttribute(W + "val", "320"), new XAttribute(W + "hRule", "atLeast")));
                    row.Add(WordCell(i == 0 ? EntryDate(entry) : "", 0, true, merge: i == 0 ? "restart" : "continue"));
                    row.Add(WordCell(i < entry.Tasks.Count ? TaskText(entry.Tasks[i].Description) : "", 1));
                    row.Add(WordCell(i < entry.Tasks.Count ? entry.Tasks[i].Quantity : "", 2, true));
                    row.Add(WordCell(i == 0 ? times[entryIndex] : "", 3, true, merge: i == 0 ? "restart" : "continue", size: 28));
                    row.Add(WordCell(i == 0 ? entry.Remarks : "", 4, merge: i == 0 ? "restart" : "continue"));
                    table.Add(row);
                }
            }
            body.Add(table);
            var gap = P("", keepNext: true); gap.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "before", "1400"); body.Add(gap);
            body.Add(new XElement(W + "tbl", new XElement(W + "tblPr", new XElement(W + "tblW", new XAttribute(W + "w", Widths.Sum()), new XAttribute(W + "type", "dxa")), new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed"))),
                new XElement(W + "tblGrid", new XElement(W + "gridCol", new XAttribute(W + "w", Widths.Sum() / 2)), new XElement(W + "gridCol", new XAttribute(W + "w", Widths.Sum() / 2))),
                new XElement(W + "tr", new XElement(W + "trPr", new XElement(W + "cantSplit")), WordSign(report.Reporter, "ผู้รายงาน"), WordSign(report.Supervisor, "ผู้ควบคุม"))));
            body.Add(P(""));
            body.Add(new XElement(W + "sectPr", new XElement(W + "pgSz", new XAttribute(W + "w", "11906"), new XAttribute(W + "h", "16838")),
                new XElement(W + "pgMar", new[] { "top", "left", "bottom", "right" }.Select(side => new XAttribute(W + side, "851")), new XAttribute(W + "header", "0"), new XAttribute(W + "footer", "0"))));
            Write(zip, "word/document.xml", new XDocument(new XElement(W + "document", new XAttribute(XNamespace.Xmlns + "w", W), body)));
            XNamespace content = "http://schemas.openxmlformats.org/package/2006/content-types";
            Write(zip, "[Content_Types].xml", new XDocument(new XElement(content + "Types",
                new XElement(content + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(content + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(content + "Override", new XAttribute("PartName", "/word/document.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml")))));
            XNamespace relationships = "http://schemas.openxmlformats.org/package/2006/relationships";
            Write(zip, "_rels/.rels", new XDocument(new XElement(relationships + "Relationships", new XElement(relationships + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "word/document.xml")))));
        }
        return output.ToArray();
    }
    private static XElement WordCell(string text, int index, bool center = false, bool bold = false, string? merge = null, int size = 32)
    {
        var properties = new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", Widths[index]), new XAttribute(W + "type", "dxa")), new XElement(W + "vAlign", new XAttribute(W + "val", "top")));
        if (merge != null) properties.Add(new XElement(W + "vMerge", new XAttribute(W + "val", merge)));
        return new(W + "tc", properties, P(text, center ? "center" : "left", bold, size: size));
    }
    internal static XElement P(string text, string alignment = "left", bool bold = false, bool keepNext = false, int size = 32)
    {
        var properties = new XElement(W + "pPr", new XElement(W + "jc", new XAttribute(W + "val", alignment)), new XElement(W + "spacing", new XAttribute(W + "after", "0"), new XAttribute(W + "line", "240"), new XAttribute(W + "lineRule", "auto")));
        if (keepNext) properties.Add(new XElement(W + "keepNext"));
        var p = new XElement(W + "p", properties);
        var style = new XElement(W + "rPr", new XElement(W + "rFonts", new XAttribute(W + "ascii", "TH SarabunPSK"), new XAttribute(W + "hAnsi", "TH SarabunPSK"), new XAttribute(W + "cs", "TH SarabunPSK")), new XElement(W + "sz", new XAttribute(W + "val", size)), new XElement(W + "szCs", new XAttribute(W + "val", size)));
        if (bold) style.Add(new XElement(W + "b"), new XElement(W + "bCs"));
        var run = new XElement(W + "r", style);
        foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) { if (run.Elements(W + "t").Any()) run.Add(new XElement(W + "br")); run.Add(new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), line)); }
        p.Add(run); return p;
    }
    private static XElement WordSign(MemoPerson person, string label)
    {
        var cell = new XElement(W + "tc", new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", Widths.Sum() / 2), new XAttribute(W + "type", "dxa"))));
        var lines = new[] { $"ลงชื่อ.......................................{label}", $"({person.Name})", person.Position! };
        for (var i = 0; i < lines.Length; i++)
        {
            var p = P(lines[i], "center", keepNext: i < lines.Length - 1); var properties = p.Element(W + "pPr")!;
            properties.Element(W + "spacing")!.SetAttributeValue(W + "before", i == 0 ? 0 : 60); cell.Add(p);
        }
        return cell;
    }
    private static void Write(ZipArchive zip, string name, XDocument document)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); document.Save(writer);
    }
}
