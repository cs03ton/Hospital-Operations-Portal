using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record WorkflowTransitionRequest(string Scope, string EntityType, Guid EntityId, string CurrentState, string Action, string TargetState, Guid ActorUserId, string RequiredPermissionCode, Guid ConcurrencyToken, string? Reason, string? ReturnTarget = null);
public sealed record WorkflowTransitionResult(string PreviousState, string NewState, string Action, Guid ActorUserId, Guid EffectiveActorUserId, Guid? DelegationId, Guid? DelegatorUserId, DateTime OccurredAt);
public sealed record WorkflowRule(string Source, string Action, string Target, string Permission, bool ReasonRequired = false, bool DelegationAllowed = false, string[]? ReturnTargets = null);

public interface IWorkflowDefinition { WorkflowRule? Find(string source, string action, string target); }
public interface IWorkflowPermissionValidator { Task<bool> HasAsync(Guid userId, string permission, CancellationToken ct); }
public interface IWorkflowDelegationResolver { Task<ApprovalDelegation?> ResolveAsync(Guid delegateUserId, string scope, string permission, DateTime atUtc, CancellationToken ct); }
public interface IWorkflowTransitionService { Task<WorkflowTransitionResult> ValidateAsync(WorkflowTransitionRequest request, CancellationToken ct); }

public sealed class WorkflowPermissionValidator(AppDbContext db) : IWorkflowPermissionValidator
{
    public Task<bool> HasAsync(Guid userId, string permission, CancellationToken ct) => db.UserRoles.AsNoTracking().Where(x => x.UserId == userId && x.Role != null && x.Role.IsActive).SelectMany(x => x.Role!.RolePermissions).AnyAsync(x => x.Permission != null && x.Permission.IsActive && x.Permission.Code == permission, ct);
}

public sealed class WorkflowDelegationResolver(AppDbContext db, IWorkflowPermissionValidator permissions) : IWorkflowDelegationResolver
{
    public async Task<ApprovalDelegation?> ResolveAsync(Guid delegateUserId, string scope, string permission, DateTime atUtc, CancellationToken ct)
    {
        if (!await permissions.HasAsync(delegateUserId, permission, ct)) return null;
        return await db.ApprovalDelegations.AsNoTracking().Where(x => x.IsActive && x.DelegateUserId == delegateUserId && x.Scope == scope && x.RequiredPermissionCode == permission && x.StartAt <= atUtc && x.EndAt > atUtc).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
    }
}

public sealed class FleetWorkflowDefinition : IWorkflowDefinition
{
    private static readonly WorkflowRule[] Rules =
    [
        new(FleetRequestStatuses.PendingAdminReview, "ADMIN_APPROVE", FleetRequestStatuses.PendingDirector, Authorization.FleetPermissions.AdminReviewApprove),
        new(FleetRequestStatuses.PendingAdminReview, "ADMIN_RETURN", FleetRequestStatuses.Returned, Authorization.FleetPermissions.AdminReviewReturn, true, false, [FleetReturnTargets.Requester, FleetReturnTargets.Dispatcher]),
        new(FleetRequestStatuses.PendingAdminReview, "ADMIN_REJECT", FleetRequestStatuses.Rejected, Authorization.FleetPermissions.AdminReviewReject, true),
        new(FleetRequestStatuses.PendingDirector, "DIRECTOR_APPROVE", FleetRequestStatuses.PendingDriverAck, Authorization.FleetPermissions.DirectorApprove, false, true),
        new(FleetRequestStatuses.PendingDirector, "DIRECTOR_RETURN", FleetRequestStatuses.Returned, Authorization.FleetPermissions.DirectorReturn, true, true, [FleetReturnTargets.AdminReview, FleetReturnTargets.Dispatcher]),
        new(FleetRequestStatuses.PendingDirector, "DIRECTOR_REJECT", FleetRequestStatuses.Rejected, Authorization.FleetPermissions.DirectorReject, true, true),
        new(FleetRequestStatuses.PendingDriverAck, "DRIVER_ACCEPT", FleetRequestStatuses.Ready, Authorization.FleetPermissions.DriverAcknowledge),
        new(FleetRequestStatuses.PendingDriverAck, "DRIVER_DECLINE", FleetRequestStatuses.Returned, Authorization.FleetPermissions.DriverAcknowledge, true, false, [FleetReturnTargets.Dispatcher]),
        new(FleetRequestStatuses.Ready, "TRIP_START", FleetRequestStatuses.InProgress, Authorization.FleetPermissions.DriverStart),
        new(FleetRequestStatuses.InProgress, "TRIP_COMPLETE", FleetRequestStatuses.Completed, Authorization.FleetPermissions.DriverComplete),
    ];
    public WorkflowRule? Find(string source, string action, string target) => Rules.FirstOrDefault(x => x.Source == source && x.Action == action && x.Target == target);
}

public sealed class WorkflowTransitionService(IWorkflowDefinition definition, IWorkflowPermissionValidator permissions, IWorkflowDelegationResolver delegations) : IWorkflowTransitionService
{
    public async Task<WorkflowTransitionResult> ValidateAsync(WorkflowTransitionRequest request, CancellationToken ct)
    {
        if (request.Scope != "FLEET") throw new InvalidOperationException("Unsupported workflow scope.");
        var rule = definition.Find(request.CurrentState, request.Action, request.TargetState) ?? throw new InvalidOperationException("Invalid workflow transition.");
        if (rule.Permission != request.RequiredPermissionCode) throw new UnauthorizedAccessException("Workflow permission mismatch.");
        if (rule.ReasonRequired && string.IsNullOrWhiteSpace(request.Reason)) throw new ArgumentException("Reason is required.");
        if (request.ReturnTarget is not null && (rule.ReturnTargets is null || !rule.ReturnTargets.Contains(request.ReturnTarget))) throw new ArgumentException("Invalid return target.");
        if (!await permissions.HasAsync(request.ActorUserId, rule.Permission, ct)) throw new UnauthorizedAccessException("Permission denied.");
        ApprovalDelegation? delegation = null;
        if (rule.DelegationAllowed) delegation = await delegations.ResolveAsync(request.ActorUserId, request.Scope, rule.Permission, DateTime.UtcNow, ct);
        return new(request.CurrentState, request.TargetState, request.Action, request.ActorUserId, delegation?.ApproverUserId ?? request.ActorUserId, delegation?.Id, delegation?.ApproverUserId, DateTime.UtcNow);
    }
}
