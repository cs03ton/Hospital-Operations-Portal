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
[Route("api/approval-delegations")]
[Authorize]
public class ApprovalDelegationsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ApprovalDelegation.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalDelegationResponse>>>> GetDelegations()
    {
        var items = await db.ApprovalDelegations
            .AsNoTracking()
            .Include(item => item.ApproverUser)
            .Include(item => item.DelegateUser)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<ApprovalDelegationResponse>>.Ok(items);
    }

    [HttpPost]
    [RequirePermission("ApprovalDelegation.Create")]
    public async Task<ActionResult<ApiResponse<ApprovalDelegationResponse>>> CreateDelegation(SaveApprovalDelegationRequest request)
    {
        if (request.EndDate < request.StartDate)
        {
            return BadRequest(ApiResponse<ApprovalDelegationResponse>.Fail("ช่วงวันที่มอบหมายไม่ถูกต้อง"));
        }

        var delegation = new ApprovalDelegation
        {
            ApproverUserId = request.ApproverUserId,
            DelegateUserId = request.DelegateUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason.Trim(),
            IsActive = request.IsActive
        };

        db.ApprovalDelegations.Add(delegation);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalDelegation.Create", "ApprovalDelegation", delegation.Id.ToString(), "Created approval delegation.", "Success", HttpContext);

        var created = await LoadDelegation(delegation.Id);
        return ApiResponse<ApprovalDelegationResponse>.Ok(ToResponse(created!));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("ApprovalDelegation.Edit")]
    public async Task<ActionResult<ApiResponse<ApprovalDelegationResponse>>> UpdateDelegation(Guid id, SaveApprovalDelegationRequest request)
    {
        var delegation = await db.ApprovalDelegations.FirstOrDefaultAsync(item => item.Id == id);
        if (delegation is null)
        {
            return NotFound(ApiResponse<ApprovalDelegationResponse>.Fail("Approval delegation not found."));
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(ApiResponse<ApprovalDelegationResponse>.Fail("ช่วงวันที่มอบหมายไม่ถูกต้อง"));
        }

        delegation.ApproverUserId = request.ApproverUserId;
        delegation.DelegateUserId = request.DelegateUserId;
        delegation.StartDate = request.StartDate;
        delegation.EndDate = request.EndDate;
        delegation.Reason = request.Reason.Trim();
        delegation.IsActive = request.IsActive;
        delegation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalDelegation.Update", "ApprovalDelegation", delegation.Id.ToString(), "Updated approval delegation.", "Success", HttpContext);

        var updated = await LoadDelegation(id);
        return ApiResponse<ApprovalDelegationResponse>.Ok(ToResponse(updated!));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("ApprovalDelegation.Delete")]
    public async Task<IActionResult> DeleteDelegation(Guid id)
    {
        var delegation = await db.ApprovalDelegations.FirstOrDefaultAsync(item => item.Id == id);
        if (delegation is null)
        {
            return NotFound(ApiResponse<string>.Fail("Approval delegation not found."));
        }

        delegation.IsActive = false;
        delegation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalDelegation.Delete", "ApprovalDelegation", delegation.Id.ToString(), "Deactivated approval delegation.", "Success", HttpContext);
        return NoContent();
    }

    private Task<ApprovalDelegation?> LoadDelegation(Guid id)
    {
        return db.ApprovalDelegations
            .AsNoTracking()
            .Include(item => item.ApproverUser)
            .Include(item => item.DelegateUser)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    private static ApprovalDelegationResponse ToResponse(ApprovalDelegation item)
    {
        return new ApprovalDelegationResponse(
            item.Id,
            item.ApproverUserId,
            item.ApproverUser?.FullName,
            item.DelegateUserId,
            item.DelegateUser?.FullName,
            item.StartDate,
            item.EndDate,
            item.Reason,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
