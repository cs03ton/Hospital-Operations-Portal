using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using System.Data;
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
    ILineMessagingService lineMessagingService,
    ILeavePdfService leavePdfService,
    IFileScanningService fileScanningService,
    ILeaveNotificationEventPublisher leaveNotificationEventPublisher,
    ILeaveRequestAccessService leaveRequestAccessService,
    ILeaveRequestNumberService leaveRequestNumberService,
    IConfiguration configuration,
    ILogger<LeaveRequestsController> logger) : ControllerBase
{
    [HttpGet]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveRequestResponse>>>> GetLeaveRequests(
        [FromQuery] Guid? leaveTypeId,
        [FromQuery] string? status,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery(Name = "userId")] Guid? filterUserId)
    {
        var currentUserId = GetCurrentUserId();
        var visibility = await leaveRequestAccessService.GetVisibilityAsync(currentUserId);

        var query = leaveRequestAccessService.ApplyVisibility(LoadLeaveRequests(), currentUserId, visibility);

        if (leaveTypeId is not null)
        {
            query = query.Where(item => item.LeaveTypeId == leaveTypeId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status.Trim());
        }

        if (departmentId is not null)
        {
            query = query.Where(item => item.User != null && item.User.DepartmentId == departmentId);
        }

        if (fromDate is not null)
        {
            query = query.Where(item => item.EndDate >= fromDate.Value);
        }

        if (toDate is not null)
        {
            query = query.Where(item => item.StartDate <= toDate.Value);
        }

        if (filterUserId is not null)
        {
            query = query.Where(item => item.UserId == filterUserId);
        }

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToResponse(item))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<LeaveRequestResponse>>.Ok(items);
    }

    [HttpGet("{id:guid}")]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll)]
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

    [HttpGet("{id:guid}/pdf")]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll)]
    public async Task<IActionResult> DownloadLeaveRequestPdf(Guid id)
    {
        var leaveRequest = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.Department)
            .Include(item => item.User)
                .ThenInclude(user => user!.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover)
            .Include(item => item.Approvals)
                .ThenInclude(approval => approval.Approver)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<string>.Fail("Leave request not found."));
        }

        if (!await CanAccessLeaveRequest(leaveRequest))
        {
            return Forbid();
        }

        var hospitalName = configuration["Hospital:Name"]
            ?? configuration["VITE_HOSPITAL_NAME"]
            ?? configuration["Vite:HospitalName"]
            ?? "Hospital";
        var pdfBytes = leavePdfService.GenerateLeaveRequestPdf(leaveRequest, hospitalName);
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveRequest.PdfGenerated", "LeaveRequest", leaveRequest.Id.ToString(), "Generated leave request PDF.", "Success", HttpContext);

        var fileName = string.IsNullOrWhiteSpace(leaveRequest.RequestNumber)
            ? $"leave-request-{leaveRequest.Id}.pdf"
            : $"leave-request-{leaveRequest.RequestNumber}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.Create)]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> CreateLeaveRequest(SaveLeaveRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LeaveRequestResponse>.Fail("Invalid access token."));
        }

        if (await CurrentUserHasAnyRoleAsync("Admin", "SuperAdmin"))
        {
            return Forbid();
        }

        var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(item => item.Id == request.LeaveTypeId && item.IsActive);
        if (leaveType is null)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("Leave type not found."));
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("วันที่ลาไม่ถูกต้อง"));
        }

        var createdAt = DateTime.UtcNow;
        var leaveRequest = new LeaveRequest
        {
            UserId = userId.Value,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DurationType = LeaveDurationTypes.Normalize(request.DurationType),
            TotalDays = request.TotalDays,
            Reason = request.Reason.Trim(),
            Status = "Draft",
            CreatedAt = createdAt
        };

        var validation = await leaveValidationService.ValidateDraftAsync(leaveRequest);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail(validation.Message ?? "ข้อมูลวันลาไม่ถูกต้อง"));
        }

        leaveRequest.TotalDays = validation.CalculatedDays;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, HttpContext.RequestAborted);
        leaveRequest.RequestNumber = await leaveRequestNumberService.GenerateAsync(createdAt, HttpContext.RequestAborted);
        db.LeaveRequests.Add(leaveRequest);
        await db.SaveChangesAsync(HttpContext.RequestAborted);
        await transaction.CommitAsync(HttpContext.RequestAborted);
        await auditLogService.WriteAsync(userId, "LeaveRequest.Create", "LeaveRequest", leaveRequest.Id.ToString(), "Created leave request draft.", "Success", HttpContext);

        var created = await LoadLeaveRequests().SingleAsync(item => item.Id == leaveRequest.Id);
        return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, ApiResponse<LeaveRequestResponse>.Ok(ToResponse(created)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(LeavePermissions.EditOwn)]
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
        leaveRequest.DurationType = LeaveDurationTypes.Normalize(request.DurationType);
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
    [RequirePermission(LeavePermissions.Create)]
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
        if (leaveRequest.User?.LeaveApprovalRuleId is null)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("ยังไม่ได้กำหนดกฎการอนุมัติวันลาให้ผู้ใช้งานนี้"));
        }

        var approvalRuleIsReady = await db.ApprovalChains
            .AsNoTracking()
            .AnyAsync(item => item.Id == leaveRequest.User.LeaveApprovalRuleId && item.IsActive && item.Steps.Any(step => step.IsActive));
        if (!approvalRuleIsReady)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("กฎการอนุมัติวันลาของผู้ใช้งานยังไม่พร้อมใช้งาน"));
        }

        var approvalPlan = await approvalChainService.BuildApprovalPlanAsync(leaveRequest);
        if (approvalPlan.Count == 0)
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("ไม่พบผู้อนุมัติที่มีสิทธิ์ถูกต้อง หรือกฎการอนุมัติมี self approval โดยไม่มีผู้อนุมัติสำรอง"));
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
        await leaveNotificationEventPublisher.PublishAsync("LeaveSubmitted", leaveRequest.Id, leaveRequest.CurrentApproverId, HttpContext.RequestAborted);
        await leaveNotificationEventPublisher.PublishAsync("ApprovalStepActivated", leaveRequest.Id, leaveRequest.CurrentApproverId, HttpContext.RequestAborted);
        await NotifyPlaceholder(leaveRequest, "Pending", null);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(LeavePermissions.CancelOwn)]
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
        var cancelled = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        await NotifyPlaceholder(cancelled, "Cancelled", null);

        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(cancelled));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(LeavePermissions.ApproveCurrentStep)]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> ApproveLeaveRequest(Guid id, LeaveDecisionRequest request)
    {
        return await Decide(id, "Approved", request.Remark);
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(LeavePermissions.ApproveCurrentStep)]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> RejectLeaveRequest(Guid id, LeaveDecisionRequest request)
    {
        return await Decide(id, "Rejected", request.Remark);
    }

    [HttpPost("{id:guid}/override-approve")]
    [RequirePermission(LeavePermissions.Override)]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> OverrideApproveLeaveRequest(Guid id, LeaveOverrideDecisionRequest request)
    {
        return await OverrideDecide(id, "Approved", request.Reason);
    }

    [HttpPost("{id:guid}/override-reject")]
    [RequirePermission(LeavePermissions.Override)]
    public async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> OverrideRejectLeaveRequest(Guid id, LeaveOverrideDecisionRequest request)
    {
        return await OverrideDecide(id, "Rejected", request.Reason);
    }

    [HttpGet("{id:guid}/attachments")]
    [RequireAnyPermission(LeavePermissions.ViewOwn, LeavePermissions.ViewPendingApproval, LeavePermissions.ViewDepartment, LeavePermissions.ViewAll)]
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
    [RequirePermission(LeavePermissions.EditOwn)]
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
            var scanResult = await fileScanningService.ScanAsync(file, HttpContext.RequestAborted);
            if (!scanResult.IsClean)
            {
                await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveAttachment.ScanFailed", "LeaveRequest", id.ToString(), scanResult.Message, "Denied", HttpContext);
                return BadRequest(ApiResponse<LeaveAttachmentResponse>.Fail(scanResult.Message));
            }

            await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveAttachment.ScanPassed", "LeaveRequest", id.ToString(), $"{scanResult.Provider}: {scanResult.Message}", "Success", HttpContext);

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
            logger.LogWarning(ex, "Leave attachment upload failed for leave request {LeaveRequestId}.", id);
            await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveAttachment.UploadFailed", "LeaveRequest", id.ToString(), ex.Message, "Denied", HttpContext);
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

        if (leaveRequest.UserId == approverId)
        {
            await auditLogService.WriteAsync(
                approverId,
                "SelfApprovalBlocked",
                "LeaveRequest",
                leaveRequest.Id.ToString(),
                $"Blocked self-approval for approval step {approval.StepOrder}.",
                "Denied",
                HttpContext);
            return Forbid();
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
                await leaveNotificationEventPublisher.PublishAsync("ApprovalStepActivated", leaveRequest.Id, nextApproval.ApproverId, HttpContext.RequestAborted);
            }
            else
            {
                leaveRequest.Status = "Approved";
                leaveRequest.CurrentApproverId = null;
                await UpdatePendingBalance(leaveRequest, -leaveRequest.TotalDays);
                await UpdateUsedBalance(leaveRequest, leaveRequest.TotalDays);
                await leaveNotificationEventPublisher.PublishAsync("LeaveApproved", leaveRequest.Id, leaveRequest.UserId, HttpContext.RequestAborted);
            }
        }

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(approverId, $"LeaveRequest.{decision}", "LeaveRequest", leaveRequest.Id.ToString(), $"{decision} leave request step {approval.StepOrder}.", "Success", HttpContext);
        if (decision == "Rejected")
        {
            await leaveNotificationEventPublisher.PublishAsync("LeaveRejected", leaveRequest.Id, leaveRequest.UserId, HttpContext.RequestAborted);
        }
        await NotifyPlaceholder(leaveRequest, decision, remark);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    private async Task<ActionResult<ApiResponse<LeaveRequestResponse>>> OverrideDecide(Guid id, string decision, string reason)
    {
        var overrideByUserId = GetCurrentUserId();
        if (overrideByUserId is null)
        {
            return Unauthorized(ApiResponse<LeaveRequestResponse>.Fail("Invalid access token."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("กรุณาระบุเหตุผลการดำเนินการแทน"));
        }

        var leaveRequest = await db.LeaveRequests
            .Include(item => item.Approvals)
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveRequestResponse>.Fail("Leave request not found."));
        }

        if (leaveRequest.UserId == overrideByUserId)
        {
            await auditLogService.WriteAsync(
                overrideByUserId,
                "SelfApprovalBlocked",
                "LeaveRequest",
                leaveRequest.Id.ToString(),
                "Blocked requester override attempt.",
                "Denied",
                HttpContext);
            return Forbid();
        }

        if (leaveRequest.Status != "Pending")
        {
            return BadRequest(ApiResponse<LeaveRequestResponse>.Fail("Override ได้เฉพาะคำขอที่รออนุมัติเท่านั้น"));
        }

        var originalApproverId = leaveRequest.CurrentApproverId;
        var pendingApproval = leaveRequest.Approvals
            .OrderBy(item => item.StepOrder)
            .FirstOrDefault(item => item.Status == "Pending");
        if (pendingApproval is not null)
        {
            pendingApproval.Status = decision;
            pendingApproval.Remark = $"[Override] {reason.Trim()}";
            pendingApproval.ActionAt = DateTime.UtcNow;
        }

        foreach (var waitingApproval in leaveRequest.Approvals.Where(item => item.Status == "Waiting"))
        {
            waitingApproval.Status = "Skipped";
            waitingApproval.Remark ??= "Skipped by override.";
        }

        if (decision == "Rejected")
        {
            leaveRequest.Status = "Rejected";
            await UpdatePendingBalance(leaveRequest, -leaveRequest.TotalDays);
            await leaveNotificationEventPublisher.PublishAsync("LeaveRejected", leaveRequest.Id, leaveRequest.UserId, HttpContext.RequestAborted);
        }
        else
        {
            leaveRequest.Status = "Approved";
            await UpdatePendingBalance(leaveRequest, -leaveRequest.TotalDays);
            await UpdateUsedBalance(leaveRequest, leaveRequest.TotalDays);
            await leaveNotificationEventPublisher.PublishAsync("LeaveApproved", leaveRequest.Id, leaveRequest.UserId, HttpContext.RequestAborted);
        }

        leaveRequest.CurrentApproverId = null;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        db.ApprovalOverrideLogs.Add(new ApprovalOverrideLog
        {
            LeaveRequestId = leaveRequest.Id,
            OriginalApproverId = originalApproverId,
            OverrideByUserId = overrideByUserId.Value,
            Action = decision,
            Reason = reason.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
        });

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(
            overrideByUserId,
            decision == "Approved" ? "LeaveApproval.OverrideApproved" : "LeaveApproval.OverrideRejected",
            "LeaveRequest",
            leaveRequest.Id.ToString(),
            $"Override {decision} by {overrideByUserId}. Reason: {reason.Trim()}",
            "Success",
            HttpContext);
        if (originalApproverId is not null)
        {
            await leaveNotificationEventPublisher.PublishAsync("LeaveOverride", leaveRequest.Id, originalApproverId, HttpContext.RequestAborted);
        }
        await NotifyPlaceholder(leaveRequest, decision, reason);

        var updated = await LoadLeaveRequests().SingleAsync(item => item.Id == id);
        return ApiResponse<LeaveRequestResponse>.Ok(ToResponse(updated));
    }

    private IQueryable<LeaveRequest> LoadLeaveRequests()
    {
        return db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(user => user!.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover)
                .ThenInclude(user => user!.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(item => item.Approvals)
                .ThenInclude(approval => approval.Approver);
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
        return await leaveRequestAccessService.CanAccessLeaveRequestAsync(leaveRequest, GetCurrentUserId());
    }

    private async Task<bool> CanEditLeaveRequest(LeaveRequest leaveRequest)
    {
        return leaveRequest.UserId == GetCurrentUserId() && await HasPermissionAsync(LeavePermissions.EditOwn);
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

    private async Task<bool> CurrentUserHasAnyRoleAsync(params string[] roleNames)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return false;
        }

        return await db.UserRoles
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == userId &&
                item.Role != null &&
                item.Role.IsActive &&
                roleNames.Contains(item.Role.Name));
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
        var currentApproval = item.Approvals
            .OrderBy(approval => approval.StepOrder)
            .FirstOrDefault(approval => approval.Status == "Pending");
        var latestActionAt = item.Approvals
            .Where(approval => approval.ActionAt is not null)
            .Select(approval => approval.ActionAt)
            .OrderByDescending(actionAt => actionAt)
            .FirstOrDefault();
        var currentApproverRole = item.CurrentApprover?.UserRoles
            .Select(userRole => userRole.Role?.Name)
            .FirstOrDefault(role => !string.IsNullOrWhiteSpace(role));
        var currentStepName = currentApproval?.StepName;
        var currentStatusLabel = GetCurrentStatusLabel(item.Status, currentStepName, item.CurrentApprover?.FullName);
        var trackingMessage = GetTrackingMessage(item.Status, item.RequestNumber, item.Id, currentStepName, item.CurrentApprover?.FullName);

        return new LeaveRequestResponse(
            item.Id,
            item.RequestNumber,
            item.UserId,
            item.User?.FullName,
            item.LeaveTypeId,
            item.LeaveType?.Name,
            item.StartDate,
            item.EndDate,
            item.DurationType,
            item.TotalDays,
            item.Reason,
            item.Status,
            item.CurrentApproverId,
            item.CurrentApprover?.FullName,
            currentApproverRole,
            currentStepName,
            latestActionAt,
            currentStatusLabel,
            trackingMessage,
            item.CreatedAt,
            item.SubmittedAt,
            item.UpdatedAt
        );
    }

    private static string GetCurrentStatusLabel(string status, string? currentStepName, string? currentApproverName)
    {
        return status switch
        {
            "Draft" => "แบบร่าง",
            "Pending" when !string.IsNullOrWhiteSpace(currentApproverName) => $"รออนุมัติจาก {currentApproverName}",
            "Pending" when !string.IsNullOrWhiteSpace(currentStepName) => $"รอ{currentStepName}",
            "Pending" => "ส่งคำขอแล้ว",
            "Approved" => "อนุมัติแล้ว",
            "Rejected" => "ไม่อนุมัติ",
            "Cancelled" => "ยกเลิกแล้ว",
            _ => status
        };
    }

    private static string GetTrackingMessage(string status, string? requestNumber, Guid requestId, string? currentStepName, string? currentApproverName)
    {
        var requestCode = string.IsNullOrWhiteSpace(requestNumber) ? "-" : requestNumber;
        return status switch
        {
            "Draft" => $"คำขอลา {requestCode} ยังเป็นแบบร่าง",
            "Pending" when !string.IsNullOrWhiteSpace(currentApproverName) && !string.IsNullOrWhiteSpace(currentStepName) =>
                $"คำขอลา {requestCode} อยู่ที่ขั้นตอน {currentStepName} และรออนุมัติจาก {currentApproverName}",
            "Pending" when !string.IsNullOrWhiteSpace(currentApproverName) =>
                $"คำขอลา {requestCode} รออนุมัติจาก {currentApproverName}",
            "Pending" => $"คำขอลา {requestCode} ส่งคำขอแล้วและอยู่ระหว่างรออนุมัติ",
            "Approved" => $"คำขอลา {requestCode} ได้รับการอนุมัติแล้ว",
            "Rejected" => $"คำขอลา {requestCode} ถูกไม่อนุมัติ",
            "Cancelled" => $"คำขอลา {requestCode} ถูกยกเลิกแล้ว",
            _ => $"คำขอลา {requestCode} สถานะ {status}"
        };
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
