import { describe, expect, it } from "vitest";
import { getLeaveTypeAccentColor, getLeaveTypeColor } from "./leaveLabels";

describe("leave calendar colors", () => {
  it("uses the same color for Thai leave names and their codes", () => {
    expect(getLeaveTypeColor("ลาพักผ่อน")).toBe(getLeaveTypeColor("VACATION_LEAVE"));
    expect(getLeaveTypeColor("ลาป่วย")).toBe(getLeaveTypeColor("SICK_LEAVE"));
    expect(getLeaveTypeColor("ลากิจส่วนตัว")).toBe(getLeaveTypeColor("PERSONAL_LEAVE"));
    expect(getLeaveTypeColor("ลาคลอดบุตร")).toBe(getLeaveTypeColor("MATERNITY_LEAVE"));
  });

  it("distinguishes common leave types", () => {
    expect(new Set(["ลาพักผ่อน", "ลาป่วย", "ลากิจส่วนตัว"].map(getLeaveTypeColor)).size).toBe(3);
    expect(new Set(["ลาพักผ่อน", "ลาป่วย", "ลากิจส่วนตัว"].map(getLeaveTypeAccentColor)).size).toBe(3);
  });
});
