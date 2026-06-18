using Hop.Api.Data;
using Hop.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class AuditRetentionService(AppDbContext db, IConfiguration configuration) : IAuditRetentionService
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var retentionDays = configuration.GetValue("AuditLog:RetentionDays", 365);
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var expiredLogs = await db.AuditLogs
            .Where(item => item.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        db.AuditLogs.RemoveRange(expiredLogs);
        await db.SaveChangesAsync(cancellationToken);
        return expiredLogs.Count;
    }
}
