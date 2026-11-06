using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Authorization;

public static class CorrespondenceAccess
{
    public const string Permission = "Correspondence.Use";
    public const string RoleName = "ผู้ใช้งานสารบรรณ";
    public const string DeniedMessage = "เฉพาะ SuperAdmin เท่านั้นที่กำหนดสิทธิ์ใช้งานสารบรรณได้";
    public static bool IsProtected(Role role) => role.Name is RoleName or "Admin" or "SuperAdmin" || role.RolePermissions.Any(p => p.Permission?.Code == Permission);
    public static Task<bool> IsSuperAdminAsync(AppDbContext db, Guid? actor) => db.UserRoles.AsNoTracking().AnyAsync(r =>
        r.UserId == actor && r.User != null && r.User.IsActive && r.Role != null && r.Role.IsActive && r.Role.Name == "SuperAdmin");
    public static Task<bool> IsProtectedRoleAsync(AppDbContext db, Guid id) => db.Roles.AsNoTracking().AnyAsync(r => r.Id == id &&
        (r.Name == RoleName || r.Name == "Admin" || r.Name == "SuperAdmin" || r.RolePermissions.Any(p => p.Permission!.Code == Permission)));
    public static async Task<bool> CanChangeUserRolesAsync(AppDbContext db, Guid? actor, IEnumerable<Guid> before, IEnumerable<Guid> after)
    {
        var changed = before.Except(after).Concat(after.Except(before)).ToArray();
        if (!await db.Roles.AnyAsync(r => changed.Contains(r.Id) && (r.Name == RoleName || r.Name == "Admin" || r.Name == "SuperAdmin" || r.RolePermissions.Any(p => p.Permission!.Code == Permission)))) return true;
        return await IsSuperAdminAsync(db, actor);
    }
}
