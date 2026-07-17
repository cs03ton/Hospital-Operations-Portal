import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";
import type { DepartmentSummary } from "./adminApi";

export type LeaveType = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  defaultDaysPerYear: number;
  requiresBalance: boolean;
  allowCarryOver: boolean;
  carryOverMaxDays: number;
  useFiscalYear: boolean;
  requiresAttachment: boolean;
  isPaid: boolean;
  isActive: boolean;
};

export type SaveLeaveTypeRequest = Omit<LeaveType, "id">;

export type LeaveRequest = {
  id: string;
  requestNumber?: string | null;
  userId: string;
  fullname?: string | null;
  leaveTypeId: string;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
  durationType: string;
  totalDays: number;
  reason: string;
  status: string;
  currentApproverId?: string | null;
  currentApproverName?: string | null;
  currentApproverRole?: string | null;
  currentStepName?: string | null;
  latestActionAt?: string | null;
  currentStatusLabel: string;
  trackingMessage: string;
  createdAt: string;
  submittedAt?: string | null;
  returnedForRevisionAt?: string | null;
  returnedForRevisionByUserId?: string | null;
  returnedForRevisionByName?: string | null;
  revisionReason?: string | null;
  revisionCount: number;
  lastResubmittedAt?: string | null;
  updatedAt?: string | null;
  cancellationRequestId?: string | null;
  cancellationRequestNumber?: string | null;
  cancellationStatus?: string | null;
};

export type SaveLeaveRequest = {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  durationType?: string | null;
  totalDays: number;
  reason: string;
};

export type LeavePolicyPreviewRequest = {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  durationType?: string | null;
};

export type LeavePolicyPreview = {
  employmentType?: string | null;
  employmentTypeName: string;
  fiscalYear: number;
  entitlementDays: number;
  usedDays: number;
  pendingDays: number;
  availableDays: number;
  requestedDays: number;
  canSubmit: boolean;
  warnings: string[];
  errors: string[];
  policyNotes: string[];
};

export type LeaveAttachment = {
  id: string;
  leaveRequestId: string;
  fileName: string;
  contentType?: string | null;
  fileSizeBytes: number;
  uploadedByUserId: string;
  uploadedByName?: string | null;
  createdAt: string;
};

export type LeaveApproval = {
  id: string;
  leaveRequestId: string;
  approverId: string;
  approverName?: string | null;
  approvalChainId?: string | null;
  approvalChainStepId?: string | null;
  stepOrder: number;
  stepName?: string | null;
  status: string;
  requiredPermissionCode: string;
  remark?: string | null;
  createdAt: string;
  actionAt?: string | null;
  returnedAt?: string | null;
  returnReason?: string | null;
};

export type LeaveCancellationRequest = {
  id: string;
  cancellationRequestNumber: string;
  originalLeaveRequestId: string;
  originalRequestNumber?: string | null;
  requesterUserId: string;
  requesterName?: string | null;
  leaveTypeId: string;
  leaveTypeName?: string | null;
  originalStartDate?: string | null;
  originalEndDate?: string | null;
  originalLeaveDays: number;
  reason: string;
  status: string;
  currentApproverId?: string | null;
  currentApproverName?: string | null;
  currentStepName?: string | null;
  createdAt: string;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  cancelledAt?: string | null;
  returnedForRevisionAt?: string | null;
  balanceRestoredAt?: string | null;
  revisionReason?: string | null;
  revisionCount: number;
  updatedAt?: string | null;
};

export type LeaveCancellationApproval = {
  id: string;
  leaveCancellationRequestId: string;
  approverId: string;
  approverName?: string | null;
  approvalChainId?: string | null;
  approvalChainStepId?: string | null;
  stepOrder: number;
  stepName?: string | null;
  status: string;
  requiredPermissionCode: string;
  remark?: string | null;
  createdAt: string;
  actionAt?: string | null;
  returnedAt?: string | null;
  returnReason?: string | null;
};

export type LeaveCancellationEligibility = {
  originalLeaveRequestId: string;
  originalRequestNumber?: string | null;
  canCreate: boolean;
  message?: string | null;
  originalLeaveDays: number;
  alreadyCancelled: boolean;
  hasActiveCancellation: boolean;
};

export type CreateLeaveCancellationRequest = {
  originalLeaveRequestId: string;
  reason: string;
  submit?: boolean;
};

