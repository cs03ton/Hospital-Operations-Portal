using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public static class MeetingRoomGroupSubscriptions
{
    public const string EventType = "MeetingRoom.BookingCreated";
    public const string Prefix = EventType + ".Room.";
    public static string ForRoom(Guid id) => Prefix + id.ToString("D");
    public static bool TryRoom(string value, out Guid room) { room = default; return value.StartsWith(Prefix, StringComparison.Ordinal) && Guid.TryParse(value[Prefix.Length..], out room); }
    public static async Task<Dictionary<string, bool>> DefinitionsAsync(AppDbContext db, CancellationToken ct)
    {
        var result = new Dictionary<string, bool>(Hop.Api.Models.LineGroupEvents.Defaults);
        foreach (var id in await db.MeetingRooms.AsNoTracking().Select(x => x.Id).ToListAsync(ct)) result[ForRoom(id)] = false;
        return result;
    }
}
