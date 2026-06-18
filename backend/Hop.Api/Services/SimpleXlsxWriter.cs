using System.IO.Compression;
using System.Xml.Linq;

namespace Hop.Api.Services;

public static class SimpleXlsxWriter
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static byte[] CreateWorkbook(IReadOnlyList<IReadOnlyList<string>> rows, IReadOnlyList<double> columnWidths)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", CreateContentTypes());
            WriteEntry(archive, "_rels/.rels", CreateRootRelationships());
            WriteEntry(archive, "xl/workbook.xml", CreateWorkbookXml());
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", CreateWorkbookRelationships());
            WriteEntry(archive, "xl/styles.xml", CreateStyles());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", CreateWorksheet(rows, columnWidths));
        }

        return output.ToArray();
    }

    private static XDocument CreateContentTypes()
    {
        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";
        return new XDocument(
            new XElement(ns + "Types",
                new XElement(ns + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ns + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(ns + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ns + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")),
                new XElement(ns + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))));
    }

    private static XDocument CreateRootRelationships()
    {
        return new XDocument(
            new XElement(PackageRelationships + "Relationships",
                new XElement(PackageRelationships + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));
    }

    private static XDocument CreateWorkbookXml()
    {
        return new XDocument(
            new XElement(Spreadsheet + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", Relationships),
                new XElement(Spreadsheet + "sheets",
                    new XElement(Spreadsheet + "sheet",
                        new XAttribute("name", "รายงานการลา"),
                        new XAttribute("sheetId", "1"),
                        new XAttribute(Relationships + "id", "rId1")))));
    }

    private static XDocument CreateWorkbookRelationships()
    {
        return new XDocument(
            new XElement(PackageRelationships + "Relationships",
                new XElement(PackageRelationships + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet1.xml")),
                new XElement(PackageRelationships + "Relationship",
                    new XAttribute("Id", "rId2"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                    new XAttribute("Target", "styles.xml"))));
    }

    private static XDocument CreateStyles()
    {
        return new XDocument(
            new XElement(Spreadsheet + "styleSheet",
                new XElement(Spreadsheet + "fonts", new XAttribute("count", "1"), new XElement(Spreadsheet + "font")),
                new XElement(Spreadsheet + "fills", new XAttribute("count", "1"), new XElement(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "none")))),
                new XElement(Spreadsheet + "borders", new XAttribute("count", "1"), new XElement(Spreadsheet + "border")),
                new XElement(Spreadsheet + "cellStyleXfs", new XAttribute("count", "1"), new XElement(Spreadsheet + "xf", new XAttribute("numFmtId", "0"), new XAttribute("fontId", "0"), new XAttribute("fillId", "0"), new XAttribute("borderId", "0"))),
                new XElement(Spreadsheet + "cellXfs", new XAttribute("count", "1"), new XElement(Spreadsheet + "xf", new XAttribute("numFmtId", "0"), new XAttribute("fontId", "0"), new XAttribute("fillId", "0"), new XAttribute("borderId", "0"), new XAttribute("xfId", "0")))));
    }

    private static XDocument CreateWorksheet(IReadOnlyList<IReadOnlyList<string>> rows, IReadOnlyList<double> columnWidths)
    {
        return new XDocument(
            new XElement(Spreadsheet + "worksheet",
                new XElement(Spreadsheet + "cols",
                    columnWidths.Select((width, index) =>
                        new XElement(Spreadsheet + "col",
                            new XAttribute("min", index + 1),
                            new XAttribute("max", index + 1),
                            new XAttribute("width", width),
                            new XAttribute("customWidth", "1")))),
                new XElement(Spreadsheet + "sheetData",
                    rows.Select((row, rowIndex) =>
                        new XElement(Spreadsheet + "row",
                            new XAttribute("r", rowIndex + 1),
                            row.Select((value, columnIndex) =>
                                new XElement(Spreadsheet + "c",
                                    new XAttribute("r", $"{ColumnName(columnIndex + 1)}{rowIndex + 1}"),
                                    new XAttribute("t", "inlineStr"),
                                    new XElement(Spreadsheet + "is",
                                        new XElement(Spreadsheet + "t", value)))))))));
    }

    private static string ColumnName(int columnNumber)
    {
        var name = string.Empty;
        while (columnNumber > 0)
        {
            columnNumber--;
            name = (char)('A' + columnNumber % 26) + name;
            columnNumber /= 26;
        }

        return name;
    }

    private static void WriteEntry(ZipArchive archive, string path, XDocument document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        document.Save(stream);
    }
}
