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
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController(
    AppDbContext db,
    IAuditLogService auditLogService,
    ILeaveValidationService leaveValidationService,
    IApprovalChainService approvalChainService,
    ILeaveAttachmentStorageService attachmentStorage,
    ILineMessagingService lineMessagingService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("LeaveManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveRequestResponse>>>> GetLeaveRequests()
    {
        var userId = GetCurrentUserId();
        var canApprove = await HasPermissionAsync("LeaveManagement.Approve");

        var query = LoadLeaveRequests();
        if (!canApprove && userId is not null)
        {
            query = query.Where(item => item.UserId == userId);
        }

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveRequestResponse>>.Ok(items);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("LeaveManagement.View")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> GetLeaveRequest(Guid id)
    {
        var leaveRequest = await LoadLeaveRequests().FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveRequestResponse>.Fail("Leave request not found."));
        }

        if (!await CanAccessLeaveRequest(leaveRequest))
        {
            return Forbid();
        }

        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(leaveRequest));
    }

    [HttpPost]
    [RequirePermission("LeaveManagement.Create")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> CreateLeaveRequest(SaveLeaveRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LeaveRequestResponse>.Fail("Invalid access token."));
        }

        var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(item => item.Id == request.LeaveTypeId && item.IsActive);
        if (leaveType is null)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("Leave type not found."));
        }

        if (request.EndDate < request.StartDate || request.TotalDays <= 0)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("วันที่ลาไม่ถูกต้อง"));
        }

        var leaveRequest = new LeaveRequest
        {
            UserId = userId.Value,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = request.TotalDays,
            Reason = request.Reason.Trim(),
            Status = "Draft"
        };

        var validation = await leaveValidationService.ValidateDraftAsync(leaveRequest);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail(validation.Message ?? "ข้อมูลวันลาไม่ถูกต้อง"));
        }

        leaveRequest.TotalDays = validation.CalculatedDays;

        db.LeaveRequests.Add(leaveRequest);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(userId, "LeaveRequest.Create", "LeaveRequest", leaveRequest.Id.ToString(), "Created leave request draft.", "Success", HttpContext);

        var created = await LoadLeaveRequests().SingleAsync(item => item.Id == leaveRequest.Id);
        return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, ApiResponse<LeaveRequestResponse>.Ok(ToResponse(created)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("LeaveManagement.Edit")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> UpdateLeaveRequest(Guid id, SaveLeaveRequestRequest request)
    {
        var leaveRequest = await db.LeaveRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveRequestResponse>.Fail("Leave request not found."));
        }

        if (!await CanEditLeaveRequest(leaveRequest))
        {
            return Forbid();
        }

        if (leaveRequest.Status != "Draft")
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("แก้ไขได้เฉพาะคำขอที่เป็นแบบร่างเท่านั้น"));
        }

        leaveRequest.LeaveTypeId = request.LeaveTypeId;
        leaveRequest.StartDate = request.StartDate;
        leaveRequest.EndDate = request.EndDate;
        leaveRequest.TotalDays = request.TotalDays;
        leaveRequest.Reason = request.Reason.Trim();
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        var validation = await leaveValidationService.ValidateDraftAsync(leaveRequest, leaveRequest.Id);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail(validation.Message ?? "ข้อมูลวันลาไม่ถูกต้อง"));
        }

        leaveRequest.TotalDays = validation.CalculatedDays;

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveRequest.Update", "LeaveRequest", leaveRequest.Id.ToString(), "Updated leave request draft.", "Success", HttpContext);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    [HttpPost("{id:guid}/submit")]
    [RequirePermission("LeaveManagement.Create")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> SubmitLeaveRequest(Guid id)
    {
        var leaveRequest = await LoadLeaveRequestsForMutation().FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveRequestResponse>.Fail("Leave request not found."));
        }

        if (leaveRequest.UserId != GetCurrentUserId())
        {
            return Forbid();
        }

        if (leaveRequest.Status != "Draft")
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("ส่งอนุมัติได้เฉพาะคำขอที่เป็นแบบร่างเท่านั้น"));
        }

        var validation = await leaveValidationService.ValidateSubmitAsync(leaveRequest);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail(validation.Message ?? "ไม่สามารถส่งคำขอลาได้"));
        }

        leaveRequest.TotalDays = validation.CalculatedDays;
        var approvalPlan = await approvalChainService.BuildApprovalPlanAsync(leaveRequest);
        if (approvalPlan.Count == 0)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("ไม่พบผู้อนุมัติที่มีสิทธิ์ถูกต้องสำหรับคำขอนี้"));
        }

        leaveRequest.Status = "Pending";
        leaveRequest.SubmittedAt = DateTime.UtcNow;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        leaveRequest.CurrentApproverId = approvalPlan.First().ApproverId;

        foreach (var step in approvalPlan)
        {
            db.LeaveApprovals.Add(new LeaveApproval
            {
                LeaveRequestId = leaveRequest.Id,
                ApproverId = step.ApproverId,
                ApprovalChainId = step.ApprovalChainId,
                ApprovalChainStepId = step.ApprovalChainStepId,
                StepOrder = step.StepOrder,
                StepName = step.StepName,
                RequiredPermissionCode = step.RequiredPermissionCode,
                Status = step.StepOrder == approvalPlan.Min(item => item.StepOrder) ? "Pending" : "Waiting"
            });
        }

        await UpdatePendingBalance(leaveRequest, leaveRequest.TotalDays);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveRequest.Submit", "LeaveRequest", leaveRequest.Id.ToString(), "Submitted leave request.", "Success", HttpContext);
        await NotifyPlaceholder(leaveRequest, "Pending", null);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("LeaveManagement.Edit")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> CancelLeaveRequest(Guid id)
    {
        var leaveRequest = await db.LeaveRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveRequestResponse>.Fail("Leave request not found."));
        }

        if (leaveRequest.UserId != GetCurrentUserId())
        {
            return Forbid();
        }

        if (leaveRequest.Status is "Approved" or "Rejected" or "Cancelled")
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("คำขอนี้ไม่สามารถยกเลิกได้"));
        }

        if (leaveRequest.Status == "Pending")
        {
            await UpdatePendingBalance(leaveRequest, -leaveRequest.TotalDays);
        }

        leaveRequest.Status = "Cancelled";
        leaveRequest.CurrentApproverId = null;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveRequest.Cancel", "LeaveRequest", leaveRequest.Id.ToString(), "Cancelled leave request.", "Success", HttpContext);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission("LeaveManagement.Approve")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> ApproveLeaveRequest(Guid id, LeaveDecisionRequest request)
    {
        return await Decide(id, "Approved", request.Remark);
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission("LeaveManagement.Approve")]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> RejectLeaveRequest(Guid id, LeaveDecisionRequest request)
    {
        return await Decide(id, "Rejected", request.Remark);
    }

    [HttpGet("{id:guid}/attachments")]
    [RequirePermission("LeaveManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveAttachmentResponse>>>> GetAttachments(Guid id)
    {
        var leaveRequest = await db.LeaveRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<LeaveAttachmentResponse>>.Fail("Leave request not found."));
        }

        if (!await CanAccessLeaveRequest(leaveRequest))
        {
            return Forbid();
        }

        var items = await db.LeaveAttachments
            .AsNoTracking()
            .Include(item => item.UploadedByUser)
            .Where(item => item.LeaveRequestId == id)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToAttachmentResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveAttachmentResponse>>.Ok(items);
    }

    [HttpPost("{id:guid}/attachments")]
    [RequirePermission("LeaveManagement.Edit")]
    public async Task<ActionResult<ApiResponse<LeaveAttachmentResponse>>> UploadAttachment(Guid id, IFormFile file)
    {
        var leaveRequest = await db.LeaveRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveAttachmentResponse>.Fail("Leave request not found."));
        }

        if (!await CanEditLeaveRequest(leaveRequest))
        {
            return Forbid();
        }

        try
        {
            var attachment = await attachmentStorage.SaveAsync(id, GetCurrentUserId()!.Value, file);
            db.LeaveAttachments.Add(attachment);
            await db.SaveChangesAsync();
            await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveAttachment.Upload", "LeaveRequest", id.ToString(), $"Uploaded attachment {attachment.FileName}.", "Success", HttpContext);

            var created = await db.LeaveAttachments
                .Include(item => item.UploadedByUser)
                .SingleAsync(item => item.Id == attachment.Id);
            return ApiResponse<LeaveAttachmentResponse>.Ok(ToAttachmentResponse(created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveAttachmentResponse>.Fail(ex.Message));
        }
    }

    private async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> Decide(Guid id, string decision, string? remark)
    {
        var approverId = GetCurrentUserId();
        var leaveRequest = await db.LeaveRequests
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveRequestResponse>.Fail("Leave request not found."));
        }

        if (leaveRequest.Status != "Pending")
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("ดำเนินการได้เฉพาะคำขอที่รออนุมัติเท่านั้น"));
        }

        var approval = leaveRequest.Approvals
            .OrderBy(item => item.StepOrder)
            .FirstOrDefault(item => item.Status == "Pending");

        if (approval is null || approverId is null)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("ไม่พบขั้นตอนอนุมัติที่รอดำเนินการ"));
        }

        if (approval.ApproverId != approverId || !await HasPermissionAsync(approval.RequiredPermissionCode))
        {
            return Forbid();
        }

        approval.Status = decision;
        approval.Remark = remark;
        approval.ActionAt = DateTime.UtcNow;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        if (decision == "Rejected")
        {
            leaveRequest.Status = "Rejected";
            leaveRequest.CurrentApproverId = null;
            foreach (var waitingApproval in leaveRequest.Approvals.Where(item => item.Status == "Waiting"))
            {
                waitingApproval.Status = "Skipped";
            }
            await UpdatePendingBalance(leaveRequest, -leaveRequest.TotalDays);
        }
        else
        {
            var nextApproval = leaveRequest.Approvals
                .OrderBy(item => item.StepOrder)
                .FirstOrDefault(item => item.Status == "Waiting");

            if (nextApproval is not null)
            {
                nextApproval.Status = "Pending";
                leaveRequest.CurrentApproverId = nextApproval.ApproverId;
            }
            else
            {
                leaveRequest.Status = "Approved";
                leaveRequest.CurrentApproverId = null;
                await UpdatePendingBalance(leaveRequest, -leaveRequest.TotalDays);
                await UpdateUsedBalance(leaveRequest, leaveRequest.TotalDays);
            }
        }

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(approverId, $"LeaveRequest.{decision}", "LeaveRequest", leaveRequest.Id.ToString(), $"{decision} leave request step {approval.StepOrder}.", "Success", HttpContext);
        await NotifyPlaceholder(leaveRequest, decision, remark);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    private IQueryable<LeaveRequest> LoadLeaveRequests()
    {
        return db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover);
    }

    private IQueryable<LeaveRequest> LoadLeaveRequestsForMutation()
    {
        return db.LeaveRequests
            .Include(item => item.User)
            .Include(item => item.LeaveType);
    }

    private async Task UpdatePendingBalance(LeaveRequest leaveRequest, decimal delta)
    {
        var balance = await GetOrCreateBalance(leaveRequest);
        balance.PendingDays = Math.Max(0, balance.PendingDays + delta);
        balance.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpdateUsedBalance(LeaveRequest leaveRequest, decimal delta)
    {
        var balance = await GetOrCreateBalance(leaveRequest);
        balance.UsedDays += delta;
        balance.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<LeaveBalance> GetOrCreateBalance(LeaveRequest leaveRequest)
    {
        var year = leaveRequest.StartDate.Year;
        var balance = await db.LeaveBalances.FirstOrDefaultAsync(item =>
            item.UserId == leaveRequest.UserId &&
            item.LeaveTypeId == leaveRequest.LeaveTypeId &&
            item.Year == year);

        if (balance is not null)
        {
            return balance;
        }

        var leaveType = await db.LeaveTypes.SingleAsync(item => item.Id == leaveRequest.LeaveTypeId);
        balance = new LeaveBalance
        {
            UserId = leaveRequest.UserId,
            LeaveTypeId = leaveRequest.LeaveTypeId,
            Year = year,
            EntitledDays = leaveType.DefaultDaysPerYear
        };
        db.LeaveBalances.Add(balance);
        return balance;
    }

    private async Task<bool> CanAccessLeaveRequest(LeaveRequest leaveRequest)
    {
        return leaveRequest.UserId == GetCurrentUserId() || await HasPermissionAsync("LeaveManagement.Approve");
    }

    private async Task<bool> CanEditLeaveRequest(LeaveRequest leaveRequest)
    {
        return leaveRequest.UserId == GetCurrentUserId() || await HasPermissionAsync("LeaveManagement.Manage");
    }

    private async Task<bool> HasPermissionAsync(string permissionCode)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode);
    }

    private async Task NotifyPlaceholder(LeaveRequest leaveRequest, string status, string? remark)
    {
        if (leaveRequest.User is null || leaveRequest.LeaveType is null)
        {
            return;
        }

        await lineMessagingService.NotifyLeaveRequestAsync(new LeaveNotificationMessage(
            leaveRequest.Id,
            leaveRequest.UserId,
            leaveRequest.User.FullName,
            leaveRequest.LeaveType.Name,
            status,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            remark
        ));
    }

    private static LeaveRequestResponse ToResponse(LeaveRequest item)
    {
        return new LeaveRequestResponse(
            item.Id,
            item.UserId,
            item.User?.FullName,
            item.LeaveTypeId,
            item.LeaveType?.Name,
            item.StartDate,
            item.EndDate,
            item.TotalDays,
            item.Reason,
            item.Status,
            item.CurrentApproverId,
            item.CurrentApprover?.FullName,
            item.CreatedAt,
            item.SubmittedAt,
            item.UpdatedAt
        );
    }

    private static LeaveAttachmentResponse ToAttachmentResponse(LeaveAttachment item)
    {
        return new LeaveAttachmentResponse(
            item.Id,
            item.LeaveRequestId,
            item.FileName,
            item.ContentType,
            item.FileSizeBytes,
            item.UploadedByUserId,
            item.UploadedByUser?.FullName,
            item.CreatedAt
        );
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
