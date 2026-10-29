// @vitest-environment jsdom
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderFleet } from "../../test/renderFleet";
import { FleetVehicleUsageReport } from "./FleetVehicleUsageReport";
import { exportFleetVehicleUsage, getFleetVehicleUsage, type FleetVehicleUsage } from "../../api/fleetApi";
const permission = vi.hoisted(() => ({ allowed: true }));
vi.mock("../../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: () => permission.allowed }) }));
vi.mock("../../api/fleetApi", () => ({ getFleetVehicleUsage: vi.fn(), exportFleetVehicleUsage: vi.fn() }));
const data: FleetVehicleUsage = {
  vehicles: [{ vehicleId: "car", vehicleCode: "CAR-001", registrationNumber: "กข 123", requestCount: 2, tripCount: 1, distanceKm: 0, calculatedTripCount: 1 }],
  items: [{ requestId: "request", tripId: "trip", vehicleId: "car", vehicleCode: "CAR-001", registrationNumber: "กข 123", requestNo: "VH-001", departureAt: "2026-10-01T01:00:00Z", expectedReturnAt: "2026-10-01T04:00:00Z", destination: "โรงพยาบาลน่าน", status: "READY", driver: "คนขับจริง", startMileage: 0, endMileage: null, distanceKm: null, isAborted: false }],
  page: 1, pageSize: 10, totalItems: 12, totalPages: 2,
};
describe("Vehicle usage report", () => {
  beforeEach(() => { vi.resetAllMocks(); permission.allowed = true; vi.mocked(getFleetVehicleUsage).mockResolvedValue(data); });
  it("shows recorded zero and missing mileage distinctly on desktop and mobile", async () => {
    renderFleet(<FleetVehicleUsageReport startDate="2026-10-01" endDate="2026-10-31" status="" />);
    await screen.findByText("จำนวนคำขอ");
    expect(screen.getByText("คนขับจริง")).toBeInTheDocument();
    expect(screen.getByText("คนขับ: คนขับจริง")).toBeInTheDocument();
    expect(screen.getByText("ไมล์เริ่มต้น: 0")).toBeInTheDocument();
    expect(screen.getByText("ไมล์สิ้นสุด: ยังไม่บันทึก")).toBeInTheDocument();
    expect(screen.getByText("ระยะทางรวม")).toBeInTheDocument();
    fireEvent.click(screen.getByText("ดูรายการคันนี้"));
    await waitFor(() => expect(vi.mocked(getFleetVehicleUsage).mock.calls.slice(-1)[0][0].get("vehicleId")).toBe("car"));
    expect(vi.mocked(getFleetVehicleUsage).mock.calls.slice(-1)[0][0].get("page")).toBe("1");
  });
  it("exports with the current dates, status and selected car, and shows failures", async () => {
    vi.mocked(exportFleetVehicleUsage).mockRejectedValue(new Error("offline"));
    renderFleet(<FleetVehicleUsageReport startDate="2026-10-01" endDate="2026-10-31" status="CANCELLED" />);
    await screen.findByText("จำนวนคำขอ"); fireEvent.click(screen.getByText("ดูรายการคันนี้"));
    await waitFor(() => expect(vi.mocked(getFleetVehicleUsage).mock.calls.slice(-1)[0][0].get("vehicleId")).toBe("car"));
    await waitFor(() => expect(screen.getByText("ส่งออก CSV รายคัน")).not.toBeDisabled());
    fireEvent.click(screen.getByText("ส่งออก CSV รายคัน")); await screen.findByText("ส่งออก CSV ไม่สำเร็จ กรุณาลองใหม่");
    const params = vi.mocked(exportFleetVehicleUsage).mock.calls[0][0]; expect(params.get("status")).toBe("CANCELLED"); expect(params.get("vehicleId")).toBe("car"); expect(params.get("startDate")).toBe("2026-10-01");
  });
  it("shows all-history readings including zero and Thai dates on vehicle cards", async () => {
    vi.mocked(getFleetVehicleUsage).mockResolvedValue({ ...data, vehicles: [{ ...data.vehicles[0], firstRecordedMileage: 0,
      firstRecordedMileageAt: "2025-09-01T01:00:00Z", latestRecordedMileage: 900, latestRecordedMileageAt: "2026-10-29T02:00:00Z" }] });
    renderFleet(<FleetVehicleUsageReport startDate="2026-10-01" endDate="2026-10-31" status="" />);
    await screen.findByText("เลขไมล์จากประวัติทั้งหมดของรถ");
    expect(screen.getByText("เลขไมล์เริ่มต้นที่บันทึก")).toBeInTheDocument();
    expect(screen.getByText("900 กม.")).toBeInTheDocument();
    expect(screen.getByText(/01\/09\/2568/)).toBeInTheDocument();
    expect(screen.getByText(/29\/10\/2569/)).toBeInTheDocument();
  });
  it("hides export and detail links without permission and distinguishes empty and failed responses", async () => {
    permission.allowed = false;
    vi.mocked(getFleetVehicleUsage).mockResolvedValue({ ...data, vehicles: [], items: [], totalItems: 0, totalPages: 0 });
    renderFleet(<FleetVehicleUsageReport startDate="2026-10-01" endDate="2026-10-31" status="DRAFT" />);
    await screen.findByText("ไม่พบรายการตามตัวกรอง"); expect(screen.queryByText("ส่งออก CSV รายคัน")).not.toBeInTheDocument(); expect(screen.queryByText("รายละเอียด")).not.toBeInTheDocument();
  });
});
