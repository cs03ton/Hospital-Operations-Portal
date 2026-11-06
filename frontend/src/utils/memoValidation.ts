import type { MemoForm, MemoOptions } from "../api/correspondenceApi";
import { isValidApiDate } from "./dateFormat";
import { unresolvedMemoVariables } from "./memoContentTemplates";
import { memoApprovers } from "./memoPeople";
import { isMemoOutgoingNumber } from "./memoOutgoingNumber";

export type MemoValidationIssue = { field: string; message: string; variable?: string };
export function validateMemo(form: MemoForm, options: MemoOptions, unit: string, outgoing: string): MemoValidationIssue[] {
  const issues: MemoValidationIssue[] = [];
  const check = (field: string, valid: boolean, message: string) => { if (!valid) issues.push({ field, message }); };
  check("departmentId", options.departments.some(d => d.id === form.departmentId), "กรุณาเลือกหน่วยงานที่เปิดใช้งาน");
  check("extension", /^\d{1,12}$/.test(form.extension), "กรุณากรอกเบอร์ต่อ");
  check("unitNumber", /^\d{1,10}$/.test(unit), "กรุณากรอกเลขที่หน่วยงาน");
  check("outgoingNumber", isMemoOutgoingNumber(outgoing), "กรุณากรอกเลขทะเบียนส่ง เช่น 29 หรือ 29.1");
  check("documentDate", isValidApiDate(form.documentDate), "กรุณาเลือกวันที่ออกหนังสือที่ถูกต้อง");
  check("subject", !!form.subject.trim() && form.subject.length <= 300, "กรุณากรอกเรื่อง ไม่เกิน 300 ตัวอักษร");
  check("recipient", !!form.recipient.trim() && form.recipient.length <= 300, "กรุณากรอกผู้รับในช่องเรียน ไม่เกิน 300 ตัวอักษร");
  check("proposer", !!options.proposer.name.trim() && !!options.proposer.position?.trim(), "ชื่อหรือตำแหน่งผู้เสนอไม่ครบ กรุณาแก้ไข Profile ก่อนสร้างไฟล์");
  const body = form.body.map(p => p.runs.map(r => r.text).join("")).join("");
  check("body", !!body.trim() && body.length <= 30000, "กรุณากรอกเนื้อหา ไม่เกิน 30,000 ตัวอักษร");
  check("closing", !!form.closing.trim() && form.closing.length <= 500, "กรุณากรอกข้อความลงท้าย ไม่เกิน 500 ตัวอักษร");
  if (form.includeHead) check("headUserId", memoApprovers(options).some(h => h.id === form.headUserId && !!h.position?.trim()), "กรุณาเลือกผู้ลงนามจากรายชื่อผู้อนุมัติที่มีข้อมูลตำแหน่งครบถ้วน");
  if (form.includeDirector) check("director", options.directors.length === 1, "กรุณาให้ผู้ดูแลตรวจสอบผู้ดำรงบทบาท Director ก่อนสร้างไฟล์");
  for (const token of unresolvedMemoVariables(form.subject, form.body, form.closing)) issues.push({ field: "template", variable: token.slice(1, -1), message: `กรุณาเติมตัวแปร ${token} ให้ครบก่อนสร้างไฟล์` });
  return issues;
}

export function focusMemoElement(element?: HTMLElement | null) {
  if (!element) return false;
  element.scrollIntoView?.({ behavior: "smooth", block: "center" });
  element.focus({ preventScroll: true });
  return true;
}