export type LeaveBalance = {
  id?: string | null;
  userId: string;
  fullname?: string | null;
  departmentId?: string | null;
  departmentName?: string | null;
  leaveTypeId: string;
  leaveTypeName: string;
  year: number;
  entitledDays: number;
  carriedOverDays: number;
  adjustedDays: number;
  usedDays: number;
  pendingDays: number;
  availableDays: number;
  remainingDays: number;
  notes?: string | null;
};

export type ApprovalChain = {
  id: string;
  name: string;
  description?: string | null;
  departmentId?: string | null;
  departmentName?: string | null;
  leaveTypeId?: string | null;
  leaveTypeName?: string | null;
  minimumDays: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
  userCount: number;
};

export type SaveApprovalChainRequest = {
  name: string;
  description?: string | null;
  departmentId?: string | null;
  leaveTypeId?: string | null;
  minimumDays: number;
  isActive: boolean;
};

export type ApprovalChainStep = {
  id: string;
  approvalChainId: string;
  stepOrder: number;
  name: string;
  approverRoleId?: string | null;
  approverRoleName?: string | null;
  approverUserId?: string | null;
  approverUserName?: string | null;
  requiredPermissionCode: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type SaveApprovalChainStepRequest = {
  stepOrder: number;
  name: string;
  approverRoleId?: string | null;
  approverUserId?: string | null;
  requiredPermissionCode: string;
  isActive: boolean;
};

export type ApprovalRulePreviewStep = {
  stepOrder: number;
  stepName: string;
  approverId?: string | null;
  approverName?: string | null;
  approverRoleName?: string | null;
  status: string;
  warnings: string[];
};

export type ApprovalRulePreview = {
  userId?: string | null;
  fullname?: string | null;
  approvalRuleId?: string | null;
  approvalRuleName?: string | null;
  isRuleActive: boolean;
  steps: ApprovalRulePreviewStep[];
  warnings: string[];
};

export type ResolveApprovalRulePreviewRequest = {
  userId?: string | null;
  approvalRuleId?: string | null;
};

export type LeaveBalanceAdjustment = {
  id: string;
  userId: string;
  fullname?: string | null;
  leaveTypeId: string;
  leaveTypeName?: string | null;
  year: number;
  adjustmentDays: number;
  reason: string;
  adjustedByUserId: string;
  adjustedByName?: string | null;
  createdAt: string;
};

export type CreateLeaveBalanceAdjustmentRequest = {
  userId: string;
  leaveTypeId: string;
  year: number;
  adjustmentDays: number;
  reason: string;
};

export type LeaveHoliday = {
  id: string;
  holidayDate: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type LeaveHolidayQuery = {
  year?: number;
  page?: number;
  pageSize?: number;
  search?: string;
};

export type SaveLeaveBalanceRequest = {
  userId: string;
  leaveTypeId: string;
  year: number;
  entitledDays: number;
  carriedOverDays: number;
  adjustedDays: number;
  usedDays: number;
  pendingDays: number;
  notes?: string | null;
};

export type LeaveBalanceRolloverPreview = {
  userId: string;
  userName?: string | null;
  leaveTypeId: string;
  leaveTypeName: string;
  fromFiscalYear: number;
  toFiscalYear: number;
  entitlementDays: number;
  carriedOverDays: number;
  adjustedDays: number;
  usedDays: number;
  pendingDays: number;
  endYearRemaining: number;
  carryOverMaxDays: number;
  carryOverDays: number;
  forfeitedDays: number;
  newEntitlementDays: number;
  newAvailableDays: number;
  targetBalanceExists: boolean;
  warnings: string[];
};

export type LeaveBalanceRolloverItem = {
  userId: string;
  employeeName: string;
  departmentName?: string | null;
  employmentType?: string | null;
  employmentTypeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  fromFiscalYear: number;
  toFiscalYear: number;
  entitlementDays: number;
  carriedOverDays: number;
  adjustedDays: number;
  usedDays: number;
  pendingDays: number;
  endYearRemaining: number;
  carryOverCap: number;
  carryOverDays: number;
  forfeitedDays: number;
  newEntitlementDays: number;
  newAvailableDays: number;
  action: string;
  reason: string;
  warnings: string[];
};

export type LeaveBalanceRolloverBatch = {
  rolloverRunId?: string | null;
  fromFiscalYear: number;
  toFiscalYear: number;
  totalUsers: number;
  created: number;
  updated: number;
  skipped: number;
  blocked: number;
  items: LeaveBalanceRolloverItem[];
};

export type LeaveBalanceRolloverFilterRequest = {
  fromFiscalYear: number;
  toFiscalYear: number;
  departmentId?: string | null;
  employmentType?: string | null;
  leaveTypeId?: string | null;
  userId?: string | null;
};

export type ConfirmLeaveBalanceRolloverBatchRequest = LeaveBalanceRolloverFilterRequest & {
  reason: string;
};

export type ConfirmLeaveBalanceRolloverRequest = {
  toFiscalYear: number;
  newEntitlementDays: number;
  reason: string;
  updateExistingCarriedOverOnly?: boolean;
};

export type LeaveHolidayImportPreviewRow = {
  rowNumber: number;
  holidayDate?: string | null;
  name: string;
  holidayType: string;
  isValid: boolean;
  errors: string[];
};

export type LeaveHolidayImportPreview = {
  totalRows: number;
  validRows: number;
  invalidRows: number;
  rows: LeaveHolidayImportPreviewRow[];
};

export type LeaveHolidayImportConfirmRequest = {
  rows: {
    holidayDate: string;
    name: string;
    holidayType: string;
  }[];
};

export type LeaveHolidayImportConfirmResult = {
  addedCount: number;
  failedRows: LeaveHolidayImportPreviewRow[];
};

export type SaveLeaveHolidayRequest = {
  holidayDate: string;
  name: string;
  isActive: boolean;
};

export type LeaveCalendarItem = {
  id: string;
  userId: string;
  fullname?: string | null;
  departmentId?: string | null;
  departmentName?: string | null;
  leaveTypeId: string;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
  durationType: string;
  totalDays: number;
  status: string;
};

export type ApprovalDelegation = {
  id: string;
  approverUserId: string;
  approverName?: string | null;
  delegateUserId: string;
  delegateName?: string | null;
  startDate: string;
  endDate: string;
  reason: string;
  isActive: boolean;
  createdByUserId?: string | null;
  createdByName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  cancelledAt?: string | null;
};

export type LeaveRequestQuery = {
  leaveTypeId?: string;
  status?: string;
  scope?: "mine" | "department" | string;
  departmentId?: string;
  fromDate?: string;
  toDate?: string;
  userId?: string;
  page?: number;
  pageSize?: number;
};

export type SaveApprovalDelegationRequest = {
  approverUserId: string;
  delegateUserId: string;
  startDate: string;
  endDate: string;
  reason: string;
  isActive: boolean;
};

export type ApprovalEscalationRule = {
  id: string;
  name: string;
  departmentId?: string | null;
  departmentName?: string | null;
  leaveTypeId?: string | null;
  leaveTypeName?: string | null;
  escalateAfterHours: number;
  escalateToUserId?: string | null;
  escalateToUserName?: string | null;
  escalateToRoleId?: string | null;
  escalateToRoleName?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type SaveApprovalEscalationRuleRequest = {
  name: string;
  departmentId?: string | null;
  leaveTypeId?: string | null;
  escalateAfterHours: number;
  escalateToUserId?: string | null;
  escalateToRoleId?: string | null;
  isActive: boolean;
};

export type LeaveReportItem = {
  id: string;
  requestNumber?: string | null;
  fullname?: string | null;
  departmentName?: string | null;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
  durationType?: string | null;
  totalDays: number;
  status: string;
  currentApproverName?: string | null;
};

export type LeaveBalanceReportItem = {
  userId: string;
  fullname?: string | null;
  leaveTypeName: string;
  year: number;
  entitledDays: number;
  carriedOverDays: number;
  usedDays: number;
  pendingDays: number;
  availableDays: number;
  remainingDays: number;
};

export type LeaveReport = {
  leaveRequests: LeaveReportItem[];
  leaveBalances: LeaveBalanceReportItem[];
  pendingApprovalCount: number;
};

export type LeaveReportQuery = {
  from?: string;
  to?: string;
  departmentId?: string;
  leaveTypeId?: string;
};

export type LeaveAnalyticsQuery = {
  fiscalYear?: number;
  year?: number;
  month?: number;
  departmentId?: string;
  leaveTypeId?: string;
  status?: string;
  coreOnly?: boolean;
};

export type LeaveAnalyticsFilters = {
  fiscalYear: number;
  year?: number | null;
  month?: number | null;
  departmentId?: string | null;
  leaveTypeId?: string | null;
  status: string;
  coreOnly: boolean;
  startDate: string;
  endDate: string;
};

export type LeaveAnalyticsSummary = {
  totalRequests: number;
  uniqueUsers: number;
  totalDays: number;
  sickDays: number;
  personalDays: number;
  vacationDays: number;
  topDepartment?: string | null;
  topLeaveType?: string | null;
};

export type LeaveAnalyticsMonthlyTrend = {
  month: string;
  requestCount: number;
  uniqueUsers: number;
  totalDays: number;
};

export type LeaveAnalyticsDepartmentStack = {
  departmentId?: string | null;
  departmentName: string;
  sickDays: number;
  personalDays: number;
  vacationDays: number;
  totalDays: number;
};

export type LeaveAnalyticsLeaveTypeBreakdown = {
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeName: string;
  requestCount: number;
  totalDays: number;
};

export type LeaveAnalyticsHeatmap = {
  date: string;
  requestCount: number;
  uniqueUsers: number;
  totalDays: number;
};

export type LeaveAnalyticsTableItem = {
  id: string;
  requestNumber?: string | null;
  fullname?: string | null;
  departmentName?: string | null;
  leaveTypeCode?: string | null;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
  durationType: string;
  totalDays: number;
  status: string;
};

export type LeaveAnalytics = {
  filters: LeaveAnalyticsFilters;
  summary: LeaveAnalyticsSummary;
  monthlyTrend: LeaveAnalyticsMonthlyTrend[];
  departmentStacked: LeaveAnalyticsDepartmentStack[];
  leaveTypeBreakdown: LeaveAnalyticsLeaveTypeBreakdown[];
  heatmap: LeaveAnalyticsHeatmap[];
  items: LeaveAnalyticsTableItem[];
};

export type LeaveAnalyticsOptions = {
  departments: DepartmentSummary[];
  leaveTypes: LeaveType[];
};

export type PendingApprovalNotification = {
  requestId: string;
  requestNumber?: string | null;
  employeeName?: string | null;
  leaveType?: string | null;
  startDate: string;
  endDate: string;
  submittedAt?: string | null;
  currentStep: number;
  priority: string;
  sourceType?: string | null;
  detailPath?: string | null;
};

export type LeaveNotificationItem = {
  id: string;
  type: string;
  requestId?: string | null;
  title: string;
  message: string;
  createdAt: string;
  unread: boolean;
  path: string;
  category: string;
  priority: "Critical" | "High" | "Normal" | "Information" | "Success" | string;
  notificationType: "ActionRequired" | "Information" | string;
  targetRole?: string | null;
  referenceEntity?: string | null;
  referenceId?: string | null;
  expiresAt?: string | null;
};

export type NotificationCenterQuery = {
  page?: number;
  pageSize?: number;
  filter?: string;
  category?: string;
  search?: string;
};

export type NotificationReadResponse = {
  id: string;
  isRead: boolean;
  readAt?: string | null;
};

export type LeaveSupportRequest = {
  id: string;
  requestNumber: string;
  userId: string;
  fullname?: string | null;
  departmentName?: string | null;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
  durationType: string;
  totalDays: number;
  status: string;
  currentApproverId?: string | null;
  currentApproverName?: string | null;
  createdAt: string;
  submittedAt?: string | null;
  updatedAt?: string | null;
  isOverdue: boolean;
  blockingReason?: string | null;
};

export type ApprovalOverrideLog = {
  id: string;
  leaveRequestId: string;
  originalApproverId?: string | null;
  originalApproverName?: string | null;
  overrideByUserId: string;
  overrideByName?: string | null;
  action: string;
  reason: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt: string;
};

export type SupportAuditLog = {
  id: string;
  userId?: string | null;
  username?: string | null;
  fullname?: string | null;
  action: string;
  resource: string;
  resourceId?: string | null;
  detail?: string | null;
  ipAddress?: string | null;
  result: string;
  timestamp: string;
};

export type LeaveSupportDetail = {
  request: LeaveSupportRequest;
  approvals: LeaveApproval[];
  overrideLogs: ApprovalOverrideLog[];
  auditLogs: SupportAuditLog[];
};

export type LeaveSupportQuery = {
  search?: string;
  departmentId?: string;
  status?: string;
  currentApproverId?: string;
  fromDate?: string;
  toDate?: string;
};

export async function getLeaveTypes() {
  const response = await httpClient.get<ApiResponse<LeaveType[]>>("/api/leave-types");
  return response.data.data;
}

export async function createLeaveType(payload: SaveLeaveTypeRequest) {
  const response = await httpClient.post<ApiResponse<LeaveType>>("/api/leave-types", payload);
  return response.data.data;
}

export async function updateLeaveType(id: string, payload: SaveLeaveTypeRequest) {
  const response = await httpClient.put<ApiResponse<LeaveType>>(`/api/leave-types/${id}`, payload);
  return response.data.data;
}

export async function deactivateLeaveType(id: string) {
  await httpClient.delete(`/api/leave-types/${id}`);
}

export async function getLeaveRequests(params?: LeaveRequestQuery) {
  const response = await httpClient.get<ApiResponse<LeaveRequest[]>>("/api/leave-requests", { params });
  return response.data.data;
}

export async function getLeaveRequestsPaged(params: LeaveRequestQuery) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LeaveRequest>>>("/api/leave-requests", { params });
  return response.data.data;
}

export async function getLeaveRequest(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}`);
  return response.data.data;
}

export async function getLeaveCancellationEligibility(leaveRequestId: string) {
  const response = await httpClient.get<ApiResponse<LeaveCancellationEligibility>>(`/api/leave-cancellation-requests/eligibility/${leaveRequestId}`);
  return response.data.data;
}

export async function getLeaveCancellationRequests(params?: {
  page?: number;
  pageSize?: number;
  status?: string;
  scope?: string;
  leaveTypeId?: string;
  requesterId?: string;
  userId?: string;
  fromDate?: string;
  toDate?: string;
}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LeaveCancellationRequest>>>("/api/leave-cancellation-requests", { params });
  return response.data.data;
}

export async function getLeaveCancellationRequest(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}`);
  return response.data.data;
}

