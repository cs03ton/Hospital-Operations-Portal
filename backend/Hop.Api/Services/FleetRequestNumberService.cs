using System.Globalization;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class FleetRequestNumberService(AppDbContext db)
{
    public async Task<string> GenerateAsync(DateTime createdAtUtc, CancellationToken cancellationToken)
    {
        var prefix = $"VH-{createdAtUtc.ToString("yyyyMM", CultureInfo.InvariantCulture)}-";
        if (db.Database.IsRelational())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({prefix}))", cancellationToken);

        var latest = await db.FleetRequests.Where(x => x.RequestNo.StartsWith(prefix)).OrderByDescending(x => x.RequestNo).Select(x => x.RequestNo).FirstOrDefaultAsync(cancellationToken);
        var sequence = latest is not null && int.TryParse(latest[prefix.Length..], out var current) ? current + 1 : 1;
        return $"{prefix}{sequence:0000}";
    }
}
