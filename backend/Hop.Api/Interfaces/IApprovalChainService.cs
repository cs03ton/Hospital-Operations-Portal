using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public sealed record ApprovalStepPlan(
    Guid? ApprovalChainId,
    Guid? ApprovalChainStepId,
    int StepOrder,
    string StepName,
    Guid ApproverId,
    string RequiredPermissionCode
);

public interface IApprovalChainService
{
    Task<IReadOnlyList<ApprovalStepPlan>> BuildApprovalPlanAsync(LeaveRequest leaveRequest);
}