export async function getLeaveCancellationApprovals(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveCancellationApproval[]>>(`/api/leave-cancellation-requests/${id}/approvals`);
  return response.data.data;
}

export async function createLeaveCancellationRequest(payload: CreateLeaveCancellationRequest) {
  const response = await httpClient.post<ApiResponse<LeaveCancellationRequest>>("/api/leave-cancellation-requests", payload);
  return response.data.data;
}

export async function updateLeaveCancellationRequest(id: string, payload: { reason: string }) {
  const response = await httpClient.put<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}`, payload);
  return response.data.data;
}

export async function submitLeaveCancellationRequest(id: string) {
  const response = await httpClient.post<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}/submit`);
  return response.data.data;
}

export async function cancelLeaveCancellationRequest(id: string) {
  const response = await httpClient.post<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}/cancel`);
  return response.data.data;
}

export async function approveLeaveCancellationRequest(id: string, remark?: string) {
  const response = await httpClient.post<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}/approve`, { remark });
  return response.data.data;
}

export async function rejectLeaveCancellationRequest(id: string, remark?: string) {
  const response = await httpClient.post<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}/reject`, { remark });
  return response.data.data;
}

export async function returnLeaveCancellationForRevision(id: string, reason: string) {
  const response = await httpClient.post<ApiResponse<LeaveCancellationRequest>>(`/api/leave-cancellation-requests/${id}/return-for-revision`, { reason });
  return response.data.data;
}

export async function createLeaveRequest(payload: SaveLeaveRequest) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>("/api/leave-requests", payload);
  return response.data.data;
}

export async function previewLeavePolicy(payload: LeavePolicyPreviewRequest) {
  const response = await httpClient.post<ApiResponse<LeavePolicyPreview>>("/api/leave-requests/policy-preview", payload);
  return response.data.data;
}

export async function updateLeaveRequest(id: string, payload: SaveLeaveRequest) {
  const response = await httpClient.put<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}`, payload);
  return response.data.data;
}

