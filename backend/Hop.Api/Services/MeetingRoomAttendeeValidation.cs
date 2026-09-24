using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public static class MeetingRoomAttendeeValidation
{
    public static async Task<string?> ValidateAsync(AppDbContext db, IReadOnlyList<Guid>? attendeeIds, Guid bookerId, int total, CancellationToken ct)
    {
        if (attendeeIds is null) return null;
        if (attendeeIds.Count > 199) return "ระบุรายชื่อผู้เข้าประชุมได้สูงสุด 199 คน";
        var distinct = attendeeIds.Append(bookerId).Distinct().ToArray();
        if (total < distinct.Length) return "จำนวนผู้เข้าประชุมน้อยกว่าจำนวนรายชื่อรวมผู้จอง";
        var selected = distinct.Where(x => x != bookerId).ToArray();
        var found = await db.Users.CountAsync(x => selected.Contains(x.Id) && x.IsActive &&
            !x.UserRoles.Any(ur => ur.Role != null && (ur.Role.Name == "Admin" || ur.Role.Name == "SuperAdmin")), ct);
        return found == selected.Length ? null : "รายชื่อผู้เข้าประชุมมีบัญชีที่ไม่พร้อมใช้งานหรือเป็น Admin";
    }
}
