using System.Globalization;
using ICU4N.Globalization;
using ICU4N.Text;

namespace Hop.Api.Services;

internal static class MemoLineBreaks
{
    // Never share a mutable iterator (or retain document text) across requests.
    public static int[] Preferred(string text)
    {
        var safe = Graphemes(text).ToHashSet();
        var iterator = BreakIterator.GetLineInstance(new UCultureInfo("th-TH"));
        iterator.SetText(text);
        var boundaries = new List<int>();
        for (var end = iterator.First(); end != BreakIterator.Done; end = iterator.Next())
            if (end > 0 && safe.Contains(end)) boundaries.Add(end);
        return boundaries.ToArray();
    }

    public static int[] Graphemes(string text) => StringInfo.ParseCombiningCharacters(text)
        // Preposed Thai vowels are separate Unicode graphemes, but belong with
        // the following consonant even when an oversized word needs splitting.
        .Where(i => i > 0 && text[i - 1] is not (>= '\u0e40' and <= '\u0e44'))
        .Append(text.Length).Distinct().ToArray();
}
