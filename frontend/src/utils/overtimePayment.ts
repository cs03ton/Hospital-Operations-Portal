import type { OvertimeClaimDay, OvertimePaymentOptions, OvertimePaymentRules, OvertimeReportEntry } from "../api/correspondenceApi";
import { isValidApiDate } from "./dateFormat";

export type OvertimeWorkDay = { date: string; minutes: number; hours: number; segments: { start: number; end: number }[] };
export function dailyOvertimeWork(entries: Pick<OvertimeReportEntry, "date" | "endDate" | "startTime" | "endTime">[], month: string): { days: OvertimeWorkDay[]; error: string } {
  const fail = (error: string) => ({ days: [], error });
  if (!/^\d{4}-\d{2}$/.test(month) || !isValidApiDate(month + "-01")) return fail("กรุณาเลือกเดือนและปีของเอกสาร");
  const startMonth = Date.parse(month + "-01T00:00:00Z"); const next = new Date(startMonth); next.setUTCMonth(next.getUTCMonth() + 1);
  const validTime = (value: string) => /^([01]\d|2[0-3]):[0-5]\d$/.test(value);
  if (!entries.length || entries.some(e => !isValidApiDate(e.date) || !isValidApiDate(e.endDate) || !validTime(e.startTime) || !validTime(e.endTime))) return fail("กรอกวันและเวลาให้ครบเพื่อสรุปชั่วโมงเบิก");
  const intervals = entries.map(e => ({ start: Date.parse(`${e.date}T${e.startTime}:00Z`), end: Date.parse(`${e.endDate}T${e.endTime}:00Z`) })).sort((a, b) => a.start - b.start);
  const minutes = new Map<string, number>(), segments = new Map<string, OvertimeWorkDay["segments"]>(); let previousEnd = -Infinity;
  for (const entry of intervals) {
    if (entry.end <= entry.start) return fail("วัน–เวลาสิ้นสุดต้องอยู่หลังวัน–เวลาเริ่ม");
    if (entry.start < startMonth || entry.end > next.getTime()) return fail("วันปฏิบัติงานต้องอยู่ภายในเดือนเอกสารที่เลือก กรุณาแยกเอกสารรายเดือน");
    if (entry.start < previousEnd) return fail("ช่วงเวลาปฏิบัติงานซ้อนกัน กรุณาตรวจสอบก่อนยืนยันชั่วโมงเบิก");
    previousEnd = entry.end;
    for (let start = entry.start; start < entry.end;) {
      const midnight = new Date(start); midnight.setUTCHours(24, 0, 0, 0);
      const end = Math.min(entry.end, midnight.getTime()); const date = new Date(start).toISOString().slice(0, 10);
      minutes.set(date, (minutes.get(date) ?? 0) + (end - start) / 60000);
      const dayStart = Date.parse(`${date}T00:00:00Z`);
      segments.set(date, [...segments.get(date) ?? [], { start: (start - dayStart) / 60000, end: (end - dayStart) / 60000 }]); start = end;
    }
  }
  return { days: [...minutes].map(([date, minutes]) => ({ date, minutes, hours: Math.floor(minutes / 60), segments: segments.get(date)! })), error: "" };
}
export function overtimeDayLimit(day: OvertimeWorkDay, dayType: OvertimeClaimDay["dayType"], rules: OvertimePaymentRules) {
  const minute = (time: string) => { const [hours, minutes] = time.split(":").map(Number); return hours * 60 + minutes; };
  const eligibleMinutes = day.segments.reduce((sum, segment) => {
    if (dayType === "weekday") return sum + Math.max(0, segment.end - Math.max(segment.start, minute(rules.weekdayStart)));
    const lunch = Math.max(0, Math.min(segment.end, minute(rules.breakEnd)) - Math.max(segment.start, minute(rules.breakStart)));
    return sum + segment.end - segment.start - lunch;
  }, 0);
  return { eligibleMinutes, excludedMinutes: day.minutes - eligibleMinutes, maxClaimHours: Math.min(Math.floor(eligibleMinutes / 60), dayType === "holiday" ? rules.holidayMaxHours : rules.weekdayMaxHours) };
}
export function paymentAmount(days: OvertimeClaimDay[], options: OvertimePaymentOptions) {
  return days.reduce((total, day) => total + Math.round((day.dayType === "holiday" ? options.holidayRate : options.weekdayRate) * 100) * day.claimHours, 0) / 100;
}
