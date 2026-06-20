import dayjs from "dayjs";
import "dayjs/locale/th";

dayjs.locale("th");

const uiDateFormat = "DD/MM/YYYY";
const uiDateTimeFormat = "DD/MM/YYYY HH:mm";
const apiDateFormat = "YYYY-MM-DD";

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

export function isValidApiDate(value?: string | null) {
  return Boolean(value && dayjs(value).format(apiDateFormat) === value);
}

export function isStartDateBeforeOrSameEndDate(start?: string | null, end?: string | null) {
  if (!start || !end) {
    return true;
  }

  return !dayjs(start).isAfter(dayjs(end), "day");
}
