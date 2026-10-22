using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace Hop.Api.Services;

public sealed record ParsedHolidayRow(int RowNumber, string Date, string Name, string HolidayType, string? Error = null);

public static class LeaveHolidayImportReader
{
    public const int MaxRows = 5000;
    private const long MaxFileBytes = 2 * 1024 * 1024;
    private const long MaxEntryBytes = 5 * 1024 * 1024;
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static async Task<IReadOnlyList<ParsedHolidayRow>> ReadAsync(IFormFile file)
    {
        if (file.Length == 0) return Error("ไฟล์ว่าง ไม่มีข้อมูลวันหยุดสำหรับนำเข้า");
        if (file.Length > MaxFileBytes) return Error("ไฟล์นำเข้าต้องมีขนาดไม่เกิน 2 MB");
        try
        {
            await using var stream = file.OpenReadStream();
            return Path.GetExtension(file.FileName).ToLowerInvariant() switch
            {
                ".csv" => await ReadCsv(stream),
                ".xlsx" => ReadXlsx(stream),
                _ => Error("รองรับเฉพาะไฟล์ .csv หรือ .xlsx")
            };
        }
        catch (Exception ex) when (ex is InvalidDataException or XmlException or IOException or FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            return Error("อ่านไฟล์ไม่สำเร็จ ไฟล์อาจเสียหรือมีรูปแบบไม่ถูกต้อง กรุณาบันทึกเป็น CSV UTF-8 หรือ XLSX ใหม่");
        }
    }

