using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

internal static class FleetSchedulingLock
{
    // Take before reading assignments, in every assignment writer. This also avoids
    // taking resource locks in opposite orders when two swaps share a request.
    internal static async Task EnterAsync(AppDbContext db, CancellationToken ct)
    {
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"fleet-scheduling"}, 0))", ct);
    }

    internal static async Task ResourcesAsync(AppDbContext db, IEnumerable<Guid> requests, IEnumerable<Guid> vehicles, IEnumerable<Guid> drivers, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return;
        var keys = requests.Select(x => $"fleet-request:{x}").Concat(vehicles.Select(x => $"fleet-vehicle:{x}"))
            .Concat(drivers.Select(x => $"fleet-driver:{x}")).Distinct().OrderBy(x => x, StringComparer.Ordinal);
        foreach (var key in keys)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
    }
}
