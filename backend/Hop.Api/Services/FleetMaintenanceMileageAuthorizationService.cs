using Hop.Api.Authorization;

namespace Hop.Api.Services;

public sealed record FleetMileageDecision(bool IsOverride, decimal PreviousMileage, decimal? SubmittedMileage, decimal RetainedMileage, Guid EffectiveActorUserId, Guid? DelegationId, Guid? DelegatorUserId);

public sealed class FleetMaintenanceMileageAuthorizationService(IWorkflowPermissionValidator permissions, IWorkflowDelegationResolver delegations)
{
    public async Task<FleetMileageDecision> Validate(Guid actorUserId, decimal currentMileage, decimal? submittedMileage, string? reason, CancellationToken ct)
    {
        var isOverride = submittedMileage is not null && submittedMileage.Value < currentMileage;
        if (!isOverride) return new(false, currentMileage, submittedMileage, Math.Max(currentMileage, submittedMileage ?? currentMileage), actorUserId, null, null);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Mileage override reason is required.");
        if (!await permissions.HasAsync(actorUserId, FleetPermissions.MaintenanceOverrideMileage, ct)) throw new UnauthorizedAccessException("FleetMaintenance.OverrideMileage permission is required.");
        var delegation = await delegations.ResolveAsync(actorUserId, "FLEET", FleetPermissions.MaintenanceOverrideMileage, DateTime.UtcNow, ct);
        return new(true, currentMileage, submittedMileage, currentMileage, delegation?.ApproverUserId ?? actorUserId, delegation?.Id, delegation?.ApproverUserId);
    }
}
