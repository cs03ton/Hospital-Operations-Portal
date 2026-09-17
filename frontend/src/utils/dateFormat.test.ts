import { describe, expect, it } from "vitest";
import { normalizeBuddhistApiDate } from "./dateFormat";

describe("normalizeBuddhistApiDate", () => {
  it("normalizes Buddhist and Gregorian years to the same API date", () => {
    expect(normalizeBuddhistApiDate("2569-09-18")).toBe("2026-09-18");
    expect(normalizeBuddhistApiDate("2026-09-18")).toBe("2026-09-18");
  });

  it("rejects invalid converted dates", () => {
    expect(normalizeBuddhistApiDate("2568-02-29")).toBe("");
    expect(normalizeBuddhistApiDate("not-a-date")).toBe("");
  });
});
