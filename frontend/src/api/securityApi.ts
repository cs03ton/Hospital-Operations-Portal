import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type SessionSummary = {
  id: string;
  userId: string;
  username?: string | null;
  fullname?: string | null;
  createdAt: string;
  expiresAt: string;
  revokedAt?: string | null;
  revokedReason?: string | null;
  createdByIp?: string | null;
  userAgent?: string | null;
  lastUsedAt?: string | null;
  isActive: boolean;
};

export async function getSessions() {
  const response = await httpClient.get<ApiResponse<SessionSummary[]>>("/api/sessions");
  return response.data.data;
}

export async function revokeSession(id: string) {
  await httpClient.post(`/api/sessions/${id}/revoke`);
}

export function getAuditLogExportUrl() {
  const baseUrl = import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE_URL ?? "https://localhost:5000";
  return `${baseUrl}/api/audit-logs/export`;
}

export async function runAuditRetention() {
  const response = await httpClient.post<ApiResponse<{ deletedCount: number }>>("/api/audit-logs/retention/run");
  return response.data.data;
}
