import { describe, expect, it } from "vitest";
import { executiveViewOptions, parseExecutiveView } from "./executiveDashboardView";

describe("Executive Dashboard view selection", () => {
  it("defaults missing and unsupported URL values to overview", () => {
    expect(parseExecutiveView(null)).toBe("overview");
    expect(parseExecutiveView("unknown")).toBe("overview");
  });

  it.each([
    ["overview", "ภาพรวม"],
    ["leave", "ระบบลา"],
    ["fleet", "ระบบขอรถ"],
    ["repair", "ระบบแจ้งซ่อม"],
  ] as const)("accepts the %s view", (value, label) => {
    expect(parseExecutiveView(value)).toBe(value);
    expect(executiveViewOptions).toContainEqual({ value, label });
  });
});
