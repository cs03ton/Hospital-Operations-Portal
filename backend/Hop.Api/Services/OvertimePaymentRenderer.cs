using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static Hop.Api.Services.OvertimeWorkReportRenderer;

namespace Hop.Api.Services;

public sealed class OvertimePaymentRenderer(IWebHostEnvironment environment)
{
    public const string Title = "หลักฐานการเบิกจ่ายเงินค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ";
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static string AmountLine(ResolvedOvertimePayment p) => $"รวมจ่ายเงินทั้งสิ้น (ตัวอักษร)      ({p.AmountText})";
    private static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
    private static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static int Count(ResolvedOvertimePayment p) => DateTime.DaysInMonth(p.MonthStart.Year, p.MonthStart.Month);
    private static bool IsHoliday(ResolvedOvertimePayment p, int day) => p.Days.SingleOrDefault(d => d.Date.Day == day) is { } claim ? claim.DayType == "holiday" : p.Options.HolidayDates.Contains(p.MonthStart.AddDays(day - 1));
    private static string Period(ResolvedOvertimePayment p) => $"ส่วนราชการ {p.Report.Department} โรงพยาบาลนาหมื่น ในวันที่ 1 – {Count(p)} {p.MonthStart.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("th-TH"))}";
    private static int[] Widths(int days) => new[] { 450, 1800, 650 }.Concat(Enumerable.Repeat(245, days)).Concat([700, 700, 1150, 800, 900, 700]).ToArray();
    private static string[] FixedHeaders => ["ลำดับ", "ชื่อ – สกุล", "อัตรา", "วันปกติ\n(ชั่วโมง)", "วันหยุด\n(ชั่วโมง)", "จำนวนเงิน", "วัน เดือน ปี\nที่รับเงิน", "ลายมือชื่อ\nผู้รับเงิน", "หมายเหตุ"];
    public MemoFiles Render(ResolvedOvertimePayment payment)
    {
        QuestPDF.Settings.License = LicenseType.Community; CorrespondenceFonts.Register(environment.ContentRootPath);
        var pdf = Pdf(payment); var docx = Word(payment);
        return new(Convert.ToBase64String(docx), Convert.ToBase64String(pdf), CorrespondenceFileNames.Report("หลักฐานการเบิกจ่าย", payment.MonthStart, payment.Report.Reporter.Name), "overtime-payment-v2");
    }
    private static byte[] Pdf(ResolvedOvertimePayment p) => Document.Create(document => document.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape()); page.Margin(10, Unit.Millimetre);
        page.DefaultTextStyle(s => s.FontFamily("HOPMemo").FontSize(12).LineHeight(1.1f).FontColor(Colors.Black));
        page.Header().PaddingBottom(5).Column(c => { c.Item().AlignCenter().Text(Title).FontSize(16).Bold(); c.Item().PaddingTop(2).AlignCenter().Text(Period(p)).FontSize(14); });
        page.Content().Column(c =>
        {
            var days = Count(p); var widths = Widths(days);
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(columns => { foreach (var width in widths) columns.RelativeColumn(width); });
                t.Header(h =>
                {
                    for (var i = 0; i < 3; i++) PdfCell(h.Cell().RowSpan(2)).AlignMiddle().AlignCenter().Text(FixedHeaders[i]);
                    PdfCell(h.Cell().ColumnSpan((uint)days)).AlignCenter().Text("วันที่ปฏิบัติงาน");
                    PdfCell(h.Cell().ColumnSpan(2)).AlignCenter().Text("รวมเวลาปฏิบัติงาน");
                    for (var i = 5; i < 9; i++) PdfCell(h.Cell().RowSpan(2)).AlignMiddle().AlignCenter().Text(FixedHeaders[i]);
                    for (var day = 1; day <= days; day++) PdfCell(h.Cell(), IsHoliday(p, day)).AlignCenter().Text(day.ToString()).FontSize(11);
                    PdfCell(h.Cell()).AlignCenter().Text(FixedHeaders[3]); PdfCell(h.Cell()).AlignCenter().Text(FixedHeaders[4]);
                });
                for (var kind = 0; kind < 2; kind++)
                {
                    var type = kind == 0 ? "weekday" : "holiday";
                    if (kind == 0) { PdfCell(t.Cell().RowSpan(2)).AlignCenter().Text("1"); PdfCell(t.Cell().RowSpan(2)).Text(p.Report.Reporter.Name); }
                    PdfCell(t.Cell()).MinHeight(19).AlignCenter().Text(Number(kind == 0 ? p.Options.WeekdayRate : p.Options.HolidayRate) + "/ชม.");
                    for (var day = 1; day <= days; day++)
                    {
                        var item = p.Days.SingleOrDefault(d => d.Date.Day == day && d.DayType == type);
                        PdfCell(t.Cell()).AlignCenter().Text(item == null ? "" : item.ClaimHours.ToString()).FontSize(11);
                    }
                    PdfCell(t.Cell()).AlignCenter().Text(kind == 0 ? p.Days.Where(d => d.DayType == type).Sum(d => d.ClaimHours).ToString() : "");
                    PdfCell(t.Cell()).AlignCenter().Text(kind == 1 ? p.Days.Where(d => d.DayType == type).Sum(d => d.ClaimHours).ToString() : "");
                    PdfCell(t.Cell()).AlignRight().Text(Money(p.Days.Where(d => d.DayType == type).Sum(d => d.Amount)));
                    for (var i = 0; i < 3; i++) PdfCell(t.Cell()).Text("");
                }
                for (var row = 0; row < 4; row++) for (var i = 0; i < widths.Length; i++) PdfCell(t.Cell()).Height(16);
                PdfCell(t.Cell().ColumnSpan((uint)(days + 3))).AlignRight().Text("รวม");
                PdfCell(t.Cell()).AlignCenter().Text(p.Days.Where(d => d.DayType == "weekday").Sum(d => d.ClaimHours).ToString());
                PdfCell(t.Cell()).AlignCenter().Text(p.Days.Where(d => d.DayType == "holiday").Sum(d => d.ClaimHours).ToString());
                PdfCell(t.Cell()).AlignRight().Text(Money(p.TotalAmount)).Bold();
                for (var i = 0; i < 3; i++) PdfCell(t.Cell()).Text("");
            });
            c.Item().PaddingTop(8).PaddingLeft(36).Text(AmountLine(p)).FontSize(14);
            c.Item().PaddingTop(20).ShowEntire().Column(sign =>
            {
                sign.Item().Row(row =>
                {
                    row.RelativeItem().PaddingRight(10).AlignCenter().Text("ขอรับรองว่าผู้รายงานข้างต้นได้ปฏิบัติงานนอกเวลาราชการจริง").FontSize(12);
                    row.RelativeItem();
                });
                sign.Item().PaddingTop(25).Row(row =>
                {
                    row.RelativeItem().PaddingRight(10).Column(left => { left.Item().AlignCenter().Text("ลงชื่อ ....................................... ผู้รับรองการปฏิบัติงาน"); left.Item().PaddingTop(4).AlignCenter().Text($"({p.Report.Supervisor.Name})"); left.Item().PaddingTop(4).AlignCenter().Text(p.Report.Supervisor.Position!); });
                    row.RelativeItem().PaddingLeft(10).Column(right => { right.Item().AlignCenter().Text("ลายมือชื่อ ....................................... ผู้จ่ายเงิน"); right.Item().PaddingTop(4).AlignCenter().Text($"({p.Options.PayerName})"); });
                });
            });
        });
    })).GeneratePdf();
    private static IContainer PdfCell(IContainer c, bool holiday = false) => c.Border(.5f).BorderColor(Colors.Black).Background(holiday ? "DCE8F6" : "FFFFFF").PaddingHorizontal(1).PaddingVertical(2);
    private static byte[] Word(ResolvedOvertimePayment p)
    {
        var days = Count(p); var widths = Widths(days); var body = new XElement(W + "body", P(Title, "center", true, true, 32), P(Period(p), "center", keepNext: true, size: 28));
        XElement Cell(string text, int start, int span = 1, string? merge = null, bool shaded = false, string align = "center")
        {
            var pr = new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", widths.Skip(start).Take(span).Sum()), new XAttribute(W + "type", "dxa")), new XElement(W + "vAlign", new XAttribute(W + "val", "center")));
            if (span > 1) pr.Add(new XElement(W + "gridSpan", new XAttribute(W + "val", span)));
            if (merge != null) pr.Add(new XElement(W + "vMerge", new XAttribute(W + "val", merge)));
            if (shaded) pr.Add(new XElement(W + "shd", new XAttribute(W + "fill", "DCE8F6")));
            return new(W + "tc", pr, P(text, align, size: 24));
        }
        XElement Row(bool header = false) => new(W + "tr", new XElement(W + "trPr", new XElement(W + "cantSplit"), new XElement(W + "trHeight", new XAttribute(W + "val", "360"), new XAttribute(W + "hRule", "atLeast")), header ? new XElement(W + "tblHeader") : null));
        var table = new XElement(W + "tbl", new XElement(W + "tblPr", new XElement(W + "tblW", new XAttribute(W + "w", widths.Sum()), new XAttribute(W + "type", "dxa")),
            new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed")), new XElement(W + "tblBorders", new[] { "top", "bottom", "left", "right", "insideH", "insideV" }.Select(s => new XElement(W + s, new XAttribute(W + "val", "single"), new XAttribute(W + "sz", "4")))),
            new XElement(W + "tblCellMar", new[] { "top", "bottom", "left", "right" }.Select(s => new XElement(W + s, new XAttribute(W + "w", "20"), new XAttribute(W + "type", "dxa"))))),
            new XElement(W + "tblGrid", widths.Select(w => new XElement(W + "gridCol", new XAttribute(W + "w", w)))));
        var first = Row(true); for (var i = 0; i < 3; i++) first.Add(Cell(FixedHeaders[i], i, merge: "restart"));
        first.Add(Cell("วันที่ปฏิบัติงาน", 3, days), Cell("รวมเวลาปฏิบัติงาน", days + 3, 2));
        for (var i = 5; i < 9; i++) first.Add(Cell(FixedHeaders[i], days + i, merge: "restart")); table.Add(first);
        var second = Row(true); for (var i = 0; i < 3; i++) second.Add(Cell("", i, merge: "continue"));
        for (var day = 1; day <= days; day++) second.Add(Cell(day.ToString(), day + 2, shaded: IsHoliday(p, day)));
        second.Add(Cell(FixedHeaders[3], days + 3), Cell(FixedHeaders[4], days + 4)); for (var i = 5; i < 9; i++) second.Add(Cell("", days + i, merge: "continue")); table.Add(second);
        for (var kind = 0; kind < 2; kind++)
        {
            var row = Row(); var type = kind == 0 ? "weekday" : "holiday"; var items = p.Days.Where(d => d.DayType == type).ToArray();
            row.Add(Cell(kind == 0 ? "1" : "", 0, merge: kind == 0 ? "restart" : "continue"), Cell(kind == 0 ? p.Report.Reporter.Name : "", 1, merge: kind == 0 ? "restart" : "continue", align: "left"), Cell(Number(kind == 0 ? p.Options.WeekdayRate : p.Options.HolidayRate) + "/ชม.", 2));
            for (var day = 1; day <= days; day++) row.Add(Cell(items.SingleOrDefault(d => d.Date.Day == day)?.ClaimHours.ToString() ?? "", day + 2));
            row.Add(Cell(kind == 0 ? items.Sum(d => d.ClaimHours).ToString() : "", days + 3), Cell(kind == 1 ? items.Sum(d => d.ClaimHours).ToString() : "", days + 4), Cell(Money(items.Sum(d => d.Amount)), days + 5));
            for (var i = 6; i < 9; i++) row.Add(Cell("", days + i)); table.Add(row);
        }
        for (var i = 0; i < 4; i++) { var row = Row(); for (var col = 0; col < widths.Length; col++) row.Add(Cell("", col)); table.Add(row); }
        var total = Row(); total.Add(Cell("รวม", 0, days + 3, align: "right"), Cell(p.Days.Where(d => d.DayType == "weekday").Sum(d => d.ClaimHours).ToString(), days + 3), Cell(p.Days.Where(d => d.DayType == "holiday").Sum(d => d.ClaimHours).ToString(), days + 4), Cell(Money(p.TotalAmount), days + 5));
        for (var i = 6; i < 9; i++) total.Add(Cell("", days + i)); table.Add(total); body.Add(table);
        var leftWidth = widths.Sum() / 2;
        var amountParagraph = P(AmountLine(p), "left", size: 28);
        amountParagraph.Element(W + "pPr")!.Add(new XElement(W + "ind", new XAttribute(W + "left", "720")));
        amountParagraph.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "before", "160");
        body.Add(amountParagraph);
        var statement = P("ขอรับรองว่าผู้รายงานข้างต้นได้ปฏิบัติงานนอกเวลาราชการจริง", "center", keepNext: true, size: 24);
        statement.Element(W + "pPr")!.Add(new XElement(W + "ind", new XAttribute(W + "right", 16838 - 567 * 2 - leftWidth)));
        statement.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "before", "400"); body.Add(statement);
        var gap = P("", keepNext: true); gap.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "before", "350"); body.Add(gap);
        var signatures = new XElement(W + "tbl", new XElement(W + "tblPr", new XElement(W + "tblW", new XAttribute(W + "w", widths.Sum()), new XAttribute(W + "type", "dxa")), new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed"))), new XElement(W + "tblGrid", Enumerable.Range(0, 2).Select(_ => new XElement(W + "gridCol", new XAttribute(W + "w", widths.Sum() / 2)))));
        var signRow = new XElement(W + "tr", new XElement(W + "trPr", new XElement(W + "cantSplit")));
        foreach (var lines in new[] { new[] { "ลงชื่อ ....................................... ผู้รับรองการปฏิบัติงาน", $"({p.Report.Supervisor.Name})", p.Report.Supervisor.Position! }, new[] { "ลายมือชื่อ ....................................... ผู้จ่ายเงิน", $"({p.Options.PayerName})", "" } })
        {
            var cell = new XElement(W + "tc", new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", widths.Sum() / 2), new XAttribute(W + "type", "dxa"))));
            for (var i = 0; i < lines.Length; i++) { var line = P(lines[i], "center", keepNext: i < 2, size: 28); line.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "before", i == 0 ? 0 : 80); cell.Add(line); } signRow.Add(cell);
        }
        signatures.Add(signRow); body.Add(signatures, P(""));
        body.Add(new XElement(W + "sectPr", new XElement(W + "pgSz", new XAttribute(W + "w", "16838"), new XAttribute(W + "h", "11906"), new XAttribute(W + "orient", "landscape")), new XElement(W + "pgMar", new[] { "top", "bottom", "left", "right" }.Select(s => new XAttribute(W + s, "567")), new XAttribute(W + "header", "0"), new XAttribute(W + "footer", "0"))));
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Add(string name, string content) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(content); }
            Add("word/document.xml", new XDocument(new XElement(W + "document", new XAttribute(XNamespace.Xmlns + "w", W), body)).ToString());
            Add("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Add("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
        }
        return output.ToArray();
    }
}
