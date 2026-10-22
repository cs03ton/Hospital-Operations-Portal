using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hop.Api.Services;

internal static class PendingApprovalDigest
{
    internal const string EventType = "Operations.PendingApprovalDigest";
    internal const string MarkerType = "Operations.PendingApprovalDigestMarker";
    internal static readonly TimeOnly[] Slots = [new(8, 0), new(12, 30), new(16, 0)];

    internal static int CurrentSlot(TimeOnly localTime)
    {
        for (var index = Slots.Length - 1; index >= 0; index--)
            if (localTime >= Slots[index]) return index;
        return -1;
    }

    internal static Guid StableId(DateOnly day, int slot, bool marker)
    {
        var key = $"HOP:PendingApprovalDigest:{day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{slot}:{(marker ? "marker" : "event")}";
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
    }

    internal static string? Format(DateOnly day, TimeOnly slot, IReadOnlyList<(string Title, int Leave, int Cancellation, int Fleet)> sections)
    {
        if (sections.Sum(x => x.Leave + x.Cancellation + x.Fleet) == 0) return null;
        var date = day.ToString("dd/MM", CultureInfo.InvariantCulture);
        var time = slot.ToString("HH:mm", CultureInfo.InvariantCulture);
        var text = new StringBuilder($"🔔 สรุปงานรออนุมัติ\n📅 {date}/{day.Year + 543}\n🕒 รอบเวลา {time} น.");
        foreach (var section in sections)
        {
            if (section.Leave + section.Cancellation + section.Fleet == 0) continue;
            text.Append("\n\n👤 ").Append(section.Title);
            if (section.Leave > 0) text.Append("\n📄 คำขอลา: ").Append(section.Leave).Append(" คำขอ");
            if (section.Cancellation > 0) text.Append("\n↩️ ยกเลิกใบลา: ").Append(section.Cancellation).Append(" คำขอ");
            if (section.Fleet > 0) text.Append("\n🚗 คำขอใช้รถ: ").Append(section.Fleet).Append(" คำขอ");
        }
        return text.ToString();
    }
}
