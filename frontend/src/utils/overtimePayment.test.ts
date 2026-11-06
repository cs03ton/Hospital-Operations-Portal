import { describe, expect, it } from "vitest";
import { dailyOvertimeWork, overtimeDayLimit, paymentAmount } from "./overtimePayment";
const rules = { weekdayStart: "16:30", weekdayMaxHours: 4, holidayMaxHours: 7, breakStart: "12:00", breakEnd: "13:00" };
const e = (date = "2026-11-05", startTime = "08:22", endTime = "16:44", endDate = date) => ({ date, startTime, endTime, endDate });
describe("daily overtime payment", () => {
  it("adds same-day minutes before flooring and splits overnight work", () => {
    expect(dailyOvertimeWork([e("2026-11-05", "08:00", "08:40"), e("2026-11-05", "09:00", "09:40"), e("2026-11-06", "23:30", "01:15", "2026-11-07")], "2026-11")).toMatchObject({ error: "", days: [{ date: "2026-11-05", minutes: 80, hours: 1 }, { date: "2026-11-06", minutes: 30, hours: 0 }, { date: "2026-11-07", minutes: 75, hours: 1 }] });
  });
  it("accepts an exclusive month boundary and adjacent intervals", () => {
    expect(dailyOvertimeWork([e("2026-11-30", "22:00", "23:00"), e("2026-11-30", "23:00", "00:00", "2026-12-01")], "2026-11").days).toMatchObject([{ date: "2026-11-30", minutes: 120, hours: 2 }]);
  });
  it.each([
    ["weekday", "15:00", "19:00", 2], ["weekday", "16:30", "20:30", 4], ["weekday", "16:30", "22:00", 4], ["weekday", "18:00", "22:00", 4],
    ["weekday", "08:30", "16:30", 0], ["weekday", "16:00", "17:29", 0], ["weekday", "16:00", "17:30", 1],
    ["holiday", "08:30", "16:30", 7], ["holiday", "13:00", "20:00", 7], ["holiday", "08:00", "20:00", 7], ["holiday", "11:30", "13:30", 1],
    ["holiday", "11:30", "12:30", 0], ["holiday", "12:30", "14:00", 1], ["holiday", "12:00", "13:00", 0], ["holiday", "13:00", "13:59", 0],
  ] as const)("applies %s policy for %s–%s as %s hours", (kind, start, end, expected) => {
    const day = dailyOvertimeWork([e("2026-11-05", start, end)], "2026-11").days[0];
    expect(overtimeDayLimit(day, kind, rules).maxClaimHours).toBe(expected);
  });
  it("combines eligible fragments before flooring and uses the supplied policy", () => {
    const day = dailyOvertimeWork([e("2026-11-05", "11:30", "12:30"), e("2026-11-05", "12:45", "13:30")], "2026-11").days[0];
    expect(overtimeDayLimit(day, "holiday", rules)).toEqual({ eligibleMinutes: 60, excludedMinutes: 45, maxClaimHours: 1 });
    const evening = dailyOvertimeWork([e("2026-11-05", "16:30", "22:00")], "2026-11").days[0];
    expect(overtimeDayLimit(evening, "weekday", { ...rules, weekdayStart: "17:00", weekdayMaxHours: 2 }).maxClaimHours).toBe(2);
    const overnight = dailyOvertimeWork([e("2026-11-06", "23:30", "01:15", "2026-11-07")], "2026-11").days;
    expect(overtimeDayLimit(overnight[0], "weekday", rules).maxClaimHours).toBe(0); expect(overtimeDayLimit(overnight[1], "holiday", rules).maxClaimHours).toBe(1);
  });
  it.each([
    [[e(), e("2026-11-05", "16:00", "17:00")], "2026-11"],
    [[e("2026-11-30", "23:00", "00:01", "2026-12-01")], "2026-11"],
    [[e()], "2026-10"], [[e("2026-11-05", "08:60")], "2026-11"], [[e("2026-02-30")], "2026-02"], [[e("2026-11-05", "08:00", "07:00")], "2026-11"], [[e()], "invalid"],
  ])("rejects invalid, outside-month and overlapping work", (entries, month) => { expect(dailyOvertimeWork(entries, month).error).not.toBe(""); });
  it("calculates zero and both rates in cents", () => {
    expect(paymentAmount([{ date: "2026-11-05", dayType: "weekday", claimHours: 4 }, { date: "2026-11-07", dayType: "holiday", claimHours: 7 }], { month: "2026-11", weekdayRate: 50, holidayRate: 60, payerName: "ทดสอบ", holidayDates: [], rules: { weekdayStart: "16:30", weekdayMaxHours: 4, holidayMaxHours: 7, breakStart: "12:00", breakEnd: "13:00" } })).toBe(620);
    expect(paymentAmount([], { month: "2026-11", weekdayRate: 50, holidayRate: 60, payerName: "ทดสอบ", holidayDates: [], rules: { weekdayStart: "16:30", weekdayMaxHours: 4, holidayMaxHours: 7, breakStart: "12:00", breakEnd: "13:00" } })).toBe(0);
  });
});
