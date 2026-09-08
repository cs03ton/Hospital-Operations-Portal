import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FleetDashboardPage } from "./FleetDashboardPage";
import { renderFleet } from "../test/renderFleet";
import * as api from "../api/fleetApi";

vi.mock("../api/fleetApi", () => ({
  FLEET_DASHBOARD_QUERY_KEY: ["fleet-dashboard"],
  getFleetDashboard: vi.fn(),
  getFleetFeedbackEligibleTrips: vi.fn(),
}));

const dashboard: api.FleetDashboardData = {
  capabilities: { canViewOwnRequests: true, canViewOwnTrips: false, canDispatch: true, canAdminReview: false, canDirectorApprove: false, canViewCalendar: true, canViewReports: false, canManageFleet: false, delegatedPermissions: [] },
  badges: { dispatchQueue: 3, reviewQueue: 0, approvalQueue: 0, myDriverJobs: 0 },
  shared: { todayJobs: 2, availableVehicles: 4, inUseVehicles: 1, unavailableVehicles: 1 },
  requester: { statusCounts: { DRAFT: 1, APPROVED: 2 }, nextTrip: { id: "request-1", requestNo: "VH-202608-0001", departureAt: "2026-08-06T01:00:00Z", destination: "เชียงใหม่", status: "APPROVED", vehicle: null, driver: null }, actionRequired: 1 },
  driver: null,
  dispatcher: { pendingDispatch: 3, returnedToDispatcher: 1, availableVehicles: 4, busyVehicles: 1, availableDrivers: 2, busyDrivers: 1, overdueJobs: 0 },
  adminReviewer: null,
  director: null,
  admin: null,
  feedback: undefined,
  generatedAt: "2026-08-05T01:00:00Z",
};

describe("FleetDashboardPage", () => {
  beforeEach(() => { vi.resetAllMocks(); vi.mocked(api.getFleetFeedbackEligibleTrips).mockResolvedValue([]); });

  it("renders only dashboard sections returned by the server", async () => {
    vi.mocked(api.getFleetDashboard).mockResolvedValue(dashboard);
    renderFleet(<FleetDashboardPage />);
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(await screen.findByText("คำขอใช้รถของฉัน")).toBeInTheDocument();
    expect(screen.getByText("งานจัดรถ")).toBeInTheDocument();
    expect(screen.queryByText("งานขับรถของฉัน")).not.toBeInTheDocument();
    expect(screen.getByText("VH-202608-0001 · เชียงใหม่")).toBeInTheDocument();
  });

  it("shows a scoped empty state when no role section is available", async () => {
    vi.mocked(api.getFleetDashboard).mockResolvedValue({ ...dashboard, requester: null, dispatcher: null });
    renderFleet(<FleetDashboardPage />);
    expect(await screen.findByText("ยังไม่มีข้อมูล Dashboard ที่ตรงกับขอบเขตสิทธิ์ของคุณ")).toBeInTheDocument();
  });

  it("shows API error and retries", async () => {
    vi.mocked(api.getFleetDashboard).mockRejectedValueOnce(new Error()).mockRejectedValueOnce(new Error()).mockResolvedValue(dashboard);
    renderFleet(<FleetDashboardPage />);
    fireEvent.click(await screen.findByText("ลองใหม่", {}, { timeout: 4_000 }));
    await waitFor(() => expect(api.getFleetDashboard).toHaveBeenCalledTimes(3));
  });

  it("shows delegated permission indicator", async () => {
    vi.mocked(api.getFleetDashboard).mockResolvedValue({ ...dashboard, capabilities: { ...dashboard.capabilities, delegatedPermissions: ["FleetDirector.Approve"] } });
    renderFleet(<FleetDashboardPage />);
    expect(await screen.findByText("รับมอบหมาย 1 สิทธิ์")).toBeInTheDocument();
  });

  it("shows the feedback management widget when returned by the server", async () => {
    vi.mocked(api.getFleetDashboard).mockResolvedValue({
      ...dashboard,
      feedback: { feedbackCount: 8, responseRate: 80, overallAverage: 4.5, safetyAverage: 4.8, incidentCount: 1, attentionCount: 2, attentionThreshold: 2 },
    });
    renderFleet(<FleetDashboardPage />);
    expect(await screen.findByText("Feedback การเดินทาง")).toBeInTheDocument();
    expect(screen.getByText("ควรตรวจสอบ")).toBeInTheDocument();
    expect(screen.getAllByText("2").length).toBeGreaterThan(0);
  });
});
