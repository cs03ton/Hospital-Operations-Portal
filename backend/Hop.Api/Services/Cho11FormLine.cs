using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Elements;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

// Fill the form's leader gaps, rather than stretching Thai words or numeric values.
internal sealed class Cho11FormLine(string source, Action<string> capture) : IDynamicComponent<int>
{
    public int State { get; set; }
    public DynamicComponentComposeResult Compose(DynamicContext context)
    {
        var width = context.AvailableSize.Width;
        var gaps = Regex.Matches(source, @"\.{4,}").ToArray();
        string Expand(int extra) {
            var index = 0;
            return Regex.Replace(source, @"\.{4,}", match => {
                var count = extra / gaps.Length + (index++ < extra % gaps.Length ? 1 : 0);
                return match.Value + new string('.', count);
            });
        }
        var height = context.CreateElement(c => c.Width(10000).Text(source)).Size.Height;
        bool Fits(string value) => context.CreateElement(c => c.Width(Math.Max(1, width - 2)).Text(value)).Size.Height <= height + .1f;
        var text = source;
        if (gaps.Length > 0 && Fits(source)) {
            var low = 0; var high = 600;
            while (low < high) { var mid = (low + high + 1) / 2; if (Fits(Expand(mid))) low = mid; else high = mid - 1; }
            text = Expand(low);
        }
        capture(text);
        return new() { Content = context.CreateElement(c => c.Width(width).Text(text)), HasMoreContent = false };
    }
}
