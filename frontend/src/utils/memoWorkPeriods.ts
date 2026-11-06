export type MemoWorkPeriod = { startDate: string; startTime: string; endDate: string; endTime: string };
export const emptyWorkPeriod = (): MemoWorkPeriod => ({ startDate: "", startTime: "", endDate: "", endTime: "" });
const validDate = (value: string) => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || value < "1900-01-01" || value > "2100-12-31") return false;
  const date = new Date(`${value}T00:00:00Z`); return !Number.isNaN(date.getTime()) && date.toISOString().slice(0, 10) === value;
};
const validTime = (value: string) => /^([01]\d|2[0-3]):[0-5]\d$/.test(value);
const dateText = (value: string) => new Intl.DateTimeFormat("th-TH", { timeZone: "UTC", day: "numeric", month: "long", year: "numeric", calendar: "buddhist" }).format(new Date(`${value}T12:00:00Z`));
export function formatMemoWorkPeriods(periods: MemoWorkPeriod[]) {
  const errors: Record<number, string> = {}; const seen = new Set<string>();
  const populated = periods.map((p, index) => ({ p, index })).filter(({ p }) => Object.values(p).some(Boolean));
  for (const { p, index } of populated) {
    if (!p.startDate || !p.startTime || !p.endDate || !p.endTime) { errors[index] = "กรุณาเลือกวันและเวลาเริ่ม–สิ้นสุดให้ครบ"; continue; }
    if (!validDate(p.startDate) || !validDate(p.endDate) || !validTime(p.startTime) || !validTime(p.endTime)) { errors[index] = "วันหรือเวลาไม่ถูกต้อง"; continue; }
    if (`${p.endDate}T${p.endTime}` <= `${p.startDate}T${p.startTime}`) { errors[index] = "วัน–เวลาสิ้นสุดต้องอยู่หลังวัน–เวลาเริ่ม หากข้ามคืนให้เลือกวันสิ้นสุดจริง"; continue; }
    const key = [p.startDate, p.startTime, p.endDate, p.endTime].join("|");
    if (seen.has(key)) errors[index] = "วัน–เวลาซ้ำกับรายการก่อนหน้า";
    seen.add(key);
  }
  const values: Record<string, string> = {};
  if (periods.length > 50) errors[50] = "เพิ่มได้ไม่เกิน 50 รายการ";
  if (Object.keys(errors).length || !populated.length) return { errors, values };
  const sorted = populated.map(({ p }) => p).sort((a, b) => `${a.startDate}T${a.startTime}`.localeCompare(`${b.startDate}T${b.startTime}`));
  values.work_periods = sorted.map((p, i) => p.startDate === p.endDate
    ? `${i + 1}. วันที่ ${dateText(p.startDate)} เวลา ${p.startTime}–${p.endTime} น.`
    : `${i + 1}. วันที่ ${dateText(p.startDate)} เวลา ${p.startTime} น. ถึงวันที่ ${dateText(p.endDate)} เวลา ${p.endTime} น.`).join("\n");
  values.work_dates = sorted.map((p, i) => `${i + 1}. ${dateText(p.startDate)}${p.startDate === p.endDate ? "" : ` ถึง ${dateText(p.endDate)}`}`).join("\n");
  values.work_times = sorted.map((p, i) => `${i + 1}. ${p.startTime}–${p.endTime} น.`).join("\n");
  return { errors, values };
}
