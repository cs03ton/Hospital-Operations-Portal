import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type UserSummary = {
  id: string;
  employeeCode?: string | null;
  fullname: string;
  username: string;
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
  lineEnabled: boolean;
  lineChannelAccessTokenConfigured: boolean;
  lineEndpoint: string;
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

export async function deactivateDepartment(id: string) {
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
