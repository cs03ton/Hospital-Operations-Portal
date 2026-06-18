namespace Hop.Api.DTOs;

public record LeaveTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal DefaultDaysPerYear,
    bool RequiresAttachment,
    bool IsPaid,
    bool IsActive
);

public record SaveLeaveTypeRequest(
    string Code,
    string Name,
    string? Description,
    decimal DefaultDaysPerYear,
    bool RequiresAttachment,
    bool IsPaid,
    bool IsActive
);

public record LeaveRequestResponse(
    Guid Id,
    Guid UserId,
    string? Fullname,
    Guid LeaveTypeId,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string Status,
    Guid? CurrentApproverId,
    string? CurrentApproverName,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? UpdatedAt
);

public record SaveLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason
);

public record LeaveDecisionRequest(string? Remark);

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
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    decimal EntitledDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays
);

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
    DateTime? UpdatedAt
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
    DateTime CreatedAt,
    DateTime? UpdatedAt
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
    string? Fullname,
    string? DepartmentName,
    string? LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
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
