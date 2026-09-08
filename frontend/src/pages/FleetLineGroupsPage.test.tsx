// @vitest-environment jsdom
import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  confirmFleetLineGroup, getFleetLineGroupDeliveries, getFleetLineGroups, type FleetLineGroup,
} from "../api/fleetApi";
import { renderFleet } from "../test/renderFleet";
import { FleetLineGroupsPage } from "./FleetLineGroupsPage";

vi.mock("../api/fleetApi", () => ({
  getFleetLineGroups: vi.fn(), getFleetLineGroupDeliveries: vi.fn(), confirmFleetLineGroup: vi.fn(),
  disableFleetLineGroup: vi.fn(), updateFleetLineGroupSubscriptions: vi.fn(), testFleetLineGroup: vi.fn(),
}));
vi.mock("../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: () => true }) }));

const group: FleetLineGroup = {
  id: "group-1", displayName: "กลุ่มงานยานพาหนะ", groupIdMasked: "C1234...7890", status: "Pending",
  module: "FLEET", attentionRequired: false, firstDetectedAt: "2026-08-05T01:00:00Z",
  lastDetectedAt: "2026-08-05T02:00:00Z", concurrencyToken: "token-1",
  events: [
    { eventType: "Fleet.RequestSubmitted", isEnabled: true },
    { eventType: "Fleet.AdminReviewed", isEnabled: false },
  ],
};

describe("FleetLineGroupsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getFleetLineGroups).mockResolvedValue([group]);
    vi.mocked(getFleetLineGroupDeliveries).mockResolvedValue({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });
    vi.mocked(confirmFleetLineGroup).mockResolvedValue({ id: group.id, status: "Active", concurrencyToken: "token-2" });
  });

  it("renders detected group with masked id and never exposes raw group id", async () => {
    renderFleet(<FleetLineGroupsPage />);

    expect(await screen.findByText("กลุ่มงานยานพาหนะ")).toBeInTheDocument();
    expect(screen.getByText("C1234...7890")).toBeInTheDocument();
    expect(screen.getByText("1/2")).toBeInTheDocument();
    expect(document.body.textContent).not.toContain("C12345678901234567890");
  });

  it("shows filter-aware empty state", async () => {
    vi.mocked(getFleetLineGroups).mockResolvedValue([]);
    renderFleet(<FleetLineGroupsPage />);
    const search = screen.getByLabelText("ค้นหาชื่อกลุ่ม");
    fireEvent.change(search, { target: { value: "ไม่พบ" } });

    expect(await screen.findByText("ไม่พบกลุ่มตามตัวกรอง")).toBeInTheDocument();
  });

  it("shows retry action when list loading fails", async () => {
    vi.mocked(getFleetLineGroups).mockRejectedValue(new Error("network"));
    renderFleet(<FleetLineGroupsPage />);

    expect(await screen.findByText("โหลดรายการ LINE Group ไม่สำเร็จ")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ลองใหม่" })).toBeInTheDocument();
  });

  it("confirms a pending group through a confirmation dialog", async () => {
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await screen.findByText("กลุ่มงานยานพาหนะ");

    await user.click(screen.getByRole("button", { name: /ยืนยัน/ }));
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    await user.type(screen.getByLabelText("เหตุผล/หมายเหตุ"), "ตรวจสอบกลุ่มแล้ว");
    await user.click(screen.getByRole("button", { name: "ยืนยันกลุ่ม" }));

    await waitFor(() => expect(confirmFleetLineGroup).toHaveBeenCalledWith("group-1", "token-1", "ตรวจสอบกลุ่มแล้ว"));
  });
});
