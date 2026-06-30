import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type UserSummary = {
  id: string;
  employeeCode?: string | null;
  fullname: string;
  username: string;
  email?: string | null;
  roleIds: string[];
  roles: string[];
  departmentId?: string | null;
  department?: string | null;
  leaveApprovalRuleId?: string | null;
  leaveApprovalRuleName?: string | null;
  lineUserId?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type SaveUserRequest = {
  employeeCode?: string | null;
  fullname: string;
  username?: string;
  password?: string;
  roleIds: string[];
  departmentId?: string | null;
  leaveApprovalRuleId?: string | null;
  lineUserId?: string | null;
  isActive: boolean;
};

export type DepartmentSummary = {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type SaveDepartmentRequest = {
  name: string;
  description?: string | null;
  isActive: boolean;
};

export type RoleSummary = {
  id: string;
  name: string;
  description?: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type SaveRoleRequest = {
  name: string;
  description?: string | null;
  isActive: boolean;
};

export type PermissionSummary = {
  id: string;
  code: string;
  name: string;
  group: string;
  action: string;
  isActive: boolean;
};

export type DashboardSummary = {
  totalUsers: number;
  totalDepartments: number;
  pendingApprovals: number;
  totalPendingLeaveRequests: number;
  openRepairRequests: number;
  activeBorrowRequests: number;
  inventoryItems: number;
  staffOnLeaveToday: number;
  staffOnLeaveThisWeek: number;
  staffOnLeaveThisMonth: number;
  myRemainingLeaveDays: number;
  myLeaveRequestsTotal: number;
  myLeaveRequestsPending: number;
  myLeaveRequestsApproved: number;
  myLeaveRequestsRejected: number;
  myLeaveRequestsCancelled: number;
  totalLeaveTypes: number;
  totalApprovalRules: number;
  totalHolidaysThisYear: number;
  totalAuditLogsToday: number;
  loginEventsToday: number;
  failedLoginEventsToday: number;
  permissionDeniedEventsToday: number;
  unreadNotifications: number;
  lineQueued: number;
  lineFailed: number;
  apiHealth: string;
  databaseStatus: string;
  applicationVersion: string;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AuditLogSummary = {
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

export type AuditLogQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  userId?: string;
  action?: string;
  from?: string;
  to?: string;
};

export type SystemSettings = {
  hospitalName: string;
  hospitalLogoPath: string;
  footerText: string;
  footerDeveloper: string;
  themePrimaryColor: string;
  themeSecondaryColor: string;
  applicationVersion: string;
  pdfTemplateConfigPath: string;
  pdfFontPath: string;
  pdfFontFamily: string;
  pdfFontSize: number;
  pdfLineHeight: number;
  lineEnabled: boolean;
  lineChannelAccessTokenConfigured: boolean;
  lineEndpoint: string;
};

export type LineSettings = {
  enabled: boolean;
  channelId?: string | null;
  hasChannelSecret: boolean;
  hasAccessToken: boolean;
  channelSecretConfigured: boolean;
  channelAccessTokenConfigured: boolean;
  testUserId?: string | null;
  endpoint: string;
};

export type LineTestSendRequest = {
  toUserId?: string | null;
  message: string;
};

export type LineTestSendResponse = {
  success: boolean;
  message: string;
  deliveryStatus?: string | null;
  deliveryLogId?: string | null;
  httpStatusCode?: number | null;
  responseTimeMs?: number | null;
  error?: string | null;
};

export type LineChecklistItem = {
  label: string;
  passed: boolean;
  recommendation: string;
};

export type LineOperationsStatus = {
  enabled: boolean;
  connectionStatus: string;
  channelIdMasked?: string | null;
  hasChannelSecret: boolean;
  hasAccessToken: boolean;
  hasTestUserId: boolean;
  testUserIdMasked?: string | null;
  endpoint: string;
  environment: string;
  webhookActive: boolean;
  botName?: string | null;
  lastSuccessfulDelivery?: string | null;
  lastFailedDelivery?: string | null;
  queueLength: number;
  pendingRetry: number;
  averageResponseTimeMs?: number | null;
  checklist: LineChecklistItem[];
};

export type LineConnectionValidation = {
  success: boolean;
  message: string;
  httpStatusCode?: number | null;
  responseTimeMs: number;
  botName?: string | null;
  checklist: LineChecklistItem[];
};

export type LineDeliveryLog = {
  id: string;
  date: string;
  recipient: string;
  module: string;
  event: string;
  status: string;
  retry: number;
  durationMs?: number | null;
  error?: string | null;
};

export type LineNotificationSimulatorRequest = {
  userId: string;
  eventType: string;
  message?: string | null;
};

export type LineFlexPreview = {
  payload: string;
  validation: LineChecklistItem[];
};

export type LineFlexValidateRequest = {
  payload: string;
};

export type LineFlexValidateResponse = {
  isValid: boolean;
  message: string;
  checks: LineChecklistItem[];
};

export type LineFlexTestSendRequest = {
  toUserId?: string | null;
  leaveRequestId?: string | null;
  payload?: string | null;
  variant?: string | null;
  avatarMode?: string | null;
};

export async function getUsers() {
  const response = await httpClient.get<ApiResponse<UserSummary[]>>("/api/users");
  return response.data.data;
}

export async function getUser(id: string) {
  const response = await httpClient.get<ApiResponse<UserSummary>>(`/api/users/${id}`);
  return response.data.data;
}

export async function createUser(payload: SaveUserRequest) {
  const response = await httpClient.post<ApiResponse<UserSummary>>("/api/users", payload);
  return response.data.data;
}

export async function updateUser(id: string, payload: SaveUserRequest) {
  const response = await httpClient.put<ApiResponse<UserSummary>>(`/api/users/${id}`, payload);
  return response.data.data;
}

export async function deactivateUser(id: string) {
  await httpClient.delete(`/api/users/${id}`);
}

export async function getDepartments() {
  const response = await httpClient.get<ApiResponse<DepartmentSummary[]>>("/api/departments");
  return response.data.data;
}

export async function getDepartment(id: string) {
  const response = await httpClient.get<ApiResponse<DepartmentSummary>>(`/api/departments/${id}`);
  return response.data.data;
}

export async function createDepartment(payload: SaveDepartmentRequest) {
  const response = await httpClient.post<ApiResponse<DepartmentSummary>>("/api/departments", payload);
  return response.data.data;
}

export async function updateDepartment(id: string, payload: SaveDepartmentRequest) {
  const response = await httpClient.put<ApiResponse<DepartmentSummary>>(`/api/departments/${id}`, payload);
  return response.data.data;
}

export async function deleteDepartment(id: string) {
  await httpClient.delete(`/api/departments/${id}`);
}

export async function getRoles() {
  const response = await httpClient.get<ApiResponse<RoleSummary[]>>("/api/roles");
  return response.data.data;
}

export async function getRole(id: string) {
  const response = await httpClient.get<ApiResponse<RoleSummary>>(`/api/roles/${id}`);
  return response.data.data;
}

export async function createRole(payload: SaveRoleRequest) {
  const response = await httpClient.post<ApiResponse<RoleSummary>>("/api/roles", payload);
  return response.data.data;
}

export async function updateRole(id: string, payload: SaveRoleRequest) {
  const response = await httpClient.put<ApiResponse<RoleSummary>>(`/api/roles/${id}`, payload);
  return response.data.data;
}

export async function deactivateRole(id: string) {
  await httpClient.delete(`/api/roles/${id}`);
}

export async function getPermissions() {
  const response = await httpClient.get<ApiResponse<PermissionSummary[]>>("/api/permissions");
  return response.data.data;
}

export async function getRolePermissions(roleId: string) {
  const response = await httpClient.get<ApiResponse<PermissionSummary[]>>(
    `/api/roles/${roleId}/permissions`,
  );
  return response.data.data;
}

export async function updateRolePermissions(roleId: string, permissionIds: string[]) {
  const response = await httpClient.put<ApiResponse<PermissionSummary[]>>(
    `/api/roles/${roleId}/permissions`,
    { permissionIds },
  );
  return response.data.data;
}

export async function getDashboardSummary() {
  const response = await httpClient.get<ApiResponse<DashboardSummary>>("/api/dashboard/summary");
  return response.data.data;
}

export async function getAuditLogs(params: AuditLogQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<AuditLogSummary>>>(
    "/api/audit-logs",
    { params },
  );
  return response.data.data;
}

export async function getAuditLog(id: string) {
  const response = await httpClient.get<ApiResponse<AuditLogSummary>>(`/api/audit-logs/${id}`);
  return response.data.data;
}

export async function downloadAuditLogExcel(params: AuditLogQuery = {}) {
  const response = await httpClient.get("/api/audit-logs/export-excel", { params, responseType: "blob" });
  return response.data as Blob;
}

export async function downloadAuditLogPdf(params: AuditLogQuery = {}) {
  const response = await httpClient.get("/api/audit-logs/export-pdf", { params, responseType: "blob" });
  return response.data as Blob;
}

export async function getSystemSettings() {
  const response = await httpClient.get<ApiResponse<SystemSettings>>("/api/system-settings");
  return response.data.data;
}

export async function getLineSettings() {
  const response = await httpClient.get<ApiResponse<LineSettings>>("/api/admin/line/settings");
  return response.data.data;
}

export async function getLineOperationsStatus() {
  const response = await httpClient.get<ApiResponse<LineOperationsStatus>>("/api/admin/line/operations-status");
  return response.data.data;
}

export async function validateLineConnection() {
  const response = await httpClient.post<ApiResponse<LineConnectionValidation>>("/api/admin/line/validate");
  return response.data.data;
}

export async function sendLineTestMessage(payload: LineTestSendRequest) {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/admin/line/test-send", payload);
  return response.data.data;
}

export async function getLineTestHistory(params: { page?: number; pageSize?: number; search?: string } = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LineDeliveryLog>>>("/api/admin/line/test-history", { params });
  return response.data.data;
}

export async function getLineDeliveryLogs(params: { page?: number; pageSize?: number; status?: string; search?: string } = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LineDeliveryLog>>>("/api/admin/line/delivery-logs", { params });
  return response.data.data;
}

export async function simulateLineNotification(payload: LineNotificationSimulatorRequest) {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/admin/line/simulate", payload);
  return response.data.data;
}

export async function getLineFlexPreview(params: { leaveRequestId?: string | null; variant?: string | null; avatarMode?: string | null } = {}) {
  const response = await httpClient.get<ApiResponse<LineFlexPreview>>("/api/admin/line/flex-preview", { params });
  return response.data.data;
}

export async function validateLineFlexPayload(payload: LineFlexValidateRequest) {
  const response = await httpClient.post<ApiResponse<LineFlexValidateResponse>>("/api/admin/line/validate-flex", payload);
  return response.data.data;
}

export async function sendLineTestFlex(payload: LineFlexTestSendRequest) {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/admin/line/test-flex", payload);
  return response.data.data;
}
