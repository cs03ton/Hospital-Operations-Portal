// @vitest-environment jsdom
import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SystemDialogHost } from "../components/common/SystemDialogHost";
import { renderFleet } from "../test/renderFleet";
import { CorrespondenceMemoPage } from "./CorrespondenceMemoPage";
import { getMemoOptions, getOvertimePaymentOptions, renderMemo } from "../api/correspondenceApi";
vi.mock("../api/correspondenceApi", () => ({ getMemoOptions: vi.fn(), getMemoDepartmentPeople: vi.fn(), renderMemo: vi.fn(), getOvertimePaymentOptions: vi.fn(), renderOvertimeBundle: vi.fn() }));
vi.mock("../components/common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange, error }: { label: string; value: string; onChange: (value: string) => void; error?: boolean }) => <label>{label}<input type="date" aria-label={label} value={value} aria-invalid={error} onChange={e => onChange(e.target.value)} /></label> }));
vi.mock("../components/correspondence/MemoPdfPreview", () => ({ MemoPdfPreview: () => <div title="ตัวอย่างบันทึกข้อความ PDF" /> }));
const options = { proposer: { id: "author", name: "ผู้เสนอทดสอบ", position: "นักวิชาการ", departmentId: "dept" }, departments: [{ id: "dept", name: "หน่วยงานทดสอบ" }], heads: [{ id: "head", name: "หัวหน้าทดสอบ", position: "หัวหน้างาน", departmentId: "dept" }], directors: [{ id: "director", name: "ผอ.ทดสอบ", position: null, departmentId: null }], closings: ["จึงเรียนมาเพื่อโปรดทราบ"], headComments: [], directorComments: [] };
function fillNumber() { fireEvent.change(screen.getByLabelText(/เบอร์ต่อ/), { target: { value: "103" } }); fireEvent.change(screen.getByLabelText(/เลขที่หน่วยงาน/), { target: { value: "312" } }); fireEvent.change(screen.getByLabelText(/เลขทะเบียนส่ง/), { target: { value: "29" } }); }
function fillRequired() {
  fillNumber(); fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "เรื่องทดสอบ" } });
  const body = screen.getByRole("textbox", { name: "เนื้อหาย่อหน้า 1" }); body.textContent = "เนื้อหาทดสอบ"; fireEvent.input(body);
}
describe("stateless memo form", () => {
  beforeEach(() => {
    vi.mocked(getMemoOptions).mockResolvedValue(structuredClone(options));
    vi.mocked(getOvertimePaymentOptions).mockImplementation(async month => ({ month, weekdayRate: 50, holidayRate: 60, payerName: "นางสาวรสริน แสนซ้าย", holidayDates: [], rules: { weekdayStart: "16:30", weekdayMaxHours: 4, holidayMaxHours: 7, breakStart: "12:00", breakEnd: "13:00" } }));
    vi.mocked(renderMemo).mockResolvedValue({ pdfBase64: btoa("pdf"), docxBase64: btoa("docx"), fileName: "memo", templateVersion: "memo-v1" });
    Object.defineProperty(URL, "createObjectURL", { configurable: true, value: vi.fn().mockReturnValueOnce("blob:pdf").mockReturnValueOnce("blob:docx") });
    Object.defineProperty(URL, "revokeObjectURL", { configurable: true, value: vi.fn() });

  });
  afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.clearAllMocks(); });
  it("defaults the signer to the user's own head, allows any listed approver and preserves the choice across department edits", async () => {
    const foreign = { id: "foreign", name: "ผู้อนุมัติต่างหน่วยงาน", position: "หัวหน้าฝ่าย", departmentId: "other" };
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options, departments: [...options.departments, { id: "other", name: "หน่วยงานอื่น" }], heads: [...options.heads, foreign], approvers: [foreign, ...options.heads] });
    renderFleet(<CorrespondenceMemoPage />); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    const signer = screen.getByRole("combobox", { name: "หัวหน้ากลุ่มงาน" });
    expect(signer).toHaveTextContent("หัวหน้าทดสอบ");
    fireEvent.mouseDown(signer); fireEvent.click(await screen.findByRole("option", { name: /ผู้อนุมัติต่างหน่วยงาน/ }));
    fillRequired(); fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF");
    expect(renderMemo).toHaveBeenCalledWith(expect.objectContaining({ headUserId: "foreign", departmentId: "dept" }), expect.any(AbortSignal));
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /^หน่วยงาน/ })); fireEvent.click(await screen.findByRole("option", { name: "หน่วยงานอื่น" }));
    expect(signer).toHaveTextContent("ผู้อนุมัติต่างหน่วยงาน");
    expect(screen.queryByTitle("ตัวอย่างบันทึกข้อความ PDF")).not.toBeInTheDocument();
  });
  it("selects the overtime table report in the existing template dropdown and returns to the retained memo", async () => {
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "บันทึกข้อความเดิม" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" }));
    fireEvent.click(await screen.findByRole("option", { name: "รายงานผลการปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ" }));
    await screen.findByLabelText("ผู้รายงานจาก HOP");
    expect(screen.getByLabelText("ผู้รายงานจาก HOP")).toHaveValue("ผู้เสนอทดสอบ");
    expect(screen.queryByLabelText(/เลขที่หน่วยงาน/)).not.toBeInTheDocument();
    expect(screen.getByLabelText(/โครงการ\/กิจกรรมปฏิบัติงาน/)).toHaveValue("หน่วยงานทดสอบ");
    expect(getMemoOptions).toHaveBeenCalledOnce();
    fireEvent.change(screen.getByLabelText(/ลักษณะงาน รายการ 1 งาน 1/), { target: { value: "รายละเอียดที่ยังไม่ได้ดาวน์โหลด" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /เลือกเทมเพลต/ })); fireEvent.click(await screen.findByRole("option", { name: "เขียนข้อความเอง" }));
    await screen.findByRole("dialog"); fireEvent.click(screen.getByRole("button", { name: "ยกเลิก" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(screen.getByLabelText(/ลักษณะงาน รายการ 1 งาน 1/)).toHaveValue("รายละเอียดที่ยังไม่ได้ดาวน์โหลด");
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /เลือกเทมเพลต/ })); fireEvent.click(await screen.findByRole("option", { name: "เขียนข้อความเอง" }));
    await screen.findByRole("dialog"); fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" }));
    await waitFor(() => expect(screen.getByLabelText(/เรื่อง/)).toHaveValue("บันทึกข้อความเดิม"));
    expect(screen.queryByLabelText("ผู้รายงานจาก HOP")).not.toBeInTheDocument();
  });
  it("fills the supervisor from the selected signing head while allowing a different editable supervisor", async () => {
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options,
      heads: [...options.heads, { ...options.heads[0], id: "second-head", name: "หัวหน้าคนที่สอง" }],
      contentTemplates: [{ code: "SUPERVISOR", name: "ทดสอบชื่อผู้ควบคุม", subjectTemplate: "ขออนุมัติปฏิบัติงาน", bodyTemplate: ["โดยมี {supervisor_name} เป็นผู้ควบคุม"], closingTemplate: "จึงเรียนมาเพื่อโปรดทราบ", variables: ["supervisor_name"], isActive: true }] });
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ"); fillNumber();
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: "ทดสอบชื่อผู้ควบคุม" }));
    const supervisor = screen.getByLabelText("ผู้ควบคุมการปฏิบัติงาน"); expect(supervisor).toHaveValue("");
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "หัวหน้ากลุ่มงาน" })); fireEvent.click(await screen.findByRole("option", { name: /หัวหน้าคนที่สอง/ }));
    expect(supervisor).toHaveValue("หัวหน้าคนที่สอง");
    fireEvent.change(supervisor, { target: { value: "ผู้ควบคุมที่แก้ไข" } });
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF");
    expect(renderMemo).toHaveBeenCalledWith(expect.objectContaining({ headUserId: "second-head", body: [expect.objectContaining({ runs: [{ text: "โดยมี ผู้ควบคุมที่แก้ไข เป็นผู้ควบคุม" }] })] }), expect.any(AbortSignal));
  });
  it("focuses the first invalid field and never calls render until required inputs are complete", async () => {
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    const preview = screen.getByRole("button", { name: "สร้าง Preview" });
    fireEvent.click(preview); expect(screen.getByLabelText(/เบอร์ต่อ/)).toHaveFocus(); expect(screen.getByLabelText(/เบอร์ต่อ/)).toHaveAttribute("aria-invalid", "true");
    fireEvent.change(screen.getByLabelText(/เบอร์ต่อ/), { target: { value: "103" } });
    fireEvent.click(preview); expect(screen.getByLabelText(/เลขที่หน่วยงาน/)).toHaveFocus();
    fireEvent.change(screen.getByLabelText(/เลขที่หน่วยงาน/), { target: { value: "312" } });
    fireEvent.click(preview); expect(screen.getByLabelText(/เลขทะเบียนส่ง/)).toHaveFocus();
    fireEvent.change(screen.getByLabelText(/เลขทะเบียนส่ง/), { target: { value: "29" } });
    fireEvent.change(screen.getByLabelText("วันที่ออกหนังสือ"), { target: { value: "" } });
    fireEvent.click(preview); expect(screen.getByLabelText("วันที่ออกหนังสือ")).toHaveFocus();
    fireEvent.change(screen.getByLabelText("วันที่ออกหนังสือ"), { target: { value: "2026-11-05" } });
    fireEvent.click(preview); expect(screen.getByLabelText(/เรื่อง/)).toHaveFocus();
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "เรื่องครบแล้ว" } });
    fireEvent.click(preview); expect(screen.getByRole("textbox", { name: "เนื้อหาย่อหน้า 1" })).toHaveFocus(); expect(renderMemo).not.toHaveBeenCalled();
    fillRequired(); fireEvent.click(preview); await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF"); expect(renderMemo).toHaveBeenCalledOnce();
  });
  it("focuses the missing template variable, then the apply button when its value has not yet been applied", async () => {
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options, contentTemplates: [{ code: "DETAIL", name: "ทดสอบรายละเอียด", subjectTemplate: "เรื่องทดสอบ", bodyTemplate: ["รายละเอียด {work_detail}"], closingTemplate: "จึงเรียนมาเพื่อโปรดทราบ", variables: ["work_detail"], isActive: true }] });
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ"); fillNumber();
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: "ทดสอบรายละเอียด" }));
    const apply = screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }); fireEvent.click(apply);
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); expect(screen.getByLabelText("รายละเอียดงาน")).toHaveFocus(); expect(renderMemo).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText("รายละเอียดงาน"), { target: { value: "ข้อมูลครบแล้ว" } });
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); expect(apply).toHaveFocus(); expect(screen.getByText(/กรุณากดเติมข้อความจากเทมเพลตอีกครั้ง/)).toBeInTheDocument();
    fireEvent.click(apply); await screen.findByRole("dialog"); fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" })); await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF"); expect(renderMemo).toHaveBeenCalledOnce();
  });
  it("uses HOP defaults, generates a single bundle and invalidates/reclaims files on edit", async () => {
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>);
    await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    fillRequired();
    expect(screen.getByLabelText("โทรศัพท์")).toHaveValue("054-719361"); expect(screen.getByLabelText("โทรศัพท์")).toHaveAttribute("readonly");
    expect(screen.getByLabelText("เลขที่หนังสือ")).toHaveValue("นน 0033.312/29");
    fireEvent.change(screen.getByLabelText(/เลขทะเบียนส่ง/), { target: { value: "29.1" } });
    expect(screen.getByLabelText("เลขที่หนังสือ")).toHaveValue("นน 0033.312/29.1");
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "เรื่องทดสอบ" } });
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF");
    expect(renderMemo).toHaveBeenCalledWith(expect.objectContaining({ documentNumber: "นน 0033.312/29.1", departmentId: "dept", headUserId: "head", directorUserId: "director", headComment: "- เห็นสมควรอนุมัติ", directorComment: "", recipient: "ผู้อำนวยการโรงพยาบาลนาหมื่น" }), expect.any(AbortSignal));
    expect(screen.getByRole("button", { name: "ดาวน์โหลด DOCX" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "ดาวน์โหลด PDF" })).toBeEnabled();
    const downloads: string[] = [];
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function (this: HTMLAnchorElement) { downloads.push(this.download); });
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด PDF" }));
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด DOCX" }));
    expect(downloads).toEqual(["memo.pdf", "memo.docx"]);
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "แก้ไขแล้ว" } });
    expect(screen.queryByTitle("ตัวอย่างบันทึกข้อความ PDF")).not.toBeInTheDocument();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:pdf"); expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:docx");
  });
  it("shows the current director read-only and blocks ambiguous director accounts", async () => {
    const view = renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    expect(screen.getByLabelText("ผู้อำนวยการ / รักษาการ")).toHaveValue("ผอ.ทดสอบ");
    expect(screen.getByLabelText("ผู้อำนวยการ / รักษาการ")).toHaveAttribute("readonly");
    expect(screen.queryByRole("combobox", { name: "ผู้อำนวยการ / รักษาการ" })).not.toBeInTheDocument(); view.unmount();
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options, directors: [...options.directors, { ...options.directors[0], id: "second", name: "อีกบัญชี" }] });
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByText(/พบผู้ดำรงบทบาท Director มากกว่าหนึ่งบัญชี/);
    fillRequired(); fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    expect(screen.getByLabelText("ผู้อำนวยการ / รักษาการ")).toHaveFocus(); expect(renderMemo).not.toHaveBeenCalled();
  });
  it("blocks incomplete profiles and does not invent signers", async () => {
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options, proposer: { ...options.proposer, position: null }, heads: [], directors: [] });
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>);
    await screen.findByText(/ชื่อหรือตำแหน่งใน Profile ไม่ครบ/);
    fillRequired(); fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    expect(screen.getByLabelText("ตำแหน่งผู้เสนอ")).toHaveFocus();
    expect(screen.getByText(/ไม่พบผู้อนุมัติที่เปิดใช้งาน/)).toBeInTheDocument(); expect(renderMemo).not.toHaveBeenCalled();
  });
  it("clears the form and releases both files on unmount", async () => {
    const view = renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "ข้อมูลชั่วคราว" } });
    fireEvent.click(screen.getByRole("button", { name: "ล้างฟอร์ม" }));
    await screen.findByRole("dialog"); fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" }));
    await waitFor(() => expect(screen.getByLabelText(/เรื่อง/)).toHaveValue(""));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    fillRequired();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF"); view.unmount();
    expect(URL.revokeObjectURL).toHaveBeenCalledTimes(2);
  });
  it("ignores a response from an invalidated preview", async () => {
    let resolve!: (value: Awaited<ReturnType<typeof renderMemo>>) => void;
    vi.mocked(renderMemo).mockImplementation(() => new Promise(r => { resolve = r; }));
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    fillRequired();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "ข้อมูลใหม่" } });
    resolve({ pdfBase64: btoa("old"), docxBase64: btoa("old"), fileName: "old", templateVersion: "memo-v1" });
    await waitFor(() => expect(screen.getByRole("button", { name: "สร้าง Preview" })).toBeEnabled());
    expect(URL.createObjectURL).not.toHaveBeenCalled();
  });
  it("fills a template from HOP, confirms before overwriting and keeps it editable", async () => {
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options, contentTemplates: [{ code: "OT", name: "ค่าตอบแทนนอกเวลา", subjectTemplate: "ค่าตอบแทน {month} {years}", bodyTemplate: ["ด้วย{department}", "จำนวน {amount} บาท ({amount_text})"], closingTemplate: "จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ", variables: ["department", "month", "years", "amount", "amount_text"], isActive: true }] });
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ");
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: "ค่าตอบแทนนอกเวลา" }));
    fireEvent.change(screen.getByLabelText("จำนวนเงิน (บาท)"), { target: { value: "5000" } }); fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(screen.getByRole("textbox", { name: "เนื้อหาย่อหน้า 1" })).toHaveTextContent("ด้วยหน่วยงานทดสอบ");
    expect(screen.getByRole("textbox", { name: "เนื้อหาย่อหน้า 2" })).toHaveTextContent("จำนวน 5,000 บาท (ห้าพันบาทถ้วน)");
    fireEvent.change(screen.getByLabelText(/เรื่อง/), { target: { value: "แก้ไขเอง" } });
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })); await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("button", { name: "ยกเลิก" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument()); expect(screen.getByLabelText(/เรื่อง/)).toHaveValue("แก้ไขเอง");
  });
  it("keeps missing variables visible and blocks export until the user fills them", async () => {
    vi.mocked(getMemoOptions).mockResolvedValue({ ...options, contentTemplates: [{ code: "WORK", name: "รับรองงาน", subjectTemplate: "รับรองงาน", bodyTemplate: ["{full_name} วันที่ {work_dates}"], closingTemplate: "จึงเรียนมาเพื่อโปรดทราบ", variables: ["full_name", "work_dates"], isActive: true }] });
    renderFleet(<><CorrespondenceMemoPage /><SystemDialogHost /></>); await screen.findByDisplayValue("ผู้เสนอทดสอบ"); fillNumber();
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: "รับรองงาน" }));
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(screen.getByRole("textbox", { name: "เนื้อหาย่อหน้า 1" })).toHaveTextContent("ผู้เสนอทดสอบ วันที่ {work_dates}");
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); expect(renderMemo).not.toHaveBeenCalled();
    const body = screen.getByRole("textbox", { name: "เนื้อหาย่อหน้า 1" }); body.textContent = "ผู้เสนอทดสอบ วันที่ 5 พฤศจิกายน 2569"; fireEvent.input(body);
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างบันทึกข้อความ PDF"); expect(renderMemo).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "ล้างฟอร์ม" })); await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" }));
    await waitFor(() => expect(screen.queryByLabelText("วันที่ปฏิบัติงาน")).not.toBeInTheDocument());
  });
});
