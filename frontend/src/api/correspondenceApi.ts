import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";
export type MemoRun = { text: string; bold?: boolean; italic?: boolean; underline?: boolean };
export type MemoParagraph = { runs: MemoRun[]; align: "left" | "center" | "right" | "justify"; indent: number; ordered: boolean };
export type MemoPerson = { namePrefix?: string | null; fullname?: string | null; id: string; name: string; position: string | null; departmentId: string | null };
export type MemoContentTemplate = { fileNameTitle?: string | null; code: string; name: string; subjectTemplate: string; bodyTemplate: string[]; closingTemplate: string; variables: string[]; isActive: boolean };
export type MemoOptions = { proposer: MemoPerson; departments: { id: string; name: string }[]; heads: MemoPerson[]; directors: MemoPerson[]; approvers?: MemoPerson[]; closings: string[]; headComments: string[]; directorComments: string[]; contentTemplates?: MemoContentTemplate[] };
export type MemoForm = { contentTemplateCode?: string | null; workMonth?: string | null; departmentId: string; extension: string; documentNumber: string; documentDate: string; subject: string; recipient: string; body: MemoParagraph[]; closing: string; includeHead: boolean; headUserId: string | null; headComment: string; includeDirector: boolean; directorUserId: string | null; directorComment: string };
export type MemoFiles = { docxBase64: string; pdfBase64: string; fileName: string; templateVersion: string };
export type Cho11Form = { startMonth: string; endMonth: string; employmentDate: string; hospital: string; province: string; reviewerUserId: string };
export type Cho11Options = { author: MemoPerson; reviewers: MemoPerson[]; heads: MemoPerson[]; hospital: string; province: string; areaLevel: string };
export async function getCho11Options(signal?: AbortSignal) { return (await httpClient.get<ApiResponse<Cho11Options>>("/api/correspondence/cho11/options", { signal })).data.data; }
export async function renderCho11(form: Cho11Form, signal?: AbortSignal) { return (await httpClient.post<ApiResponse<MemoFiles>>("/api/correspondence/cho11/render", form, { signal })).data.data; }
export async function getMemoOptions(signal?: AbortSignal) { return (await httpClient.get<ApiResponse<MemoOptions>>("/api/correspondence/options", { signal })).data.data; }
export async function getMemoDepartmentPeople(departmentId: string, signal?: AbortSignal) { return (await httpClient.get<ApiResponse<MemoPerson[]>>(`/api/correspondence/departments/${encodeURIComponent(departmentId)}/people`, { signal, params: { includeOtherDepartments: true } })).data.data; }
export async function renderMemo(form: MemoForm, signal?: AbortSignal) { return (await httpClient.post<ApiResponse<MemoFiles>>("/api/correspondence/render", form, { signal })).data.data; }
export type OvertimeReportTask = { description: string; quantity: string };
export type OvertimeReportEntry = { date: string; endDate: string; startTime: string; endTime: string; hours: number | null; tasks: OvertimeReportTask[]; remarks: string };
export type OvertimeReportForm = { departmentId: string; projectName: string; supervisorUserId: string; entries: OvertimeReportEntry[] };
export async function renderOvertimeReport(form: OvertimeReportForm, signal?: AbortSignal) { return (await httpClient.post<ApiResponse<MemoFiles>>("/api/correspondence/overtime-report/render", form, { signal })).data.data; }
export type OvertimePaymentRules = { weekdayStart: string; weekdayMaxHours: number; holidayMaxHours: number; breakStart: string; breakEnd: string };
export type OvertimePaymentOptions = { month: string; weekdayRate: number; holidayRate: number; payerName: string; holidayDates: string[]; rules: OvertimePaymentRules };
export type OvertimeClaimDay = { date: string; dayType: "weekday" | "holiday"; claimHours: number };
export type OvertimePaymentInput = { month: string; days: OvertimeClaimDay[] };
export type OvertimeReportBundle = { workReport: MemoFiles; paymentEvidence: MemoFiles };
export async function getOvertimePaymentOptions(month: string, signal?: AbortSignal) { return (await httpClient.get<ApiResponse<OvertimePaymentOptions>>("/api/correspondence/overtime-report/payment-options", { params: { month }, signal })).data.data; }
export async function renderOvertimeBundle(report: OvertimeReportForm, payment: OvertimePaymentInput, signal?: AbortSignal) { return (await httpClient.post<ApiResponse<OvertimeReportBundle>>("/api/correspondence/overtime-report/render-bundle", { report, payment }, { signal })).data.data; }
