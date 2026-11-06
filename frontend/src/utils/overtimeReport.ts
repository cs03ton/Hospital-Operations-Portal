import type { MemoOptions, OvertimeReportForm } from "../api/correspondenceApi";
import { isValidApiDate } from "./dateFormat";
import { memoApprovers } from "./memoPeople";
export { defaultMemoHead as defaultOvertimeSupervisor, memoApprovers as overtimeApprovers } from "./memoPeople";

export type ReportIssue = { field: string; message: string };
export function overtimeWholeHours(entry: Pick<OvertimeReportForm["entries"][number], "date" | "endDate" | "startTime" | "endTime">): number | null {
  const validTime = (value: string) => /^([01]\d|2[0-3]):[0-5]\d$/.test(value);
  if (!isValidApiDate(entry.date) || !isValidApiDate(entry.endDate) || !validTime(entry.startTime) || !validTime(entry.endTime)) return null;
  const milliseconds = Date.parse(`${entry.endDate}T${entry.endTime}:00Z`) - Date.parse(`${entry.date}T${entry.startTime}:00Z`);
  return milliseconds > 0 ? Math.floor(milliseconds / 3600000) : null;
}
export function validateOvertimeReport(form: OvertimeReportForm, options: MemoOptions): ReportIssue[] {
  const issues: ReportIssue[] = [];
  const check = (field: string, valid: boolean, message: string) => { if (!valid) issues.push({ field, message }); };
  check("departmentId", options.departments.some(d => d.id === form.departmentId), "กรุณาเลือกหน่วยงาน");
  check("projectName", !!form.projectName.trim() && form.projectName.length <= 500, "กรุณากรอกโครงการ/กิจกรรมปฏิบัติงาน ไม่เกิน 500 ตัวอักษร");
  check("reporter", !!options.proposer.name.trim() && !!options.proposer.position?.trim(), "ชื่อหรือตำแหน่งผู้รายงานไม่ครบ กรุณาแก้ Profile");
  check("supervisorUserId", memoApprovers(options).some(h => h.id === form.supervisorUserId && !!h.position?.trim()), "กรุณาเลือกผู้ควบคุมที่มีตำแหน่งครบถ้วนจากรายชื่อผู้อนุมัติ");
  check("entries", form.entries.length > 0 && form.entries.length <= 50, "เพิ่มวันปฏิบัติงานได้ 1–50 รายการ");
  const seen = new Set<string>(); let tasks = 0, total = form.projectName.length;
  form.entries.forEach((entry, index) => {
    const path = `entries.${index}`, label = `รายการ ${index + 1}`;
    check(`${path}.date`, isValidApiDate(entry.date), `${label}: กรุณาเลือกวันที่ปฏิบัติงาน`);
    check(`${path}.endDate`, isValidApiDate(entry.endDate), `${label}: กรุณาเลือกวันสิ้นสุด`);
    const validTime = (value: string) => /^([01]\d|2[0-3]):[0-5]\d$/.test(value);
    check(`${path}.startTime`, validTime(entry.startTime), `${label}: กรุณาระบุเวลาเริ่มแบบ 24 ชั่วโมง`);
    check(`${path}.endTime`, validTime(entry.endTime), `${label}: กรุณาระบุเวลาสิ้นสุดแบบ 24 ชั่วโมง`);
    const duration = (Date.parse(`${entry.endDate}T${entry.endTime}:00Z`) - Date.parse(`${entry.date}T${entry.startTime}:00Z`)) / 3600000;
    if (Number.isFinite(duration)) check(`${path}.endTime`, duration > 0, `${label}: วัน–เวลาสิ้นสุดต้องอยู่หลังวัน–เวลาเริ่ม`);
    const key = [entry.date, entry.endDate, entry.startTime, entry.endTime].join("|");
    check(`${path}.date`, !seen.has(key), `${label}: วัน–เวลาซ้ำ กรุณารวมงานไว้ในรายการเดียวกัน`); seen.add(key);
    check(`${path}.tasks`, entry.tasks.length > 0 && entry.tasks.length <= 20, `${label}: ระบุลักษณะงานได้ 1–20 งาน`);
    entry.tasks.forEach((task, taskIndex) => {
      check(`${path}.tasks.${taskIndex}.description`, !!task.description.trim() && task.description.length <= 2000, `${label} งาน ${taskIndex + 1}: กรุณากรอกลักษณะงาน ไม่เกิน 2,000 ตัวอักษร`);
      check(`${path}.tasks.${taskIndex}.quantity`, !!task.quantity.trim() && task.quantity.length <= 200, `${label} งาน ${taskIndex + 1}: กรุณากรอกปริมาณและหน่วยนับ`);
      tasks++; total += task.description.length + task.quantity.length;
    });
    check(`${path}.remarks`, entry.remarks.length <= 1000, `${label}: หมายเหตุต้องไม่เกิน 1,000 ตัวอักษร`); total += entry.remarks.length;
  });
  check("entries", tasks <= 500 && total <= 60000, "รายงานต้องไม่เกิน 500 งาน และ 60,000 ตัวอักษร");
  return issues;
}
