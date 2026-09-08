using System.Security.Claims;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed record FleetRolloutSnapshot(string Mode, IReadOnlyList<Guid> UatUserIds, IReadOnlyList<string> UatRoleCodes, Guid? ConcurrencyToken, bool IsDatabaseOverride, bool IsAllowed, string Reason);
public interface IFleetRolloutService
{
    Task<FleetRolloutSnapshot> GetAsync(ClaimsPrincipal user, CancellationToken ct);
}

public sealed class FleetRolloutService(AppDbContext db, IOptions<FleetRolloutOptions> configured) : IFleetRolloutService
{
    public async Task<FleetRolloutSnapshot> GetAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var stored = await db.FleetRolloutSettings.AsNoTracking().OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        var mode = stored?.Mode ?? configured.Value.RolloutMode;
        var userIds = stored?.UatUserIds ?? configured.Value.UatUserIds;
        var roleCodes = stored?.UatRoleCodes ?? configured.Value.UatRoleCodes;
        var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
        var roles = user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowed = mode.Equals(FleetRolloutModes.Enabled, StringComparison.OrdinalIgnoreCase) || mode.Equals(FleetRolloutModes.UatOnly, StringComparison.OrdinalIgnoreCase) && (userIds.Contains(userId) || roleCodes.Any(roles.Contains));
        var reason = mode.Equals(FleetRolloutModes.Disabled, StringComparison.OrdinalIgnoreCase) ? "Fleet module is disabled." : allowed ? "Access allowed by rollout policy." : "User is not in the Fleet UAT allowlist.";
        return new(mode, userIds, roleCodes, stored?.ConcurrencyToken, stored is not null, allowed, reason);
    }
}
