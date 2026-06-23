using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController(IPendingApprovalNotificationService notificationService) : ControllerBase
{
    [HttpGet("my-pending")]
    [RequirePermission(LeavePermissions.ViewPendingApproval)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingApprovalNotificationResponse>>>> GetMyPendingApprovals(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<PendingApprovalNotificationResponse>>.Fail("Invalid access token."));
        }

        var items = await notificationService.GetMyPendingApprovalsAsync(userId.Value, cancellationToken);
        return ApiResponse<IReadOnlyList<PendingApprovalNotificationResponse>>.Ok(items);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
