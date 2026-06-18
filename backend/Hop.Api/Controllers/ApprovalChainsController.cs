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
[Route("api/approval-chains")]
[Authorize]
public class ApprovalChainsController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ApprovalChain.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalChainResponse>>>> GetApprovalChains()
    {
        var items = await LoadChains()
            .OrderBy(item => item.Name)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<ApprovalChainResponse>>.Ok(items);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("ApprovalChain.View")]
    public async Task<ActionResult<ApiResponse<ApprovalChainResponse>>> GetApprovalChain(Guid id)
    {
        var item = await LoadChains().FirstOrDefaultAsync(chain => chain.Id == id);
        return item is null
            ? NotFound(ApiResponse<ApprovalChainResponse>.Fail("Approval chain not found."))
            : ApiResponse<ApprovalChainResponse>.Ok(ToResponse(item));
    }

    [HttpPost]
    [RequirePermission("ApprovalChain.Create")]
    public async Task<ActionResult<ApiResponse<ApprovalChainResponse>>> CreateApprovalChain(SaveApprovalChainRequest request)
    {
        var validation = await ValidateChainRequest(request);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<ApprovalChainResponse>.Fail(validation));
        }

        var item = new ApprovalChain
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            LeaveTypeId = request.LeaveTypeId,
            MinimumDays = Math.Max(0, request.MinimumDays),
            IsActive = request.IsActive
        };

        db.ApprovalChains.Add(item);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalChain.Create", "ApprovalChain", item.Id.ToString(), $"Created approval chain {item.Name}.", "Success", HttpContext);

        var created = await LoadChains().SingleAsync(chain => chain.Id == item.Id);
        return CreatedAtAction(nameof(GetApprovalChain), new { id = item.Id }, ApiResponse<ApprovalChainResponse>.Ok(ToResponse(created)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("ApprovalChain.Edit")]
    public async Task<ActionResult<ApiResponse<ApprovalChainResponse>>> UpdateApprovalChain(Guid id, SaveApprovalChainRequest request)
    {
        var item = await db.ApprovalChains.FirstOrDefaultAsync(chain => chain.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<ApprovalChainResponse>.Fail("Approval chain not found."));
        }

        var validation = await ValidateChainRequest(request, id);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<ApprovalChainResponse>.Fail(validation));
        }

        item.Name = request.Name.Trim();
        item.Description = request.Description;
        item.DepartmentId = request.DepartmentId;
        item.LeaveTypeId = request.LeaveTypeId;
        item.MinimumDays = Math.Max(0, request.MinimumDays);
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalChain.Edit", "ApprovalChain", item.Id.ToString(), $"Updated approval chain {item.Name}.", "Success", HttpContext);

        var updated = await LoadChains().SingleAsync(chain => chain.Id == id);
        return ApiResponse<ApprovalChainResponse>.Ok(ToResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("ApprovalChain.Delete")]
    public async Task<IActionResult> DeleteApprovalChain(Guid id)
    {
        var item = await db.ApprovalChains.FirstOrDefaultAsync(chain => chain.Id == id);
        if (item is null)
        {
            return NotFound(ApiResponse<string>.Fail("Approval chain not found."));
        }

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalChain.Delete", "ApprovalChain", item.Id.ToString(), $"Deactivated approval chain {item.Name}.", "Success", HttpContext);

        return NoContent();
    }

    [HttpGet("{id:guid}/steps")]
    [RequirePermission("ApprovalChain.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalChainStepResponse>>>> GetSteps(Guid id)
    {
        if (!await db.ApprovalChains.AnyAsync(item => item.Id == id))
        {
            return NotFound(ApiResponse<IReadOnlyList<ApprovalChainStepResponse>>.Fail("Approval chain not found."));
        }

        var items = await LoadSteps()
            .Where(item => item.ApprovalChainId == id)
            .OrderBy(item => item.StepOrder)
            .Select(item => ToStepResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<ApprovalChainStepResponse>>.Ok(items);
    }

    [HttpPost("{id:guid}/steps")]
    [RequirePermission("ApprovalChain.Edit")]
    public async Task<ActionResult<ApiResponse<ApprovalChainStepResponse>>> CreateStep(Guid id, SaveApprovalChainStepRequest request)
    {
        if (!await db.ApprovalChains.AnyAsync(item => item.Id == id))
        {
            return NotFound(ApiResponse<ApprovalChainStepResponse>.Fail("Approval chain not found."));
        }

        var validation = await ValidateStepRequest(id, request);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<ApprovalChainStepResponse>.Fail(validation));
        }

        var item = new ApprovalChainStep
        {
            ApprovalChainId = id,
            StepOrder = request.StepOrder,
            Name = request.Name.Trim(),
            ApproverRoleId = request.ApproverRoleId,
            ApproverUserId = request.ApproverUserId,
            RequiredPermissionCode = request.RequiredPermissionCode.Trim(),
            IsActive = request.IsActive
        };

        db.ApprovalChainSteps.Add(item);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalChain.StepCreate", "ApprovalChain", id.ToString(), $"Created approval step {item.Name}.", "Success", HttpContext);

        var created = await LoadSteps().SingleAsync(step => step.Id == item.Id);
        return ApiResponse<ApprovalChainStepResponse>.Ok(ToStepResponse(created));
    }

    private IQueryable<ApprovalChain> LoadChains()
    {
        return db.ApprovalChains
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.LeaveType);
    }

    private IQueryable<ApprovalChainStep> LoadSteps()
    {
        return db.ApprovalChainSteps
            .AsNoTracking()
            .Include(item => item.ApproverRole)
            .Include(item => item.ApproverUser);
    }

    private async Task<string?> ValidateChainRequest(SaveApprovalChainRequest request, Guid? id = null)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Approval chain name is required.";
        }

        if (await db.ApprovalChains.AnyAsync(item => item.Id != id && item.Name == name))
        {
            return "Approval chain name already exists.";
        }

        if (request.DepartmentId is not null && !await db.Departments.AnyAsync(item => item.Id == request.DepartmentId))
        {
            return "Department not found.";
        }

        if (request.LeaveTypeId is not null && !await db.LeaveTypes.AnyAsync(item => item.Id == request.LeaveTypeId))
        {
            return "Leave type not found.";
        }

        return null;
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

    private static ApprovalChainResponse ToResponse(ApprovalChain item)
    {
        return new ApprovalChainResponse(
            item.Id,
            item.Name,
            item.Description,
            item.DepartmentId,
            item.Department?.Name,
            item.LeaveTypeId,
            item.LeaveType?.Name,
            item.MinimumDays,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt);
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
