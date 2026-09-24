import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FleetSettingsPage } from "./FleetSettingsPage";
import * as api from "../api/fleetApi";

let permissions: string[] = [];
vi.mock("../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: (code:string) => permissions.includes(code) }) }));
vi.mock("../api/fleetApi", () => ({
  getFleetVehicleRecords: vi.fn(), getFleetVehicleTypes: vi.fn(), getFleetMasterDepartments: vi.fn(), getFleetMasterPersonnel: vi.fn(),
  getFleetDriverProfiles: vi.fn(), createFleetVehicle: vi.fn(), updateFleetVehicle: vi.fn(), setFleetVehicleActive: vi.fn(),
  createFleetDriverProfile: vi.fn(), updateFleetDriverProfile: vi.fn(), setFleetDriverActive: vi.fn(),
}));

function mount() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(<QueryClientProvider client={client}><FleetSettingsPage /></QueryClientProvider>);
  return () => { view.unmount(); client.clear(); };
}

describe("Fleet settings access and forms", () => {
  beforeEach(() => {
    vi.resetAllMocks(); permissions = [];
    vi.mocked(api.getFleetVehicleRecords).mockResolvedValue([]);
    vi.mocked(api.getFleetVehicleTypes).mockResolvedValue([{ id:"t1", code:"VAN", name:"รถตู้", sortOrder:0, isActive:true }]);
    vi.mocked(api.getFleetMasterDepartments).mockResolvedValue([]);
    vi.mocked(api.getFleetMasterPersonnel).mockResolvedValue([{id:"u1",fullName:"คนขับ A",employeeCode:"E1"}]);
    vi.mocked(api.getFleetDriverProfiles).mockResolvedValue([]);
  });

  it("shows only vehicle management to a vehicle manager", async () => {
    permissions = ["FleetVehicle.Manage"];
    const cleanup = mount();
    expect(await screen.findByRole("tab", { name:/รถ \(/ })).toBeInTheDocument();
    expect(screen.queryByRole("tab", { name:/คนขับ \(/ })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name:"เพิ่มรถ" }));
    expect(screen.getByLabelText("รหัสรถ *")).toBeInTheDocument();
    cleanup();
  });

  it("shows driver management and eligible employee without vehicle access", async () => {
    permissions = ["FleetDriver.Manage"];
    const cleanup = mount();
    expect(await screen.findByRole("tab", { name:/คนขับ \(/ })).toBeInTheDocument();
    expect(screen.queryByRole("tab", { name:/รถ \(/ })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name:"เพิ่มคนขับ" }));
    expect(screen.getByLabelText("เลขที่ใบขับขี่ *")).toBeInTheDocument();
    await waitFor(() => expect(api.getFleetMasterPersonnel).toHaveBeenCalled());
    cleanup();
  });

  it("filters vehicles and confirms deactivation in a dialog", async () => {
    permissions = ["FleetVehicle.Manage"];
    vi.mocked(api.getFleetVehicleRecords).mockResolvedValue([
      { id:"v1", vehicleCode:"CAR-1", registrationNumber:"กข 1234", vehicleTypeId:"t1", vehicleTypeName:"รถตู้", seatCapacityTotal:8, passengerCapacity:7, currentMileage:1200, status:"AVAILABLE", isActive:true },
      { id:"v2", vehicleCode:"CAR-2", registrationNumber:"คง 5678", vehicleTypeId:"t1", vehicleTypeName:"รถตู้", seatCapacityTotal:8, passengerCapacity:7, currentMileage:800, status:"AVAILABLE", isActive:false },
    ]);
    vi.mocked(api.setFleetVehicleActive).mockResolvedValue({ id:"v1", isActive:false });
    const cleanup = mount();
    expect(await screen.findByText("กข 1234")).toBeInTheDocument();
    expect(screen.getAllByRole("img", { name:"ภาพประกอบประเภทรถ รถตู้" })).toHaveLength(2);
    fireEvent.click(screen.getByRole("button", { name:"มุมมองรายการ" }));
    expect(screen.getByRole("button", { name:"มุมมองรายการ" })).toHaveAttribute("aria-label", "มุมมองรายการ");
    fireEvent.mouseDown(screen.getByRole("combobox", { name:"สถานะการใช้งาน" }));
    fireEvent.click(screen.getByRole("option", { name:"เปิดใช้งาน" }));
    expect(screen.queryByText("คง 5678")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name:"ปิดใช้งาน" }));
    expect(screen.getByRole("dialog", { name:"ยืนยันปิดใช้งานรถ" })).toBeInTheDocument();
    expect(api.setFleetVehicleActive).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("dialog").querySelector("button:last-child")!);
    await waitFor(() => expect(api.setFleetVehicleActive).toHaveBeenCalledWith("v1", false));
    cleanup();
  });

  it("keeps driver actions available after filtering by name", async () => {
    permissions = ["FleetDriver.Manage"];
    vi.mocked(api.getFleetDriverProfiles).mockResolvedValue([
      { id:"d1", userId:"u1", employeeCode:"E1", fullName:"คนขับ A", licenseNumber:"123", licenseType:"ท.2", licenseIssueDate:"2024-01-01", licenseExpiryDate:"2029-01-01", canDriveSedan:false, canDrivePickup:false, canDriveVan:true, canDriveAmbulance:false, canDriveOther:false, driverStatus:"AVAILABLE", isActive:true },
    ]);
    const cleanup = mount();
    expect(await screen.findByText("คนขับ A")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("ค้นหาชื่อ รหัสพนักงาน หรือใบขับขี่"), { target:{ value:"ไม่พบ" } });
    expect(screen.getByText("ไม่พบคนขับตามเงื่อนไข")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("ค้นหาชื่อ รหัสพนักงาน หรือใบขับขี่"), { target:{ value:"E1" } });
    expect(screen.getByRole("button", { name:"ดู/แก้ไข" })).toBeInTheDocument();
    cleanup();
  });
});
