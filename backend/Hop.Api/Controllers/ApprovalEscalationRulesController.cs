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
[Route("api/approval-escalation-rules")]
[Authorize]
public class ApprovalEscalationRulesController(AppDbContext db, IAuditLogService auditLogService, IApprovalEscalationService escalationService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ApprovalDelegation.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalEscalationRuleResponse>>>> GetRules()
    {
        var items = await LoadRules().OrderBy(item => item.Name).Select(item => ToResponse(item)).ToListAsync();
        return ApiResponse<IReadOnlyList<ApprovalEscalationRuleResponse>>.Ok(items);
    }

    [HttpPost]
    [RequirePermission("ApprovalDelegation.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalEscalationRuleResponse>>> CreateRule(SaveApprovalEscalationRuleRequest request)
    {
        if (request.EscalateAfterHours <= 0)
        {
            return BadRequest(ApiResponse<ApprovalEscalationRuleResponse>.Fail("จำนวนชั่วโมงก่อน escalate ต้องมากกว่า 0"));
        }

        var rule = new ApprovalEscalationRule
        {
            Name = request.Name.Trim(),
            DepartmentId = request.DepartmentId,
            LeaveTypeId = request.LeaveTypeId,
            EscalateAfterHours = request.EscalateAfterHours,
            EscalateToUserId = request.EscalateToUserId,
            EscalateToRoleId = request.EscalateToRoleId,
            IsActive = request.IsActive
        };

        db.ApprovalEscalationRules.Add(rule);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalEscalationRule.Create", "ApprovalEscalationRule", rule.Id.ToString(), "Created approval escalation rule.", "Success", HttpContext);
        var created = await LoadRules().SingleAsync(item => item.Id == rule.Id);
        return ApiResponse<ApprovalEscalationRuleResponse>.Ok(ToResponse(created));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("ApprovalDelegation.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalEscalationRuleResponse>>> UpdateRule(Guid id, SaveApprovalEscalationRuleRequest request)
    {
        var rule = await db.ApprovalEscalationRules.FirstOrDefaultAsync(item => item.Id == id);
        if (rule is null)
        {
            return NotFound(ApiResponse<ApprovalEscalationRuleResponse>.Fail("Approval escalation rule not found."));
        }

        rule.Name = request.Name.Trim();
        rule.DepartmentId = request.DepartmentId;
        rule.LeaveTypeId = request.LeaveTypeId;
        rule.EscalateAfterHours = request.EscalateAfterHours;
        rule.EscalateToUserId = request.EscalateToUserId;
        rule.EscalateToRoleId = request.EscalateToRoleId;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalEscalationRule.Update", "ApprovalEscalationRule", rule.Id.ToString(), "Updated approval escalation rule.", "Success", HttpContext);
        var updated = await LoadRules().SingleAsync(item => item.Id == id);
        return ApiResponse<ApprovalEscalationRuleResponse>.Ok(ToResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("ApprovalDelegation.Manage")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await db.ApprovalEscalationRules.FirstOrDefaultAsync(item => item.Id == id);
        if (rule is null)
        {
            return NotFound(ApiResponse<string>.Fail("Approval escalation rule not found."));
        }

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalEscalationRule.Delete", "ApprovalEscalationRule", rule.Id.ToString(), "Deactivated approval escalation rule.", "Success", HttpContext);
        return NoContent();
    }

    [HttpPost("run")]
    [RequirePermission("ApprovalDelegation.Manage")]
    public async Task<ActionResult<ApiResponse<int>>> RunEscalation()
    {
        var count = await escalationService.EscalateOverdueApprovalsAsync(HttpContext.RequestAborted);
        await auditLogService.WriteAsync(GetCurrentUserId(), "ApprovalEscalation.Run", "ApprovalEscalation", null, $"Escalated {count} approval(s).", "Success", HttpContext);
        return ApiResponse<int>.Ok(count);
    }

    private IQueryable<ApprovalEscalationRule> LoadRules()
    {
        return db.ApprovalEscalationRules
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.EscalateToUser)
            .Include(item => item.EscalateToRole);
    }

    private static ApprovalEscalationRuleResponse ToResponse(ApprovalEscalationRule item)
    {
        return new ApprovalEscalationRuleResponse(
            item.Id,
            item.Name,
            item.DepartmentId,
            item.Department?.Name,
            item.LeaveTypeId,
            item.LeaveType?.Name,
            item.EscalateAfterHours,
            item.EscalateToUserId,
            item.EscalateToUser?.FullName,
            item.EscalateToRoleId,
            item.EscalateToRole?.Name,
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
