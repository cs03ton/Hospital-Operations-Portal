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

export type SaveLeaveHolidayRequest = {
  holidayDate: string;
  name: string;
  isActive: boolean;
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

export async function getLeaveRequests() {
  const response = await httpClient.get<ApiResponse<LeaveRequest[]>>("/api/leave-requests");
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

export async function getLeaveApprovals(id: string) {
  const response = await httpClient.get<ApiResponse<LeaveApproval[]>>(`/api/leave-approvals/request/${id}`);
  return response.data.data;
}

export async function getMyLeaveBalances() {
  const response = await httpClient.get<ApiResponse<LeaveBalance[]>>("/api/leave-balances/me");
  return response.data.data;
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

export async function getLeaveHolidays() {
  const response = await httpClient.get<ApiResponse<LeaveHoliday[]>>("/api/leave-holidays");
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
