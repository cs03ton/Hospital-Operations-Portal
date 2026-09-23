import dayjs from "dayjs";
import "dayjs/locale/th";
import utc from "dayjs/plugin/utc";
import timezone from "dayjs/plugin/timezone";

dayjs.extend(utc);
dayjs.extend(timezone);

export function bangkokDayjs(value?: dayjs.ConfigType) {
  if (value === undefined) return dayjs().tz("Asia/Bangkok");
  if (typeof value === "string" && !/(Z|[+-]\d{2}:?\d{2})$/.test(value)) {
    if (!dayjs(value).isValid()) return dayjs(NaN);
    return dayjs.tz(value, "Asia/Bangkok");
  }
  return dayjs(value).tz("Asia/Bangkok");
}

dayjs.locale("th");



const apiDateFormat = "YYYY-MM-DD";

const bangkok = "Asia/Bangkok";

export function gregorianToBuddhistYear(year: number) {
  return year >= 2443 ? year : year + 543;
}

export function buddhistToGregorianYear(year: number) {
  return year >= 2443 && year <= 2643 ? year - 543 : year;
}

export function formatThaiYear(year: number) {
  return String(gregorianToBuddhistYear(year));
}

/** Fiscal years remain Gregorian internally; this function is presentation only. */
export function formatThaiFiscalYear(year: number) {
  return formatThaiYear(year);
}

export function parseThaiCalendarYear(value: string | number) {
  const year = Number(value);
  if (!Number.isInteger(year)) return null;
  const normalized = buddhistToGregorianYear(year);
  return normalized >= 1900 && normalized <= 2100 ? normalized : null;
}

export function formatThaiMonthYear(year: number, month: number, style: "long" | "short" = "long") {
  if (!Number.isInteger(year) || month < 1 || month > 12) return "-";
  const date = new Date(Date.UTC(year, month - 1, 1, 12));
  return new Intl.DateTimeFormat("th-TH", { calendar: "buddhist", timeZone: bangkok, month: style, year: "numeric" }).format(date);
}

export function formatThaiDate(value?: string | Date | null) {
  if (!value) return "-";
  // A calendar date is not an instant: never convert it through the host timezone.
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
    const normalized = normalizeBuddhistApiDate(value);
    if (!normalized) return "-";
    const [year, month, day] = normalized.split("-");
    return `${day}/${month}/${gregorianToBuddhistYear(Number(year))}`;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return new Intl.DateTimeFormat("th-TH", { calendar: "buddhist", timeZone: bangkok,
    day: "2-digit", month: "2-digit", year: "numeric" }).format(date);
}

export function formatThaiDateTime(value?: string | Date | null, withSeconds = false) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return new Intl.DateTimeFormat("th-TH", { calendar: "buddhist", timeZone: bangkok,
    day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
    ...(withSeconds ? { second: "2-digit" as const } : {}), hourCycle: "h23" }).format(date);
}
export const formatThaiBuddhistDateTime = formatThaiDateTime;

/** Local form values always describe the hospital clock, not the browser clock. */
export function bangkokInputToUtc(value: string) {
  const match = /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})(?::(\d{2}))?$/.exec(value);
  const date = match && normalizeBuddhistApiDate(match[1]);
  if (!match || !date || Number(match[2].slice(0, 2)) > 23 || Number(match[2].slice(3)) > 59 || Number(match[3] ?? 0) > 59)
    throw new Error("วันเวลาที่กรอกไม่ถูกต้อง");
  return new Date(`${date}T${match[2]}:${match[3] ?? "00"}+07:00`).toISOString();
}

export function utcToBangkokInput(value?: string | Date | null) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const parts = new Intl.DateTimeFormat("en-GB", { calendar: "gregory", timeZone: bangkok,
    year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(date);
  const part = (key: string) => parts.find(p => p.type === key)?.value;
  return `${part("year")}-${part("month")}-${part("day")}T${part("hour")}:${part("minute")}`;
}
export function formatDateForApi(value?: string | Date | null) {
  if (!value) {
    return "";
  }

  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)) return normalizeBuddhistApiDate(value);
  const parsed = bangkokDayjs(value);
  return parsed.isValid() ? normalizeBuddhistApiDate(parsed.format(apiDateFormat)) : "";
}

export function normalizeBuddhistApiDate(value?: string | null) {
  if (!value) return "";
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return "";
  const year = Number(match[1]);
  const gregorianYear = buddhistToGregorianYear(year);
  if (gregorianYear < 1900 || gregorianYear > 2100) return "";
  const normalized = `${gregorianYear}-${match[2]}-${match[3]}`;
  const parsed = dayjs(normalized);
  return parsed.isValid() && parsed.format(apiDateFormat) === normalized ? normalized : "";
}

export function isValidApiDate(value?: string | null) {
  return Boolean(value && normalizeBuddhistApiDate(value) === value);
}

export function isStartDateBeforeOrSameEndDate(start?: string | null, end?: string | null) {
  if (!start || !end) {
    return true;
  }

  return !dayjs(start).isAfter(dayjs(end), "day");
}
