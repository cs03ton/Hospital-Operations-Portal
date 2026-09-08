using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public interface INotificationRecipientResolver
{
    Task<IReadOnlyList<Guid>> ResolveAsync(string eventType, Guid fleetRequestId, CancellationToken ct);
}

public sealed class FleetNotificationRecipientResolver(AppDbContext db) : INotificationRecipientResolver
{
    public async Task<IReadOnlyList<Guid>> ResolveAsync(string eventType, Guid fleetRequestId, CancellationToken ct)
    {
        var request = await db.FleetRequests.AsNoTracking().Include(x => x.Assignments)
            .SingleOrDefaultAsync(x => x.Id == fleetRequestId, ct);
        if (request is null && eventType.StartsWith("FleetMaintenance", StringComparison.Ordinal))
        {
            var vehicle = await db.FleetVehicleMaintenanceSchedules.AsNoTracking().Where(x => x.Id == fleetRequestId).Select(x => new { ResponsibleUserId = x.Vehicle!.ResponsibleUserId }).FirstOrDefaultAsync(ct);
            var maintenanceRecipients = new HashSet<Guid>();
            if (vehicle?.ResponsibleUserId is not null) maintenanceRecipients.Add(vehicle.ResponsibleUserId.Value);
            await AddByPermission(maintenanceRecipients, FleetPermissions.MaintenanceManage, null, false, ct);
            await AddByPermission(maintenanceRecipients, FleetPermissions.DispatchView, null, false, ct);
            return maintenanceRecipients.ToList();
        }
        if (request is null) return [];

        var recipients = new HashSet<Guid>();
        var activeDriver = request.Assignments.SingleOrDefault(x => x.IsActive)?.DriverUserId;
        if (eventType.StartsWith("FleetEmergency.", StringComparison.Ordinal))
        {
            recipients.Add(request.RequesterUserId);
            if (request.ReportedByUserId is not null) recipients.Add(request.ReportedByUserId.Value);
            if (activeDriver is not null && eventType is "FleetEmergency.Assigned" or "FleetEmergency.ApprovalBypassed" or "FleetEmergency.DriverAccepted") recipients.Add(activeDriver.Value);
        }

        if (eventType is "Fleet.RequestRejected" or "Fleet.DirectorApproved" or "Fleet.CancellationApproved" or "Fleet.CancellationRejected" or "Fleet.AssignmentReplaced" or "Fleet.TripAborted")
            recipients.Add(request.RequesterUserId);
        if (activeDriver is not null && (eventType is "Fleet.DirectorApproved" or "Fleet.DriverAcknowledgementRequested" or "Fleet.CancellationApproved" or "Fleet.AssignmentReplaced" or "Fleet.TripAborted"))
            recipients.Add(activeDriver.Value);

        if (eventType == "Fleet.RequestReturned")
        {
            if (request.ReturnTarget == FleetReturnTargets.Requester) recipients.Add(request.RequesterUserId);
            else await AddByPermission(recipients, FleetPermissions.DispatchView, request.RequesterDepartmentId, false, ct);
        }

        foreach (var permission in PermissionsFor(eventType))
            await AddByPermission(recipients, permission, request.RequesterDepartmentId, false, ct);

        if (eventType == "Fleet.AssignmentReplaced")
            foreach (var driverId in request.Assignments.Select(x => x.DriverUserId)) recipients.Add(driverId);

        return recipients.ToList();
    }

    private async Task AddByPermission(HashSet<Guid> recipients, string permission, Guid? departmentId, bool sameDepartmentOnly, CancellationToken ct)
    {
        var query = db.UserRoles.AsNoTracking().Where(x => x.User != null && x.User.IsActive && x.Role != null && x.Role.IsActive && x.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.IsActive && rp.Permission.Code == permission));
        if (sameDepartmentOnly && departmentId is not null) query = query.Where(x => x.User!.DepartmentId == departmentId);
        foreach (var id in await query.Select(x => x.UserId).Distinct().ToListAsync(ct)) recipients.Add(id);

        var now = DateTime.UtcNow;
        var delegates = await db.ApprovalDelegations.AsNoTracking()
            .Where(x => x.IsActive && x.Scope == "FLEET" && x.RequiredPermissionCode == permission && x.StartAt <= now && x.EndAt > now &&
                db.UserRoles.Any(ur => ur.UserId == x.DelegateUserId && ur.Role != null && ur.Role.IsActive && ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.IsActive && rp.Permission.Code == permission)))
            .Select(x => x.DelegateUserId).Distinct().ToListAsync(ct);
        foreach (var id in delegates) recipients.Add(id);
    }

    private static IReadOnlyList<string> PermissionsFor(string eventType) => eventType switch
    {
        "Fleet.RequestSubmitted" or "Fleet.DriverDeclined" or "Fleet.TripCompleted" or "Fleet.TripAborted" or "Fleet.CancellationApproved" or "Fleet.AssignmentReplaced" => [FleetPermissions.DispatchView],
        "Fleet.Assigned" => [FleetPermissions.AdminReviewApprove],
        "Fleet.AdminReviewApproved" => [FleetPermissions.DirectorApprove],
        "Fleet.CancellationRequested" => [FleetPermissions.CancellationReview, FleetPermissions.DirectorApprove],
        "FleetEmergency.Submitted" or "FleetEmergency.Assigned" => [FleetPermissions.EmergencyDispatch],
        "FleetEmergency.ApprovalBypassed" or "FleetEmergency.TripCompleted" => [FleetPermissions.EmergencyReview],
        "Fleet.CompatibilityOverridden" => [FleetPermissions.CompatibilityOverride, FleetPermissions.EmergencyViewAudit],
        _ => []
    };
}