    public static bool TryParseDate(string input, out DateOnly date)
    {
        input = input.Trim();
        if (CalendarDateInput.TryParse(input, out date)) return true;
        var parts = input.Split('/');
        return parts.Length == 3 && parts[0].Length is 1 or 2 && parts[1].Length is 1 or 2 && parts[2].Length == 4 &&
            int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var day) &&
            int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month) &&
            int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var year) &&
            CalendarDateInput.TryParse($"{year:0000}-{month:00}-{day:00}", out date);
    }

    private static async Task<IReadOnlyList<ParsedHolidayRow>> ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var rows = new List<(int Number, Dictionary<int, string> Cells)>();
        var number = 0;
        while (await reader.ReadLineAsync() is { } line)
        {
            number++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("คำอธิบาย:", StringComparison.Ordinal)) continue;
            var cells = SplitCsv(line);
            rows.Add((number, cells.Select((value, index) => (value, index)).ToDictionary(x => x.index, x => x.value)));
            if (rows.Count > MaxRows + 1) break;
        }
        return MapRows(rows);
    }

    private static IReadOnlyList<ParsedHolidayRow> ReadXlsx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        if (archive.Entries.Any(entry => entry.FullName.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(entry.FullName) || entry.Length > MaxEntryBytes) || archive.Entries.Sum(x => x.Length) > 20 * MaxEntryBytes)
            return Error("ไฟล์ .xlsx ไม่ปลอดภัยหรือมีขนาดภายในใหญ่เกินกำหนด");
        XDocument? Read(string path)
        {
            var entry = archive.GetEntry(path);
            if (entry is null) return null;
            using var source = entry.Open();
            using var xml = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null, MaxCharactersInDocument = MaxEntryBytes });
            return XDocument.Load(xml);
        }
        var workbook = Read("xl/workbook.xml");
        var date1904 = workbook?.Root?.Element(Sheet + "workbookPr")?.Attribute("date1904")?.Value is "1" or "true";
        var sheetPath = "xl/worksheets/sheet1.xml";
        var firstSheet = workbook?.Descendants(Sheet + "sheet").FirstOrDefault();
        if (firstSheet is not null)
        {
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var id = firstSheet.Attribute(rel + "id")?.Value;
            var target = Read("xl/_rels/workbook.xml.rels")?.Root?.Elements()
                .FirstOrDefault(x => x.Attribute("Id")?.Value == id && x.Attribute("TargetMode")?.Value != "External")?.Attribute("Target")?.Value;
            if (target is null || target.Contains("..", StringComparison.Ordinal)) return Error("ไม่พบ worksheet ในไฟล์ .xlsx");
            sheetPath = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        }
        var document = Read(sheetPath);
        if (document is null) return Error("ไม่พบ worksheet ในไฟล์ .xlsx");
        var strings = Read("xl/sharedStrings.xml")?.Descendants(Sheet + "si")
            .Select(x => string.Concat(x.Descendants(Sheet + "t").Select(t => t.Value))).ToArray() ?? [];
        var rows = new List<(int Number, Dictionary<int, string> Cells)>();
        var numericDates = new HashSet<(int Row, int Column)>();
        foreach (var row in document.Descendants(Sheet + "row"))
        {
            var number = int.TryParse(row.Attribute("r")?.Value, out var n) ? n : rows.Count + 1;
            var cells = new Dictionary<int, string>();
            var next = 0;
            foreach (var cell in row.Elements(Sheet + "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                var column = reference is null ? next : ColumnIndex(reference);
                next = column + 1;
                var type = cell.Attribute("t")?.Value;
                var raw = cell.Element(Sheet + "v")?.Value ?? "";
                cells[column] = type switch
                {
                    "s" => int.TryParse(raw, out var index) && index >= 0 && index < strings.Length ? strings[index] : "",
                    "inlineStr" => string.Concat(cell.Descendants(Sheet + "t").Select(x => x.Value)),
                    "d" => raw.Length >= 10 ? raw[..10] : raw,
                    _ => raw
                };
                if ((type is null or "n") && cell.Element(Sheet + "f") is null) numericDates.Add((number, column));
            }
            if (cells.Values.All(string.IsNullOrWhiteSpace)) continue;
            rows.Add((number, cells));
            if (rows.Count > MaxRows + 1) break;
        }
        return MapRows(rows, (row, col, value) =>
        {
            if (!numericDates.Contains((row, col))) return value;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) || !double.IsFinite(serial)) return "";
            serial = Math.Floor(serial);
            // Excel's fictitious 29 February 1900 must not become another date.
            if ((!date1904 && serial == 60) || serial < 0 || serial > 100000) return "";
            var origin = date1904 ? new DateTime(1904, 1, 1) : serial < 60 ? new DateTime(1899, 12, 31) : new DateTime(1899, 12, 30);
            return origin.AddDays(serial).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        });
    }

    private static IReadOnlyList<ParsedHolidayRow> MapRows(List<(int Number, Dictionary<int, string> Cells)> rows,
        Func<int, int, string, string>? dateConverter = null)
    {
        if (rows.Count == 0) return Error("ไฟล์ว่าง ไม่มีข้อมูลวันหยุดสำหรับนำเข้า");
        var header = rows[0];
        var dates = header.Cells.Where(x => x.Value.Trim().TrimStart('\uFEFF') == "วันที่").ToArray();
        var names = header.Cells.Where(x => x.Value.Trim() == "ชื่อวันหยุด").ToArray();
        if (dates.Length != 1 || names.Length != 1) return Error("หัวตารางต้องมีคอลัมน์ วันที่ และ ชื่อวันหยุด อย่างละหนึ่งคอลัมน์", header.Number);
        if (rows.Count == 1) return Error("ไม่พบรายการวันหยุดใต้หัวตาราง", header.Number + 1);
        if (rows.Count - 1 > MaxRows) return Error($"รองรับการนำเข้าไม่เกิน {MaxRows:N0} รายการต่อครั้ง");
        var typeColumn = header.Cells.Where(x => x.Value.Trim() == "ประเภทวันหยุด").Select(x => (int?)x.Key).FirstOrDefault();
        return rows.Skip(1).Select(row =>
        {
            var date = row.Cells.GetValueOrDefault(dates[0].Key, "");
            return new ParsedHolidayRow(row.Number, dateConverter?.Invoke(row.Number, dates[0].Key, date) ?? date,
                row.Cells.GetValueOrDefault(names[0].Key, ""), typeColumn is { } col ? row.Cells.GetValueOrDefault(col, "") : "");
        }).ToArray();
    }

    private static int ColumnIndex(string reference)
    {
        var value = 0;
        foreach (var c in reference.TakeWhile(char.IsLetter)) value = checked(value * 26 + char.ToUpperInvariant(c) - 'A' + 1);
        if (value == 0) throw new FormatException();
        return value - 1;
    }

    private static List<string> SplitCsv(string line)
    {
        var cells = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
            else if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { cells.Add(cell.ToString().Trim()); cell.Clear(); }
            else cell.Append(c);
        }
        if (quoted) throw new FormatException();
        cells.Add(cell.ToString().Trim()); return cells;
    }

    private static IReadOnlyList<ParsedHolidayRow> Error(string message, int row = 1) => [new(row, "", "", "", message)];
}
