import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type LeaveType = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  defaultDaysPerYear: number;
  requiresAttachment: boolean;
  isPaid: boolean;
  isActive: boolean;
};

export type SaveLeaveTypeRequest = Omit<LeaveType, "id">;

export type LeaveRequest = {
  id: string;
  userId: string;
  fullname?: string | null;
  leaveTypeId: string;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
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
  updatedAt?: string | null;
};

export type SaveLeaveRequest = {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
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
};

export type LeaveBalance = {
  id?: string | null;
  userId: string;
  fullname?: string | null;
  leaveTypeId: string;
  leaveTypeName: string;
  year: number;
  entitledDays: number;
  usedDays: number;
  pendingDays: number;
  remainingDays: number;
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

export type SaveLeaveBalanceRequest = {
  userId: string;
  leaveTypeId: string;
  year: number;
  entitledDays: number;
  usedDays: number;
  pendingDays: number;
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
  createdAt: string;
  updatedAt?: string | null;
};

export type LeaveRequestQuery = {
  leaveTypeId?: string;
  status?: string;
  departmentId?: string;
  fromDate?: string;
  toDate?: string;
  userId?: string;
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
  fullname?: string | null;
  departmentName?: string | null;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
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
  usedDays: number;
  pendingDays: number;
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

export type PendingApprovalNotification = {
  requestId: string;
  employeeName?: string | null;
  leaveType?: string | null;
  startDate: string;
  endDate: string;
  submittedAt?: string | null;
  currentStep: number;
  priority: string;
};

export type LeaveNotificationItem = {
  id: string;
  type: string;
  requestId: string;
  title: string;
  message: string;
  createdAt: string;
  unread: boolean;
  path: string;
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

export async function getLeaveRequest(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveRequest>>(`/api/leave-requests/${id}`);
  return response.data.data;
}

export async function createLeaveRequest(payload: SaveLeaveRequest) {
  const response = await httpClient.post<ApiResponse<LeaveRequest>>("/api/leave-requests", payload);
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

export async function getMyLeaveBalances() {
  const response = await httpClient.get<ApiResponse<LeaveBalance[]>>("/api/leave-balances/me");
  return response.data.data;
}

export async function getLeaveBalances(params?: { year?: number; userId?: string; leaveTypeId?: string }) {
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

export async function deleteLeaveBalance(id: string) {
  await httpClient.delete(`/api/leave-balances/${id}`);
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

export async function getLeaveBalanceAdjustments() {
  const response = await httpClient.get<ApiResponse<LeaveBalanceAdjustment[]>>("/api/leave-balance-adjustments");
  return response.data.data;
}

export async function createLeaveBalanceAdjustment(payload: CreateLeaveBalanceAdjustmentRequest) {
  const response = await httpClient.post<ApiResponse<LeaveBalanceAdjustment>>("/api/leave-balance-adjustments", payload);
  return response.data.data;
}

export async function getLeaveHolidays(params?: { year?: number }) {
  const response = await httpClient.get<ApiResponse<LeaveHoliday[]>>("/api/leave-holidays", { params });
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

export async function deactivateLeaveHoliday(id: string) {
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
