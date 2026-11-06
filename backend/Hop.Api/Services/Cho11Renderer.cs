using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

public sealed class Cho11Renderer(IWebHostEnvironment environment)
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string Title = "ใบขอรับเงินค่าตอบแทนเบี้ยเลี้ยงเหมาจ่ายสำหรับเจ้าหน้าที่";
    private static string Thai(string text) => string.Concat(text.Select(c => c is >= '0' and <= '9' ? (char)('๐' + c - '0') : c));
    private static string Date(DateOnly date) => Thai(date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("th-TH")));
    private static string Month(DateOnly date) => Thai(date.ToDateTime(TimeOnly.MinValue).ToString("MMMM พ.ศ. yyyy", CultureInfo.GetCultureInfo("th-TH")));
    private static string Duration(ResolvedCho11 report, DateOnly month)
    {
        var tenure = Cho11Service.Tenure(report.EmploymentDate, month);
        return Thai($"...{tenure.Years}... ปี ...{tenure.Months}... เดือน ...{tenure.Days}... วัน");
    }
    private static string[] Lines(ResolvedCho11 r, DateOnly month)
    {
        var end = month.AddMonths(1).AddDays(-1);
        return [
            $"ข้าพเจ้าชื่อ....{r.Author.Name}....ตำแหน่ง....{r.Author.Position}....",
            $"ปัจจุบันปฏิบัติงานที่....{r.Input.Hospital}....จังหวัด....{r.Input.Province}....ระดับ/กลุ่ม {Thai(r.AreaLevel)}",
            $"ปฏิบัติงานในหน่วยบริการ หรือหน่วยบริการเครือข่าย {Duration(r, month)} (นับถึงสิ้นเดือนที่เบิกจ่าย)",
            "โดยมีรายละเอียดการปฏิบัติงาน ดังนี้ (เฉพาะสายแพทย์ตอบข้อ ๑ ด้วย)",
            "๑. ฝึกเพิ่มพูนทักษะ (ปีที่ ๑) รวมระยะเวลาการปฏิบัติงาน...........ปี...........เดือนดังนี้",
            "    ○ รพศ./รพท........................จังหวัด........................ตั้งแต่................ถึง................",
            "    ○ รพช...............................จังหวัด........................ตั้งแต่................ถึง................",
            $"๒. ปฏิบัติงานที่....{r.Input.Hospital}....จังหวัด....{r.Input.Province}....จัดระดับ....{Thai(r.AreaLevel)}....",
            $"ตั้งแต่วันที่....{Date(r.EmploymentDate)}....ถึงวันที่....{Date(end)}....รวม {Duration(r, month)}",
            "๓. ปฏิบัติงานที่โรงพยาบาล........................จังหวัด........................จัดระดับ........................",
            "ตั้งแต่วันที่........................ถึงวันที่........................รวม........ปี........เดือน........วัน",
            "๔. ปฏิบัติงานที่โรงพยาบาล........................จังหวัด........................จัดระดับ........................",
            "ตั้งแต่วันที่........................ถึงวันที่........................รวม........ปี........เดือน........วัน",
            "๕. ปฏิบัติงานที่โรงพยาบาล........................จังหวัด........................จัดระดับ........................",
            "ตั้งแต่วันที่........................ถึงวันที่........................รวม........ปี........เดือน........วัน",
            "๖. ปฏิบัติงานที่โรงพยาบาล........................จังหวัด........................จัดระดับ........................",
            "ตั้งแต่วันที่........................ถึงวันที่........................รวม........ปี........เดือน........วัน",
            $"รวมทั้งสิ้น {Duration(r, month)}",
            "ข้าพเจ้าขอรับรองว่า ข้อมูลดังกล่าวเป็นความจริง ทุกประการ และ หากมีการเรียกเงินคืน",
            "ข้าพเจ้าขอรับผิดชอบคืนเงินแต่เพียงผู้เดียว"
        ];
    }
    public MemoFiles Render(ResolvedCho11 report)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        CorrespondenceFonts.Register(environment.ContentRootPath);
        var formLines = new Dictionary<(DateOnly Month, int Index), string>();
        var pdf = Document.Create(document => {
            foreach (var month in report.Months) document.Page(page => {
                page.Size(PageSizes.A4); page.MarginHorizontal(22, Unit.Millimetre); page.MarginVertical(18, Unit.Millimetre);
                page.DefaultTextStyle(s => s.FontFamily("HOPMemo").FontSize(14).LineHeight(1.05f).FontColor(Colors.Black));
                page.Content().Column(c => {
                    c.Item().AlignCenter().Text(Title).Bold();
                    c.Item().AlignCenter().Text("ที่ปฏิบัติงานในหน่วยบริการสังกัดกระทรวงสาธารณสุข").Bold();
                    c.Item().AlignRight().Text("หน่วยบริการ " + report.Input.Hospital);
                    c.Item().PaddingTop(8).PaddingBottom(14).AlignRight().Text("ประจำเดือน " + Month(month));
                    var lines = Lines(report, month);
                    for (var index = 0; index < lines.Length; index++) {
                        var line = lines[index]; var lineIndex = index;
                        var item = c.Item().PaddingBottom(2);
                        if (line.Contains("....", StringComparison.Ordinal)) item.Dynamic(new Cho11FormLine(line, value => formLines[(month, lineIndex)] = value));
                        else item.Dynamic(new MemoPdfText([new MemoRun(line)], true,
                            firstIndent: line.StartsWith("ข้าพเจ้าขอ", StringComparison.Ordinal) ? 35.43f : 0));
                    }
                    c.Item().PaddingTop(18).ShowEntire().Row(row => {
                        row.RelativeItem().PaddingRight(38.35f).PaddingTop(26).Column(s => {
                            s.Item().AlignCenter().Text("ได้รับการตรวจสอบความถูกต้อง");
                            s.Item().AlignLeft().Text("ผู้ตรวจสอบ");
                            s.Item().PaddingTop(4).AlignCenter().Text("(.....................................)");
                            s.Item().PaddingTop(4).AlignCenter().Text("(" + report.Reviewer.Name + ")");
                            s.Item().AlignCenter().Text(report.Reviewer.Position!);
                        });
                        row.RelativeItem().Column(s => {
                            s.Item().AlignCenter().Text("................................................");
                            s.Item().PaddingTop(4).AlignCenter().Text("(" + report.Author.Name + ")");
                            s.Item().AlignCenter().Text("ตำแหน่ง " + report.Author.Position);
                        });
                    });
                });
            });
        }).GeneratePdf();
        var name = CorrespondenceFileNames.Cho11(report.Months[0], report.Months[^1], report.Author.Name);
        return new(Convert.ToBase64String(Word(report, formLines)), Convert.ToBase64String(pdf), name, "cho11-v3");
    }
    private static XElement P(string text, string align = "left", bool bold = false, int before = 0)
    {
        var p = OvertimeWorkReportRenderer.P(text, align, bold, size: 28);
        p.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "before", before);
        p.Element(W + "pPr")!.Add(new XElement(W + "keepLines"));
        return p;
    }
    private static byte[] Word(ResolvedCho11 r, IReadOnlyDictionary<(DateOnly Month, int Index), string> formLines)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true)) {
            var body = new XElement(W + "body");
            foreach (var month in r.Months) {
                var title = P(Title, "center", true);
                if (month != r.Months[0]) title.Element(W + "pPr")!.Add(new XElement(W + "pageBreakBefore"));
                body.Add(title, P("ที่ปฏิบัติงานในหน่วยบริการสังกัดกระทรวงสาธารณสุข", "center", true),
                    P("หน่วยบริการ " + r.Input.Hospital, "right"), P("ประจำเดือน " + Month(month), "right", before: 160), P(""));
                var lines = Lines(r, month);
                for (var index = 0; index < lines.Length; index++) {
                    var line = formLines.TryGetValue((month, index), out var filled) ? filled : lines[index];
                    var paragraph = P(line, "both");
                    if (line.StartsWith("ข้าพเจ้าขอ", StringComparison.Ordinal)) paragraph.Element(W + "pPr")!.Add(new XElement(W + "ind", new XAttribute(W + "firstLine", "709")));
                    body.Add(paragraph);
                }
                body.Add(P("", before: 200));
                XElement Cell(bool reviewer) => new(W + "tc", new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", "4706"), new XAttribute(W + "type", "dxa")),
                    reviewer ? new XElement(W + "tcMar", new XElement(W + "right", new XAttribute(W + "w", "567"), new XAttribute(W + "type", "dxa"))) : null),
                    reviewer ? new[] { P("ได้รับการตรวจสอบความถูกต้อง", "center", before: 520), P("ผู้ตรวจสอบ"), P("(.....................................)", "center", before: 80), P("(" + r.Reviewer.Name + ")", "center", before: 80), P(r.Reviewer.Position!, "center") }
                    : new[] { P("................................................", "center"), P("(" + r.Author.Name + ")", "center", before: 80), P("ตำแหน่ง " + r.Author.Position, "center") });
                body.Add(new XElement(W + "tbl", new XElement(W + "tblPr", new XElement(W + "tblW", new XAttribute(W + "w", "9412"), new XAttribute(W + "type", "dxa")), new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed"))),
                    new XElement(W + "tblGrid", new XElement(W + "gridCol", new XAttribute(W + "w", "4706")), new XElement(W + "gridCol", new XAttribute(W + "w", "4706"))),
                    new XElement(W + "tr", new XElement(W + "trPr", new XElement(W + "cantSplit")), Cell(true), Cell(false))), P(""));
            }
            body.Add(new XElement(W + "sectPr", new XElement(W + "pgSz", new XAttribute(W + "w", "11906"), new XAttribute(W + "h", "16838")),
                new XElement(W + "pgMar", new XAttribute(W + "left", "1247"), new XAttribute(W + "right", "1247"), new XAttribute(W + "top", "1020"), new XAttribute(W + "bottom", "1020"))));
            void Write(string path, string xml) { using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false)); writer.Write(xml); }
            Write("word/document.xml", new XDocument(new XElement(W + "document", new XAttribute(XNamespace.Xmlns + "w", W), body)).ToString());
            Write("[Content_Types].xml", """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>""");
            Write("_rels/.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>""");
        }
        return output.ToArray();
    }
}

