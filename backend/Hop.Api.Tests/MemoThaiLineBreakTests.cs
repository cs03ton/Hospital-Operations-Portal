using System.Globalization;
using Hop.Api.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;

namespace Hop.Api.Tests;

public sealed class MemoThaiLineBreakTests
{
    private const string Sample = "ด้วย กลุ่มงานสุขภาพดิจิทัล โรงพยาบาลนาหมื่น มีความจำเป็นที่ต้องปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ เนื่องจากมีปริมาณงาน และมีเจ้าหน้าที่รับผิดชอบงานไม่เพียงพอ ทำให้มีภาระงานเพิ่มมากขึ้น มีงานคั่งค้าง ไม่สามารถทำเสร็จได้ทันเวลาในวันทำการ ตั้งแต่วันที่ 1 ตุลาคม 2569 ถึงวันที่ 31 ตุลาคม 2569 ตามหลักเกณฑ์และอัตรา ชั่วโมงละห้าสิบบาท";

    [Theory]
    [InlineData("ราชการ")][InlineData("ตุลาคม")][InlineData("ชั่วโมง")]
    public void Dictionary_keeps_known_thai_words_together(string word)
    {
        Assert.Equal(new[] { word.Length }, MemoLineBreaks.Preferred(word));
    }

    [Fact]
    public void Compound_word_breaks_only_between_complete_dictionary_words()
    {
        // ICU treats this compound as หลัก + เกณฑ์, not arbitrary characters.
        Assert.Equal(new[] { "หลัก".Length, "หลักเกณฑ์".Length }, MemoLineBreaks.Preferred("หลักเกณฑ์"));
    }

    [Theory]
    [InlineData(150, false)][InlineData(250, false)][InlineData(440, false)][InlineData(180, true)]
    public void Measured_lines_end_at_dictionary_boundaries_even_across_styles(float width, bool underline)
    {
        var split = Sample.IndexOf("ราชการ", StringComparison.Ordinal) + 3;
        var runs = new MemoRun[] { new(Sample[..split]), new(Sample[split..], Bold: true, Italic: true, Underline: true) };
        var (pdf, lines) = Render(runs, width, underline);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf));
        Assert.True(lines.Count > 1);
        var boundaries = MemoLineBreaks.Preferred(Sample).ToHashSet();
        var offset = 0;
        foreach (var line in lines)
        {
            Assert.StartsWith(line, Sample[offset..]);
            offset += line.Length;
            while (offset < Sample.Length && Sample[offset] == ' ') offset++;
            Assert.Contains(offset, boundaries);
        }
        Assert.Equal(Sample.Length, offset);
    }

    [Fact]
    public void Explicit_empty_lines_and_newlines_are_preserved()
    {
        var (_, lines) = Render([new("ราชการ\r\n\r\nตุลาคม\n")], 440);
        Assert.Equal(new[] { "ราชการ", "", "ตุลาคม", "" }, lines);
    }

    [Fact]
    public void Oversized_tokens_fall_back_without_losing_graphemes_or_preposed_vowels()
    {
        var text = string.Concat(Enumerable.Repeat("เกิ่ำ", 80));
        var (_, lines) = Render([new(text)], 100);
        Assert.True(lines.Count > 1); Assert.Equal(text, string.Concat(lines));
        var safe = MemoLineBreaks.Graphemes(text).ToHashSet(); var offset = 0;
        foreach (var line in lines) { offset += line.Length; Assert.Contains(offset, safe); }
        Assert.All(lines, line => Assert.False(line.EndsWith('เ')));
    }

    [Fact]
    public void Mixed_text_long_english_numbers_and_punctuation_are_not_lost()
    {
        const string text = "ราชการ (October 2569) 08:30–16:30 https://example.com/abcdefghijklmnopqrstuvwxyz0123456789 ชั่วโมง";
        var (_, lines) = Render([new(text)], 110);
        Assert.Equal(string.Concat(text.Where(c => !char.IsWhiteSpace(c))), string.Concat(lines).Replace(" ", ""));
    }

    [Fact]
    public async Task Concurrent_documents_do_not_share_iterator_text_or_output()
    {
        var results = await Task.WhenAll(Enumerable.Range(1, 12).Select(i => Task.Run(() => {
            var text = $"เอกสาร {i} " + Sample;
            return (text, lines: Render([new(text)], 180).Lines);
        })));
        foreach (var (text, lines) in results)
            Assert.Equal(text.Replace(" ", ""), string.Concat(lines).Replace(" ", ""));
    }

    private static (byte[] Pdf, IReadOnlyList<string> Lines) Render(IReadOnlyList<MemoRun> runs, float width, bool underline = false)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "Hop.Api", "assets"))) directory = directory.Parent;
        CorrespondenceFonts.Register(Path.Combine(directory!.FullName, "backend", "Hop.Api"));
        IReadOnlyList<string> lines = [];
        var pdf = Document.Create(document => document.Page(page => {
            page.Size(width + 20, 800); page.Margin(10);
            page.DefaultTextStyle(s => s.FontFamily("HOPMemo").FontSize(16));
            page.Content().Dynamic(new MemoPdfText(runs, true, underlineLines: underline, captureLines: captured => lines = captured));
        })).GeneratePdf();
        return (pdf, lines);
    }
}
