import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FleetMaintenanceFormPage } from "./FleetMaintenanceFormPage";
import { FleetMaintenanceDetailPage } from "./FleetMaintenanceDetailPage";
import { FleetVehicleDocumentsPage } from "./FleetVehicleDocumentsPage";
import { renderFleet } from "../test/renderFleet";
import * as api from "../api/fleetApi";
vi.mock("../api/fleetApi", () => ({
  getFleetMaintenanceDetail: vi.fn(),
  getFleetVehicles: vi.fn(),
  getMaintenanceTypes: vi.fn(),
  createFleetMaintenance: vi.fn(),
  updateFleetMaintenance: vi.fn(),
  maintenanceAction: vi.fn(),
  uploadMaintenanceAttachment: vi.fn(),
  deleteMaintenanceAttachment: vi.fn(),
  getVehicleDocuments: vi.fn(),
  createVehicleDocument: vi.fn(),
  disableVehicleDocument: vi.fn(),
}));
const detail = {
  id: "m1",
  vehicleId: "v1",
  maintenanceTypeId: "t1",
  status: "IN_PROGRESS",
  concurrencyToken: "c1",
  records: [],
  dueDate: null,
  dueMileage: 100,
};
describe("Fleet maintenance UI", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(api.getFleetVehicles).mockResolvedValue([
      {
        id: "v1",
        vehicleCode: "QA",
        registrationNumber: "TEST",
        currentMileage: 100,
        isActive: true,
      },
    ]);
    vi.mocked(api.getMaintenanceTypes).mockResolvedValue([
      {
        id: "t1",
        code: "OIL",
        name: "Oil",
        isDateBased: true,
        isMileageBased: false,
        blocksAvailabilityWhenOverdue: true,
        isActive: true,
      },
    ]);
  });
  it("validates date-based schedule", async () => {
    renderFleet(
      <FleetMaintenanceFormPage />,
      "/fleet/maintenance/create",
      "/fleet/maintenance/create",
    );
    fireEvent.mouseDown(await screen.findByLabelText("รถ *"));
    fireEvent.click(await screen.findByRole("option", { name: /QA/ }));
    fireEvent.mouseDown(screen.getByLabelText("ประเภทบำรุงรักษา *"));
    fireEvent.click(await screen.findByRole("option", { name: "Oil" }));
    fireEvent.click(screen.getByText("บันทึก"));
    expect(
      await screen.findByText(/ประเภทนี้ต้องระบุวันครบกำหนด/),
    ).toBeInTheDocument();
  });
  it("shows start and complete dialog", async () => {
    vi.mocked(api.getFleetMaintenanceDetail).mockResolvedValue(detail);
    renderFleet(
      <FleetMaintenanceDetailPage />,
      "/fleet/maintenance/m1",
      "/fleet/maintenance/:id",
    );
    fireEvent.click(await screen.findByText("เสร็จสิ้นงานบำรุงรักษา"));
    expect(screen.getByText("Complete maintenance")).toBeInTheDocument();
  });
  it("shows conflict error from action", async () => {
    vi.mocked(api.getFleetMaintenanceDetail).mockResolvedValue(detail);
    vi.mocked(api.maintenanceAction).mockRejectedValue(new Error("409"));
    renderFleet(
      <FleetMaintenanceDetailPage />,
      "/fleet/maintenance/m1",
      "/fleet/maintenance/:id",
    );
    fireEvent.click(await screen.findByText("เสร็จสิ้นงานบำรุงรักษา"));
    fireEvent.click(screen.getByText("ยืนยัน"));
    expect(await screen.findByText(/concurrency conflict/)).toBeInTheDocument();
  });
  it("renders empty documents and create dialog", async () => {
    vi.mocked(api.getVehicleDocuments).mockResolvedValue([]);
    renderFleet(
      <FleetVehicleDocumentsPage />,
      "/fleet/vehicles/v1/documents",
      "/fleet/vehicles/:vehicleId/documents",
    );
    expect(await screen.findByText("ยังไม่มีเอกสาร")).toBeInTheDocument();
    fireEvent.click(screen.getByText("เพิ่มเอกสาร"));
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });
});
