using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record RepairAccess(Guid UserId, HashSet<string> Permissions)
{
    public bool Admin => Permissions.Contains(RepairPermissions.Manage);
    public bool Work(string team) => team switch {
        "IT" => Permissions.Contains(RepairPermissions.WorkIT),
        "GENERAL" => Permissions.Contains(RepairPermissions.WorkGeneral),
        _ => false
    };
    public bool View(RepairRequest r) => Permissions.Contains(RepairPermissions.ViewAll) || Work(r.TeamCode) ||
        (r.RequesterId == UserId && Permissions.Contains(RepairPermissions.ViewOwn));
}

public static class RepairWorkflow
{
    public static readonly string[] Active = ["Submitted", "InProgress", "WaitingParts"];
    public static async Task<RepairAccess> Access(AppDbContext db, Guid actor, CancellationToken ct)
    {
        var codes = await db.UserRoles.Where(x => x.UserId == actor && x.Role != null && x.Role.IsActive)
            .SelectMany(x => x.Role!.RolePermissions).Where(x => x.Permission != null && x.Permission.IsActive)
            .Select(x => x.Permission!.Code).ToListAsync(ct);
        return new(actor, codes.ToHashSet());
    }

    public static string? Next(RepairRequest r, string action, RepairAccess access)
    {
        var own = r.RequesterId == access.UserId && access.Permissions.Contains(RepairPermissions.ViewOwn);
        var work = access.Work(r.TeamCode);
        return action switch
        {
            "start" when work && r.Status == "Submitted" => "InProgress",
            "wait" when work && r.Status == "InProgress" => "WaitingParts",
            "resume" when work && r.Status == "WaitingParts" => "InProgress",
            "return" when work && Active.Contains(r.Status) => "Returned",
            "priority" when work && Active.Contains(r.Status) => r.Status,
            "note" when (work || own) && r.Status != "Cancelled" && r.Status != "Closed" => r.Status,
            "solve" when work && r.Status == "InProgress" => "Resolved",
            "accept" when own && r.Status == "Resolved" => "Closed",
            "reject-solution" when own && r.Status == "Resolved" => "InProgress",
            "reopen" when own && r.Status == "Closed" => "Submitted",
            "resubmit" when own && r.Status == "Returned" => "Submitted",
            "cancel" when r.Status is not ("Closed" or "Cancelled") && (access.Admin || (own && !r.HasStarted)) => "Cancelled",
            _ => null
        };
    }
}
