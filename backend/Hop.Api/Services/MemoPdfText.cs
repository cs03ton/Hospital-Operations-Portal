using QuestPDF.Elements;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

// Measure with the same shaping engine and font used to draw the PDF. Each visual
// line receives character spacing, rather than expanding only the Thai word spaces.
internal sealed class MemoPdfText(IReadOnlyList<MemoRun> runs, bool distribute, float firstIndent = 0,
    bool underlineLines = false, Action<IReadOnlyList<string>>? captureLines = null, string alignment = "left") : IDynamicComponent<int>
{
    private sealed record Line(IReadOnlyList<MemoRun> Runs, float Indent, float Spacing);
    private readonly List<Line> lines = [];
    private float measuredWidth = -1;
    public int State { get; set; }

    public DynamicComponentComposeResult Compose(DynamicContext context)
    {
        var width = context.AvailableSize.Width;
        if (Math.Abs(measuredWidth - width) > .01f) Plan(context, width);
        var elements = new List<IDynamicElement>();
        var usedHeight = 0f; var next = State;
        while (next < lines.Count)
        {
            var line = lines[next];
            var element = context.CreateElement(c => Draw(c.Width(width), line));
            if (usedHeight + element.Size.Height > context.AvailableSize.Height + .01f) break;
            elements.Add(element); usedHeight += element.Size.Height; next++;
        }
        if (!underlineLines && next < lines.Count) {
            // Keep at least two lines together at either side of a page break.
            if (lines.Count - next == 1 && elements.Count > 1) { elements.RemoveAt(elements.Count - 1); next--; }
            if (State == 0 && elements.Count == 1) { elements.Clear(); next = 0; }
        }
        State = next;
        return new DynamicComponentComposeResult {
            Content = context.CreateElement(c => c.Width(width).Column(column => {
                foreach (var element in elements) column.Item().Element(element);
            })),
            HasMoreContent = next < lines.Count
        };
    }

    private void Plan(DynamicContext context, float width)
    {
        lines.Clear(); measuredWidth = width;
        var source = runs.Select(r => r with { Text = r.Text.Replace("\r\n", "\n").Replace('\r', '\n') }).ToArray();
        var text = string.Concat(source.Select(r => r.Text));
        var boundaries = MemoLineBreaks.Preferred(text);
        var graphemes = MemoLineBreaks.Graphemes(text);
        var offset = 0;
        while (offset < text.Length)
        {
            var indent = offset == 0 ? firstIndent : 0;
            var available = Math.Max(1, width - indent - (underlineLines ? 3 : 0));
            var newline = text.IndexOf('\n', offset); if (newline < 0) newline = text.Length;
            var ends = boundaries.Where(end => end > offset && end <= newline).ToArray();
            if (ends.Length == 0 || ends[^1] != newline) ends = [..ends, newline];
            var baseline = Measure([new MemoRun("กิ่ำAg")], 0).Height;
            // Only fall back to graphemes if the first complete word cannot fit.
            // Rich-text runs are sliced after segmentation, so style changes do
            // not introduce additional word boundaries.
            var end = LastFit(ends);
            if (end < offset) end = LastFit(graphemes.Where(e => e > offset && e <= newline).ToArray());
            if (end < offset) end = graphemes.First(e => e > offset);
            var lineRuns = Slice(offset, end - offset);
            // Leave the final line of each explicit paragraph at its natural width.
            var spacing = 0f;
            if (distribute && end < newline && lineRuns.Any(r => !string.IsNullOrWhiteSpace(r.Text))) {
                var min = 0f; var max = 1f;
                for (var i = 0; i < 14; i++) { var middle = (min + max) / 2; if (Fits(end, middle)) min = middle; else max = middle; }
                spacing = Math.Max(0, min - .0001f);
            }
            lines.Add(new(lineRuns, indent, spacing));
            offset = end;
            if (offset == newline && offset < text.Length) offset++;
            else while (offset < text.Length && text[offset] == ' ') offset++;

            bool Fits(int candidateEnd, float letterSpacing) => Measure(Slice(offset, candidateEnd - offset), letterSpacing).Height <= baseline + .1f;
            Size Measure(IReadOnlyList<MemoRun> content, float letterSpacing) => context.CreateElement(c => c.Width(available).Text(t => Write(t, content, letterSpacing))).Size;
            int LastFit(int[] candidates)
            {
                var low = -1; var high = candidates.Length - 1;
                while (low < high) {
                    var middle = (low + high + 1) / 2;
                    if (Fits(candidates[middle], 0)) low = middle; else high = middle - 1;
                }
                return low < 0 ? -1 : candidates[low];
            }
        }
        if (text.EndsWith('\n')) lines.Add(new([new MemoRun("")], 0, 0));
        if (lines.Count == 0) lines.Add(new([new MemoRun("")], firstIndent, 0));
        captureLines?.Invoke(lines.Select(line => string.Concat(line.Runs.Select(r => r.Text))).ToArray());

        IReadOnlyList<MemoRun> Slice(int start, int length)
        {
            var result = new List<MemoRun>(); var position = 0;
            foreach (var run in source) {
                var left = Math.Max(start, position); var right = Math.Min(start + length, position + run.Text.Length);
                if (right > left) result.Add(run with { Text = run.Text.Substring(left - position, right - left) });
                position += run.Text.Length;
            }
            // Trailing spaces must not make the measured line wrap on their own.
            while (result.Count > 0) {
                result[^1] = result[^1] with { Text = result[^1].Text.TrimEnd(' ') };
                if (result[^1].Text.Length > 0) break;
                result.RemoveAt(result.Count - 1);
            }
            return result;
        }
    }

    private void Draw(IContainer container, Line line)
    {
        if (underlineLines) container = container.BorderBottom(.5f).BorderColor("808080").PaddingLeft(3);
        container.PaddingLeft(line.Indent).Text(t => Write(t, line.Runs, line.Spacing, alignment));
    }
    private static void Write(TextDescriptor text, IReadOnlyList<MemoRun> runs, float letterSpacing, string alignment = "left")
    {
        if (alignment == "center") text.AlignCenter(); else if (alignment == "right") text.AlignRight(); else text.AlignLeft();
        if (runs.Count == 0) text.Span(" ");
        foreach (var run in runs) {
            var span = text.Span(run.Text).LetterSpacing(letterSpacing);
            if (run.Bold) span.Bold(); if (run.Italic) span.Italic(); if (run.Underline) span.Underline();
        }
    }
}
