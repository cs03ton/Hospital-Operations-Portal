using QuestPDF.Drawing;

namespace Hop.Api.Services;

public static class CorrespondenceFonts
{
    private static readonly object FontLock = new();
    private static readonly HashSet<string> FontPaths = [];
    public static void Register(string contentRoot)
    {
        lock (FontLock)
        {
            foreach (var file in Directory.GetFiles(Path.Combine(contentRoot, "assets", "fonts"), "THSarabunPSK*.ttf"))
                if (FontPaths.Add(file)) { using var font = File.OpenRead(file); FontManager.RegisterFontWithCustomName("HOPMemo", font); }
        }
    }
}
