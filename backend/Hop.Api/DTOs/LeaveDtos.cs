namespace Hop.Api.DTOs;

public record LeaveTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal DefaultDaysPerYear,
    bool RequiresBalance,
    bool AllowCarryOver,
    decimal CarryOverMaxDays,
    bool UseFiscalYear,
    bool RequiresAttachment,
    bool IsPaid,
    bool IsActive
);

public record SaveLeaveTypeRequest(
    string Code,
    string Name,
    string? Description,
    decimal DefaultDaysPerYear,
    bool RequiresBalance,
    bool AllowCarryOver,
    decimal CarryOverMaxDays,
    bool UseFiscalYear,
    bool RequiresAttachment,
    bool IsPaid,
    bool IsActive
);

public record LeaveRequestResponse(
    Guid Id,
    string? RequestNumber,
    Guid UserId,
    string? Fullname,
    Guid LeaveTypeId,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string DurationType,
    decimal TotalDays,
    string Reason,
    string Status,
    Guid? CurrentApproverId,
    string? CurrentApproverName,
    string? CurrentApproverRole,
    string? CurrentStepName,
    DateTime? LatestActionAt,
    string CurrentStatusLabel,
    string TrackingMessage,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? UpdatedAt
);

public record SaveLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? DurationType,
    decimal TotalDays,
    string Reason
);

public record LeaveDecisionRequest(string? Remark);

public record LeaveOverrideDecisionRequest(string Reason);

public record LeaveAttachmentResponse(
    Guid Id,
    Guid LeaveRequestId,
    string FileName,
    string? ContentType,
    long FileSizeBytes,
    Guid UploadedByUserId,
    string? UploadedByName,
    DateTime CreatedAt
);

public record LeaveApprovalResponse(
    Guid Id,
    Guid LeaveRequestId,
    Guid ApproverId,
    string? ApproverName,
    Guid? ApprovalChainId,
    Guid? ApprovalChainStepId,
    int StepOrder,
    string? StepName,
    string Status,
    string RequiredPermissionCode,
    string? Remark,
    DateTime CreatedAt,
    DateTime? ActionAt
);

public record LeaveBalanceResponse(
    Guid? Id,
    Guid UserId,
    string? Fullname,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    decimal EntitledDays,
    decimal CarriedOverDays,
    decimal AdjustedDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays,
    decimal RemainingDays,
    string? Notes
);

public record SaveLeaveBalanceRequest(
    Guid UserId,
    Guid LeaveTypeId,
    int Year,
    decimal EntitledDays,
    decimal CarriedOverDays,
    decimal AdjustedDays,
    decimal UsedDays,
    decimal PendingDays,
    string? Notes
);

public record LeaveBalanceRolloverRequest(int TargetFiscalYear);

public record LeaveBalanceRolloverResponse(
    int TargetFiscalYear,
    int PreviousFiscalYear,
    int CreatedCount,
    int SkippedCount
);

public record LeaveBalanceRolloverPreviewResponse(
    Guid UserId,
    string? UserName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int FromFiscalYear,
    int ToFiscalYear,
    decimal EntitlementDays,
    decimal CarriedOverDays,
    decimal AdjustedDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal EndYearRemaining,
    decimal CarryOverMaxDays,
    decimal CarryOverDays,
    decimal ForfeitedDays,
    decimal NewEntitlementDays,
    decimal NewAvailableDays,
    bool TargetBalanceExists,
    IReadOnlyList<string> Warnings
);

public record LeaveBalanceRolloverConfirmRequest(
    int ToFiscalYear,
    decimal NewEntitlementDays,
    string Reason,
    bool UpdateExistingCarriedOverOnly = false
);

public record LeaveBalanceAdjustmentRequest(decimal AdjustmentDays, string Reason);

public record ApprovalChainResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? LeaveTypeId,
    string? LeaveTypeName,
    decimal MinimumDays,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int UserCount
);

public record SaveApprovalChainRequest(
    string Name,
    string? Description,
    Guid? DepartmentId,
    Guid? LeaveTypeId,
    decimal MinimumDays,
    bool IsActive
);

