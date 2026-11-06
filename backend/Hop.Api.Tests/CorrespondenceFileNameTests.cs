using System.Globalization;
using System.Text;
using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class CorrespondenceFileNameTests
{
    [Fact]
    public void Memo_matches_requested_format_and_retains_fractional_registration()
    {
        Assert.Equal("บันทึกข้อความ-ขอปฏิบัติงานนอกเวลา-เดือนตุลาคม-2569-28.1-นายกฤษฎา แสงวันเพ็ญ",
            CorrespondenceFileNames.Memo("ขอปฏิบัติงานนอกเวลา", new(2026, 10, 1), "นน 0033.312/28.1", "นายกฤษฎา แสงวันเพ็ญ"));
    }

    [Theory]
    [InlineData("รายงานผลการปฏิบัติงานนอกเวลา")][InlineData("หลักฐานการเบิกจ่าย")]
    public void Reports_have_distinct_types_and_no_registration(string kind)
    {
        Assert.Equal($"{kind}-เดือนตุลาคม-2569-ทดสอบ ชื่อ", CorrespondenceFileNames.Report(kind, new(2026, 10, 1), "ทดสอบ ชื่อ"));
    }

    [Fact]
    public void Sanitizes_names_without_removing_Thai_or_decimal_points()
    {
        var name = CorrespondenceFileNames.Memo("เรื่อง / \\ : * ? \" < > |\nทดสอบ", new(2026, 10, 1), "นน 0033.312/028.01", "นายทดสอบ/ชื่อ\r\n");
        Assert.Contains("028.01", name); Assert.Contains("ทดสอบ", name);
        Assert.DoesNotContain(name, c => char.IsControl(c) || "/\\:*?\"<>|".Contains(c));
        Assert.False(name.EndsWith('.')); Assert.False(name.EndsWith(' '));
    }

    [Fact]
    public void Bounds_utf8_including_docx_extension_without_breaking_graphemes()
    {
        var title = string.Concat(Enumerable.Repeat("เกิ่ำ", 100));
        var name = CorrespondenceFileNames.Memo(title, new(2026, 10, 1), "นน 0033.312/28.1", "นาย" + string.Concat(Enumerable.Repeat("เกิ่ำ", 100)));
        Assert.True(Encoding.UTF8.GetByteCount(name + ".docx") <= 240);
        Assert.Contains("-เดือนตุลาคม-2569-28.1-", name);
        Assert.False(name.EndsWith('เ'));
        Assert.True(Encoding.UTF8.GetByteCount(CorrespondenceFileNames.Report("หลักฐานการเบิกจ่าย", new(2026, 10, 1), title) + ".pdf") <= 240);
    }

    [Theory]
    [InlineData("2026-13")][InlineData("2569-10")][InlineData("2026-1")][InlineData("x")]
    public void Rejects_invalid_api_month(string month) => Assert.Throws<ArgumentException>(() => CorrespondenceFileNames.ParseMonth(month));
}
