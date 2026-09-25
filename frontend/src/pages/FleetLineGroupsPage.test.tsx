// @vitest-environment jsdom
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  confirmFleetLineGroup, getFleetLineGroupDeliveries, getFleetLineGroups, migrateRepairLineGroup, testFleetLineGroup, updateFleetLineGroupEndpoint, updateFleetLineGroupSubscriptions, type FleetLineGroup,
} from "../api/fleetApi";
import { renderFleet } from "../test/renderFleet";
import { FleetLineGroupsPage } from "./FleetLineGroupsPage";

const permissionState = vi.hoisted(() => ({ canManage: true }));
vi.mock("../api/fleetApi", () => ({
  getFleetLineGroups: vi.fn(), getFleetLineGroupDeliveries: vi.fn(), confirmFleetLineGroup: vi.fn(),
  disableFleetLineGroup: vi.fn(), updateFleetLineGroupSubscriptions: vi.fn(), testFleetLineGroup: vi.fn(),
  createFleetLineGroupEndpoint: vi.fn(), updateFleetLineGroupEndpoint: vi.fn(), migrateRepairLineGroup: vi.fn(),
}));
vi.mock("../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: (permission: string) => permission === "LineGroup.Manage" ? permissionState.canManage : true }) }));

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
    permissionState.canManage = true;
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

  it("shows legacy repair groups without offering central delivery actions", async () => {
    vi.mocked(getFleetLineGroups).mockResolvedValue([
      group,
      { ...group, id: "repair-it", displayName: "แจ้งซ่อม IT", module: "REPAIR_IT", status: "Active" },
      { ...group, id: "repair-general", displayName: "แจ้งซ่อม ช่างทั่วไป", module: "REPAIR_GENERAL", status: "Disabled" },
    ]);
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    expect(await screen.findByText("แจ้งซ่อม IT")).toBeInTheDocument();
    expect(screen.getByText("แจ้งซ่อม ช่างทั่วไป")).toBeInTheDocument();
    expect(screen.getAllByText("กลุ่มแจ้งซ่อมเดิม · ยังไม่ได้ย้ายเข้าส่วนกลาง")).toHaveLength(2);
    expect(screen.getAllByRole("button", { name: "แก้ไข" })).toHaveLength(1);
    await user.click(screen.getAllByRole("button", { name: "รายละเอียด" })[1]);
    expect(screen.getByText(/แสดงเพื่อให้ตรวจสอบเท่านั้น/)).toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: "ส่งคำขอใหม่" })).toBeDisabled();
    expect(screen.queryByRole("button", { name: "บันทึกเหตุการณ์แจ้งเตือน" })).not.toBeInTheDocument();
  });

  it("shows grouped events as read only without manage permission", async () => {
    permissionState.canManage = false;
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await user.click(await screen.findByRole("button", { name: "รายละเอียด" }));
    expect(screen.getByRole("region", { name: "ระบบขอรถ" })).toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: "ส่งคำขอใหม่" })).toBeDisabled();
    expect(screen.queryByRole("button", { name: "บันทึกเหตุการณ์แจ้งเตือน" })).not.toBeInTheDocument();
  });

  it("groups events by system, preserves selections and identifies unknown events", async () => {
    const events = [
      { eventType: "Fleet.RequestSubmitted", isEnabled: true },
      { eventType: "Repair.Submitted", isEnabled: false },
      { eventType: "MeetingRoom.BookingCreated", isEnabled: true },
      { eventType: "Future.Event", isEnabled: false },
    ];
    vi.mocked(getFleetLineGroups).mockResolvedValue([{ ...group, events }]);
    vi.mocked(updateFleetLineGroupSubscriptions).mockResolvedValue({ id: group.id, concurrencyToken: "token-2" });
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await user.click(await screen.findByRole("button", { name: "รายละเอียด" }));

    expect(within(screen.getByRole("region", { name: "ระบบขอรถ" })).getByRole("checkbox", { name: "ส่งคำขอใหม่" })).toBeChecked();
    expect(within(screen.getByRole("region", { name: "ระบบแจ้งซ่อม" })).getByRole("checkbox", { name: "แจ้งซ่อมใหม่" })).not.toBeChecked();
    expect(within(screen.getByRole("region", { name: "ระบบจองห้องประชุม" })).getByRole("checkbox", { name: "มีการจองห้องประชุมใหม่" })).toBeChecked();
    expect(within(screen.getByRole("region", { name: "เหตุการณ์อื่น" })).getByRole("checkbox", { name: "Future.Event" })).toBeInTheDocument();
    expect(screen.getAllByText("เปิด 1/1")).toHaveLength(2);
    await user.click(screen.getByRole("checkbox", { name: "แจ้งซ่อมใหม่" }));
    expect(within(screen.getByRole("region", { name: "ระบบแจ้งซ่อม" })).getByText("เปิด 1/1")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "บันทึกเหตุการณ์แจ้งเตือน" }));
    await waitFor(() => expect(updateFleetLineGroupSubscriptions).toHaveBeenCalledWith(group.id, group.concurrencyToken, {
      "Fleet.RequestSubmitted": true, "Repair.Submitted": true, "MeetingRoom.BookingCreated": true, "Future.Event": false,
    }));
  });

  it("shows the source system alongside delivery history events", async () => {
    vi.mocked(getFleetLineGroupDeliveries).mockResolvedValue({
      items: [{ id: "delivery-1", eventType: "Repair.Submitted", destinationType: "GROUP", destinationIdMasked: "masked", requestId: "request-1", status: "Sent", attemptCount: 1, correlationId: "corr-1", createdAt: "2026-09-25T01:00:00Z" }],
      page: 1, pageSize: 20, totalItems: 1, totalPages: 1,
    });
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await user.click(await screen.findByRole("button", { name: "รายละเอียด" }));
    expect(await screen.findByText("แจ้งซ่อมใหม่")).toBeInTheDocument();
    expect(within(screen.getByRole("row", { name: /แจ้งซ่อมใหม่/ })).getByText("ระบบแจ้งซ่อม")).toBeInTheDocument();
  });

  it("confirms masked id, status and team before migrating a legacy group", async () => {
    const legacy = { ...group, id: "repair-it", displayName: "แจ้งซ่อม IT", module: "REPAIR_IT", status: "Active" as const };
    vi.mocked(getFleetLineGroups).mockResolvedValue([legacy]);
    vi.mocked(migrateRepairLineGroup).mockResolvedValue({ id: legacy.id, module: "CENTRAL", status: "Active", repairTeamCode: "IT", concurrencyToken: "new" });
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await user.click(await screen.findByRole("button", { name: "ย้ายเข้าส่วนกลาง" }));
    expect(screen.getByRole("dialog")).toHaveTextContent(legacy.groupIdMasked);
    expect(screen.getByRole("dialog")).toHaveTextContent("ทีมปลายทาง: IT");
    await user.click(screen.getByRole("button", { name: "ยืนยันการย้าย" }));
    await waitFor(() => expect(migrateRepairLineGroup).toHaveBeenCalledWith(legacy, "IT"));
  });

  it("offers a manual test for a disabled central group", async () => {
    vi.mocked(getFleetLineGroups).mockResolvedValue([{ ...group, module: "CENTRAL", status: "Disabled", repairTeamCode: "GENERAL" }]);
    vi.mocked(testFleetLineGroup).mockResolvedValue({ id: "test", status: "Sent", attemptCount: 1 });
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await user.click(await screen.findByRole("button", { name: "ทดสอบ" }));
    await user.click(screen.getByRole("button", { name: "ส่งทดสอบ" }));
    await waitFor(() => expect(testFleetLineGroup).toHaveBeenCalledWith(group.id, undefined));
    expect(await screen.findByRole("button", { name: "เปิดใช้งาน" })).toBeInTheDocument();
  });

  it("shows filter-aware empty state", async () => {
    vi.mocked(getFleetLineGroups).mockResolvedValue([]);
    renderFleet(<FleetLineGroupsPage />);
    const search = screen.getByLabelText("ค้นหาชื่อกลุ่ม");
    fireEvent.change(search, { target: { value: "ไม่พบ" } });

    expect(await screen.findByText("ไม่พบกลุ่มตามตัวกรอง")).toBeInTheDocument();
  });

  it("shows retry action when list loading fails", async () => {
    const user = userEvent.setup();
    vi.mocked(getFleetLineGroups).mockRejectedValue(new Error("network"));
    renderFleet(<FleetLineGroupsPage />);

    expect(await screen.findByText("โหลดรายการ LINE Group ไม่สำเร็จ")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "ลองใหม่" }));
    await waitFor(() => expect(getFleetLineGroups).toHaveBeenCalledTimes(2));
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

  it("saves the repair team on a central group", async () => {
    vi.mocked(getFleetLineGroups).mockResolvedValue([{ ...group, endpointUrl: "https://notify.example.test/send", clientId: "client", hasClientSecret: true }]);
    vi.mocked(updateFleetLineGroupEndpoint).mockResolvedValue({ id: group.id, status: "Pending", concurrencyToken: "token-2" });
    const user = userEvent.setup();
    renderFleet(<FleetLineGroupsPage />);
    await screen.findByText("กลุ่มงานยานพาหนะ");

    await user.click(screen.getByRole("button", { name: "แก้ไข" }));
    await user.click(screen.getByRole("combobox", { name: "ทีมแจ้งซ่อมที่รับ" }));
    await user.click(screen.getByRole("option", { name: "IT" }));
    await user.click(screen.getByRole("button", { name: "บันทึกการแก้ไข" }));
    await waitFor(() => expect(updateFleetLineGroupEndpoint).toHaveBeenCalledWith("group-1", expect.objectContaining({ repairTeamCode: "IT" })));
  });
});