public record ApprovalChainStepResponse(
    Guid Id,
    Guid ApprovalChainId,
    int StepOrder,
    string Name,
    Guid? ApproverRoleId,
    string? ApproverRoleName,
    Guid? ApproverUserId,
    string? ApproverUserName,
    string RequiredPermissionCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SaveApprovalChainStepRequest(
    int StepOrder,
    string Name,
    Guid? ApproverRoleId,
    Guid? ApproverUserId,
    string RequiredPermissionCode,
    bool IsActive
);

public record LeaveBalanceAdjustmentResponse(
    Guid Id,
    Guid UserId,
    string? Fullname,
    Guid LeaveTypeId,
    string? LeaveTypeName,
    int Year,
    decimal AdjustmentDays,
    string Reason,
    Guid AdjustedByUserId,
    string? AdjustedByName,
    DateTime CreatedAt
);

public record CreateLeaveBalanceAdjustmentRequest(
    Guid UserId,
    Guid LeaveTypeId,
    int Year,
    decimal AdjustmentDays,
    string Reason
);

public record LeaveHolidayResponse(
    Guid Id,
    DateOnly HolidayDate,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SaveLeaveHolidayRequest(
    DateOnly HolidayDate,
    string Name,
    bool IsActive
);

public record ApprovalRuleResolvePreviewRequest(
    Guid? UserId,
    Guid? ApprovalRuleId
);

public record ApprovalRulePreviewStepResponse(
    int StepOrder,
    string StepName,
    Guid? ApproverId,
    string? ApproverName,
    string? ApproverRoleName,
    string Status,
    IReadOnlyList<string> Warnings
);

public record ApprovalRuleResolvePreviewResponse(
    Guid? UserId,
    string? Fullname,
    Guid? ApprovalRuleId,
    string? ApprovalRuleName,
    bool IsRuleActive,
    IReadOnlyList<ApprovalRulePreviewStepResponse> Steps,
    IReadOnlyList<string> Warnings
);

public record LeaveHolidayImportRowRequest(
    DateOnly HolidayDate,
    string Name,
    string HolidayType
);

public record LeaveHolidayImportPreviewRow(
    int RowNumber,
    DateOnly? HolidayDate,
    string Name,
    string HolidayType,
    bool IsValid,
    IReadOnlyList<string> Errors
);

public record LeaveHolidayImportPreviewResponse(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<LeaveHolidayImportPreviewRow> Rows
);

public record LeaveHolidayImportConfirmRequest(
    IReadOnlyList<LeaveHolidayImportRowRequest> Rows
);

public record LeaveHolidayImportConfirmResponse(
    int AddedCount,
    IReadOnlyList<LeaveHolidayImportPreviewRow> FailedRows
);

public record LeaveCalendarItemResponse(
    Guid Id,
    Guid UserId,
    string? Fullname,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid LeaveTypeId,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string DurationType,
    decimal TotalDays,
    string Status
);

public record ApprovalDelegationResponse(
    Guid Id,
    Guid ApproverUserId,
    string? ApproverName,
    Guid DelegateUserId,
    string? DelegateName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    bool IsActive,
    Guid? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CancelledAt
);

public record SaveApprovalDelegationRequest(
    Guid ApproverUserId,
    Guid DelegateUserId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    bool IsActive
);

public record ApprovalEscalationRuleResponse(
    Guid Id,
    string Name,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? LeaveTypeId,
    string? LeaveTypeName,
    int EscalateAfterHours,
    Guid? EscalateToUserId,
    string? EscalateToUserName,
    Guid? EscalateToRoleId,
    string? EscalateToRoleName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SaveApprovalEscalationRuleRequest(
    string Name,
    Guid? DepartmentId,
    Guid? LeaveTypeId,
    int EscalateAfterHours,
    Guid? EscalateToUserId,
    Guid? EscalateToRoleId,
    bool IsActive
);

public record LeaveReportItemResponse(
    Guid Id,
    string? RequestNumber,
    string? Fullname,
    string? DepartmentName,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string DurationType,
    decimal TotalDays,
    string Status,
    string? CurrentApproverName
);

public record LeaveBalanceReportItemResponse(
    Guid UserId,
    string? Fullname,
    string LeaveTypeName,
    int Year,
    decimal EntitledDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays
);

public record LeaveReportResponse(
    IReadOnlyList<LeaveReportItemResponse> LeaveRequests,
    IReadOnlyList<LeaveBalanceReportItemResponse> LeaveBalances,
    int PendingApprovalCount
);

public record PendingApprovalNotificationResponse(
    Guid RequestId,
    string? RequestNumber,
    string? EmployeeName,
    string? LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime? SubmittedAt,
    int CurrentStep,
    string Priority
);

public record LeaveNotificationItemResponse(
    string Id,
    string Type,
    Guid RequestId,
    string Title,
    string Message,
    DateTime CreatedAt,
    bool Unread,
    string Path
);

public record LeaveSupportRequestResponse(
    Guid Id,
    string RequestNumber,
    Guid UserId,
    string? Fullname,
    string? DepartmentName,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string DurationType,
    decimal TotalDays,
    string Status,
    Guid? CurrentApproverId,
    string? CurrentApproverName,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? UpdatedAt,
    bool IsOverdue,
    string? BlockingReason
);

public record LeaveSupportDetailResponse(
    LeaveSupportRequestResponse Request,
    IReadOnlyList<LeaveApprovalResponse> Approvals,
    IReadOnlyList<ApprovalOverrideLogResponse> OverrideLogs,
    IReadOnlyList<AuditLogItemResponse> AuditLogs
);

public record ApprovalOverrideLogResponse(
    Guid Id,
    Guid LeaveRequestId,
    Guid? OriginalApproverId,
    string? OriginalApproverName,
    Guid OverrideByUserId,
    string? OverrideByName,
    string Action,
    string Reason,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt
);

public record AuditLogItemResponse(
    Guid Id,
    Guid? UserId,
    string? Username,
    string? Fullname,
    string Action,
    string Resource,
    string? ResourceId,
    string? Detail,
    string? IpAddress,
    string Result,
    DateTime Timestamp
);
