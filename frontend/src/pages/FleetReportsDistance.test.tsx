import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FleetReportsPage } from "./FleetReportsPage";
import { renderFleet } from "../test/renderFleet";
import * as api from "../api/fleetApi";

vi.mock("../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: () => false }) }));
vi.mock("../api/fleetApi", async () => ({ ...await vi.importActual("../api/fleetApi"), getFleetKpis: vi.fn(), getFleetCalendar: vi.fn(),
  getFleetDepartmentReport: vi.fn(), getFleetDriverReport: vi.fn(), getFleetRouteReport: vi.fn(), getFleetDistanceSummary: vi.fn() }));

describe("Report distance from recorded mileage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(api.getFleetKpis).mockResolvedValue({ totalRequests: 1 } as api.FleetKpiSummary);
    vi.mocked(api.getFleetCalendar).mockResolvedValue([]);
    vi.mocked(api.getFleetDepartmentReport).mockResolvedValue([]);
    vi.mocked(api.getFleetDriverReport).mockResolvedValue([]);
    vi.mocked(api.getFleetRouteReport).mockResolvedValue([]);
    vi.mocked(api.getFleetDistanceSummary).mockResolvedValue({ totalDistanceKm: 1234.5, calculatedTripCount: 2, incompleteTripCount: 1 });
  });
  it("uses the same total in both cards and sends custom dates without table search", async () => {
    renderFleet(<FleetReportsPage />);
    await waitFor(() => expect(screen.getAllByText("1,234.5 กม.")).toHaveLength(2));
    expect(screen.getByText("คำนวณจาก 2 ทริป")).toBeInTheDocument();
    expect(screen.getByText(/เลขไมล์ไม่ครบหรือผิดปกติ 1 ทริป/)).toBeInTheDocument();
    expect(vi.mocked(api.getFleetKpis).mock.calls[0][0].get("preset")).toBe("custom");
    const calls = vi.mocked(api.getFleetDistanceSummary).mock.calls.length;
    fireEvent.change(screen.getByPlaceholderText("ค้นหาเลขคำขอ หน่วยงาน หรือปลายทาง"), { target: { value: "ค้นหา" } });
    expect(api.getFleetDistanceSummary).toHaveBeenCalledTimes(calls);
  });
  it("shows zero as a real result", async () => {
    vi.mocked(api.getFleetDistanceSummary).mockResolvedValue({ totalDistanceKm: 0, calculatedTripCount: 1, incompleteTripCount: 0 });
    renderFleet(<FleetReportsPage />);
    await waitFor(() => expect(screen.getAllByText("0 กม.")).toHaveLength(2));
    expect(screen.queryByText("ยังไม่มีข้อมูลเลขไมล์ครบถ้วน")).not.toBeInTheDocument();
  });
  it("distinguishes missing mileage from an API failure without hiding the other reports", async () => {
    vi.mocked(api.getFleetDistanceSummary).mockResolvedValue({ totalDistanceKm: null, calculatedTripCount: 0, incompleteTripCount: 1 });
    renderFleet(<FleetReportsPage />);
    await waitFor(() => expect(screen.getAllByText("ยังไม่มีข้อมูลเลขไมล์ครบถ้วน")).toHaveLength(2));
  });
  it("shows a distance error separately", async () => {
    vi.mocked(api.getFleetDistanceSummary).mockRejectedValue(new Error("offline"));
    renderFleet(<FleetReportsPage />);
    await waitFor(() => expect(screen.getAllByText("โหลดระยะทางไม่สำเร็จ")).toHaveLength(2));
    expect(screen.getByText("จำนวนเที่ยวทั้งหมด")).toBeInTheDocument();
  });
});
