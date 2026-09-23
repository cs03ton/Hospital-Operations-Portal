import { describe, expect, it } from "vitest";
import { normalizeBuddhistApiDate, formatThaiDate, formatThaiDateTime, bangkokInputToUtc, utcToBangkokInput } from "./dateFormat";

describe("normalizeBuddhistApiDate", () => {
  it("normalizes Buddhist and Gregorian years to the same API date", () => {
    expect(normalizeBuddhistApiDate("2569-09-18")).toBe("2026-09-18");
    expect(normalizeBuddhistApiDate("2026-09-18")).toBe("2026-09-18");
  });

  it("rejects invalid converted dates", () => {
    expect(normalizeBuddhistApiDate("2568-02-29")).toBe("");
    expect(normalizeBuddhistApiDate("not-a-date")).toBe("");
  });
  it("accepts Buddhist leap days and supported boundary years", () => {
    expect(normalizeBuddhistApiDate("2567-02-29")).toBe("2024-02-29");
    expect(normalizeBuddhistApiDate("2443-01-01")).toBe("1900-01-01");
    expect(normalizeBuddhistApiDate("2643-12-31")).toBe("2100-12-31");
  });
  it("keeps date-only values and Bangkok instants distinct", () => {
    expect(formatThaiDate("2026-09-24")).toBe("24/09/2569");
    expect(formatThaiDate("2569-09-24")).toBe("24/09/2569");
    expect(formatThaiDateTime("2026-09-23T18:00:00Z")).toBe("24/09/2569 01:00");
    expect(bangkokInputToUtc("2569-09-24T08:00")).toBe("2026-09-24T01:00:00.000Z");
    expect(utcToBangkokInput("2026-09-24T01:00:00Z")).toBe("2026-09-24T08:00");
    expect(() => bangkokInputToUtc("2568-02-29T08:00")).toThrow();
  });
});
