import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";
export type Repair = { id: string; number: number; requesterId: string; departmentId?: string; categoryId: string; teamCode: string; title: string; description: string; location: string; contact: string; status: string; priority?: string; currentRound: number; createdAt: string; updatedAt: string; concurrencyToken: string };
export type RepairInput = Pick<Repair, "categoryId" | "title" | "description" | "location" | "contact">;
export type RepairCategory = { id: string; name: string; teamCode: string; isActive: boolean; concurrencyToken: string };
export type RepairEvent = { id: string; round: number; actorId: string; action: string; fromStatus: string; toStatus: string; note: string; solverId?: string; priority?: string; createdAt: string };
export type RepairDetail = { departmentName?: string; categoryName?: string; request: Repair; events: RepairEvent[]; people: { id: string; fullName: string }[]; contributors: { eventId: string; userId: string }[]; rounds: { id: string; number: number; startedAt: string; closedAt?: string; acceptedById?: string; acceptanceNote?: string }[]; waiting: { id: string; round: number; startedAt: string; endedAt?: string }[]; images: { id: string; eventId: string; createdAt: string }[]; actions: string[]; canUpload: boolean };
export type RepairSummary = { generatedAtUtc: string; counts: { status: string; count: number }[]; teamPending: number };
export const repairWorkPermissions = ["RepairManagement.WorkIT", "RepairManagement.WorkGeneral", "RepairManagement.ViewAll"];
export const repairViewPermissions = ["RepairManagement.ViewOwn", ...repairWorkPermissions];
const unwrap = <T>(x: { data: ApiResponse<T> }) => x.data.data;
export async function repairOptions() { return unwrap(await httpClient.get<ApiResponse<{ categories: RepairCategory[]; teams: { code: string; name: string }[] }>>("/api/repairs/options")); }
export async function repairList(params: { scope: string; page: number; status: string; search: string; pageSize?: number }) { return unwrap(await httpClient.get<ApiResponse<{ items: Repair[]; total: number; pageSize: number }>>("/api/repairs", { params })); }
export async function repairSummary() { return unwrap(await httpClient.get<ApiResponse<RepairSummary>>("/api/repairs/summary")); }
export async function repairDetail(id: string) { return unwrap(await httpClient.get<ApiResponse<RepairDetail>>(`/api/repairs/${id}`)); }
export async function repairCreate(data: RepairInput) { return unwrap(await httpClient.post<ApiResponse<Repair>>("/api/repairs", data)); }
export async function repairChange(id: string, action: string, data: { concurrencyToken: string; note: string; priority?: string; solverId?: string; contributorIds?: string[]; request?: RepairInput }) { return unwrap(await httpClient.post<ApiResponse<Repair>>(`/api/repairs/${id}/actions/${action}`, data)); }
export async function repairSolvers(id: string) { return unwrap(await httpClient.get<ApiResponse<{ id: string; fullName: string }[]>>(`/api/repairs/${id}/solvers`)); }
export async function repairUpload(id: string, token: string, files: File[]) {
  const data = new FormData(); data.append("concurrencyToken", token); files.forEach(f => data.append("files", f));
  return unwrap(await httpClient.post<ApiResponse<{ concurrencyToken: string }>>(`/api/repairs/${id}/images`, data));
}
export async function repairImage(id: string) { return (await httpClient.get<Blob>(`/api/repairs/images/${id}`, { responseType: "blob" })).data; }
export async function repairSettings() { return unwrap(await httpClient.get<ApiResponse<{ categories: RepairCategory[]; deliveries: { id: string; requestId: string; teamCode: string; status: string; attempts: number; errorCode?: string }[] }>>("/api/repairs/settings")); }
export async function saveRepairCategory(data: Partial<RepairCategory>) { return httpClient.post("/api/repairs/settings/categories", data); }
