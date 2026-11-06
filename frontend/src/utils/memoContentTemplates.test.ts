import { describe, expect, it } from "vitest";
import { fillMemoTemplate, formatMemoTemplateDates, memoMonthPeriod, moneyValues, unresolvedMemoVariables } from "./memoContentTemplates";
describe("memo content templates", () => {
  it("indents every work period as its own paragraph while preserving numbering and unresolved input", () => {
    const template = { code: "WORK", name: "รับรองงาน", subjectTemplate: "รับรองงาน", bodyTemplate: ["ก่อนรายการ", "{work_periods}", "หลังรายการ"], closingTemplate: "ลงท้าย", variables: ["work_periods"], isActive: true };
    const first = "1. วันที่ 5 ตุลาคม 2569 เวลา 08:30–16:30 น.", second = "2. วันที่ 6 พฤศจิกายน 2569 เวลา 03:00–06:00 น.";
    const result = fillMemoTemplate(template, { work_periods: `${first}\r\n${second}` });
    expect(result.body.map(p => p.runs[0].text)).toEqual(["ก่อนรายการ", first, second, "หลังรายการ"]);
    expect(result.body.slice(1, 3)).toEqual([first, second].map(text => ({ runs: [{ text }], align: "left", indent: 1, ordered: false })));
    const missing = fillMemoTemplate(template, {});
    expect(missing.body[1]).toEqual({ runs: [{ text: "{work_periods}" }], align: "left", indent: 1, ordered: false });
    expect(unresolvedMemoVariables(missing.subject, missing.body, missing.closing)).toEqual(["{work_periods}"]);
  });
  it("allows an empty database prefix without leaving an unresolved variable or guessing", () => {
    const template = { code: "NAME", name: "ชื่อ", subjectTemplate: "{name_prefix}{full_name}", bodyTemplate: ["{name_prefix}{full_name}"], closingTemplate: "ลงท้าย", variables: ["name_prefix", "full_name"], isActive: true };
    const result = fillMemoTemplate(template, { name_prefix: "", full_name: "นายก ทดสอบ" });
    expect(result.subject).toBe("นายก ทดสอบ");
    expect(unresolvedMemoVariables(result.subject, result.body, result.closing)).toEqual([]);
  });
  it("creates numbered staff paragraphs without duplicate numbers and retains a missing roster placeholder", () => {
    const template = { code: "OT", name: "OT", subjectTemplate: "เรื่อง", bodyTemplate: ["ก่อนรายชื่อ", "{staff_list}", "หลังรายชื่อ"], closingTemplate: "ลงท้าย", variables: ["staff_list"], isActive: true };
    const result = fillMemoTemplate(template, { staff_list: "1. นายหนึ่ง ตำแหน่ง นักวิชาการ\r\n\n2) นายสอง ตำแหน่ง เจ้าหน้าที่" });
    expect(result.body).toHaveLength(4);
    expect(result.body[1]).toEqual({ runs: [{ text: "นายหนึ่ง ตำแหน่ง นักวิชาการ" }], align: "left", indent: 1, ordered: true });
    expect(result.body[2].ordered).toBe(true); expect(result.body[3].ordered).toBe(false);
    const missing = fillMemoTemplate(template, { staff_list: "1.\n2)" });
    expect(unresolvedMemoVariables(missing.subject, missing.body, missing.closing)).toEqual(["{staff_list}"]);
  });
  it.each([
    ["มกราคม", "2569", "2026-01-01", "2026-01-31"],
    ["เมษายน", "2569", "2026-04-01", "2026-04-30"],
    ["พฤศจิกายน", "2569", "2026-11-01", "2026-11-30"],
    ["ธันวาคม", "2569", "2026-12-01", "2026-12-31"],
    ["กุมภาพันธ์", "2567", "2024-02-01", "2024-02-29"],
    ["กุมภาพันธ์", "2568", "2025-02-01", "2025-02-28"],
    ["กุมภาพันธ์", "2543", "2000-02-01", "2000-02-29"],
    ["กุมภาพันธ์", "2643", "2100-02-01", "2100-02-28"],
  ])("defaults %s %s to the complete calendar month", (month, year, start, end) => {
    expect(memoMonthPeriod(month, year)).toEqual({ period_start: start, period_end: end });
  });
  it.each([["กุมภาพันธ์", ""], ["มกราคม", "256"], ["มกราคม", "2026"], ["มกราคม", "3000"], ["unknown", "2569"]])("does not calculate a range for an invalid month or Buddhist year", (month, year) => {
    expect(memoMonthPeriod(month, year)).toBeNull();
  });
  it.each([["5000", "ห้าพันบาทถ้วน"], ["21", "ยี่สิบเอ็ดบาทถ้วน"], ["101.25", "หนึ่งร้อยเอ็ดบาทยี่สิบห้าสตางค์"], ["0", "ศูนย์บาทถ้วน"], ["1.01", "หนึ่งบาทหนึ่งสตางค์"], ["1000001", "หนึ่งล้านเอ็ดบาทถ้วน"], ["100000000001", "หนึ่งแสนล้านเอ็ดบาทถ้วน"]])("converts %s without dropping satang", (value, expected) => { expect(moneyValues(value)?.amount_text).toBe(expected); });
  it.each(["-1", "1.234", "1e3", "NaN", "1000000000000", "5,000", ""])("rejects invalid amount %s", value => { expect(moneyValues(value)).toBeNull(); });
  it("fills plain text paragraphs, preserves missing tokens and detects tokens split by formatting", () => {
    const content = fillMemoTemplate({ code: "TEST", name: "test", subjectTemplate: "เรื่อง {month} {years}", bodyTemplate: ["ด้วย{department}", "{full_name} {work_dates}"], closingTemplate: "จึงเรียนมาเพื่อโปรดทราบ", variables: [], isActive: true }, { month: "ตุลาคม", years: "2569", department: "กลุ่มงานสุขภาพดิจิทัล", full_name: "ผู้เสนอ" });
    expect(content.subject).toBe("เรื่อง ตุลาคม 2569"); expect(content.body).toHaveLength(2); expect(content.body[0].runs[0].text).toBe("ด้วยกลุ่มงานสุขภาพดิจิทัล");
    expect(unresolvedMemoVariables(content.subject, content.body, content.closing)).toEqual(["{work_dates}"]);
    expect(unresolvedMemoVariables("", [{ runs: [{ text: "{work_", bold: true }, { text: "dates}" }], align: "left", indent: 0, ordered: false }], "")).toEqual(["{work_dates}"]);
  });
  it("formats reference and work-range dates in Buddhist years without accepting impossible or reversed dates", () => {
    const variables = ["reference_document_date", "period_start", "period_end"];
    const formatted = formatMemoTemplateDates({ reference_document_date: "2026-10-01", period_start: "2569-10-01", period_end: "2026-10-31" }, variables);
    expect(formatted.errors).toEqual({});
    expect(formatted.values).toEqual({ reference_document_date: "1 ตุลาคม 2569", period_start: "1 ตุลาคม 2569", period_end: "31 ตุลาคม 2569" });
    expect(formatMemoTemplateDates({ reference_document_date: "2026-02-30" }, variables).errors.reference_document_date).toBeTruthy();
    expect(formatMemoTemplateDates({ period_start: "2026-11-01", period_end: "2026-10-31" }, variables).errors.period_end).toBeTruthy();
    expect(formatMemoTemplateDates({}, variables).values.reference_document_date).toBe("");
    expect(formatMemoTemplateDates({ period_start: "2026-11-01", period_end: "2026-10-31" }, ["month"])).toEqual({ values: {}, errors: {} });
  });
});