export async function submitLeaveRequest(id: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/submit`);
  return response.data.data;
}

export async function resubmitLeaveRequest(id: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/resubmit`);
  return response.data.data;
}

export async function cancelLeaveRequest(id: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/cancel`);
  return response.data.data;
}

export async function approveLeaveRequest(id: string, remark?: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/approve`, { remark });
  return response.data.data;
}

export async function rejectLeaveRequest(id: string, remark?: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/reject`, { remark });
  return response.data.data;
}

export async function returnLeaveRequestForRevision(id: string, reason: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/return-for-revision`, { reason });
  return response.data.data;
}

export async function recordLineApprovalActionOpened(id: string, action: string) {
  const response = await httpClient.post<ApiResponse<boolean>>(`/api/leave-requests/${id}/line-action-opened`, { action });
  return response.data.data;
}

export async function getLeaveAttachments(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveAttachment[]>>(`/api/leave-requests/${id}/attachments`);
  return response.data.data;
}

export async function uploadLeaveAttachment(id: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  const response = await httpClient.post<ApiResponse<LeaveAttachment>>(
    `/api/leave-requests/${id}/attachments`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  );
  return response.data.data;
}

export async function deleteLeaveAttachment(id: string) {
  await httpClient.delete(`/api/leave-attachments/${id}`);
}

