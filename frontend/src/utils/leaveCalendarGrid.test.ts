import { describe, expect, it } from "vitest";
import { getLeaveCalendarGrid, leaveCalendarWeekdays } from "./leaveCalendarGrid";

describe("leave calendar grid", () => {
  it("always places Sunday first and Saturday last", () => {
    expect(leaveCalendarWeekdays[0]).toBe("อา.");
    expect(leaveCalendarWeekdays[6]).toBe("ส.");
  });

  it.each([
    [2026, 2, 0],
    [2026, 6, 1],
    [2026, 8, 6],
  ])("aligns a month beginning at weekday %i-%i", (year, month, expectedLeading) => {
    const grid = getLeaveCalendarGrid(year, month);
    expect(grid.leadingEmptyDays).toBe(expectedLeading);
    expect((grid.leadingEmptyDays + grid.daysInMonth + grid.trailingEmptyDays) % 7).toBe(0);
  });
});
