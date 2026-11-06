import { describe, expect, it } from "vitest";
import type { MemoOptions, OvertimeReportForm } from "../api/correspondenceApi";
import { defaultOvertimeSupervisor, overtimeApprovers, overtimeWholeHours, validateOvertimeReport } from "./overtimeReport";
const options: MemoOptions = { proposer: { id: "author", name: "นายผู้รายงาน", position: "นักวิชาการ", departmentId: "dept" }, departments: [{ id: "dept", name: "กลุ่มงานทดสอบ" }], heads: [{ id: "head", name: "หัวหน้า", position: "หัวหน้ากลุ่มงาน", departmentId: "dept" }], directors: [], closings: [], headComments: [], directorComments: [] };
const form = (): OvertimeReportForm => ({ departmentId: "dept", projectName: "งานทดสอบ", supervisorUserId: "head", entries: [{ date: "2026-10-18", endDate: "2026-10-18", startTime: "08:29", endTime: "16:34", hours: 8, tasks: [{ description: "กวาดส่งข้อมูล", quantity: "จำนวน 1 งาน" }, { description: "เคลมสิทธิ", quantity: "จำนวน 159 ราย" }], remarks: "" }] });
describe("overtime report validation", () => {
  it("accepts valid shifts and keeps each task paired with its quantity", () => { expect(validateOvertimeReport(form(), options)).toEqual([]); });
  it("allows overnight work with an explicit end date", () => { const data = form(); Object.assign(data.entries[0], { startTime: "23:00", endTime: "07:00", endDate: "2026-10-19", hours: 8 }); expect(validateOvertimeReport(data, options)).toEqual([]); });
  it.each([
    ["2026-11-05", "03:00", "2026-11-05", "07:00", 4],
    ["2026-11-05", "03:00", "2026-11-05", "07:59", 4],
    ["2026-11-05", "08:29", "2026-11-05", "16:34", 8],
    ["2026-11-05", "08:59", "2026-11-05", "09:00", 0],
    ["2026-12-31", "23:45", "2027-01-01", "00:44", 0],
    ["2026-12-31", "23:45", "2027-01-01", "00:45", 1],
    ["2026-12-31", "23:30", "2027-01-01", "07:29", 7],
    ["2026-11-05", "08:60", "2026-11-05", "09:00", null],
    ["2026-11-05", "08:00", "2026-11-05", "07:59", null],
    ["2026-11-05", "08:00", "2026-11-05", "08:00", null],
    ["2026-02-30", "08:00", "2026-02-30", "09:00", null],
    ["2026-11-05", "", "2026-11-05", "09:00", null],
  ])("calculates whole hours for %s %s to %s %s as %s", (date, startTime, endDate, endTime, expected) => {
    expect(overtimeWholeHours({ date, endDate, startTime, endTime })).toBe(expected);
  });
  it("reports exact fields for missing work, quantity and invalid time", () => {
    const data = form(); data.entries[0].tasks[1].quantity = ""; data.entries[0].tasks[0].description = ""; data.entries[0].endTime = "07:00";
    const fields = validateOvertimeReport(data, options).map(issue => issue.field);
    expect(fields).toContain("entries.0.tasks.1.quantity"); expect(fields).toContain("entries.0.tasks.0.description"); expect(fields).toContain("entries.0.endTime");
  });
  it("rejects an unknown supervisor, duplicate shifts, impossible dates and excessive task counts", () => {
    const data = form(); data.supervisorUserId = "outside"; data.entries.push(structuredClone(data.entries[0]));
    expect(validateOvertimeReport(data, options).map(issue => issue.field)).toEqual(expect.arrayContaining(["supervisorUserId", "entries.1.date"]));
    data.entries[0].date = "2026-02-30"; data.entries[0].tasks = Array.from({ length: 21 }, () => ({ description: "งาน", quantity: "1 งาน" }));
    expect(validateOvertimeReport(data, options).map(issue => issue.field)).toEqual(expect.arrayContaining(["entries.0.date", "entries.0.tasks"]));
  });
  it("allows every listed approver regardless of department and defaults only to the user's own head", () => {
    const foreign = { id: "foreign", name: "หัวหน้าต่างหน่วยงาน", position: "หัวหน้าฝ่าย", departmentId: "other" };
    const director = { ...foreign, id: "director", name: "ผู้อำนวยการ" };
    const all = { ...options, heads: [...options.heads, foreign], directors: [director], approvers: [foreign, director, ...options.heads] };
    expect(defaultOvertimeSupervisor(all, foreign.id)).toBe("head");
    for (const supervisorUserId of [foreign.id, director.id]) expect(validateOvertimeReport({ ...form(), supervisorUserId }, all)).toEqual([]);
    expect(defaultOvertimeSupervisor({ ...all, proposer: { ...all.proposer, departmentId: "missing" } })).toBe("");
    const twoHeads = { ...all, heads: [...all.heads, { ...options.heads[0], id: "head2" }], approvers: [...all.approvers, { ...options.heads[0], id: "head2" }] };
    expect(defaultOvertimeSupervisor(twoHeads)).toBe("");
    expect(defaultOvertimeSupervisor(twoHeads, "head2")).toBe("head2");
    expect(overtimeApprovers({ ...all, approvers: [] })).toEqual([]);
    expect(defaultOvertimeSupervisor({ ...all, approvers: [] })).toBe("");
  });
});
