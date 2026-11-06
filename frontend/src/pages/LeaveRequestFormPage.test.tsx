// @vitest-environment jsdom
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";
import { LeaveRequestFormPage } from "./LeaveRequestFormPage";
import { renderFleet } from "../test/renderFleet";
import { createLeaveRequest, getLeaveRequest, getLeaveTypes, previewLeavePolicy, submitLeaveRequest, updateLeaveRequest, type LeavePolicyPreview, type LeaveType } from "../api/leaveApi";
import { getMyProfile } from "../api/profileApi";
import { notifyGlobal } from "../contexts/NotificationContext";
vi.mock("../api/leaveApi", () => ({ createLeaveRequest: vi.fn(), getLeaveRequest: vi.fn(), getLeaveTypes: vi.fn(), previewLeavePolicy: vi.fn(), submitLeaveRequest: vi.fn(), updateLeaveRequest: vi.fn() }));
vi.mock("../api/profileApi", () => ({ getMyProfile: vi.fn() }));
vi.mock("../contexts/NotificationContext", () => ({ notifyGlobal: vi.fn() }));
vi.mock("../hooks/useNotification", () => ({ useNotification: () => ({ showSuccess: vi.fn() }) }));
vi.mock("../components/common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange, disabled, error, helperText }: { label: string; value: string; onChange: (value: string) => void; disabled?: boolean; error?: boolean; helperText?: string }) => <label>{label}<input aria-label={label} aria-invalid={error} disabled={disabled} value={value} onChange={e => onChange(e.target.value)} />{helperText}</label> }));
const leaveType: LeaveType = { id: "sick", code: "SICK", name: "ลาป่วย", defaultDaysPerYear: 30, requiresBalance: true, allowCarryOver: false, carryOverMaxDays: 0, useFiscalYear: true, requiresAttachment: false, isPaid: true, isActive: true };
const policy: LeavePolicyPreview = { employmentTypeName: "เจ้าหน้าที่", fiscalYear: 2027, entitlementDays: 30, usedDays: 0, pendingDays: 0, availableDays: 30, requestedDays: 1, canSubmit: true, requiresSpecialApproval: false, paymentSegments: [], warnings: [], errors: [], policyNotes: [] };
const change = (label: string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } });
async function chooseLeave() { fireEvent.mouseDown(screen.getByRole("combobox", { name: "ประเภทการลา" })); fireEvent.click(await screen.findByRole("option", { name: "ลาป่วย" })); }
async function fill() { await chooseLeave(); change("วันที่เริ่มลา", "2026-11-09"); change("วันที่สิ้นสุด", "2026-11-09"); }
function click(name = "บันทึกแบบร่าง") { fireEvent.click(screen.getByRole("button", { name })); }
describe("leave request validation navigation", () => {
  beforeEach(() => {
    vi.mocked(getLeaveTypes).mockResolvedValue([leaveType]);
    vi.mocked(getMyProfile).mockResolvedValue({ gender: "Male" } as Awaited<ReturnType<typeof getMyProfile>>);
    vi.mocked(previewLeavePolicy).mockResolvedValue(policy);
    vi.mocked(createLeaveRequest).mockResolvedValue({ id: "saved" } as Awaited<ReturnType<typeof createLeaveRequest>>);
    vi.mocked(submitLeaveRequest).mockResolvedValue({ id: "saved" } as Awaited<ReturnType<typeof submitLeaveRequest>>);
    vi.mocked(updateLeaveRequest).mockResolvedValue({ id: "saved" } as Awaited<ReturnType<typeof updateLeaveRequest>>);
    Object.defineProperty(HTMLElement.prototype, "scrollIntoView", { configurable: true, value: vi.fn() });
  });
  afterEach(() => { vi.clearAllMocks(); });
  it.each(["บันทึกแบบร่าง", "ส่งคำขอ"])("blocks an empty form and focuses leave type for %s", async button => {
    renderFleet(<LeaveRequestFormPage />); click(button);
    await waitFor(() => expect(notifyGlobal).toHaveBeenCalledWith("error", "กรุณาเลือกประเภทการลา"));
    expect(screen.getByRole("combobox", { name: "ประเภทการลา" })).toHaveFocus();
    expect(HTMLElement.prototype.scrollIntoView).toHaveBeenCalledWith({ behavior: "smooth", block: "center" });
    expect(createLeaveRequest).not.toHaveBeenCalled(); expect(submitLeaveRequest).not.toHaveBeenCalled();
  });
  it("focuses missing/invalid start date, then missing/end-before-start date", async () => {
    renderFleet(<LeaveRequestFormPage />); await chooseLeave(); click();
    await waitFor(() => expect(screen.getByLabelText("วันที่เริ่มลา")).toHaveFocus());
    change("วันที่เริ่มลา", "invalid"); click();
    await waitFor(() => expect(notifyGlobal).toHaveBeenLastCalledWith("error", "กรุณาเลือกวันที่เริ่มลาให้ถูกต้อง"));
    change("วันที่เริ่มลา", "2026-11-09"); click();
    await waitFor(() => expect(screen.getByLabelText("วันที่สิ้นสุด")).toHaveFocus());
    change("วันที่สิ้นสุด", "2026-11-08"); click();
    await waitFor(() => expect(notifyGlobal).toHaveBeenLastCalledWith("error", "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มลา"));
    expect(createLeaveRequest).not.toHaveBeenCalled();
  });
  it("rejects whitespace reason before policy errors", async () => {
    vi.mocked(previewLeavePolicy).mockResolvedValue({ ...policy, canSubmit: false, errors: ["สิทธิ์วันลาไม่พอ"] });
    renderFleet(<LeaveRequestFormPage />); await fill(); change("เหตุผล", "   ");
    await screen.findByText("สิทธิ์วันลาไม่พอ"); click("ส่งคำขอ");
    await waitFor(() => expect(screen.getByLabelText("เหตุผล")).toHaveFocus());
    expect(notifyGlobal).toHaveBeenLastCalledWith("error", "กรุณากรอกเหตุผล"); expect(createLeaveRequest).not.toHaveBeenCalled();
    change("เหตุผล", "ป่วย"); click("ส่งคำขอ");
    await waitFor(() => expect(notifyGlobal).toHaveBeenLastCalledWith("error", "สิทธิ์วันลาไม่พอ"));
    expect(createLeaveRequest).not.toHaveBeenCalled();
  });
  it("skips the disabled half-day end date and focuses reason", async () => {
    renderFleet(<LeaveRequestFormPage />); await chooseLeave();
    fireEvent.click(screen.getByLabelText("ครึ่งวัน (เช้า)")); change("วันที่เริ่มลา", "2026-11-09"); click();
    await waitFor(() => expect(screen.getByLabelText("เหตุผล")).toHaveFocus());
    expect(screen.getByLabelText("วันที่สิ้นสุด")).toBeDisabled(); expect(screen.getByLabelText("วันที่สิ้นสุด")).toHaveValue("2026-11-09");
    expect(createLeaveRequest).not.toHaveBeenCalled();
  });
  it.each(["บันทึกแบบร่าง", "ส่งคำขอ"])("saves a complete form for %s and trims reason", async button => {
    renderFleet(<LeaveRequestFormPage />); await fill(); change("เหตุผล", "  ป่วย  ");
    await waitFor(() => expect(screen.getByRole("button", { name: button })).toBeEnabled()); click(button);
    await waitFor(() => expect(createLeaveRequest).toHaveBeenCalledWith(expect.objectContaining({ reason: "ป่วย", startDate: "2026-11-09", endDate: "2026-11-09" })));
    if (button === "ส่งคำขอ") await waitFor(() => expect(submitLeaveRequest).toHaveBeenCalledWith("saved")); else expect(submitLeaveRequest).not.toHaveBeenCalled();
  });
  it("validates editing before calling update", async () => {
    vi.mocked(getLeaveRequest).mockResolvedValue({ leaveTypeId: "sick", startDate: "2026-11-09", endDate: "2026-11-09", durationType: "FULL_DAY", totalDays: 1, reason: "เดิม" } as Awaited<ReturnType<typeof getLeaveRequest>>);
    renderFleet(<LeaveRequestFormPage />, "/leave/request/edit", "/leave/:id/edit"); await screen.findByDisplayValue("เดิม");
    change("เหตุผล", " "); click("บันทึกการแก้ไข");
    await waitFor(() => expect(screen.getByLabelText("เหตุผล")).toHaveFocus()); expect(updateLeaveRequest).not.toHaveBeenCalled();
    change("เหตุผล", "แก้ไข"); click("บันทึกการแก้ไข"); await waitFor(() => expect(updateLeaveRequest).toHaveBeenCalledWith("request", expect.objectContaining({ reason: "แก้ไข" })));
  });
});
