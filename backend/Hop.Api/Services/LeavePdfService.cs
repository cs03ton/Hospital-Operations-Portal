using System.Globalization;
using System.IO.Compression;
using System.Text;
using Hop.Api.Interfaces;
using Hop.Api.Models;

namespace Hop.Api.Services;

public sealed class LeavePdfService(IWebHostEnvironment environment, IConfiguration configuration) : ILeavePdfService
{
    public byte[] GenerateLeaveRequestPdf(LeaveRequest leaveRequest, string hospitalName)
    {
        var lines = BuildLines(leaveRequest, hospitalName);
        var logo = TryLoadLogo();
        return SimplePdfWriter.CreateA4(lines, logo);
    }

    private PdfImage? TryLoadLogo()
    {
        var configuredPath = configuration["Branding:LogoPath"];
        var candidates = new[]
        {
            configuredPath,
            Path.Combine(environment.ContentRootPath, "assets", "logo", "hospital-logo.png"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "frontend", "src", "assets", "logo", "hospital-logo.png"))
        };

        foreach (var candidate in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (File.Exists(candidate) && PngImageReader.TryReadRgb(candidate, out var image))
            {
                return image;
            }
        }

        return null;
    }

    private static IReadOnlyList<PdfLine> BuildLines(LeaveRequest leaveRequest, string hospitalName)
    {
        var requester = leaveRequest.User?.FullName ?? "-";
        var department = leaveRequest.User?.Department?.Name ?? "-";
        var leaveType = leaveRequest.LeaveType?.Name ?? "-";
        var currentApprover = leaveRequest.CurrentApprover?.FullName ?? "-";
        var approvals = leaveRequest.Approvals.OrderBy(item => item.StepOrder).ToList();

        var lines = new List<PdfLine>
        {
            new("แบบฟอร์มใบลา", 50, 790, 20),
            new(hospitalName, 50, 766, 15),
            new("Hospital Operations Portal", 50, 746, 10),
            new("LOGO", 490, 776, 18),
            new("ข้อมูลผู้ขอลา", 50, 710, 15),
            new($"ชื่อผู้ขอลา: {requester}", 70, 686, 12),
            new($"หน่วยงาน: {department}", 70, 666, 12),
            new($"ประเภทการลา: {leaveType}", 70, 646, 12),
            new($"วันที่ลา: {FormatDate(leaveRequest.StartDate)} - {FormatDate(leaveRequest.EndDate)}", 70, 626, 12),
            new($"จำนวนวัน: {leaveRequest.TotalDays:0.##}", 70, 606, 12),
            new($"เหตุผล: {leaveRequest.Reason}", 70, 586, 12),
            new($"สถานะ: {TranslateStatus(leaveRequest.Status)}", 70, 566, 12),
            new($"ผู้อนุมัติปัจจุบัน: {currentApprover}", 70, 546, 12),
            new("ประวัติการอนุมัติ", 50, 510, 15)
        };

        if (approvals.Count == 0)
        {
            lines.Add(new("ยังไม่มีประวัติการอนุมัติ", 70, 486, 12));
        }
        else
        {
            var y = 486;
            foreach (var approval in approvals)
            {
                var approver = approval.Approver?.FullName ?? "-";
                var actionAt = approval.ActionAt is null
                    ? "-"
                    : approval.ActionAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                lines.Add(new(
                    $"{approval.StepOrder}. {approval.StepName ?? "ขั้นอนุมัติ"} - {approver} - {TranslateStatus(approval.Status)} - {actionAt}",
                    70,
                    y,
                    11));
                y -= 18;
                if (y < 150)
                {
                    break;
                }
            }
        }

        lines.Add(new("ลายเซ็นผู้ขอลา ______________________________", 70, 110, 12));
        lines.Add(new("ลายเซ็นผู้อนุมัติ ____________________________", 330, 110, 12));

        return lines;
    }

    private static string FormatDate(DateOnly date)
    {
        var buddhistYear = date.Year + 543;
        return $"{date.Day:00}/{date.Month:00}/{buddhistYear}";
    }

    private static string TranslateStatus(string status)
    {
        return status switch
        {
            "Draft" => "แบบร่าง",
            "Pending" => "รออนุมัติ",
            "Approved" => "อนุมัติแล้ว",
            "Rejected" => "ไม่อนุมัติ",
            "Cancelled" => "ยกเลิก",
            "Waiting" => "รอดำเนินการ",
            "Skipped" => "ข้ามขั้นตอน",
            _ => status
        };
    }
}

