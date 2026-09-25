using System.Text.Json;
using Hop.Api.Models;

namespace Hop.Api.Services;

public static class MeetingRoomLineFlexMessageTemplates
{
    public static string BookingConfirmed(MeetingRoomBooking booking, string roomName, string publicAppUrl)
    {
        var number = $"MR-{booking.Number:D6}";
        var detailUrl = DetailUrl(publicAppUrl, booking.Id);
        var bubble = new Dictionary<string, object>
        {
            ["type"] = "bubble",
            ["size"] = "mega",
            ["header"] = new
            {
                type = "box", layout = "vertical", backgroundColor = "#175548", paddingAll = "20px",
                contents = new object[]
                {
                    new { type = "text", text = "HOP · จองห้องประชุม", color = "#FFFFFF", size = "sm", weight = "bold" },
                    new { type = "text", text = "ยืนยันการจองแล้ว", color = "#FFFFFF", size = "xl", weight = "bold", margin = "md", wrap = true }
                }
            },
            ["body"] = new
            {
                type = "box", layout = "vertical", paddingAll = "20px", spacing = "md",
                contents = new object[]
                {
                    Row("เลขจอง", number),
                    Row("ห้อง", Clean(roomName, 120)),
                    Row("หัวข้อ", Clean(booking.Subject, 180)),
                    Row("เริ่ม", ThaiDateDisplay.InstantWithSuffix(booking.StartAt)),
                    Row("สิ้นสุด", ThaiDateDisplay.InstantWithSuffix(booking.EndAt))
                }
            }
        };

        if (detailUrl is not null)
        {
            bubble["footer"] = new
            {
                type = "box", layout = "vertical", paddingAll = "16px",
                contents = new object[]
                {
                    new { type = "button", style = "primary", color = "#175548",
                        action = new { type = "uri", label = "เปิดรายละเอียดคำขอ", uri = detailUrl } }
                }
            };
        }

        return JsonSerializer.Serialize(new
        {
            to = string.Empty,
            messages = new object[] { new { type = "flex", altText = $"ยืนยันการจองห้องประชุมแล้ว · {number}", contents = bubble } }
        });
    }

    private static object Row(string label, string value) => new
    {
        type = "box", layout = "horizontal", spacing = "md",
        contents = new object[]
        {
            new { type = "text", text = label, size = "sm", color = "#64748B", flex = 2 },
            new { type = "text", text = value, size = "sm", color = "#172554", weight = "bold", wrap = true, flex = 5 }
        }
    };

    private static string Clean(string? value, int maximum)
    {
        var cleaned = string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(cleaned) ? "-" : cleaned.Length <= maximum ? cleaned : cleaned[..(maximum - 1)] + "…";
    }

    private static string? DetailUrl(string? publicAppUrl, Guid bookingId)
    {
        if (!Uri.TryCreate(publicAppUrl?.Trim(), UriKind.Absolute, out var root) || root.Scheme != Uri.UriSchemeHttps)
            return null;
        return $"{root.GetLeftPart(UriPartial.Authority)}{root.AbsolutePath.TrimEnd('/')}/meeting-rooms/bookings/{bookingId}";
    }
}
