import type { MemoContentTemplate, MemoParagraph } from "../api/correspondenceApi";
import { normalizeBuddhistApiDate } from "./dateFormat";
export const thaiMonths = ["มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];
export function memoMonthPeriod(month: string, buddhistYear: string) {
  const monthIndex = thaiMonths.indexOf(month), year = Number(buddhistYear) - 543;
  if (!/^\d{4}$/.test(buddhistYear) || monthIndex < 0 || year < 1900 || year > 2100) return null;
  const prefix = `${year}-${String(monthIndex + 1).padStart(2, "0")}`;
  const lastDay = new Date(Date.UTC(year, monthIndex + 1, 0)).getUTCDate();
  return { period_start: `${prefix}-01`, period_end: `${prefix}-${lastDay}` };
}
export const memoTemplateDateKeys = ["reference_document_date", "period_start", "period_end"];
export function formatMemoTemplateDates(fields: Record<string, string>, variables: string[]) {
  const values: Record<string, string> = {}, errors: Record<string, string> = {};
  for (const key of memoTemplateDateKeys.filter(key => variables.includes(key))) {
    const raw = fields[key]; values[key] = "";
    if (!raw) continue;
    const date = normalizeBuddhistApiDate(raw);
    if (!date) { errors[key] = "กรุณาเลือกวันที่ที่ถูกต้อง"; continue; }
    values[key] = new Intl.DateTimeFormat("th-TH", { calendar: "buddhist", timeZone: "UTC", day: "numeric", month: "long", year: "numeric" }).format(new Date(`${date}T12:00:00Z`));
  }
  if (variables.includes("period_start") && variables.includes("period_end")) {
    const start = normalizeBuddhistApiDate(fields.period_start), end = normalizeBuddhistApiDate(fields.period_end);
    if (start && end && end < start) errors.period_end = "วันสิ้นสุดต้องไม่อยู่ก่อนวันเริ่มปฏิบัติงาน";
  }
  return { values, errors };
}
const digits = ["ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า"];
function integerText(value: string): string {
  value = value.replace(/^0+/, "") || "0";
  if (value.length > 6) { const tail = value.slice(-6); return integerText(value.slice(0, -6)) + "ล้าน" + (Number(tail) === 1 ? "เอ็ด" : Number(tail) ? integerText(tail) : ""); }
  if (value === "0") return "ศูนย์";
  const places = ["", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน"];
  return [...value].map((char, i) => { const n = Number(char), place = value.length - i - 1;
    if (!n) return ""; if (place === 1) return (n === 1 ? "" : n === 2 ? "ยี่" : digits[n]) + "สิบ";
    if (place === 0 && n === 1 && value.length > 1) return "เอ็ด";
    return digits[n] + places[place];
  }).join("");
}
export function moneyValues(input: string): { amount: string; amount_text: string } | null {
  if (!/^\d{1,12}(\.\d{1,2})?$/.test(input)) return null;
  const [whole, fraction = ""] = input.split("."); const cents = fraction.padEnd(2, "0");
  return { amount: Number(input).toLocaleString("th-TH", { minimumFractionDigits: Number(cents) ? 2 : 0, maximumFractionDigits: 2 }),
    amount_text: integerText(whole) + "บาท" + (Number(cents) ? integerText(cents) + "สตางค์" : "ถ้วน") };
}
export function unresolvedMemoVariables(subject: string, body: MemoParagraph[], closing: string): string[] {
  const text = [subject, ...body.map(p => p.runs.map(r => r.text).join("")), closing].join("\n");
  return [...new Set(text.match(/\{[A-Za-z_][A-Za-z0-9_]*\}/g) ?? [])];
}
export function fillMemoTemplate(template: MemoContentTemplate, values: Record<string, string>) {
  const fill = (text: string) => text.replace(/\{([A-Za-z_][A-Za-z0-9_]*)\}/g, (placeholder, key: string) => (key === "name_prefix" && key in values ? values[key].trim() : values[key]?.trim() || placeholder));
  return { subject: fill(template.subjectTemplate), closing: fill(template.closingTemplate),
    body: template.bodyTemplate.flatMap((text): MemoParagraph[] => {
      if (text.trim() === "{staff_list}") {
        const staff = (values.staff_list ?? "").split(/\r?\n/).map(line => line.trim().replace(/^\d+[.)]\s*/, "")).filter(Boolean);
        return staff.length ? staff.map(line => ({ runs: [{ text: line }], align: "left", indent: 1, ordered: true })) : [{ runs: [{ text: "{staff_list}" }], align: "left", indent: 1, ordered: false }];
      }
      if (text.trim() === "{work_periods}") {
        return fill(text).split(/\r?\n/).map(line => line.trim()).filter(Boolean)
          .map(line => ({ runs: [{ text: line }], align: "left", indent: 1, ordered: false }));
      }
      return [{ runs: [{ text: fill(text) }], align: "justify", indent: 1, ordered: false }];
    }) };
}
