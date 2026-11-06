// @vitest-environment jsdom
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";
import { FleetRequestFormPage } from "./FleetRequestFormPage";
import { renderFleet } from "../test/renderFleet";
import { getFleetRequest, getFleetPersonnelOptions, getFleetDestinationOptions, getFleetRequestAttachments } from "../api/fleetApi";
import { getMyProfile } from "../api/profileApi";
import { persistFleetRequestWithDocuments } from "../utils/fleetRequestDocumentFlow";
import { REFER_MISSION_TYPE } from "../utils/fleetReferPolicy";
const notice = vi.hoisted(() => ({ showSuccess: vi.fn(), showError: vi.fn() }));
vi.mock("../hooks/useNotification", () => ({ useNotification: () => notice }));
vi.mock("../context/AuthContext", () => ({ useAuth: () => ({ user: { id: "author", fullname: "ผู้ขอ ทดสอบ" } }) }));
vi.mock("../api/profileApi", () => ({ getMyProfile: vi.fn() }));
vi.mock("../utils/fleetRequestDocumentFlow", () => ({ persistFleetRequestWithDocuments: vi.fn() }));
vi.mock("../api/fleetApi", () => ({ getFleetRequest: vi.fn(), getFleetPersonnelOptions: vi.fn(), getFleetDestinationOptions: vi.fn(), getFleetRequestAttachments: vi.fn(), deleteFleetRequestAttachment: vi.fn(), openFleetRequestAttachment: vi.fn() }));
const saved = { id: "existing", concurrencyToken: "token", purpose: "ประชุม", missionType: "ทั่วไป", patientName: "", destination: "น่าน", contactPhone: "0800000000", requesterName: "ผู้ขอ ทดสอบ", departureAt: "2026-11-09T01:00:00Z", expectedReturnAt: "2026-11-09T09:00:00Z", specialRequirement: "", isUrgent: false, urgentReason: "", passengers: [{ userId: "author", fullName: "ผู้ขอ ทดสอบ", passengerType: "EMPLOYEE", isRequester: true }] };
const change = (label: string, value: string) => fireEvent.change(screen.getByLabelText(new RegExp(`^${label}`)), { target: { value } });
const click = (label = "บันทึกร่าง") => fireEvent.click(screen.getByRole("button", { name: label }));
async function edit(data = saved) {
  vi.mocked(getFleetRequest).mockResolvedValue(data as Awaited<ReturnType<typeof getFleetRequest>>);
  renderFleet(<FleetRequestFormPage />, "/fleet/requests/existing/edit", "/fleet/requests/:id/edit");
  await screen.findByDisplayValue(data.purpose); await waitFor(() => expect(screen.getByRole("button", { name: "บันทึกร่าง" })).toBeEnabled());
}
describe("fleet request validation focus", () => {
  beforeEach(() => {
    vi.mocked(getMyProfile).mockResolvedValue({ id: "author", fullname: "ผู้ขอ ทดสอบ", phoneNumber: "", departmentName: "ทดสอบ" } as Awaited<ReturnType<typeof getMyProfile>>);
    vi.mocked(getFleetPersonnelOptions).mockResolvedValue([]); vi.mocked(getFleetDestinationOptions).mockResolvedValue([]); vi.mocked(getFleetRequestAttachments).mockResolvedValue([]);
    vi.mocked(persistFleetRequestWithDocuments).mockResolvedValue(saved as Awaited<ReturnType<typeof persistFleetRequestWithDocuments>>);
    Object.defineProperty(HTMLElement.prototype, "scrollIntoView", { configurable: true, value: vi.fn() });
  });
  afterEach(() => { vi.clearAllMocks(); });
  it.each(["บันทึกร่าง", "ส่งคำขอ"])("lets %s validate an empty form instead of disabling it", async button => {
    renderFleet(<FleetRequestFormPage />); await waitFor(() => expect(screen.getByRole("button", { name: button })).toBeEnabled()); click(button);
    await waitFor(() => expect(screen.getByLabelText(/^วัตถุประสงค์/)).toHaveFocus());
    expect(notice.showError).toHaveBeenCalledWith("กรุณากรอกวัตถุประสงค์/ภารกิจ"); expect(persistFleetRequestWithDocuments).not.toHaveBeenCalled();
  });
  it("rejects whitespace then focuses destination, phone and departure in form order", async () => {
    renderFleet(<FleetRequestFormPage />); await waitFor(() => expect(screen.getByRole("button", { name: "บันทึกร่าง" })).toBeEnabled());
    change("วัตถุประสงค์/ภารกิจ", " "); click(); expect(screen.getByLabelText(/^วัตถุประสงค์/)).toHaveFocus();
    change("วัตถุประสงค์/ภารกิจ", "ประชุม"); click(); expect(screen.getByRole("combobox", { name: /^ปลายทาง/ })).toHaveFocus();
    change("ปลายทาง", "น่าน"); click(); expect(screen.getByLabelText(/^หมายเลขโทรศัพท์/)).toHaveFocus();
    change("หมายเลขโทรศัพท์", "0800000000"); click(); expect(screen.getByRole("button", { name: "เลือกวันที่ออกเดินทาง" })).toHaveFocus();
    expect(HTMLElement.prototype.scrollIntoView).toHaveBeenCalled(); expect(persistFleetRequestWithDocuments).not.toHaveBeenCalled();
  });
  it("rejects return-before-departure and focuses its date button", async () => {
    await edit({ ...saved, expectedReturnAt: "2026-11-09T00:00:00Z" }); click("ส่งคำขอ");
    expect(screen.getByRole("button", { name: "เลือกวันที่คาดว่าจะกลับ" })).toHaveFocus(); expect(persistFleetRequestWithDocuments).not.toHaveBeenCalled();
  });
  it("validates urgent reason and no passengers", async () => {
    await edit({ ...saved, isUrgent: true }); click(); expect(screen.getByLabelText(/^เหตุผลความเร่งด่วน/)).toHaveFocus();
    change("เหตุผลความเร่งด่วน", "ด่วน"); fireEvent.click(screen.getByLabelText("ผู้ขอร่วมเดินทางด้วย")); click();
    expect(screen.getByRole("combobox", { name: "เพิ่มผู้ร่วมเดินทาง" })).toHaveFocus(); expect(persistFleetRequestWithDocuments).not.toHaveBeenCalled();
  });
  it("requires Refer patient full name only when submitting and still permits draft", async () => {
    await edit({ ...saved, missionType: REFER_MISSION_TYPE }); click("ส่งคำขอ");
    expect(screen.getByLabelText("ชื่อ–สกุลผู้ป่วย")).toHaveFocus(); expect(persistFleetRequestWithDocuments).not.toHaveBeenCalled();
    click(); await waitFor(() => expect(persistFleetRequestWithDocuments).toHaveBeenCalledWith(expect.objectContaining({ submit: false, payload: expect.objectContaining({ missionType: REFER_MISSION_TYPE }) })));
  });
  it("passes valid edits to the existing document flow with submission intent", async () => {
    await edit(); change("วัตถุประสงค์/ภารกิจ", "  ประชุมทดสอบ  "); click("ส่งคำขอ");
    await waitFor(() => expect(persistFleetRequestWithDocuments).toHaveBeenCalledWith(expect.objectContaining({ requestId: "existing", submit: true, payload: expect.objectContaining({ purpose: "ประชุมทดสอบ", concurrencyToken: "token", passengerCount: 1 }) })));
  });
});
