import { describe, expect, it } from "vitest";
import { emptyWorkPeriod, formatMemoWorkPeriods, type MemoWorkPeriod } from "./memoWorkPeriods";
const row = (date = "2026-11-05", start = "08:30", endDate = date, end = "16:30"): MemoWorkPeriod => ({ startDate: date, startTime: start, endDate, endTime: end });
describe("memo work periods", () => {
  it("keeps all-empty input unresolved and ignores untouched blank rows", () => {
    expect(formatMemoWorkPeriods([emptyWorkPeriod()]).values).toEqual({});
    expect(formatMemoWorkPeriods([row(), emptyWorkPeriod()]).values.work_periods).toBe("1. วันที่ 5 พฤศจิกายน 2569 เวลา 08:30–16:30 น.");
  });
  it("sorts same-day shifts and keeps legacy dates/times paired", () => {
    const result = formatMemoWorkPeriods([row("2026-11-05", "17:00", "2026-11-05", "20:00"), row()]);
    expect(result.values.work_periods.split("\n")[0]).toBe("1. วันที่ 5 พฤศจิกายน 2569 เวลา 08:30–16:30 น.");
    expect(result.values.work_periods.split("\n")[1]).toBe("2. วันที่ 5 พฤศจิกายน 2569 เวลา 17:00–20:00 น.");
    expect(result.values.work_dates.split("\n")[1]).toMatch(/^2\./); expect(result.values.work_times.split("\n")[1]).toBe("2. 17:00–20:00 น.");
  });
  it("formats overnight periods across months and years with explicit dates", () => {
    expect(formatMemoWorkPeriods([row("2026-12-31", "16:00", "2027-01-01", "08:00")]).values.work_periods).toBe("1. วันที่ 31 ธันวาคม 2569 เวลา 16:00 น. ถึงวันที่ 1 มกราคม 2570 เวลา 08:00 น.");
  });
  it.each([
    { ...row(), endTime: "" }, row("2026-02-30"), row("2026-11-05", "24:00"), row("2026-11-05", "16:30", "2026-11-05", "16:30"), row("2026-11-05", "16:30", "2026-11-05", "08:00"), row("2026-11-05", "08:00", "2026-11-04", "17:00"),
  ])("rejects incomplete, invalid or reversed input without partial text", invalid => {
    const result = formatMemoWorkPeriods([row("2026-10-01"), invalid]); expect(result.errors[1]).toBeTruthy(); expect(result.values).toEqual({});
  });
  it("rejects exact duplicates and more than 50 rows", () => {
    expect(formatMemoWorkPeriods([row(), row()]).errors[1]).toContain("ซ้ำ"); expect(formatMemoWorkPeriods(Array.from({ length: 51 }, emptyWorkPeriod)).errors[50]).toBeTruthy();
  });
});
