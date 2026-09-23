import { bangkokDayjs } from "./dateFormat";

export const leaveCalendarWeekdays = ["อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส."] as const;

export function getLeaveCalendarGrid(year: number, month: number) {
  const firstDay = bangkokDayjs(`${year}-${String(month).padStart(2, "0")}-01`);
  const daysInMonth = firstDay.daysInMonth();
  const leadingEmptyDays = firstDay.day();
  const trailingEmptyDays = (7 - ((leadingEmptyDays + daysInMonth) % 7)) % 7;
  return { firstDay, daysInMonth, leadingEmptyDays, trailingEmptyDays };
}
