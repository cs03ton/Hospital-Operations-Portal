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
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalChainResponse>>>> GetApprovalChains()
    {
        var chains = await LoadChains()
            .OrderBy(item => item.Name)
            .ToListAsync();
        var counts = await db.Users
            .AsNoTracking()
            .Where(user => user.LeaveApprovalRuleId != null)
            .GroupBy(user => user.LeaveApprovalRuleId!.Value)
            .Select(group => new { ApprovalRuleId = group.Key, UserCount = group.Count() })
            .ToDictionaryAsync(item => item.ApprovalRuleId, item => item.UserCount);
        var items = chains.Select(item => ToResponse(item, counts.GetValueOrDefault(item.Id))).ToList();

        return ApiResponse<IReadOnlyList<ApprovalChainResponse>>.Ok(items);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
    public async Task<ActionResult<ApiResponse<ApprovalChainResponse>>> GetApprovalChain(Guid id)
    {
        var item = await LoadChains().FirstOrDefaultAsync(chain => chain.Id == id);
        return item is null
            ? NotFound(ApiResponse<ApprovalChainResponse>.Fail("Approval chain not found."))
            : ApiResponse<ApprovalChainResponse>.Ok(ToResponse(item, await db.Users.CountAsync(user => user.LeaveApprovalRuleId == item.Id)));
    }

    [HttpPost("resolve-preview")]
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
    public async Task<ActionResult<ApiResponse<ApprovalRuleResolvePreviewResponse>>> ResolvePreview(ApprovalRuleResolvePreviewRequest request)
    {
        User? user = null;
        if (request.UserId is not null)
        {
            user = await db.Users
                .AsNoTracking()
                .Include(item => item.Department)
                .FirstOrDefaultAsync(item => item.Id == request.UserId);
            if (user is null)
            {
                return NotFound(ApiResponse<ApprovalRuleResolvePreviewResponse>.Fail("User not found."));
            }
        }

        var approvalRuleId = request.ApprovalRuleId ?? user?.LeaveApprovalRuleId;
        if (approvalRuleId is null)
        {
            return ApiResponse<ApprovalRuleResolvePreviewResponse>.Ok(new ApprovalRuleResolvePreviewResponse(
                user?.Id,
                user?.FullName,
                null,
                null,
                false,
                [],
                ["ยังไม่ได้กำหนดกฎการอนุมัติวันลาให้ผู้ใช้งานนี้"]));
        }

        var rule = await db.ApprovalChains
            .AsNoTracking()
            .Include(item => item.Steps.Where(step => step.IsActive))
                .ThenInclude(step => step.ApproverUser)
            .Include(item => item.Steps.Where(step => step.IsActive))
                .ThenInclude(step => step.ApproverRole)
            .FirstOrDefaultAsync(item => item.Id == approvalRuleId);
        if (rule is null)
        {
            return NotFound(ApiResponse<ApprovalRuleResolvePreviewResponse>.Fail("Approval rule not found."));
        }

        var warnings = new List<string>();
        if (!rule.IsActive)
        {
            warnings.Add("กฎการอนุมัตินี้ถูกปิดใช้งาน");
        }

        if (!rule.Steps.Any(step => step.IsActive))
        {
            warnings.Add("กฎการอนุมัตินี้ยังไม่มีขั้นอนุมัติที่เปิดใช้งาน");
        }

        var steps = new List<ApprovalRulePreviewStepResponse>();
        foreach (var step in rule.Steps.Where(item => item.IsActive).OrderBy(item => item.StepOrder))
        {
            var stepWarnings = new List<string>();
            var approver = await ResolvePreviewApprover(step, user?.DepartmentId);
            if (approver is null)
            {
                stepWarnings.Add("ไม่พบผู้อนุมัติสำหรับขั้นตอนนี้");
            }
            else
            {
                if (user is not null && approver.Id == user.Id)
                {
                    stepWarnings.Add("พบ self approval ผู้ขอลาเป็นผู้อนุมัติในขั้นนี้");
                }

                if (!await UserHasPermissionAsync(approver.Id, step.RequiredPermissionCode))
                {
                    stepWarnings.Add($"ผู้อนุมัติยังไม่มีสิทธิ์ {step.RequiredPermissionCode}");
                }
            }

            steps.Add(new ApprovalRulePreviewStepResponse(
                step.StepOrder,
                step.Name,
                approver?.Id,
                approver?.FullName,
                step.ApproverRole?.Name,
                stepWarnings.Count == 0 ? "พร้อมใช้งาน" : "ต้องตรวจสอบ",
                stepWarnings));
        }

        warnings.AddRange(steps.SelectMany(item => item.Warnings).Distinct());

        return ApiResponse<ApprovalRuleResolvePreviewResponse>.Ok(new ApprovalRuleResolvePreviewResponse(
            user?.Id,
            user?.FullName,
            rule.Id,
            rule.Name,
            rule.IsActive,
            steps,
            warnings));
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
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
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
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
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
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
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
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
    [RequirePermission(LeavePermissions.ManageApprovalChains)]
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

    private async Task<User?> ResolvePreviewApprover(ApprovalChainStep step, Guid? departmentId)
    {
        if (step.ApproverUserId is not null)
        {
            return await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == step.ApproverUserId && item.IsActive);
        }

        if (step.ApproverRoleId is not null)
        {
            var query = db.UserRoles
                .AsNoTracking()
                .Include(item => item.User)
                .Where(item => item.RoleId == step.ApproverRoleId && item.User != null && item.User.IsActive);
            var sameDepartment = departmentId is null
                ? null
                : await query.Where(item => item.User!.DepartmentId == departmentId).Select(item => item.User!).FirstOrDefaultAsync();
            return sameDepartment ?? await query.Select(item => item.User!).FirstOrDefaultAsync();
        }

        return null;
    }

    private Task<bool> UserHasPermissionAsync(Guid userId, string permissionCode)
    {
        return db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode);
    }

    private static ApprovalChainResponse ToResponse(ApprovalChain item, int userCount = 0)
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
            item.UpdatedAt,
            userCount);
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