public sealed record PdfLine(string Text, double X, double Y, int FontSize);

public sealed record PdfImage(int Width, int Height, byte[] RgbBytes);

public static class SimplePdfWriter
{
    public static byte[] CreateA4(IReadOnlyList<PdfLine> lines, PdfImage? logo)
    {
        return CreateA4Pages([lines], logo);
    }

    public static byte[] CreateA4Pages(IReadOnlyList<IReadOnlyList<PdfLine>> pages, PdfImage? logo)
    {
        if (pages.Count == 0)
        {
            pages = [[new PdfLine(string.Empty, 50, 790, 10)]];
        }

        var hasLogo = logo is not null;
        var pageCount = pages.Count;
        var fontObjectNumber = 3 + pageCount;
        var cidFontObjectNumber = fontObjectNumber + 1;
        var unicodeMapObjectNumber = fontObjectNumber + 2;
        var imageObjectNumber = hasLogo ? fontObjectNumber + 3 : 0;
        var contentStartObjectNumber = fontObjectNumber + 3 + (hasLogo ? 1 : 0);
        var pageResources = hasLogo
            ? $"<< /Font << /F1 {fontObjectNumber} 0 R >> /XObject << /Im1 {imageObjectNumber} 0 R >> >>"
            : $"<< /Font << /F1 {fontObjectNumber} 0 R >> >>";
        var kids = string.Join(' ', Enumerable.Range(3, pageCount).Select(number => $"{number} 0 R"));
        var unicodeMap = Encoding.ASCII.GetBytes("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n1 beginbfrange\n<0000> <FFFF> <0000>\nendbfrange\nendcmap\nCMapName currentdict /CMap defineresource pop\nend\nend");

        var objects = new List<byte[]>
        {
            Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>")
        };

        for (var index = 0; index < pageCount; index++)
        {
            objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources {pageResources} /Contents {contentStartObjectNumber + index} 0 R >>"));
        }

        objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Font /Subtype /Type0 /BaseFont /Tahoma /Encoding /Identity-H /DescendantFonts [{cidFontObjectNumber} 0 R] /ToUnicode {unicodeMapObjectNumber} 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /CIDFontType2 /BaseFont /Tahoma /CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> /DW 1000 >>"));
        objects.Add(WrapStream(unicodeMap));

        if (logo is not null)
        {
            objects.Add(WrapImage(logo));
        }

        foreach (var page in pages)
        {
            objects.Add(WrapStream(Encoding.ASCII.GetBytes(BuildPageContent(page, logo))));
        }

        return BuildPdf(objects);
    }

    private static string BuildPageContent(IReadOnlyList<PdfLine> lines, PdfImage? logo)
    {
        var content = new StringBuilder();
        if (logo is null)
        {
            content.AppendLine("q");
            content.AppendLine("0.95 0.95 0.95 rg 480 760 65 50 re f");
            content.AppendLine("0 0 0 RG 480 760 65 50 re S");
            content.AppendLine("Q");
        }
        else
        {
            content.AppendLine("q 55 0 0 55 485 758 cm /Im1 Do Q");
        }

        foreach (var line in lines)
        {
            content.Append("BT /F1 ")
                .Append(line.FontSize)
                .Append(" Tf 1 0 0 1 ")
                .Append(line.X.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(line.Y.ToString(CultureInfo.InvariantCulture))
                .Append(" Tm ")
                .Append(ToPdfHexString(line.Text))
                .AppendLine(" Tj ET");
        }

        return content.ToString();
    }

    private static string ToPdfHexString(string value)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes(value);
        return "<FEFF" + Convert.ToHexString(bytes) + ">";
    }

