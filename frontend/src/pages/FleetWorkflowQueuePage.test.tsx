// @vitest-environment jsdom
import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getFleetWorkflowQueue } from "../api/fleetApi";
import { FleetWorkflowQueuePage } from "./FleetWorkflowQueuePage";
import { renderFleet } from "../test/renderFleet";

vi.mock("../api/fleetApi", () => ({
  getFleetWorkflowQueue: vi.fn(),
  fleetWorkflowAction: vi.fn(),
  FLEET_DASHBOARD_QUERY_KEY: ["fleet-dashboard"],
}));

describe("FleetWorkflowQueuePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders an empty state when a legacy response has no queue array", async () => {
    vi.mocked(getFleetWorkflowQueue).mockResolvedValue(undefined as never);
    renderFleet(<FleetWorkflowQueuePage kind="admin-review" />);
    expect(await screen.findByText("ไม่มีคำขอรอตรวจในขณะนี้")).toBeInTheDocument();
  });

  it("renders queue rows from the API response", async () => {
    vi.mocked(getFleetWorkflowQueue).mockResolvedValue([{
      id: "request-1",
      requestNo: "VH-202608-0001",
      status: "PENDING_ADMIN_REVIEW",
      requesterName: "ผู้ขอทดสอบ",
      requesterDepartmentName: "ฝ่ายบริหาร",
      purpose: "ประชุมราชการ",
      missionType: "ประชุม/อบรม",
      destination: "โรงพยาบาลจังหวัด",
      departureAt: "2026-08-04T01:00:00Z",
      expectedReturnAt: "2026-08-04T05:00:00Z",
      passengerCount: 3,
      vehicle: "NMH-FLEET-001 · กก 1234 น่าน",
      driver: "พนักงานขับรถทดสอบ",
      concurrencyToken: "token-1",
    }]);
    renderFleet(<FleetWorkflowQueuePage kind="admin-review" />);
    expect(await screen.findByText("VH-202608-0001")).toBeInTheDocument();
    expect(screen.getByText(/ผู้ขอทดสอบ/)).toBeInTheDocument();
    expect(screen.getByText("ประชุมราชการ")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /ดูรายละเอียดคำขอ/ })).toHaveAttribute("href", "/fleet/requests/request-1");
  });

  it("shows a retryable error instead of crashing", async () => {
    vi.mocked(getFleetWorkflowQueue).mockRejectedValue(new Error("network"));
    renderFleet(<FleetWorkflowQueuePage kind="director-approval" />);
    expect(await screen.findByText("ไม่สามารถโหลดคิวได้")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ลองใหม่" })).toBeInTheDocument();
  });
});