export async function downloadLeaveAttachment(id: string) {
  const response = await httpClient.get(`/api/leave-attachments/${id}/download`, {
    responseType: "blob",
  });
  return response.data as Blob;
}

export async function previewLeaveAttachment(leaveRequestId: string, attachmentId: string) {
  const response = await httpClient.get(`/api/leave-requests/${leaveRequestId}/attachments/${attachmentId}/preview`, {
    responseType: "blob",
  });
  return response.data as Blob;
}

export async function downloadLeaveRequestPdf(id: string) {
  const response = await httpClient.get(`/api/leave-requests/${id}/pdf`, {
    responseType: "blob",
  });
  return response.data as Blob;
}

export async function getLeaveApprovals(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveApproval[]>>(`/api/leave-approvals/request/${id}`);
  return response.data.data;
}

export async function getMyPendingApprovals() {
  const response = await httpClient.get<ApiResponse<PendingApprovalNotification[]>>("/api/approvals/my-pending");
  return response.data.data;
}

export async function getMyNotifications() {
  const response = await httpClient.get<ApiResponse<LeaveNotificationItem[]>>("/api/notifications/me");
  return response.data.data;
}

export async function getNotificationCenter(params?: NotificationCenterQuery) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LeaveNotificationItem>>>("/api/notifications", { params });
  return response.data.data;
}

