using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class FleetPermissionDiagnosticsService(AppDbContext db)
{
    public async Task<FleetPermissionDiagnostic> GetAsync(CancellationToken ct)
    {
        var expected = typeof(FleetPermissions).GetFields().Select(x => (string)x.GetRawConstantValue()!).ToHashSet(StringComparer.Ordinal);
        var definitions = await db.Permissions.AsNoTracking().Where(x => x.Code.StartsWith("Fleet")).Select(x => x.Code).ToListAsync(ct);
        var missing = expected.Except(definitions).Order().ToList(); var duplicates = definitions.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).Order().ToList();
        var roles = await db.Roles.AsNoTracking().Where(x => x.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.Code.StartsWith("Fleet"))).Select(x => new { x.Name, Codes = x.RolePermissions.Where(rp => rp.Permission != null && rp.Permission.Code.StartsWith("Fleet")).Select(rp => rp.Permission!.Code).ToList() }).ToListAsync(ct);
        var roleRows = roles.Select(x => new FleetRolePermissionDiagnostic(x.Name, x.Codes, IsProductionRole(x.Name) ? x.Codes : [])).ToList();
        return new(missing.Count == 0 && duplicates.Count == 0 && roleRows.All(x => x.UnexpectedPermissions.Count == 0) ? "Healthy" : "Warning", missing, duplicates, roleRows, false);
    }
    private static bool IsProductionRole(string name) => new[] { "Staff", "DepartmentHead", "Director", "LeaveAdmin", "Admin" }.Contains(name, StringComparer.OrdinalIgnoreCase);
}
