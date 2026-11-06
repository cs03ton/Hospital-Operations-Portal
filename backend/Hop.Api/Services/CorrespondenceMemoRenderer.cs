using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

public sealed record MemoFiles(string DocxBase64, string PdfBase64, string FileName, string TemplateVersion);

public sealed class CorrespondenceMemoRenderer(IWebHostEnvironment environment)
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private string Asset(string file) => Path.Combine(environment.ContentRootPath, "assets", "correspondence", file);
    private static string ThaiDate(DateOnly date) => date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
    public MemoFiles Render(ResolvedMemo memo)
    {
        IReadOnlyList<string> subjectLines = [memo.Input.Subject];
        var pdf = Pdf(memo, lines => subjectLines = lines); var docx = Word(memo, subjectLines);
        return new(Convert.ToBase64String(docx), Convert.ToBase64String(pdf), memo.DownloadName ?? CorrespondenceFileNames.Memo(memo.Input.Subject, memo.Input.DocumentDate, memo.Input.DocumentNumber, memo.Proposer.Name), "memo-v1");
    }

    private byte[] Word(ResolvedMemo memo, IReadOnlyList<string> subjectLines)
    {
        using var stream = new MemoryStream(); stream.Write(File.ReadAllBytes(Asset("memo-v1.docx")));
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, true))
        {
            var entry = zip.GetEntry("word/document.xml")!; XDocument xml;
            using (var input = entry.Open()) xml = XDocument.Load(input);
            var r = memo.Input;
            var title = xml.Descendants(W + "body").First().Elements(W + "p").First();
            var titleProperties = title.Element(W + "pPr")!;
            titleProperties.Element(W + "tabs")?.Remove();
            titleProperties.Add(new XElement(W + "jc", new XAttribute(W + "val", "center")), new XElement(W + "ind", new XAttribute(W + "right", "0")));
            title.Elements(W + "r").Where(run => run.Element(W + "tab") != null).Remove();
            foreach (var spacing in title.Descendants(W + "rPr").Elements(W + "spacing").ToArray()) spacing.Remove();
            Replace("Department", [HeaderLine(("ส่วนราชการ", "", 1400), ("", $"{memo.Department} โรงพยาบาลนาหมื่น โทร {CorrespondenceMemoService.Telephone} ต่อ {r.Extension}", 7671))]);
            Replace("NumberDate", [HeaderLine(("ที่", "", 300), ("", r.DocumentNumber, 4200), ("วันที่", "", 700), ("", ThaiDate(r.DocumentDate), 3871))]);
            Replace("Subject", [SubjectHeader(subjectLines)]);
            Replace("Recipient", [Paragraph($"เรียน   {r.Recipient}")]);
            var index = 0;
            Replace("Body", r.Body.Select(p => {
                index = p.Ordered ? index + 1 : 0;
                return RichParagraph(p.Ordered ? [new MemoRun($"{index}. "), ..p.Runs] : p.Runs, p.Align, p.Indent);
            }).ToArray());
            Replace("Closing", [RichParagraph([new(r.Closing)], "left", 1)]);
            Replace("Proposer", Signature(memo.Proposer.Name, memo.Proposer.Position!, true));
            Replace("Head", memo.Head == null ? [] : Signature(memo.Head.Name, memo.Head.Position!, false, $"ความเห็นหัวหน้า{memo.Department}", HeadComment(r.HeadComment)));
            Replace("Director", memo.Director == null ? [] : Signature(memo.Director.Name, CorrespondenceMemoService.DirectorPosition, true, comment: r.DirectorComment, commentBold: true));
            using (var output = entry.Open()) { output.SetLength(0); xml.Save(output); }

            void Replace(string field, XElement[] paragraphs)
            {
                var slot = xml.Descendants(W + "p").Single(p => string.Concat(p.Descendants(W + "t").Select(t => t.Value)) == "{{" + field + "}}");
                slot.ReplaceWith(paragraphs);
            }
        }
        return stream.ToArray();
    }
    private static XElement HeaderLine(params (string Label, string Value, int Width)[] cells)
    {
        var table = new XElement(W + "tbl", new XElement(W + "tblPr",
            new XElement(W + "tblW", new XAttribute(W + "w", "9071"), new XAttribute(W + "type", "dxa")),
            new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed")),
            new XElement(W + "tblCellMar", new XElement(W + "left", new XAttribute(W + "w", "0"), new XAttribute(W + "type", "dxa")), new XElement(W + "right", new XAttribute(W + "w", "60"), new XAttribute(W + "type", "dxa")))),
            new XElement(W + "tblGrid", cells.Select(c => new XElement(W + "gridCol", new XAttribute(W + "w", c.Width)))));
        var row = new XElement(W + "tr", new XElement(W + "trPr", new XElement(W + "cantSplit")));
        foreach (var cell in cells)
        {
            var properties = new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", cell.Width), new XAttribute(W + "type", "dxa")), new XElement(W + "vAlign", new XAttribute(W + "val", "bottom")));
            if (cell.Label.Length == 0) properties.Add(new XElement(W + "tcBorders", new XElement(W + "bottom", new XAttribute(W + "val", "single"), new XAttribute(W + "sz", "4"), new XAttribute(W + "color", "808080"))));
            var paragraph = RichParagraph([new(cell.Label.Length > 0 ? cell.Label : cell.Value, Bold: cell.Label.Length > 0)], cell.Label == "วันที่" ? "right" : "left", 0);
            paragraph.Element(W + "pPr")!.Element(W + "spacing")!.SetAttributeValue(W + "after", "0");
            if (cell.Label.Length > 0) foreach (var size in paragraph.Descendants().Where(x => x.Name == W + "sz" || x.Name == W + "szCs")) size.SetAttributeValue(W + "val", "36");
            row.Add(new XElement(W + "tc", properties, paragraph));
        }
        table.Add(row); return table;
    }
    private static XElement Paragraph(string text) => RichParagraph([new(text)], "left", 0);
    private static string HeadComment(string text) => text.Trim() == "เห็นสมควรอนุมัติ" ? "- เห็นสมควรอนุมัติ" : text;
    private static XElement SubjectHeader(IReadOnlyList<string> lines)
    {
        var table = HeaderLine(("เรื่อง", "", 600), ("", "", 8471));
        foreach (var cell in table.Descendants(W + "tc")) cell.Element(W + "tcPr")!.Element(W + "vAlign")!.SetAttributeValue(W + "val", "top");
        var valueCell = table.Descendants(W + "tc").Last();
        valueCell.Element(W + "tcPr")!.Element(W + "tcBorders")?.Remove(); valueCell.Elements(W + "p").Remove();
        foreach (var line in lines) {
            var paragraph = Paragraph(line); var properties = paragraph.Element(W + "pPr")!;
            properties.Element(W + "spacing")!.SetAttributeValue(W + "after", "0");
            properties.Add(new XElement(W + "pBdr", new[] { "bottom", "between" }.Select(edge => new XElement(W + edge, new XAttribute(W + "val", "single"), new XAttribute(W + "sz", "4"), new XAttribute(W + "space", "0"), new XAttribute(W + "color", "808080")))));
            valueCell.Add(paragraph);
        }
        return table;
    }
    private static XElement RichParagraph(IEnumerable<MemoRun> runs, string align, int indent)
    {
        var properties = new XElement(W + "pPr", new XElement(W + "jc", new XAttribute(W + "val", align == "justify" ? "thaiDistribute" : align)),
            new XElement(W + "spacing", new XAttribute(W + "after", "80"), new XAttribute(W + "line", "240"), new XAttribute(W + "lineRule", "auto")),
            new XElement(W + "widowControl"));
        if (indent > 0) properties.Add(new XElement(W + "ind", new XAttribute(W + "firstLine", "1440"), new XAttribute(W + "left", ((indent - 1) * 360).ToString(CultureInfo.InvariantCulture))));
        var paragraph = new XElement(W + "p", properties);
        foreach (var run in runs)
        {
            var style = new XElement(W + "rPr", new XElement(W + "rFonts", new XAttribute(W + "ascii", "TH SarabunPSK"), new XAttribute(W + "hAnsi", "TH SarabunPSK"), new XAttribute(W + "cs", "TH SarabunPSK")),
                new XElement(W + "sz", new XAttribute(W + "val", "32")), new XElement(W + "szCs", new XAttribute(W + "val", "32")), new XElement(W + "color", new XAttribute(W + "val", "000000")),
                new XElement(W + "lang", new XAttribute(W + "val", "th-TH"), new XAttribute(W + "eastAsia", "th-TH")));
            if (run.Bold) style.Add(new XElement(W + "b"), new XElement(W + "bCs"));
            if (run.Italic) style.Add(new XElement(W + "i"), new XElement(W + "iCs"));
            if (run.Underline) style.Add(new XElement(W + "u", new XAttribute(W + "val", "single")));
            var content = new XElement(W + "r", style); var lines = run.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < lines.Length; i++) { if (i > 0) content.Add(new XElement(W + "br")); content.Add(new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), lines[i])); }
            paragraph.Add(content);
        }
        return paragraph;
    }
    private static XElement[] Signature(string name, string position, bool right, string? heading = null, string? comment = null, bool commentBold = false)
    {
        var texts = new List<string>();
        if (heading != null) texts.Add(heading);
        if (comment != null) texts.Add(comment);
        var signatureStart = texts.Count;
        texts.AddRange(["", $"({name})", position]);
        return texts.Select((text, i) => {
            var p = RichParagraph([new(text, Bold: commentBold && comment != null && i == (heading == null ? 0 : 1))], "left", 0); var properties = p.Element(W + "pPr")!;
            properties.Element(W + "jc")!.SetAttributeValue(W + "val", "center");
            properties.Add(new XElement(W + "ind", new XAttribute(W + (right ? "left" : "right"), "4535")));
            if (i < texts.Count - 1) properties.Add(new XElement(W + "keepNext"));
            properties.Add(new XElement(W + "keepLines"));
            if (i == signatureStart) properties.Element(W + "spacing")!.SetAttributeValue(W + "before", "360");
            else if (i == 0) properties.Element(W + "spacing")!.SetAttributeValue(W + "before", "160");
            return p;
        }).ToArray();
    }

    private byte[] Pdf(ResolvedMemo memo, Action<IReadOnlyList<string>> captureSubject)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        CorrespondenceFonts.Register(environment.ContentRootPath);
        var r = memo.Input;
        return Document.Create(document => document.Page(page => {
            page.Size(PageSizes.A4); page.MarginLeft(30, Unit.Millimetre); page.MarginRight(20, Unit.Millimetre); page.MarginTop(25, Unit.Millimetre); page.MarginBottom(15, Unit.Millimetre);
            page.DefaultTextStyle(s => s.FontFamily("HOPMemo").FontSize(16).LineHeight(1.05f).FontColor(Colors.Black));
            page.Content().Column(column => {
                column.Spacing(4);
                column.Item().PaddingBottom(6).Layers(layers => {
                    layers.PrimaryLayer().Height(43).AlignCenter().AlignBottom().Text("บันทึกข้อความ").FontSize(29).Bold();
                    layers.Layer().Width(43).Height(43).Image(Asset("garuda.png")).FitArea();
                });
                column.Item().Row(row => { row.ConstantItem(70).Text("ส่วนราชการ").FontSize(18).Bold(); row.RelativeItem().Element(c => PdfHeaderValue(c, $"{memo.Department} โรงพยาบาลนาหมื่น โทร {CorrespondenceMemoService.Telephone} ต่อ {r.Extension}")); });
                column.Item().Row(row => { row.ConstantItem(15).Text("ที่").FontSize(18).Bold(); row.RelativeItem(4200).Element(c => PdfHeaderValue(c, r.DocumentNumber)); row.ConstantItem(35).AlignRight().Text("วันที่").FontSize(18).Bold(); row.RelativeItem(3871).Element(c => PdfHeaderValue(c, ThaiDate(r.DocumentDate))); });
                column.Item().Row(row => { row.ConstantItem(30).Text("เรื่อง").FontSize(18).Bold(); row.RelativeItem().Dynamic(new MemoPdfText([new MemoRun(r.Subject)], false, underlineLines: true, captureLines: captureSubject)); });
                column.Item().PaddingBottom(5).Text($"เรียน   {r.Recipient}");
                var index = 0;
                foreach (var paragraph in r.Body)
                {
                    index = paragraph.Ordered ? index + 1 : 0;
                    var prefix = paragraph.Ordered ? $"{index}. " : "";
                    var item = column.Item().PaddingLeft(Math.Max(0, paragraph.Indent - 1) * 18);
                    item.Dynamic(new MemoPdfText(prefix.Length > 0 ? [new MemoRun(prefix), ..paragraph.Runs] : paragraph.Runs,
                        paragraph.Align == "justify", paragraph.Indent > 0 ? 72 : 0, alignment: paragraph.Align));
                }
                column.Item().Text(t => { t.ParagraphFirstLineIndentation(72); t.Span(r.Closing); });
                column.Item().Element(c => PdfSignature(c, memo.Proposer.Name, memo.Proposer.Position!, true));
                if (memo.Head != null) {
                    column.Item().Element(c => PdfSignature(c, memo.Head.Name, memo.Head.Position!, false, $"ความเห็นหัวหน้า{memo.Department}", HeadComment(r.HeadComment)));
                }
                if (memo.Director != null) {
                    column.Item().Element(c => PdfSignature(c, memo.Director.Name, CorrespondenceMemoService.DirectorPosition, true, comment: r.DirectorComment, commentBold: true));
                }
            });
        })).GeneratePdf();
    }
    private static void PdfHeaderValue(IContainer container, string text) => container.BorderBottom(0.5f).BorderColor("808080").PaddingLeft(3).AlignBottom().Text(text);
    private static void PdfSignature(IContainer container, string name, string position, bool right, string? heading = null, string? comment = null, bool commentBold = false)
    {
        var block = container.ShowEntire();
        block = right ? block.AlignRight() : block.AlignLeft();
        block.Width(80, Unit.Millimetre).PaddingTop(8).Column(c => {
            if (heading != null) c.Item().AlignCenter().Text(heading);
            if (comment != null) { var text = c.Item().MinHeight(16).AlignCenter().Text(comment); if (commentBold) text.Bold(); }
            c.Item().PaddingTop(28).AlignCenter().Text($"({name})");
            c.Item().AlignCenter().Text(position);
        });
    }
}