export async function getNotificationBadgeCount() {
  const response = await httpClient.get<ApiResponse<number>>("/api/notifications/badge");
  return response.data.data;
}

export async function markNotificationAsRead(id: string) {
  const response = await httpClient.post<ApiResponse<NotificationReadResponse>>(`/api/notifications/${id}/read`);
  return response.data.data;
}

export async function getLeaveSupportRequests(params?: LeaveSupportQuery) {
  const response = await httpClient.get<ApiResponse<LeaveSupportRequest[]>>("/api/leave-support/requests", { params });
  return response.data.data;
}

export async function getLeaveSupportDetail(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveSupportDetail>>(`/api/leave-support/requests/${id}`);
  return response.data.data;
}

export async function overrideApproveLeaveRequest(id: string, reason: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/override-approve`, { reason });
  return response.data.data;
}

export async function overrideRejectLeaveRequest(id: string, reason: string) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}/override-reject`, { reason });
  return response.data.data;
}

export async function getMyLeaveBalances() {
  const response = await httpClient.get<ApiResponse<LeaveBalance[]>>("/api/leave-balances/me");
  return response.data.data;
}

export async function getLeaveBalances(params?: { year?: number; userId?: string; departmentId?: string; leaveTypeId?: string }) {
  const response = await httpClient.get<ApiResponse<LeaveBalance[]>>("/api/leave-balances", { params });
  return response.data.data;
}

export async function createLeaveBalance(payload: SaveLeaveBalanceRequest) {
  const response = await httpClient.post<ApiResponse<LeaveBalance>>("/api/leave-balances", payload);
  return response.data.data;
}

export async function updateLeaveBalance(id: string, payload: SaveLeaveBalanceRequest) {
  const response = await httpClient.put<ApiResponse<LeaveBalance>>(`/api/leave-balances/${id}`, payload);
  return response.data.data;
}

export async function adjustLeaveBalance(id: string, payload: { adjustmentDays: number; reason: string }) {
  const response = await httpClient.post<ApiResponse<LeaveBalance>>(`/api/leave-balances/${id}/adjust`, payload);
  return response.data.data;
}

export async function deleteLeaveBalance(id: string) {
  await httpClient.delete(`/api/leave-balances/${id}`);
}

export async function rolloverLeaveBalances(targetFiscalYear: number) {
  const response = await httpClient.post<ApiResponse<{ targetFiscalYear: number; previousFiscalYear: number; createdCount: number; skippedCount: number }>>(
    "/api/leave-balances/rollover",
    { targetFiscalYear },
  );
  return response.data.data;
}

