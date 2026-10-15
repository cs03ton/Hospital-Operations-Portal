using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace Hop.Api.Services;

internal static class FleetDailyDriverJobsDigest
{
    internal const string EventType = "Fleet.DailyDriverJobs";
    internal const string MarkerType = "Fleet.DailyDriverJobsMarker";
    private const int MaxMessageLength = 4000;

    internal static Guid StableId(DateOnly day, int part)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"HOP:FleetDailyDriverJobs:{day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{part}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    internal static IReadOnlyList<string> BuildMessages(DateOnly day, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0) return [];
        var date = $"{day.Day:00}/{day.Month:00}/{day.Year + 543}";
        var heading = $"🚐 ภารกิจรถวันนี้ {date}\nรวม {lines.Count} ภารกิจ";
        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var line in lines)
        {
            var remaining = line;
            while (remaining.Length > 0)
            {
                var available = MaxMessageLength - heading.Length - 40 - current.Length;
                if (remaining.Length + 1 > available && current.Length > 0)
                {
                    chunks.Add(current.ToString().TrimEnd());
                    current.Clear();
                    continue;
                }
                if (available <= 0)
                {
                    chunks.Add(current.ToString().TrimEnd());
                    current.Clear();
                    continue;
                }
                var length = Math.Min(available, remaining.Length);
                current.Append(remaining.AsSpan(0, length));
                remaining = remaining[length..];
                if (remaining.Length == 0) current.AppendLine();
            }
        }
        if (current.Length > 0) chunks.Add(current.ToString().TrimEnd());
        return chunks.Select((part, index) => $"{heading}\nตอนที่ {index + 1}/{chunks.Count}\n{part}").ToArray();
    }
}
