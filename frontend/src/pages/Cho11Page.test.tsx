// @vitest-environment jsdom
import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderFleet } from "../test/renderFleet";
import { getCho11Options, renderCho11, type Cho11Options, type Cho11Form } from "../api/correspondenceApi";
import { Cho11Page, validateCho11 } from "./Cho11Page";
vi.mock("../api/correspondenceApi", () => ({ getCho11Options: vi.fn(), renderCho11: vi.fn() }));
vi.mock("../components/common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <label>{label}<input type="date" aria-label={label} value={value} onChange={e => onChange(e.target.value)} /></label> }));
vi.mock("../components/correspondence/MemoPdfPreview", () => ({ MemoPdfPreview: () => <div>PDF ฉ.11</div> }));
const author = { id: "author", name: "นายผู้ทำเอกสาร ทดสอบ", position: "นักวิชาการคอมพิวเตอร์", departmentId: "d" };
const head = { id: "head", name: "นางสาวหัวหน้า ทดสอบ", position: "หัวหน้ากลุ่มงาน", departmentId: "d" };
const foreign = { id: "foreign", name: "นายผู้อนุมัติ ต่างหน่วย", position: "หัวหน้าฝ่าย", departmentId: "other" };
const options: Cho11Options = { author, heads: [head], reviewers: [head, foreign], hospital: "โรงพยาบาลนาหมื่น", province: "น่าน", areaLevel: "พื้นที่เฉพาะระดับ 1" };
const valid: Cho11Form = { startMonth: "2026-10", endMonth: "2026-12", employmentDate: "2025-11-15", hospital: options.hospital, province: options.province, reviewerUserId: head.id };
describe("Cho11 form", () => {
  beforeEach(() => {
    vi.mocked(getCho11Options).mockResolvedValue(options);
    vi.mocked(renderCho11).mockResolvedValue({ pdfBase64: btoa("pdf"), docxBase64: btoa("word"), fileName: "ฉ.11-ทดสอบ", templateVersion: "cho11-v1" });
    Object.defineProperty(URL, "createObjectURL", { configurable: true, value: vi.fn().mockReturnValueOnce("blob:pdf").mockReturnValueOnce("blob:docx") });
    Object.defineProperty(URL, "revokeObjectURL", { configurable: true, value: vi.fn() });
  });
  afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.clearAllMocks(); });
  it("validates range limits, employment month, profile and reviewer", () => {
    expect(validateCho11(valid, options)).toEqual({});
    expect(validateCho11({ ...valid, endMonth: "2026-09" }, options)).toHaveProperty("endMonth");
    expect(validateCho11({ ...valid, endMonth: "2027-10" }, options)).toHaveProperty("endMonth");
    expect(validateCho11({ ...valid, employmentDate: "2026-11-01" }, options)).toHaveProperty("employmentDate");
    expect(validateCho11({ ...valid, reviewerUserId: "invalid" }, options)).toHaveProperty("reviewerUserId");
    expect(validateCho11(valid, { ...options, author: { ...author, position: null } })).toHaveProperty("author");
  });
  it("defaults to the own head and supports other approvers", async () => {
    renderFleet(<Cho11Page />); await screen.findByDisplayValue(author.name);
    expect(screen.getByRole("combobox", { name: /ผู้ตรวจสอบรับรอง/ })).toHaveTextContent(head.name);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /ผู้ตรวจสอบรับรอง/ }));
    fireEvent.click(await screen.findByRole("option", { name: new RegExp(foreign.name) }));
    expect(screen.getByRole("combobox", { name: /ผู้ตรวจสอบรับรอง/ })).toHaveTextContent(foreign.name);
    expect(screen.getByLabelText("ผู้ทำเอกสารจาก HOP")).toHaveAttribute("readonly");
  });
  it("focuses missing employment month without sending a request", async () => {
    renderFleet(<Cho11Page />); await screen.findByDisplayValue(author.name);
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    expect(renderCho11).not.toHaveBeenCalled();
    expect(screen.getByLabelText("วันเริ่มงาน")).toHaveFocus();
  });
  it("does not guess between multiple own heads and keeps partial Buddhist years editable", async () => {
    vi.mocked(getCho11Options).mockResolvedValue({ ...options, heads: [head, { ...foreign, departmentId: "d" }] });
    renderFleet(<Cho11Page />); await screen.findByDisplayValue(author.name);
    expect(screen.getByRole("combobox", { name: /ผู้ตรวจสอบรับรอง/ })).not.toHaveTextContent(head.name);
    const year = screen.getByLabelText("ปี พ.ศ.เอกสารเริ่มต้น");
    for (const value of ["2", "25", "256", "2568"]) { fireEvent.change(year, { target: { value } }); expect(year).toHaveValue(value); }
  });
  it("creates snapshot downloads and invalidates both files on edits", async () => {
    const downloads: string[] = [];
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function(this: HTMLAnchorElement) { downloads.push(this.download); });
    renderFleet(<Cho11Page />); await screen.findByDisplayValue(author.name);
    fireEvent.change(screen.getByLabelText("วันเริ่มงาน"), { target: { value: "2025-11-15" } });
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    await screen.findByText("PDF ฉ.11");
    expect(renderCho11).toHaveBeenCalledWith(expect.objectContaining({ employmentDate: "2025-11-15", reviewerUserId: "head" }), expect.any(AbortSignal));
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด PDF" })); fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด DOCX" }));
    expect(downloads).toEqual(["ฉ.11-ทดสอบ.pdf", "ฉ.11-ทดสอบ.docx"]);
    expect(screen.getByRole("link", { name: "ดูเต็มหน้า" })).toHaveAttribute("target", "_blank");
    fireEvent.change(screen.getByLabelText(/โรงพยาบาล/), { target: { value: "โรงพยาบาลทดสอบ" } });
    await waitFor(() => expect(screen.queryByText("PDF ฉ.11")).not.toBeInTheDocument());
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:pdf"); expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:docx");
  });
});

