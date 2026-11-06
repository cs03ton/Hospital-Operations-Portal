// @vitest-environment jsdom
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { renderFleet } from "../../test/renderFleet";
import { getFleetSwapCandidates, swapFleetAssignments, type FleetRequest } from "../../api/fleetApi";
import { FleetAssignmentSwapDialog } from "./FleetAssignmentSwapDialog";
vi.mock("../../api/fleetApi", () => ({ getFleetSwapCandidates: vi.fn(), swapFleetAssignments: vi.fn() }));
const request = { id: "a", requestNo: "VH-A", destination: "น่าน", departureAt: "2026-11-05T01:00:00Z", expectedReturnAt: "2026-11-05T03:00:00Z", concurrencyToken: "request-a",
  activeAssignment: { id: "assign-a", vehicleCode: "CAR-A", registrationNumber: "AA", driverName: "คนขับ A", concurrencyToken: "assign-token-a" } } as FleetRequest;
const candidate = { assignmentId: "assign-b", requestId: "b", requestNo: "VH-B", departureAt: request.departureAt, expectedReturnAt: request.expectedReturnAt, destination: "เชียงใหม่", status: "READY", vehicleId: "car-b", vehicle: "CAR-B · BB", driverUserId: "driver-b", driver: "คนขับ B", requestConcurrencyToken: "request-b", assignmentConcurrencyToken: "assign-token-b" };
describe("Fleet assignment swap", () => {
  beforeEach(() => { vi.resetAllMocks(); vi.mocked(getFleetSwapCandidates).mockResolvedValue([candidate]); vi.mocked(swapFleetAssignments).mockResolvedValue({ requestIds: ["a", "b"] }); });
  it("requires a second request and reason, previews both jobs, and submits both tokens", async () => {
    const close = vi.fn(); const refresh = vi.fn().mockResolvedValue(undefined);
    renderFleet(<FleetAssignmentSwapDialog request={request} onClose={close} onSuccess={refresh} />);
    expect(screen.getByRole("button", { name: "ตรวจสอบก่อนสลับ" })).toBeDisabled();
    fireEvent.click(await screen.findByRole("button", { name: "เลือกคำขอนี้" }));
    expect(screen.getByRole("button", { name: "ตรวจสอบก่อนสลับ" })).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/เหตุผลการสลับ/), { target: { value: "ปรับแผน" } });
    fireEvent.click(screen.getByRole("button", { name: "ตรวจสอบก่อนสลับ" }));
    expect(screen.getAllByText(/หลังสลับ:/)).toHaveLength(2);
    expect(swapFleetAssignments).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "ยืนยันสลับรถ" }));
    await waitFor(() => expect(close).toHaveBeenCalled());
    expect(swapFleetAssignments).toHaveBeenCalledWith("assign-a", { otherAssignmentId: "assign-b", includeDrivers: false, reason: "ปรับแผน", requestConcurrencyToken: "request-a", assignmentConcurrencyToken: "assign-token-a", otherRequestConcurrencyToken: "request-b", otherAssignmentConcurrencyToken: "assign-token-b" });
    expect(refresh).toHaveBeenCalled();
  });
  it("supports swapping drivers and refreshes selection on conflict", async () => {
    const user = userEvent.setup(); const refresh = vi.fn().mockResolvedValue(undefined);
    vi.mocked(swapFleetAssignments).mockRejectedValue({ isAxiosError: true, response: { status: 409, data: { message: "ข้อมูลเปลี่ยนแปลง" } } });
    renderFleet(<FleetAssignmentSwapDialog request={request} onClose={vi.fn()} onSuccess={refresh} />);
    await user.click(await screen.findByRole("button", { name: "เลือกคำขอนี้" }));
    await user.click(screen.getByLabelText("รูปแบบการสลับ")); await user.click(screen.getByRole("option", { name: "สลับรถพร้อมคนขับ" }));
    fireEvent.change(screen.getByLabelText(/เหตุผลการสลับ/), { target: { value: "เปลี่ยนทั้งคู่" } });
    await user.click(screen.getByRole("button", { name: "ตรวจสอบก่อนสลับ" })); await user.click(screen.getByRole("button", { name: "ยืนยันสลับรถ" }));
    await screen.findByText("ข้อมูลเปลี่ยนแปลง"); await waitFor(() => expect(refresh).toHaveBeenCalled());
    expect(vi.mocked(swapFleetAssignments).mock.calls[0][1].includeDrivers).toBe(true);
    expect(screen.getByRole("button", { name: "ตรวจสอบก่อนสลับ" })).toBeDisabled();
  });
  it("searches by request number and handles an empty candidate list", async () => {
    vi.mocked(getFleetSwapCandidates).mockResolvedValue([]);
    renderFleet(<FleetAssignmentSwapDialog request={request} onClose={vi.fn()} onSuccess={vi.fn()} />);
    await screen.findByText(/ไม่พบคำขอที่สลับได้/);
    fireEvent.change(screen.getByLabelText("ค้นหาเลขคำขอที่จะสลับ"), { target: { value: "VH-999" } }); fireEvent.click(screen.getByRole("button", { name: "ค้นหา" }));
    await waitFor(() => expect(getFleetSwapCandidates).toHaveBeenCalledWith("assign-a", "VH-999"));
  });
});
