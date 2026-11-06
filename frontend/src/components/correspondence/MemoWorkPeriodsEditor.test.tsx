// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoContentTemplatePicker } from "./MemoContentTemplatePicker";
import type { MemoOptions } from "../../api/correspondenceApi";
vi.mock("../common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) => <input aria-label={label} value={value} onChange={e => onChange(e.target.value)} /> }));
vi.mock("../common/AppTimePicker", () => ({ AppTimePicker: ({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) => <input aria-label={label} value={value} onChange={e => onChange(e.target.value)} /> }));
const options: MemoOptions = { proposer: { id: "p", name: "ผู้เสนอ", position: "ตำแหน่งเดิม", departmentId: "a" }, departments: [{ id: "a", name: "กลุ่มงานเดิม" }, { id: "b", name: "กลุ่มงานใหม่" }], heads: [], directors: [], closings: [], headComments: [], directorComments: [], contentTemplates: [{ code: "WORK", name: "รับรองงาน", subjectTemplate: "{full_name}", bodyTemplate: ["{position} {department}\n{work_periods}"], closingTemplate: "ลงท้าย", variables: ["full_name", "position", "department", "work_periods"], isActive: true }] };
afterEach(cleanup);
describe("structured work period input", () => {
  it("uses upper form data without duplicate fields, follows start date until end date is edited, and supports add/remove", () => {
    const apply = vi.fn(); const draft = vi.fn(); const view = render(<MemoContentTemplatePicker options={options} departmentId="a" onApply={apply} onDraftChange={draft} />);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(screen.getByRole("option", { name: "รับรองงาน" }));
    expect(screen.queryByLabelText("ชื่อผู้เสนอ")).not.toBeInTheDocument(); expect(screen.queryByLabelText("ตำแหน่ง")).not.toBeInTheDocument(); expect(screen.queryByLabelText("กลุ่มงาน")).not.toBeInTheDocument();
    const set = (label: string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } });
    set("วันเริ่ม รายการ 1", "2026-11-05"); expect(screen.getByLabelText("วันสิ้นสุด รายการ 1")).toHaveValue("2026-11-05");
    expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeDisabled();
    set("วันสิ้นสุด รายการ 1", "2026-11-07"); set("วันเริ่ม รายการ 1", "2026-11-06"); expect(screen.getByLabelText("วันสิ้นสุด รายการ 1")).toHaveValue("2026-11-07");
    set("เวลาเริ่ม รายการ 1", "16:00"); set("เวลาสิ้นสุด รายการ 1", "08:00");
    view.rerender(<MemoContentTemplatePicker options={options} departmentId="b" onApply={apply} onDraftChange={draft} />);
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(apply.mock.calls[0][0].body[0].runs[0].text).toContain("ตำแหน่งเดิม กลุ่มงานใหม่"); expect(apply.mock.calls[0][0].body[0].runs[0].text).toContain("ถึงวันที่ 7 พฤศจิกายน 2569");
    fireEvent.click(screen.getByRole("button", { name: "เพิ่มวัน–เวลา" })); expect(screen.getByLabelText("วันเริ่ม รายการ 2")).toHaveValue("");
    fireEvent.click(screen.getByRole("button", { name: "ลบวันเวลารายการ 1" })); expect(screen.queryByLabelText("วันเริ่ม รายการ 2")).not.toBeInTheDocument();
    expect(screen.getByLabelText("วันเริ่ม รายการ 1")).toHaveValue(""); expect(draft).toHaveBeenCalled();
  });
});