    private static byte[] WrapStream(byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"<< /Length {content.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream");
        return [.. header, .. content, .. footer];
    }

    private static byte[] WrapImage(PdfImage image)
    {
        var compressed = Compress(image.RgbBytes);
        var header = Encoding.ASCII.GetBytes(
            $"<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {compressed.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream");
        return [.. header, .. compressed, .. footer];
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var deflate = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(bytes);
        }

        return output.ToArray();
    }

    private static byte[] BuildPdf(IReadOnlyList<byte[]> objects)
    {
        using var stream = new MemoryStream();
        var header = Encoding.ASCII.GetBytes("%PDF-1.7\n%\xE2\xE3\xCF\xD3\n");
        stream.Write(header);

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n");
            stream.Write(objects[index]);
            WriteAscii(stream, "\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        stream.Write(Encoding.ASCII.GetBytes(value));
    }
}

public static class PngImageReader
{
    public static bool TryReadRgb(string path, out PdfImage? image)
    {
        image = null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (!bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            {
                return false;
            }

            var offset = 8;
            var width = 0;
            var height = 0;
            byte bitDepth = 0;
            byte colorType = 0;
            using var idat = new MemoryStream();

            while (offset < bytes.Length)
            {
                var length = ReadInt32(bytes, offset);
                var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                var dataOffset = offset + 8;
                if (type == "IHDR")
                {
                    width = ReadInt32(bytes, dataOffset);
                    height = ReadInt32(bytes, dataOffset + 4);
                    bitDepth = bytes[dataOffset + 8];
                    colorType = bytes[dataOffset + 9];
                }
                else if (type == "IDAT")
                {
                    idat.Write(bytes, dataOffset, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                offset += length + 12;
            }

            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType is not (2 or 6))
            {
                return false;
            }

            var channelCount = colorType == 6 ? 4 : 3;
            var decompressed = Decompress(idat.ToArray());
            var stride = width * channelCount;
            var previous = new byte[stride];
            var current = new byte[stride];
            var rgb = new byte[width * height * 3];
            var inputOffset = 0;
            var outputOffset = 0;

            for (var y = 0; y < height; y++)
            {
                var filter = decompressed[inputOffset++];
                Array.Copy(decompressed, inputOffset, current, 0, stride);
                inputOffset += stride;
                Unfilter(current, previous, filter, channelCount);

                for (var x = 0; x < width; x++)
                {
                    var pixelOffset = x * channelCount;
                    rgb[outputOffset++] = current[pixelOffset];
                    rgb[outputOffset++] = current[pixelOffset + 1];
                    rgb[outputOffset++] = current[pixelOffset + 2];
                }

                (previous, current) = (current, previous);
            }

            image = new PdfImage(width, height, rgb);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Decompress(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static void Unfilter(byte[] current, byte[] previous, byte filter, int bytesPerPixel)
    {
        for (var i = 0; i < current.Length; i++)
        {
            var left = i >= bytesPerPixel ? current[i - bytesPerPixel] : (byte)0;
            var up = previous[i];
            var upperLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : (byte)0;
            current[i] = filter switch
            {
                0 => current[i],
                1 => (byte)(current[i] + left),
                2 => (byte)(current[i] + up),
                3 => (byte)(current[i] + ((left + up) / 2)),
                4 => (byte)(current[i] + Paeth(left, up, upperLeft)),
                _ => current[i]
            };
        }
    }

    private static byte Paeth(byte left, byte up, byte upperLeft)
    {
        var p = left + up - upperLeft;
        var pa = Math.Abs(p - left);
        var pb = Math.Abs(p - up);
        var pc = Math.Abs(p - upperLeft);
        if (pa <= pb && pa <= pc)
        {
            return left;
        }

        return pb <= pc ? up : upperLeft;
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }
}