export async function previewLeaveBalanceRolloverBatch(payload: LeaveBalanceRolloverFilterRequest) {
  const response = await httpClient.post<ApiResponse<LeaveBalanceRolloverBatch>>("/api/leave-balances/rollover/preview", payload);
  return response.data.data;
}

export async function confirmLeaveBalanceRolloverBatch(payload: ConfirmLeaveBalanceRolloverBatchRequest) {
  const response = await httpClient.post<ApiResponse<LeaveBalanceRolloverBatch>>("/api/leave-balances/rollover/confirm", payload);
  return response.data.data;
}

export async function exportLeaveBalanceRolloverPreview(payload: LeaveBalanceRolloverFilterRequest) {
  const response = await httpClient.post("/api/leave-balances/rollover/export-preview", payload, { responseType: "blob" });
  return response.data as Blob;
}

export async function previewLeaveBalanceRollover(id: string) {
  const response = await httpClient.post<ApiResponse<LeaveBalanceRolloverPreview>>(`/api/leave-balances/${id}/rollover-preview`);
  return response.data.data;
}

export async function confirmLeaveBalanceRollover(id: string, payload: ConfirmLeaveBalanceRolloverRequest) {
  const response = await httpClient.post<ApiResponse<LeaveBalance>>(`/api/leave-balances/${id}/rollover-confirm`, payload);
  return response.data.data;
}

export async function downloadLeaveBalanceTemplate() {
  const response = await httpClient.get("/api/leave-balances/import-template", { responseType: "blob" });
  return response.data as Blob;
}

export async function getApprovalChains() {
  const response = await httpClient.get<ApiResponse<ApprovalChain[]>>("/api/approval-chains");
  return response.data.data;
}

export async function getApprovalChain(id: string) {
  const response = await httpClient.get<ApiResponse<ApprovalChain>>(`/api/approval-chains/${id}`);
  return response.data.data;
}

export async function createApprovalChain(payload: SaveApprovalChainRequest) {
  const response = await httpClient.post<ApiResponse<ApprovalChain>>("/api/approval-chains", payload);
  return response.data.data;
}

export async function updateApprovalChain(id: string, payload: SaveApprovalChainRequest) {
  const response = await httpClient.put<ApiResponse<ApprovalChain>>(`/api/approval-chains/${id}`, payload);
  return response.data.data;
}

export async function deactivateApprovalChain(id: string) {
  await httpClient.delete(`/api/approval-chains/${id}`);
}

export async function getApprovalChainSteps(id: string) {
  const response = await httpClient.get<ApiResponse<ApprovalChainStep[]>>(`/api/approval-chains/${id}/steps`);
  return response.data.data;
}

export async function createApprovalChainStep(id: string, payload: SaveApprovalChainStepRequest) {
  const response = await httpClient.post<ApiResponse<ApprovalChainStep>>(`/api/approval-chains/${id}/steps`, payload);
  return response.data.data;
}

export async function updateApprovalChainStep(id: string, payload: SaveApprovalChainStepRequest) {
  const response = await httpClient.put<ApiResponse<ApprovalChainStep>>(`/api/approval-chain-steps/${id}`, payload);
  return response.data.data;
}

export async function deleteApprovalChainStep(id: string) {
  await httpClient.delete(`/api/approval-chain-steps/${id}`);
}

export async function resolveApprovalRulePreview(payload: ResolveApprovalRulePreviewRequest) {
  const response = await httpClient.post<ApiResponse<ApprovalRulePreview>>("/api/approval-chains/resolve-preview", payload);
  return response.data.data;
}

export async function getLeaveBalanceAdjustments() {
  const response = await httpClient.get<ApiResponse<LeaveBalanceAdjustment[]>>("/api/leave-balance-adjustments");
  return response.data.data;
}

export async function createLeaveBalanceAdjustment(payload: CreateLeaveBalanceAdjustmentRequest) {
  const response = await httpClient.post<ApiResponse<LeaveBalanceAdjustment>>("/api/leave-balance-adjustments", payload);
  return response.data.data;
}

export async function getLeaveHolidays(params?: { year?: number }) {
  const response = await httpClient.get<ApiResponse<LeaveHoliday[] | PagedResponse<LeaveHoliday>>>("/api/leave-holidays", { params });
  const data = response.data.data;
  return Array.isArray(data) ? data : data.items;
}

