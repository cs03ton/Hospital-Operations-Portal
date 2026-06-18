using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/approval-chain-steps")]
[Authorize]
public class ApprovalChainStepsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [RequirePermission("ApprovalChain.Edit")]
    public async Task<ActionResult<ApiResponse<ApprovalChainStepResponse>>> UpdateStep(Guid id, SaveApprovalChainStepRequest request)
    {
        var item = await db.ApprovalChainSteps.FirstOrDefaultAsync(step => step.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<ApprovalChainStepResponse>.Fail("Approval chain step not found."));
        }

        var validation = await ValidateStepRequest(item.ApprovalChainId, request, id);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<ApprovalChainStepResponse>.Fail(validation));
        }

        item.StepOrder = request.StepOrder;
        item.Name = request.Name.Trim();
        item.ApproverRoleId = request.ApproverRoleId;
        item.ApproverUserId = request.ApproverUserId;
        item.RequiredPermissionCode = request.RequiredPermissionCode.Trim();
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalChain.StepEdit", "ApprovalChainStep", item.Id.ToString(), $"Updated approval step {item.Name}.", "Success", HttpContext);

        var updated = await LoadSteps().SingleAsync(step => step.Id == item.Id);
        return ApiResponse<ApprovalChainStepResponse>.Ok(ToStepResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("ApprovalChain.Delete")]
    public async Task<IActionResult> DeleteStep(Guid id)
    {
        var item = await db.ApprovalChainSteps.FirstOrDefaultAsync(step => step.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<string>.Fail("Approval chain step not found."));
        }

        db.ApprovalChainSteps.Remove(item);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalChain.StepDelete", "ApprovalChainStep", item.Id.ToString(), $"Deleted approval step {item.Name}.", "Success", HttpContext);

        return NoContent();
    }

    private IQueryable<ApprovalChainStep> LoadSteps()
    {
        return db.ApprovalChainSteps
            .AsNoTracking()
            .Include(item => item.ApproverRole)
            .Include(item => item.ApproverUser);
    }

    private async Task<string?> ValidateStepRequest(Guid approvalChainId, SaveApprovalChainStepRequest request, Guid? id = null)
    {
        if (request.StepOrder <= 0)
        {
            return "Step order must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Step name is required.";
        }

        if (request.ApproverRoleId is null && request.ApproverUserId is null)
        {
            return "Approver role or approver user is required.";
        }

        if (request.ApproverRoleId is not null && !await db.Roles.AnyAsync(item => item.Id == request.ApproverRoleId && item.IsActive))
        {
            return "Approver role not found.";
        }

        if (request.ApproverUserId is not null && !await db.Users.AnyAsync(item => item.Id == request.ApproverUserId && item.IsActive))
        {
            return "Approver user not found.";
        }

        if (!await db.Permissions.AnyAsync(item => item.Code == request.RequiredPermissionCode && item.IsActive))
        {
            return "Required permission not found.";
        }

        if (await db.ApprovalChainSteps.AnyAsync(item => item.Id != id && item.ApprovalChainId == approvalChainId && item.StepOrder == request.StepOrder))
        {
            return "Step order already exists in this approval chain.";
        }

        return null;
    }

    private static ApprovalChainStepResponse ToStepResponse(ApprovalChainStep item)
    {
        return new ApprovalChainStepResponse(
            item.Id,
            item.ApprovalChainId,
            item.StepOrder,
            item.Name,
            item.ApproverRoleId,
            item.ApproverRole?.Name,
            item.ApproverUserId,
            item.ApproverUser?.FullName,
            item.RequiredPermissionCode,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
