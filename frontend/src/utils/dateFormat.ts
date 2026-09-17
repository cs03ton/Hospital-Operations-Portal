import dayjs from "dayjs";
import "dayjs/locale/th";

dayjs.locale("th");

const uiDateFormat = "DD/MM/YYYY";
const uiDateTimeFormat = "DD/MM/YYYY HH:mm";
const apiDateFormat = "YYYY-MM-DD";

export function formatThaiBuddhistDateTime(value?: string | Date | null) {
  if (!value || !dayjs(value).isValid()) return "-";
  return new Intl.DateTimeFormat("th-TH", {
    calendar: "buddhist", day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit", hourCycle: "h23",
  }).format(new Date(value));
}

export function formatThaiDate(value?: string | Date | null) {
  if (!value) {
    return "-";
  }

  const parsed = dayjs(value);
  return parsed.isValid() ? parsed.format(uiDateFormat) : "-";
}

export function formatThaiDateTime(value?: string | Date | null, withSeconds = false) {
  if (!value) {
    return "-";
  }

  const parsed = dayjs(value);
  return parsed.isValid() ? parsed.format(withSeconds ? `${uiDateTimeFormat}:ss` : uiDateTimeFormat) : "-";
}

export function formatDateForApi(value?: string | Date | null) {
  if (!value) {
    return "";
  }

  const parsed = dayjs(value);
  return parsed.isValid() ? parsed.format(apiDateFormat) : "";
}

export function normalizeBuddhistApiDate(value?: string | null) {
  if (!value) return "";
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return "";
  const year = Number(match[1]);
  const gregorianYear = year >= 2500 && year <= 2599 ? year - 543 : year;
  if (gregorianYear < 1900 || gregorianYear > 2100) return "";
  const normalized = `${gregorianYear}-${match[2]}-${match[3]}`;
  const parsed = dayjs(normalized);
  return parsed.isValid() && parsed.format(apiDateFormat) === normalized ? normalized : "";
}

export function isValidApiDate(value?: string | null) {
  return Boolean(value && dayjs(value).format(apiDateFormat) === value);
}

export function isStartDateBeforeOrSameEndDate(start?: string | null, end?: string | null) {
  if (!start || !end) {
    return true;
  }

  return !dayjs(start).isAfter(dayjs(end), "day");
}
