using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class AnnouncementAudienceResolver(AppDbContext db) : IAnnouncementAudienceResolver
{
    public async Task<AnnouncementAudienceResult> ResolveAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        var targets = announcement.Targets.Count == 0
            ? [new AnnouncementTarget { TargetType = AnnouncementTargetTypes.Everyone }]
            : announcement.Targets.ToList();

        var candidateIds = new HashSet<Guid>();
        if (targets.Any(target => string.Equals(target.TargetType, AnnouncementTargetTypes.Everyone, StringComparison.OrdinalIgnoreCase)))
        {
            candidateIds.UnionWith(await db.Users
                .AsNoTracking()
                .Select(user => user.Id)
                .ToListAsync(cancellationToken));
        }

        foreach (var target in targets)
        {
            if (string.IsNullOrWhiteSpace(target.TargetValue) &&
                !string.Equals(target.TargetType, AnnouncementTargetTypes.Everyone, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(target.TargetType, AnnouncementTargetTypes.User, StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(target.TargetValue, out var userId))
            {
                candidateIds.Add(userId);
                continue;
            }

            if (string.Equals(target.TargetType, AnnouncementTargetTypes.Department, StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(target.TargetValue, out var departmentId))
            {
                candidateIds.UnionWith(await db.Users
                    .AsNoTracking()
                    .Where(user => user.DepartmentId == departmentId)
                    .Select(user => user.Id)
                    .ToListAsync(cancellationToken));
                continue;
            }

            if (string.Equals(target.TargetType, AnnouncementTargetTypes.Role, StringComparison.OrdinalIgnoreCase))
            {
                var roleName = target.TargetValue!.Trim();
                candidateIds.UnionWith(await db.UserRoles
                    .AsNoTracking()
                    .Where(userRole => userRole.Role != null &&
                        userRole.Role.IsActive &&
                        userRole.Role.Name == roleName)
                    .Select(userRole => userRole.UserId)
                    .ToListAsync(cancellationToken));
                continue;
            }

            if (string.Equals(target.TargetType, AnnouncementTargetTypes.Permission, StringComparison.OrdinalIgnoreCase))
            {
                var permissionCode = target.TargetValue!.Trim();
                candidateIds.UnionWith(await db.UserRoles
                    .AsNoTracking()
                    .Where(userRole => userRole.Role != null &&
                        userRole.Role.IsActive &&
                        userRole.Role.RolePermissions.Any(rolePermission =>
                            rolePermission.Permission != null &&
                            rolePermission.Permission.IsActive &&
                            rolePermission.Permission.Code == permissionCode))
                    .Select(userRole => userRole.UserId)
                    .ToListAsync(cancellationToken));
            }
        }

        if (candidateIds.Count == 0)
        {
            return new AnnouncementAudienceResult([], 0, 0, 0, 0, 0);
        }

        var users = await db.Users
            .AsNoTracking()
            .Where(user => candidateIds.Contains(user.Id))
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        var activeUsers = users.Where(user => user.IsActive).ToList();
        var lineBound = activeUsers.Count(user => !string.IsNullOrWhiteSpace(user.LineUserId));

        return new AnnouncementAudienceResult(
            activeUsers,
            users.Count,
            activeUsers.Count,
            users.Count - activeUsers.Count,
            lineBound,
            activeUsers.Count - lineBound);
    }
}
