// @vitest-environment jsdom
import { act, cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderFleet } from "../test/renderFleet";
import { getMemoOptions, getOvertimePaymentOptions, renderOvertimeBundle, type MemoFiles, type OvertimeReportBundle } from "../api/correspondenceApi";
import { OvertimeWorkReportPage } from "./OvertimeWorkReportPage";
import { SystemDialogHost } from "../components/common/SystemDialogHost";
vi.mock("../api/correspondenceApi", () => ({ getMemoOptions: vi.fn(), getOvertimePaymentOptions: vi.fn(), renderOvertimeBundle: vi.fn() }));
vi.mock("../components/common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <label>{label}<input type="date" aria-label={label} value={value} onChange={e => onChange(e.target.value)} /></label> }));
vi.mock("../components/common/AppTimePicker", () => ({ AppTimePicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <label>{label}<input type="time" aria-label={label} value={value} onChange={e => onChange(e.target.value)} /></label> }));
vi.mock("../components/correspondence/MemoPdfPreview", () => ({ MemoPdfPreview: ({ documentLabel }: { documentLabel: string }) => <div title={`ตัวอย่าง${documentLabel} PDF`} /> }));
const options = { proposer: { id: "author", name: "นายผู้รายงาน ทดสอบ", position: "นักวิชาการ", departmentId: "dept" }, departments: [{ id: "dept", name: "กลุ่มงานทดสอบ" }], heads: [{ id: "head", name: "นายหัวหน้า ทดสอบ", position: "หัวหน้ากลุ่มงาน", departmentId: "dept" }], directors: [], closings: [], headComments: [], directorComments: [] };
const files: MemoFiles = { pdfBase64: btoa("pdf"), docxBase64: btoa("docx"), fileName: "report", templateVersion: "overtime-report-v1" };
const change = (label: string, value: string) => fireEvent.change(screen.getByLabelText(new RegExp(`^${label}`)), { target: { value } });
function fill() { change("เวลาเริ่ม รายการ 1", "08:29"); change("เวลาสิ้นสุด รายการ 1", "16:34"); change("ลักษณะงาน รายการ 1 งาน 1", "กวาดส่งข้อมูลผู้ป่วยนอก"); change("ปริมาณ/หน่วยนับ รายการ 1 งาน 1", "จำนวน 159 ราย"); }
async function confirmHours() { const button = await screen.findByRole("button", { name: "ยืนยันชั่วโมงเบิก" }); await waitFor(() => expect(button).toBeEnabled()); fireEvent.click(button); }
describe("overtime work report form", () => {
  beforeEach(() => {
    vi.mocked(getMemoOptions).mockResolvedValue(structuredClone(options)); vi.mocked(renderOvertimeBundle).mockResolvedValue({ workReport: files, paymentEvidence: { ...files, fileName: "payment" } });
    vi.mocked(getOvertimePaymentOptions).mockImplementation(async month => ({ month, weekdayRate: 50, holidayRate: 60, payerName: "นางสาวรสริน แสนซ้าย", holidayDates: [], rules: { weekdayStart: "16:30", weekdayMaxHours: 4, holidayMaxHours: 7, breakStart: "12:00", breakEnd: "13:00" } }));
    Object.defineProperty(URL, "createObjectURL", { configurable: true, value: vi.fn().mockReturnValueOnce("blob:pdf").mockReturnValueOnce("blob:docx").mockReturnValueOnce("blob:payment-pdf").mockReturnValueOnce("blob:payment-docx") });
    Object.defineProperty(URL, "revokeObjectURL", { configurable: true, value: vi.fn() });
  });
  afterEach(() => { cleanup(); vi.clearAllMocks(); });
  it("defaults to the policy limit and recomputes it when the day type changes", async () => {
    renderFleet(<OvertimeWorkReportPage />); await screen.findByDisplayValue(options.proposer.name); fill();
    change("วันที่ปฏิบัติงาน รายการ 1", "2026-11-07"); change("เวลาเริ่ม รายการ 1", "08:30"); change("เวลาสิ้นสุด รายการ 1", "16:30");
    await waitFor(() => expect(screen.getByRole("combobox", { name: /^ประเภทวัน / })).toBeInTheDocument());
    const claim = screen.getByLabelText(/^ชั่วโมงเบิกที่ยืนยัน /); expect(claim).toHaveValue(0);
    expect(screen.getByLabelText("ชั่วโมงทำงานจริง รายการ 1")).toHaveValue(8);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /^ประเภทวัน / })); fireEvent.click(await screen.findByRole("option", { name: "วันหยุด" }));
    expect(claim).toHaveValue(7); expect(claim).toHaveAttribute("max", "7"); expect(screen.getByText("เบิกได้สูงสุด 7 ชม.")).toBeInTheDocument();
    fireEvent.change(claim, { target: { value: "8" } }); expect(screen.getByRole("button", { name: "ยืนยันชั่วโมงเบิก" })).toBeDisabled();
    fireEvent.change(claim, { target: { value: "5" } }); await confirmHours();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF");
    expect(renderOvertimeBundle).toHaveBeenCalledWith(expect.any(Object), expect.objectContaining({ days: [expect.objectContaining({ claimHours: 5, dayType: "holiday" })] }), expect.any(AbortSignal));
    change("เวลาเริ่ม รายการ 1", "13:00"); expect(screen.queryByText("ยืนยันชั่วโมงเบิกแล้ว")).not.toBeInTheDocument();
  });
  it("requires a valid claim confirmation, previews both reports and reclaims all four files after an edit", async () => {
    renderFleet(<OvertimeWorkReportPage />); await screen.findByDisplayValue(options.proposer.name); fill();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    expect(renderOvertimeBundle).not.toHaveBeenCalled();
    await waitFor(() => expect(screen.getByRole("button", { name: "ยืนยันชั่วโมงเบิก" })).toBeEnabled());
    expect(screen.getByLabelText("ผู้จ่ายเงิน")).toHaveAttribute("readonly");
    const claim = screen.getByLabelText(/^ชั่วโมงเบิกที่ยืนยัน /);
    fireEvent.change(claim, { target: { value: "9" } }); expect(screen.getByRole("button", { name: "ยืนยันชั่วโมงเบิก" })).toBeDisabled();
    fireEvent.change(claim, { target: { value: "1.5" } }); expect(screen.getByRole("button", { name: "ยืนยันชั่วโมงเบิก" })).toBeDisabled();
    fireEvent.change(claim, { target: { value: "7" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /^ประเภทวัน / })); fireEvent.click(await screen.findByRole("option", { name: "วันหยุด" }));
    await confirmHours(); fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    await screen.findByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF");
    expect(renderOvertimeBundle).toHaveBeenCalledWith(expect.any(Object), expect.objectContaining({ days: [expect.objectContaining({ dayType: "holiday", claimHours: 7 })] }), expect.any(AbortSignal));
    const fullPage = screen.getByRole("link", { name: "ดูเต็มหน้า เปิด PDF ในแท็บใหม่" });
    const downloads: string[] = [];
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function (this: HTMLAnchorElement) { downloads.push(this.download); });
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด PDF" }));
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด DOCX" }));
    expect(fullPage).toHaveAttribute("href", "blob:pdf"); expect(fullPage).toHaveAttribute("target", "_blank"); expect(fullPage).toHaveAttribute("rel", "noopener noreferrer");
    fireEvent.click(screen.getByRole("tab", { name: "หลักฐานการเบิกจ่าย" })); expect(screen.getByTitle("ตัวอย่างหลักฐานการเบิกจ่าย PDF")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด PDF" }));
    fireEvent.click(screen.getByRole("button", { name: "ดาวน์โหลด DOCX" }));
    expect(downloads).toEqual(["report.pdf", "report.docx", "payment.pdf", "payment.docx"]);
    expect(fullPage).toHaveAttribute("href", "blob:payment-pdf");
    change("หมายเหตุ รายการ 1", "เปลี่ยนข้อมูล");
    expect(screen.queryByRole("tab", { name: "หลักฐานการเบิกจ่าย" })).not.toBeInTheDocument();
    expect(screen.queryByText("ยืนยันชั่วโมงเบิกแล้ว")).not.toBeInTheDocument();
    for (const url of ["blob:pdf", "blob:docx", "blob:payment-pdf", "blob:payment-docx"]) expect(URL.revokeObjectURL).toHaveBeenCalledWith(url);
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); expect(renderOvertimeBundle).toHaveBeenCalledTimes(1);
  });
  it("blocks rendering after a calendar load failure and allows retry", async () => {
    vi.mocked(getOvertimePaymentOptions).mockRejectedValueOnce(new Error("offline"));
    renderFleet(<OvertimeWorkReportPage />); await screen.findByDisplayValue(options.proposer.name); fill();
    await screen.findByText("โหลดอัตราและปฏิทินวันหยุดไม่สำเร็จ กรุณาลองใหม่");
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); expect(renderOvertimeBundle).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "ลองใหม่" })); await confirmHours();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF");
  });
  it("defaults to the user's own head and preserves a chosen approver when the report department changes", async () => {
    const foreign = { id: "foreign", name: "นายหัวหน้า ต่างหน่วยงาน", position: "หัวหน้าฝ่าย", departmentId: "other" };
    const director = { ...foreign, id: "director", name: "นายผู้อำนวยการ" };
    renderFleet(<OvertimeWorkReportPage memoOptions={{ ...options, departments: [...options.departments, { id: "other", name: "หน่วยงานอื่น" }], heads: [...options.heads, foreign], directors: [director], approvers: [foreign, director, ...options.heads] }} initialDepartmentId="other" initialSupervisorUserId="foreign" />);
    await screen.findByDisplayValue(options.proposer.name);
    const supervisor = screen.getByRole("combobox", { name: /^ผู้ควบคุม/ });
    expect(supervisor).toHaveTextContent(options.heads[0].name);
    fireEvent.mouseDown(supervisor);
    expect(await screen.findByRole("option", { name: /นายผู้อำนวยการ/ })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("option", { name: /นายหัวหน้า ต่างหน่วยงาน/ }));
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /^หน่วยงาน/ }));
    fireEvent.click(await screen.findByRole("option", { name: "กลุ่มงานทดสอบ" }));
    expect(supervisor).toHaveTextContent(foreign.name);
    fill(); await confirmHours(); fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" }));
    await screen.findByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF");
    expect(renderOvertimeBundle).toHaveBeenCalledWith(expect.objectContaining({ departmentId: "dept", supervisorUserId: "foreign" }), expect.objectContaining({ days: expect.any(Array) }), expect.any(AbortSignal));
  });
  it("focuses missing fields, uses the real reporter and head, and invalidates files when edited", async () => {
    renderFleet(<OvertimeWorkReportPage />); await screen.findByDisplayValue(options.proposer.name);
    expect(screen.getByLabelText("ผู้รายงานจาก HOP")).toHaveAttribute("readonly");
    const preview = screen.getByRole("button", { name: "สร้าง Preview" }); fireEvent.click(preview);
    expect(screen.getByLabelText("เวลาเริ่ม รายการ 1")).toHaveFocus(); expect(renderOvertimeBundle).not.toHaveBeenCalled();
    change("เวลาเริ่ม รายการ 1", "08:29"); change("เวลาสิ้นสุด รายการ 1", "16:34"); fireEvent.click(preview);
    expect(screen.getByLabelText(/ลักษณะงาน รายการ 1 งาน 1/)).toHaveFocus(); fill();
    expect(screen.getByLabelText("ชั่วโมงทำงานจริง รายการ 1")).toBeDisabled(); expect(screen.getByLabelText("ชั่วโมงทำงานจริง รายการ 1")).toHaveValue(8); await confirmHours(); fireEvent.click(preview);
    await screen.findByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF");
    expect(renderOvertimeBundle).toHaveBeenCalledWith(expect.objectContaining({ supervisorUserId: "head", entries: [expect.objectContaining({ hours: 8, tasks: [{ description: "กวาดส่งข้อมูลผู้ป่วยนอก", quantity: "จำนวน 159 ราย" }] })] }), expect.objectContaining({ days: expect.any(Array) }), expect.any(AbortSignal));
    expect(screen.getByRole("button", { name: "ดาวน์โหลด DOCX" })).toBeEnabled();
    change("หมายเหตุ รายการ 1", "ตรวจสอบแล้ว"); expect(screen.queryByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF")).not.toBeInTheDocument();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:pdf"); expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:docx");
  });
  it("recalculates disabled whole hours on date/time edits including zero and an overnight interval", async () => {
    renderFleet(<OvertimeWorkReportPage />); await screen.findByDisplayValue(options.proposer.name);
    const hours = screen.getByLabelText("ชั่วโมงทำงานจริง รายการ 1"); expect(hours).toBeDisabled(); expect(hours).toHaveValue(null);
    change("วันที่ปฏิบัติงาน รายการ 1", "2026-12-31"); change("เวลาเริ่ม รายการ 1", "23:45");
    change("วันสิ้นสุด รายการ 1", "2027-01-01"); change("เวลาสิ้นสุด รายการ 1", "00:44"); expect(hours).toHaveValue(0);
    change("เวลาสิ้นสุด รายการ 1", "00:45"); expect(hours).toHaveValue(1);
    change("เวลาสิ้นสุด รายการ 1", ""); expect(hours).toHaveValue(null);
  });
  it("preserves multiple tasks and quantities in the same shift and supports removing a task", async () => {
    renderFleet(<OvertimeWorkReportPage />); await screen.findByDisplayValue(options.proposer.name); fill();
    fireEvent.click(screen.getByRole("button", { name: "เพิ่มงานในรายการ 1" }));
    change("ลักษณะงาน รายการ 1 งาน 2", "เคลม MOPH CLAIM"); change("ปริมาณ/หน่วยนับ รายการ 1 งาน 2", "จำนวน 1 งาน"); await confirmHours();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await screen.findByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF");
    expect(vi.mocked(renderOvertimeBundle).mock.calls[0][0].entries[0].tasks).toEqual([{ description: "กวาดส่งข้อมูลผู้ป่วยนอก", quantity: "จำนวน 159 ราย" }, { description: "เคลม MOPH CLAIM", quantity: "จำนวน 1 งาน" }]);
    fireEvent.click(screen.getByRole("button", { name: "ลบงาน 1 รายการ 1" }));
    expect(screen.getByLabelText(/ลักษณะงาน รายการ 1 งาน 1/)).toHaveValue("เคลม MOPH CLAIM");
  });
  it("discards a render response after an edit and clears the form only after system confirmation", async () => {
    let resolve!: (result: OvertimeReportBundle) => void; vi.mocked(renderOvertimeBundle).mockImplementationOnce(() => new Promise(r => { resolve = r; }));
    renderFleet(<><OvertimeWorkReportPage /><SystemDialogHost /></>); await screen.findByDisplayValue(options.proposer.name); fill(); await confirmHours();
    fireEvent.click(screen.getByRole("button", { name: "สร้าง Preview" })); await waitFor(() => expect(renderOvertimeBundle).toHaveBeenCalled());
    change("โครงการ/กิจกรรมปฏิบัติงาน", "โครงการใหม่"); await act(async () => resolve({ workReport: files, paymentEvidence: files }));
    expect(screen.queryByTitle("ตัวอย่างรายงานผลการปฏิบัติงาน PDF")).not.toBeInTheDocument(); expect(URL.createObjectURL).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "ล้างฟอร์ม" })); await screen.findByRole("dialog");
    expect(screen.getByLabelText(/โครงการ\/กิจกรรมปฏิบัติงาน/)).toHaveValue("โครงการใหม่");
    fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" })); await waitFor(() => expect(screen.getByLabelText(/โครงการ\/กิจกรรมปฏิบัติงาน/)).toHaveValue("กลุ่มงานทดสอบ"));
    expect(screen.getByLabelText(/ลักษณะงาน รายการ 1 งาน 1/)).toHaveValue("");
  });
});
