using System.Data;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/leave-cancellation-requests")]
[Authorize]
public sealed class LeaveCancellationRequestsController(
    AppDbContext db,
    IApprovalChainService approvalChainService,
    IAuditLogService auditLogService,
    ILeaveRequestNumberService requestNumberService,
    ILineMessagingService lineMessagingService,
    LineConfigurationResolver lineConfiguration,
    ILogger<LeaveCancellationRequestsController> logger) : ControllerBase
{
    private const string OriginalLeaveCancelledStatus = "CancelledAfterApproval";

    [HttpGet]
    [RequireAnyPermission(
        LeavePermissions.CancellationViewOwn,
        LeavePermissions.CancellationApproveCurrentStep,
        LeavePermissions.CancellationViewDepartment,
        LeavePermissions.CancellationViewAll,
        LeavePermissions.CancellationManage)]
    public async Task<ActionResult<ApiResponse<PagedResponse<LeaveCancellationRequestResponse>>>> GetCancellationRequests(
        [FromQuery] string? status,
        [FromQuery] string? scope,
        [FromQuery] Guid? leaveTypeId,
        [FromQuery] Guid? requesterId,
        [FromQuery(Name = "userId")] Guid? filterUserId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<PagedResponse<LeaveCancellationRequestResponse>>.Fail("Invalid access token."));
        }

        var query = await ApplyVisibilityAsync(LoadCancellationRequests(), userId.Value, cancellationToken);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == NormalizeCancellationStatus(status));
        }

        if (leaveTypeId is not null)
        {
            query = query.Where(item => item.LeaveTypeId == leaveTypeId);
        }

        var requesterFilterId = requesterId ?? filterUserId;
        if (requesterFilterId is not null)
        {
            query = query.Where(item => item.RequesterUserId == requesterFilterId);
        }

        if (fromDate is not null)
        {
            query = query.Where(item => item.OriginalLeaveRequest != null && item.OriginalLeaveRequest.StartDate >= fromDate.Value);
        }

        if (toDate is not null)
        {
            query = query.Where(item => item.OriginalLeaveRequest != null && item.OriginalLeaveRequest.EndDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            var normalizedScope = scope.Trim().ToLowerInvariant();
            if (normalizedScope == "mine")
            {
                query = query.Where(item => item.RequesterUserId == userId.Value);
            }
            else if (normalizedScope == "pending-approval")
            {
                query = query.Where(item => item.Status == LeaveCancellationStatuses.Pending && item.CurrentApproverId == userId.Value);
            }
            else if (normalizedScope == "department" && await HasAnyPermissionAsync(
                userId.Value,
                [LeavePermissions.CancellationViewDepartment, LeavePermissions.CancellationViewAll, LeavePermissions.CancellationManage],
                cancellationToken))
            {
                var departmentId = await db.Users
                    .AsNoTracking()
                    .Where(item => item.Id == userId.Value)
                    .Select(item => item.DepartmentId)
                    .FirstOrDefaultAsync(cancellationToken);

                query = departmentId is null
                    ? query.Where(item => false)
                    : query.Where(item => item.RequesterUser != null && item.RequesterUser.DepartmentId == departmentId);
            }
        }

        var currentPage = Math.Max(1, page);
        var currentPageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<LeaveCancellationRequestResponse>>.Ok(new PagedResponse<LeaveCancellationRequestResponse>(
            items.Select(ToResponse).ToList(),
            currentPage,
            currentPageSize,
            total,
            (int)Math.Ceiling(total / (double)currentPageSize)));
    }

    [HttpGet("{id:guid}")]
    [RequireAnyPermission(
        LeavePermissions.CancellationViewOwn,
        LeavePermissions.CancellationApproveCurrentStep,
        LeavePermissions.CancellationViewDepartment,
        LeavePermissions.CancellationViewAll,
        LeavePermissions.CancellationManage)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> GetCancellationRequest(Guid id, CancellationToken cancellationToken)
    {
        var request = await LoadCancellationRequests().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (!await CanAccessCancellationAsync(request, cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(request));
    }

    [HttpGet("{id:guid}/approvals")]
    [RequireAnyPermission(
        LeavePermissions.CancellationViewOwn,
        LeavePermissions.CancellationApproveCurrentStep,
        LeavePermissions.CancellationViewDepartment,
        LeavePermissions.CancellationViewAll,
        LeavePermissions.CancellationManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveCancellationApprovalResponse>>>> GetCancellationApprovals(Guid id, CancellationToken cancellationToken)
    {
        var request = await LoadCancellationRequests().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<LeaveCancellationApprovalResponse>>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (!await CanAccessCancellationAsync(request, cancellationToken))
        {
            return Forbid();
        }

        return ApiResponse<IReadOnlyList<LeaveCancellationApprovalResponse>>.Ok(request.Approvals
            .OrderBy(item => item.StepOrder)
            .Select(ToApprovalResponse)
            .ToList());
    }

    [HttpGet("eligibility/{leaveRequestId:guid}")]
    [RequirePermission(LeavePermissions.CancellationCreate)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationEligibilityResponse>>> GetEligibility(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LeaveCancellationEligibilityResponse>.Fail("Invalid access token."));
        }

        var original = await LoadOriginalLeaveRequestAsync(leaveRequestId, cancellationToken);
        if (original is null)
        {
            return NotFound(ApiResponse<LeaveCancellationEligibilityResponse>.Fail("ไม่พบใบลาที่ต้องการยกเลิก"));
        }

        return ApiResponse<LeaveCancellationEligibilityResponse>.Ok(await BuildEligibilityAsync(original, userId.Value, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(LeavePermissions.CancellationCreate)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> CreateCancellationRequest(CreateLeaveCancellationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LeaveCancellationRequestResponse>.Fail("Invalid access token."));
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("กรุณาระบุเหตุผลในการขอยกเลิกใบลา"));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var original = await LoadOriginalLeaveRequestAsync(request.OriginalLeaveRequestId, cancellationToken);
        if (original is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบใบลาที่ต้องการยกเลิก"));
        }

        var validation = await BuildEligibilityAsync(original, userId.Value, cancellationToken);
        if (!validation.CanCreate)
        {
            await auditLogService.WriteAsync(userId, "LeaveCancellation.DuplicateBlocked", "LeaveRequest", original.Id.ToString(), validation.Message ?? "Cancellation blocked.", "Denied", HttpContext);
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail(validation.Message ?? "ไม่สามารถสร้างคำขอยกเลิกใบลาได้"));
        }

        var now = DateTime.UtcNow;
        var cancellation = new LeaveCancellationRequest
        {
            CancellationRequestNumber = await requestNumberService.GenerateCancellationAsync(now, cancellationToken),
            OriginalLeaveRequestId = original.Id,
            RequesterUserId = original.UserId,
            LeaveTypeId = original.LeaveTypeId,
            OriginalLeaveDays = original.TotalDays,
            Reason = request.Reason.Trim(),
            Status = LeaveCancellationStatuses.Draft,
            CreatedAt = now,
            CreatedByUserId = userId
        };
        db.LeaveCancellationRequests.Add(cancellation);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "LeaveCancellation.Created", "LeaveCancellationRequest", cancellation.Id.ToString(), $"Created cancellation request for leave {original.Id}.", "Success", HttpContext);

        if (request.Submit)
        {
            var submitResult = await SubmitCancellationCoreAsync(cancellation, original, userId.Value, cancellationToken);
            if (!submitResult.Success)
            {
                return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail(submitResult.Message));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (request.Submit)
        {
            await NotifyCancellationAsync("LeaveCancellationSubmitted", cancellation.Id, cancellation.CurrentApproverId, cancellationToken);
        }

        var created = await LoadCancellationRequests().SingleAsync(item => item.Id == cancellation.Id, cancellationToken);
        return CreatedAtAction(nameof(GetCancellationRequest), new { id = cancellation.Id }, ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(created)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(LeavePermissions.CancellationCreate)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> UpdateCancellationRequest(Guid id, UpdateLeaveCancellationRequest request, CancellationToken cancellationToken)
    {
        var cancellation = await db.LeaveCancellationRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cancellation is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (cancellation.RequesterUserId != GetCurrentUserId())
        {
            return Forbid();
        }

        if (cancellation.Status is not (LeaveCancellationStatuses.Draft or LeaveCancellationStatuses.ReturnedForRevision))
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("แก้ไขได้เฉพาะแบบร่างหรือคำขอที่ถูกตีกลับรอแก้ไขเท่านั้น"));
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("กรุณาระบุเหตุผลในการขอยกเลิกใบลา"));
        }

        cancellation.Reason = request.Reason.Trim();
        cancellation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(GetCurrentUserId(), "LeaveCancellation.Updated", "LeaveCancellationRequest", cancellation.Id.ToString(), "Updated cancellation request.", "Success", HttpContext);

        var updated = await LoadCancellationRequests().SingleAsync(item => item.Id == id, cancellationToken);
        return ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(updated));
    }

    [HttpPost("{id:guid}/submit")]
    [RequirePermission(LeavePermissions.CancellationSubmit)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> SubmitCancellationRequest(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<LeaveCancellationRequestResponse>.Fail("Invalid access token."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var cancellation = await db.LeaveCancellationRequests
            .Include(item => item.OriginalLeaveRequest)
                .ThenInclude(item => item!.User)
            .Include(item => item.OriginalLeaveRequest)
                .ThenInclude(item => item!.LeaveType)
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cancellation is null || cancellation.OriginalLeaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (cancellation.RequesterUserId != userId)
        {
            return Forbid();
        }

        if (cancellation.Status is not (LeaveCancellationStatuses.Draft or LeaveCancellationStatuses.ReturnedForRevision))
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("ส่งอนุมัติได้เฉพาะแบบร่างหรือคำขอที่ถูกตีกลับรอแก้ไขเท่านั้น"));
        }

        var result = await SubmitCancellationCoreAsync(cancellation, cancellation.OriginalLeaveRequest, userId.Value, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail(result.Message));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await NotifyCancellationAsync("LeaveCancellationSubmitted", cancellation.Id, cancellation.CurrentApproverId, cancellationToken);

        var updated = await LoadCancellationRequests().SingleAsync(item => item.Id == id, cancellationToken);
        return ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(updated), "ส่งคำขอยกเลิกใบลาเข้าสู่กระบวนการอนุมัติเรียบร้อยแล้ว");
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(LeavePermissions.CancellationApproveCurrentStep)]
    public Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> ApproveCancellationRequest(Guid id, LeaveCancellationDecisionRequest request, CancellationToken cancellationToken)
    {
        return DecideAsync(id, "Approved", request.Remark, cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(LeavePermissions.CancellationApproveCurrentStep)]
    public Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> RejectCancellationRequest(Guid id, LeaveCancellationDecisionRequest request, CancellationToken cancellationToken)
    {
        return DecideAsync(id, "Rejected", request.Remark, cancellationToken);
    }

    [HttpPost("{id:guid}/return-for-revision")]
    [RequirePermission(LeavePermissions.CancellationApproveCurrentStep)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> ReturnCancellationForRevision(Guid id, LeaveCancellationReturnForRevisionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("กรุณาระบุเหตุผลในการตีกลับ"));
        }

        var approverId = GetCurrentUserId();
        var cancellation = await db.LeaveCancellationRequests.Include(item => item.Approvals).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cancellation is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (cancellation.Status != LeaveCancellationStatuses.Pending)
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("ตีกลับได้เฉพาะคำขอที่รออนุมัติเท่านั้น"));
        }

        var approval = cancellation.Approvals.OrderBy(item => item.StepOrder).FirstOrDefault(item => item.Status == "Pending");
        if (approval is null || approverId is null || approval.ApproverId != approverId)
        {
            return Forbid();
        }

        approval.Status = LeaveCancellationStatuses.ReturnedForRevision;
        approval.ReturnReason = request.Reason.Trim();
        approval.ReturnedAt = DateTime.UtcNow;
        cancellation.Status = LeaveCancellationStatuses.ReturnedForRevision;
        cancellation.CurrentApproverId = null;
        cancellation.ReturnedForRevisionAt = DateTime.UtcNow;
        cancellation.ReturnedForRevisionByUserId = approverId;
        cancellation.RevisionReason = request.Reason.Trim();
        cancellation.RevisionCount += 1;
        cancellation.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(approverId, "LeaveCancellation.Returned", "LeaveCancellationRequest", cancellation.Id.ToString(), request.Reason.Trim(), "Success", HttpContext);
        await ClearActionRequiredAsync(cancellation.Id, null, cancellationToken);
        await NotifyCancellationAsync("LeaveCancellationReturned", cancellation.Id, cancellation.RequesterUserId, cancellationToken);

        var updated = await LoadCancellationRequests().SingleAsync(item => item.Id == id, cancellationToken);
        return ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(updated), "ตีกลับคำขอยกเลิกใบลาเรียบร้อยแล้ว");
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(LeavePermissions.CancellationCancelOwn)]
    public async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> CancelCancellationRequest(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var cancellation = await db.LeaveCancellationRequests.Include(item => item.Approvals).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cancellation is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (userId is null || cancellation.RequesterUserId != userId)
        {
            return Forbid();
        }

        if (cancellation.Status == LeaveCancellationStatuses.Approved)
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("คำขอยกเลิกใบลาที่อนุมัติแล้วไม่สามารถยกเลิกได้"));
        }

        cancellation.Status = LeaveCancellationStatuses.Cancelled;
        cancellation.CurrentApproverId = null;
        cancellation.CancelledAt = DateTime.UtcNow;
        cancellation.UpdatedAt = DateTime.UtcNow;
        foreach (var approval in cancellation.Approvals.Where(item => item.Status is "Pending" or "Waiting"))
        {
            approval.Status = "Cancelled";
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "LeaveCancellation.Cancelled", "LeaveCancellationRequest", cancellation.Id.ToString(), "Cancelled leave cancellation request.", "Success", HttpContext);
        await ClearActionRequiredAsync(cancellation.Id, null, cancellationToken);
        await NotifyCancellationAsync("LeaveCancellationCancelled", cancellation.Id, cancellation.RequesterUserId, cancellationToken);

        var updated = await LoadCancellationRequests().SingleAsync(item => item.Id == id, cancellationToken);
        return ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(updated), "ยกเลิกคำขอยกเลิกใบลาเรียบร้อยแล้ว");
    }

    private async Task<(bool Success, string Message)> SubmitCancellationCoreAsync(LeaveCancellationRequest cancellation, LeaveRequest original, Guid actorUserId, CancellationToken cancellationToken)
    {
        var validation = await BuildEligibilityAsync(original, cancellation.RequesterUserId, cancellationToken, cancellation.Id);
        if (!validation.CanCreate)
        {
            return (false, validation.Message ?? "ไม่สามารถส่งคำขอยกเลิกใบลาได้");
        }

        if (original.User?.LeaveApprovalRuleId is null)
        {
            return (false, "ยังไม่ได้กำหนดกฎการอนุมัติวันลาให้ผู้ใช้งานนี้");
        }

        var planSource = new LeaveRequest
        {
            Id = cancellation.Id,
            UserId = original.UserId,
            LeaveTypeId = original.LeaveTypeId,
            StartDate = original.StartDate,
            EndDate = original.EndDate,
            DurationType = original.DurationType,
            TotalDays = original.TotalDays,
            Status = "Draft",
            User = original.User,
            LeaveType = original.LeaveType
        };
        var approvalPlan = await approvalChainService.BuildApprovalPlanAsync(planSource);
        if (approvalPlan.Count == 0)
        {
            return (false, "ไม่พบผู้อนุมัติที่มีสิทธิ์ถูกต้อง หรือกฎการอนุมัติมี self approval โดยไม่มีผู้อนุมัติสำรอง");
        }

        cancellation.Status = LeaveCancellationStatuses.Pending;
        cancellation.SubmittedAt ??= DateTime.UtcNow;
        cancellation.LastResubmittedAt = cancellation.RevisionCount > 0 ? DateTime.UtcNow : cancellation.LastResubmittedAt;
        cancellation.UpdatedAt = DateTime.UtcNow;
        cancellation.ApprovalChainId = approvalPlan.First().ApprovalChainId;
        cancellation.CurrentApproverId = approvalPlan.First().ApproverId;
        db.LeaveCancellationApprovals.RemoveRange(cancellation.Approvals);
        cancellation.Approvals.Clear();
        var firstStep = approvalPlan.Min(item => item.StepOrder);
        foreach (var approval in approvalPlan)
        {
            cancellation.Approvals.Add(new LeaveCancellationApproval
            {
                LeaveCancellationRequestId = cancellation.Id,
                ApproverId = approval.ApproverId,
                ApprovalChainId = approval.ApprovalChainId,
                ApprovalChainStepId = approval.ApprovalChainStepId,
                StepOrder = approval.StepOrder,
                StepName = approval.StepName,
                RequiredPermissionCode = LeavePermissions.CancellationApproveCurrentStep,
                Status = approval.StepOrder == firstStep ? "Pending" : "Waiting"
            });
        }

        await auditLogService.WriteAsync(actorUserId, cancellation.RevisionCount > 0 ? "LeaveCancellation.Resubmitted" : "LeaveCancellation.Submitted", "LeaveCancellationRequest", cancellation.Id.ToString(), "Submitted cancellation request.", "Success", HttpContext);
        return (true, "Success");
    }

    private async Task<ActionResult<ApiResponse<LeaveCancellationRequestResponse>>> DecideAsync(Guid id, string decision, string? remark, CancellationToken cancellationToken)
    {
        var approverId = GetCurrentUserId();
        if (approverId is null)
        {
            return Unauthorized(ApiResponse<LeaveCancellationRequestResponse>.Fail("Invalid access token."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var cancellation = await db.LeaveCancellationRequests
            .Include(item => item.OriginalLeaveRequest)
                .ThenInclude(item => item!.LeaveType)
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cancellation is null || cancellation.OriginalLeaveRequest is null)
        {
            return NotFound(ApiResponse<LeaveCancellationRequestResponse>.Fail("ไม่พบคำขอยกเลิกใบลา"));
        }

        if (cancellation.Status != LeaveCancellationStatuses.Pending)
        {
            return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail("ดำเนินการได้เฉพาะคำขอยกเลิกใบลาที่รออนุมัติเท่านั้น"));
        }

        if (cancellation.RequesterUserId == approverId)
        {
            await auditLogService.WriteAsync(approverId, "SelfApprovalBlocked", "LeaveCancellationRequest", cancellation.Id.ToString(), "Blocked self approval for leave cancellation.", "Denied", HttpContext);
            return Forbid();
        }

        var approval = cancellation.Approvals.OrderBy(item => item.StepOrder).FirstOrDefault(item => item.Status == "Pending");
        if (approval is null || approval.ApproverId != approverId || !await HasPermissionAsync(approverId.Value, LeavePermissions.CancellationApproveCurrentStep, cancellationToken))
        {
            return Forbid();
        }

        approval.Status = decision;
        approval.Remark = remark;
        approval.ActionAt = DateTime.UtcNow;
        cancellation.UpdatedAt = DateTime.UtcNow;

        string? notificationEvent;
        Guid? notificationRecipientId;

        if (decision == "Rejected")
        {
            cancellation.Status = LeaveCancellationStatuses.Rejected;
            cancellation.CurrentApproverId = null;
            cancellation.RejectedAt = DateTime.UtcNow;
            foreach (var waitingApproval in cancellation.Approvals.Where(item => item.Status == "Waiting"))
            {
                waitingApproval.Status = "Skipped";
            }
            notificationEvent = "LeaveCancellationRejected";
            notificationRecipientId = cancellation.RequesterUserId;
        }
        else
        {
            var nextApproval = cancellation.Approvals.OrderBy(item => item.StepOrder).FirstOrDefault(item => item.Status == "Waiting");
            if (nextApproval is not null)
            {
                nextApproval.Status = "Pending";
                cancellation.CurrentApproverId = nextApproval.ApproverId;
                notificationEvent = "LeaveCancellationSubmitted";
                notificationRecipientId = nextApproval.ApproverId;
            }
            else
            {
                var applyResult = await ExecuteCancellationAsync(cancellation, approverId.Value, cancellationToken);
                if (!applyResult.Success)
                {
                    await auditLogService.WriteAsync(approverId, "LeaveCancellation.BalanceApplyFailed", "LeaveCancellationRequest", cancellation.Id.ToString(), applyResult.Message, "Failed", HttpContext);
                    return BadRequest(ApiResponse<LeaveCancellationRequestResponse>.Fail(applyResult.Message));
                }

                cancellation.Status = LeaveCancellationStatuses.Approved;
                cancellation.CurrentApproverId = null;
                cancellation.ApprovedAt = DateTime.UtcNow;
                notificationEvent = "LeaveCancellationCompleted";
                notificationRecipientId = cancellation.RequesterUserId;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await auditLogService.WriteAsync(approverId, decision == "Approved" ? "LeaveCancellation.Approved" : "LeaveCancellation.Rejected", "LeaveCancellationRequest", cancellation.Id.ToString(), $"{decision} cancellation step {approval.StepOrder}.", "Success", HttpContext);
        await ClearActionRequiredAsync(cancellation.Id, null, cancellationToken);
        await NotifyCancellationAsync(notificationEvent, cancellation.Id, notificationRecipientId, cancellationToken);

        var updated = await LoadCancellationRequests().SingleAsync(item => item.Id == id, cancellationToken);
        return ApiResponse<LeaveCancellationRequestResponse>.Ok(ToResponse(updated), decision == "Approved" ? "อนุมัติคำขอยกเลิกใบลาเรียบร้อยแล้ว" : "ไม่อนุมัติคำขอยกเลิกใบลาเรียบร้อยแล้ว");
    }

    private async Task<(bool Success, string Message)> ExecuteCancellationAsync(LeaveCancellationRequest cancellation, Guid actorUserId, CancellationToken cancellationToken)
    {
        var alreadyApplied = await db.LeaveBalanceTransactions.AnyAsync(item =>
            item.ReferenceType == "LeaveCancellationRequest" &&
            item.ReferenceId == cancellation.Id &&
            item.TransactionType == LeaveBalanceTransactionTypes.LeaveCancellationRestore,
            cancellationToken);
        if (alreadyApplied)
        {
            return (false, "คำขอยกเลิกใบลานี้ถูกคืนยอดไปแล้ว");
        }

        var original = cancellation.OriginalLeaveRequest!;
        if (original.Status != "Approved")
        {
            return (false, "ใบลาเดิมไม่ได้อยู่ในสถานะอนุมัติแล้ว");
        }

        var leaveType = original.LeaveType ?? await db.LeaveTypes.FirstAsync(item => item.Id == cancellation.LeaveTypeId, cancellationToken);
        var fiscalYear = FiscalYearHelper.ResolveBalanceYear(original.StartDate, leaveType);
        var balance = await db.LeaveBalances.FirstOrDefaultAsync(item =>
            item.UserId == cancellation.RequesterUserId &&
            item.LeaveTypeId == cancellation.LeaveTypeId &&
            item.Year == fiscalYear,
            cancellationToken);
        if (balance is null)
        {
            return (false, "ไม่พบยอดวันลาของปีงบประมาณใบลาเดิม กรุณาติดต่อ HR");
        }

        balance.UsedDays = Math.Max(0, balance.UsedDays - cancellation.OriginalLeaveDays);
        balance.UpdatedAt = DateTime.UtcNow;
        original.Status = OriginalLeaveCancelledStatus;
        original.CurrentApproverId = null;
        original.UpdatedAt = DateTime.UtcNow;
        cancellation.BalanceRestoredAt = DateTime.UtcNow;
        db.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
        {
            UserId = cancellation.RequesterUserId,
            LeaveTypeId = cancellation.LeaveTypeId,
            FiscalYear = fiscalYear,
            TransactionType = LeaveBalanceTransactionTypes.LeaveCancellationRestore,
            AmountDays = cancellation.OriginalLeaveDays,
            ReferenceType = "LeaveCancellationRequest",
            ReferenceId = cancellation.Id,
            Reason = cancellation.Reason,
            CreatedByUserId = actorUserId
        });

        await auditLogService.WriteAsync(actorUserId, "LeaveCancellation.BalanceRestored", "LeaveCancellationRequest", cancellation.Id.ToString(), $"Restored {cancellation.OriginalLeaveDays} days to fiscal year {fiscalYear}.", "Success", HttpContext);
        await auditLogService.WriteAsync(actorUserId, "LeaveCancellation.Completed", "LeaveCancellationRequest", cancellation.Id.ToString(), $"Original leave {original.Id} changed to {OriginalLeaveCancelledStatus}.", "Success", HttpContext);
        return (true, "Success");
    }

    private async Task<LeaveCancellationEligibilityResponse> BuildEligibilityAsync(LeaveRequest original, Guid requesterUserId, CancellationToken cancellationToken, Guid? currentCancellationId = null)
    {
        var alreadyCancelled = original.Status == OriginalLeaveCancelledStatus || original.Status == "Cancelled";
        var hasApprovedCancellation = await db.LeaveCancellationRequests
            .AsNoTracking()
            .AnyAsync(item => item.OriginalLeaveRequestId == original.Id && item.Status == LeaveCancellationStatuses.Approved && item.Id != currentCancellationId, cancellationToken);
        var hasActiveCancellation = await db.LeaveCancellationRequests
            .AsNoTracking()
            .AnyAsync(item => item.OriginalLeaveRequestId == original.Id && LeaveCancellationStatuses.ActiveStatuses.Contains(item.Status) && item.Id != currentCancellationId, cancellationToken);

        string? message = null;
        var canCreate = true;
        if (original.UserId != requesterUserId)
        {
            canCreate = false;
            message = "ขอยกเลิกใบลาได้เฉพาะใบลาของตนเอง";
        }
        else if (original.Status != "Approved")
        {
            canCreate = false;
            message = "ขอยกเลิกได้เฉพาะใบลาที่อนุมัติแล้ว";
        }
        else if (alreadyCancelled || hasApprovedCancellation)
        {
            canCreate = false;
            message = "ใบลานี้ถูกยกเลิกหลังอนุมัติแล้ว";
        }
        else if (original.LeaveType is not null && !original.LeaveType.RequiresBalance)
        {
            canCreate = false;
            message = "ประเภทลานี้ไม่ใช้ยอดวันลาคงเหลือ จึงไม่ต้องคืนยอดวันลา";
        }
        else if (hasActiveCancellation && currentCancellationId is null)
        {
            canCreate = false;
            message = "มีคำขอยกเลิกใบลานี้ที่ยังรอดำเนินการอยู่";
        }

        return new LeaveCancellationEligibilityResponse(
            original.Id,
            original.RequestNumber,
            canCreate,
            message,
            original.TotalDays,
            alreadyCancelled || hasApprovedCancellation,
            hasActiveCancellation);
    }

    private async Task<LeaveRequest?> LoadOriginalLeaveRequestAsync(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        return await db.LeaveRequests
            .Include(item => item.User)
            .Include(item => item.LeaveType)
            .FirstOrDefaultAsync(item => item.Id == leaveRequestId, cancellationToken);
    }

    private IQueryable<LeaveCancellationRequest> LoadCancellationRequests()
    {
        return db.LeaveCancellationRequests
            .AsNoTracking()
            .Include(item => item.OriginalLeaveRequest)
            .Include(item => item.RequesterUser)
                .ThenInclude(item => item!.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover)
            .Include(item => item.ReturnedForRevisionByUser)
            .Include(item => item.Approvals)
                .ThenInclude(item => item.Approver);
    }

    private async Task<IQueryable<LeaveCancellationRequest>> ApplyVisibilityAsync(IQueryable<LeaveCancellationRequest> query, Guid userId, CancellationToken cancellationToken)
    {
        if (await HasAnyPermissionAsync(userId, [LeavePermissions.CancellationViewAll, LeavePermissions.CancellationManage], cancellationToken))
        {
            return query;
        }

        if (await HasPermissionAsync(userId, LeavePermissions.CancellationViewDepartment, cancellationToken))
        {
            var departmentId = await db.Users.AsNoTracking().Where(item => item.Id == userId).Select(item => item.DepartmentId).FirstOrDefaultAsync(cancellationToken);
            return departmentId is null
                ? query.Where(item => item.RequesterUserId == userId || item.CurrentApproverId == userId)
                : query.Where(item => item.RequesterUserId == userId || item.CurrentApproverId == userId || item.RequesterUser!.DepartmentId == departmentId);
        }

        if (await HasPermissionAsync(userId, LeavePermissions.CancellationApproveCurrentStep, cancellationToken))
        {
            return query.Where(item => item.RequesterUserId == userId || item.CurrentApproverId == userId || item.Approvals.Any(approval => approval.ApproverId == userId && approval.ActionAt != null));
        }

        return query.Where(item => item.RequesterUserId == userId);
    }

    private async Task<bool> CanAccessCancellationAsync(LeaveCancellationRequest cancellation, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return false;
        }

        if (await HasAnyPermissionAsync(userId.Value, [LeavePermissions.CancellationViewAll, LeavePermissions.CancellationManage], cancellationToken))
        {
            return true;
        }

        if (cancellation.RequesterUserId == userId || cancellation.CurrentApproverId == userId || cancellation.Approvals.Any(item => item.ApproverId == userId && item.ActionAt != null))
        {
            return true;
        }

        if (await HasPermissionAsync(userId.Value, LeavePermissions.CancellationViewDepartment, cancellationToken))
        {
            var departmentId = await db.Users.AsNoTracking().Where(item => item.Id == userId).Select(item => item.DepartmentId).FirstOrDefaultAsync(cancellationToken);
            return departmentId is not null && cancellation.RequesterUser?.DepartmentId == departmentId;
        }

        return false;
    }

    private async Task NotifyCancellationAsync(string eventType, Guid cancellationRequestId, Guid? recipientUserId, CancellationToken cancellationToken)
    {
        var cancellation = await LoadCancellationRequests().FirstOrDefaultAsync(item => item.Id == cancellationRequestId, cancellationToken);
        if (cancellation is null || recipientUserId is null)
        {
            return;
        }

        var (title, message, notificationType, priority) = eventType switch
        {
            "LeaveCancellationSubmitted" => ($"คำขอยกเลิกใบลา {cancellation.CancellationRequestNumber} รออนุมัติ", BuildApproverMessage(cancellation), "ActionRequired", "High"),
            "LeaveCancellationCompleted" => ($"คำขอยกเลิกใบลา {cancellation.CancellationRequestNumber} อนุมัติแล้ว", $"ใบลาเดิม {cancellation.OriginalLeaveRequest?.RequestNumber ?? "-"} ถูกยกเลิกและคืนวันลา {cancellation.OriginalLeaveDays} วันแล้ว", "Information", "Success"),
            "LeaveCancellationRejected" => ($"คำขอยกเลิกใบลา {cancellation.CancellationRequestNumber} ไม่อนุมัติ", "คำขอยกเลิกใบลาของคุณไม่ได้รับการอนุมัติ", "Information", "High"),
            "LeaveCancellationCancelled" => ($"คำขอยกเลิกใบลา {cancellation.CancellationRequestNumber} ถูกยกเลิก", "คำขอยกเลิกใบลาถูกยกเลิกแล้ว", "Information", "Information"),
            "LeaveCancellationReturned" => ($"คำขอยกเลิกใบลา {cancellation.CancellationRequestNumber} ถูกตีกลับ", $"เหตุผล: {Blank(cancellation.RevisionReason)}", "Information", "High"),
            _ => ($"คำขอยกเลิกใบลา {cancellation.CancellationRequestNumber}", "มีรายการคำขอยกเลิกใบลาที่เกี่ยวข้องกับคุณ", "Information", "Information")
        };

        await ClearActionRequiredAsync(cancellation.Id, notificationType == "ActionRequired" ? recipientUserId : null, cancellationToken);
        db.Notifications.Add(new Notification
        {
            UserId = recipientUserId.Value,
            Channel = "InApp",
            Category = "Leave",
            NotificationType = notificationType,
            Priority = priority,
            Title = title,
            Message = message,
            ActionUrl = $"/leave/cancellations/{cancellation.Id}",
            ReferenceEntity = eventType,
            ReferenceId = cancellation.Id.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await lineMessagingService.NotifyUserPayloadAsync(
                recipientUserId.Value,
                eventType,
                BuildCancellationLinePayload(eventType, cancellation),
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LINE cancellation notification failed but workflow continues. Event={EventType}, CancellationRequestId={CancellationRequestId}", eventType, cancellation.Id);
        }
    }

    private string BuildCancellationLinePayload(string eventType, LeaveCancellationRequest cancellation)
    {
        return eventType switch
        {
            "LeaveCancellationSubmitted" => LeaveLineFlexMessageTemplates.BuildCancellationSubmittedCard(cancellation, lineConfiguration.PublicAppUrl),
            "LeaveCancellationCompleted" => LeaveLineFlexMessageTemplates.BuildCancellationApprovedCard(cancellation, lineConfiguration.PublicAppUrl),
            "LeaveCancellationRejected" => LeaveLineFlexMessageTemplates.BuildCancellationRejectedCard(cancellation, lineConfiguration.PublicAppUrl),
            "LeaveCancellationCancelled" => LeaveLineFlexMessageTemplates.BuildCancellationCancelledCard(cancellation, lineConfiguration.PublicAppUrl),
            "LeaveCancellationReturned" => LeaveLineFlexMessageTemplates.BuildCancellationReturnedCard(cancellation, lineConfiguration.PublicAppUrl),
            _ => LeaveLineFlexMessageTemplates.BuildCancellationSubmittedCard(cancellation, lineConfiguration.PublicAppUrl)
        };
    }

    private async Task ClearActionRequiredAsync(Guid cancellationRequestId, Guid? userId, CancellationToken cancellationToken)
    {
        var referenceId = cancellationRequestId.ToString();
        var query = db.Notifications.Where(item =>
            item.ReferenceId == referenceId &&
            item.NotificationType == "ActionRequired" &&
            item.ArchivedAt == null);

        if (userId is not null)
        {
            query = query.Where(item => item.UserId == userId);
        }

        var items = await query.ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt ??= DateTime.UtcNow;
            item.ArchivedAt = DateTime.UtcNow;
        }

        if (items.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken)
    {
        return db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode, cancellationToken);
    }

    private async Task<bool> HasAnyPermissionAsync(Guid userId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken)
    {
        return await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && permissionCodes.Contains(item.Permission.Code), cancellationToken);
    }

    private static string NormalizeCancellationStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "draft" => LeaveCancellationStatuses.Draft,
            "pending" or "submitted" or "inapproval" or "in_approval" => LeaveCancellationStatuses.Pending,
            "approved" => LeaveCancellationStatuses.Approved,
            "rejected" => LeaveCancellationStatuses.Rejected,
            "cancelled" or "canceled" => LeaveCancellationStatuses.Cancelled,
            "returned" or "returnedforrevision" or "returned_for_revision" => LeaveCancellationStatuses.ReturnedForRevision,
            _ => status.Trim()
        };
    }

    private static LeaveCancellationRequestResponse ToResponse(LeaveCancellationRequest item)
    {
        var currentApproval = item.Approvals.OrderBy(approval => approval.StepOrder).FirstOrDefault(approval => approval.Status == "Pending");
        return new LeaveCancellationRequestResponse(
            item.Id,
            item.CancellationRequestNumber,
            item.OriginalLeaveRequestId,
            item.OriginalLeaveRequest?.RequestNumber,
            item.RequesterUserId,
            item.RequesterUser?.FullName,
            item.LeaveTypeId,
            item.LeaveType?.Name,
            item.OriginalLeaveRequest?.StartDate,
            item.OriginalLeaveRequest?.EndDate,
            item.OriginalLeaveDays,
            item.Reason,
            item.Status,
            item.CurrentApproverId,
            item.CurrentApprover?.FullName,
            currentApproval?.StepName,
            item.CreatedAt,
            item.SubmittedAt,
            item.ApprovedAt,
            item.RejectedAt,
            item.CancelledAt,
            item.ReturnedForRevisionAt,
            item.BalanceRestoredAt,
            item.RevisionReason,
            item.RevisionCount,
            item.UpdatedAt);
    }

    private static LeaveCancellationApprovalResponse ToApprovalResponse(LeaveCancellationApproval item)
    {
        return new LeaveCancellationApprovalResponse(
            item.Id,
            item.LeaveCancellationRequestId,
            item.ApproverId,
            item.Approver?.FullName,
            item.ApprovalChainId,
            item.ApprovalChainStepId,
            item.StepOrder,
            item.StepName,
            item.Status,
            item.RequiredPermissionCode,
            item.Remark,
            item.CreatedAt,
            item.ActionAt,
            item.ReturnedAt,
            item.ReturnReason);
    }

    private static string BuildApproverMessage(LeaveCancellationRequest cancellation)
    {
        return $"ผู้ขอ: {cancellation.RequesterUser?.FullName ?? "-"} · ใบลาเดิม: {cancellation.OriginalLeaveRequest?.RequestNumber ?? "-"} · ประเภท: {cancellation.LeaveType?.Name ?? "-"} · คืนยอด {cancellation.OriginalLeaveDays} วัน";
    }

    private static string Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