export async function getLeaveHolidaysPaged(params: LeaveHolidayQuery) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LeaveHoliday>>>("/api/leave-holidays", { params });
  return response.data.data;
}

export async function createLeaveHoliday(payload: SaveLeaveHolidayRequest) {
  const response = await httpClient.post<ApiResponse<LeaveHoliday>>("/api/leave-holidays", payload);
  return response.data.data;
}

export async function updateLeaveHoliday(id: string, payload: SaveLeaveHolidayRequest) {
  const response = await httpClient.put<ApiResponse<LeaveHoliday>>(`/api/leave-holidays/${id}`, payload);
  return response.data.data;
}

export async function deleteLeaveHoliday(id: string) {
  await httpClient.delete(`/api/leave-holidays/${id}`);
}

export async function downloadLeaveHolidayTemplate() {
  const response = await httpClient.get("/api/leave-holidays/import-template", { responseType: "blob" });
  return response.data as Blob;
}

export async function previewLeaveHolidayImport(file: File) {
  const formData = new FormData();
  formData.append("file", file);
  const response = await httpClient.post<ApiResponse<LeaveHolidayImportPreview>>(
    "/api/leave-holidays/import/preview",
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  );
  return response.data.data;
}

export async function confirmLeaveHolidayImport(payload: LeaveHolidayImportConfirmRequest) {
  const response = await httpClient.post<ApiResponse<LeaveHolidayImportConfirmResult>>(
    "/api/leave-holidays/import/confirm",
    payload,
  );
  return response.data.data;
}

export async function getLeaveCalendar(params: { year?: number; month?: number; departmentId?: string; leaveTypeId?: string; status?: string }) {
  const response = await httpClient.get<ApiResponse<LeaveCalendarItem[]>>("/api/leave-calendar", { params });
  return response.data.data;
}

export async function getApprovalDelegations() {
  const response = await httpClient.get<ApiResponse<ApprovalDelegation[]>>("/api/approval-delegations");
  return response.data.data;
}

export async function createApprovalDelegation(payload: SaveApprovalDelegationRequest) {
  const response = await httpClient.post<ApiResponse<ApprovalDelegation>>("/api/approval-delegations", payload);
  return response.data.data;
}

export async function updateApprovalDelegation(id: string, payload: SaveApprovalDelegationRequest) {
  const response = await httpClient.put<ApiResponse<ApprovalDelegation>>(`/api/approval-delegations/${id}`, payload);
  return response.data.data;
}

export async function deactivateApprovalDelegation(id: string) {
  await httpClient.delete(`/api/approval-delegations/${id}`);
}

export async function getApprovalEscalationRules() {
  const response = await httpClient.get<ApiResponse<ApprovalEscalationRule[]>>("/api/approval-escalation-rules");
  return response.data.data;
}

export async function createApprovalEscalationRule(payload: SaveApprovalEscalationRuleRequest) {
  const response = await httpClient.post<ApiResponse<ApprovalEscalationRule>>("/api/approval-escalation-rules", payload);
  return response.data.data;
}

export async function runApprovalEscalation() {
  const response = await httpClient.post<ApiResponse<number>>("/api/approval-escalation-rules/run");
  return response.data.data;
}

export async function getLeaveReport(params: LeaveReportQuery) {
  const response = await httpClient.get<ApiResponse<LeaveReport>>("/api/reports/leaves", { params });
  return response.data.data;
}

export async function downloadLeaveReportExcel(params: LeaveReportQuery) {
  const response = await httpClient.get("/api/reports/leaves/export-excel", { params, responseType: "blob" });
  return response.data as Blob;
}

export async function downloadLeaveReportPdf(params: LeaveReportQuery) {
  const response = await httpClient.get("/api/reports/leaves/export-pdf", { params, responseType: "blob" });
  return response.data as Blob;
}

export async function getLeaveAnalytics(params: LeaveAnalyticsQuery) {
  const response = await httpClient.get<ApiResponse<LeaveAnalytics>>("/api/reports/leave-analytics", { params });
  return response.data.data;
}

export async function getLeaveAnalyticsOptions() {
  const response = await httpClient.get<ApiResponse<LeaveAnalyticsOptions>>("/api/reports/leave-analytics/options");
  return response.data.data;
}

export async function downloadLeaveAnalyticsExcel(params: LeaveAnalyticsQuery) {
  const response = await httpClient.get("/api/reports/leave-analytics/export-excel", { params, responseType: "blob" });
  return response.data as Blob;
}
