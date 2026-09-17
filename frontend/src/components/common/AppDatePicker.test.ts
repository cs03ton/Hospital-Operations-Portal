import { describe, expect, it } from "vitest";
import { AdapterDayjsBuddhist } from "./AppDatePicker";

describe("Buddhist date picker parser", () => {
  const adapter = new AdapterDayjsBuddhist({ locale: "th" });

  it("parses Buddhist and Gregorian typed years as the same date", () => {
    expect(adapter.parse("18/09/2569", "DD/MM/BBBB")?.format("YYYY-MM-DD")).toBe("2026-09-18");
    expect(adapter.parse("18/09/2026", "DD/MM/BBBB")?.format("YYYY-MM-DD")).toBe("2026-09-18");
  });
});
