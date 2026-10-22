import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminAnnouncementFormPage } from "./AdminAnnouncementFormPage";
import { renderFleet } from "../test/renderFleet";
import * as api from "../api/announcementsApi";
import { AnnouncementGroupDeliveryPanel } from "../components/announcements/AnnouncementGroupDeliveryPanel";

const state = vi.hoisted(() => ({ canSend: true, canView: false, showError: vi.fn(), showSuccess: vi.fn(), showWarning: vi.fn() }));
vi.mock("../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: () => state.canSend, hasAnyPermission: () => state.canView }) }));
vi.mock("../hooks/useNotification", () => ({ useNotification: () => state }));
vi.mock("../api/adminApi", () => ({ getDepartments: async () => [], getPermissions: async () => [], getRoles: async () => [], getUsers: async () => [] }));
vi.mock("../api/announcementsApi", () => ({ createAnnouncement: vi.fn(), updateAnnouncement: vi.fn(), getAdminAnnouncementDetail: vi.fn(),
  getAnnouncementCategories: vi.fn(), getAnnouncementLineGroupOptions: vi.fn(), getAnnouncementLineGroupDeliveries: vi.fn(),
  deleteAnnouncementFile: vi.fn(), deleteAnnouncementImage: vi.fn(), reorderAnnouncementImages: vi.fn(),
  uploadAnnouncementAttachments: vi.fn(), uploadAnnouncementImage: vi.fn() }));

const groups = [{ id: "a", displayName: "กลุ่ม A", status: "Active" }, { id: "b", displayName: "กลุ่ม B", status: "Active" }];
const detail: api.AnnouncementDetail = { id: "news", title: "ประกาศเดิม", body: "เนื้อหา", summary: "สรุป", status: "Draft", priority: "Normal",
  isFeatured: false, showAsPopup: false, showAsBanner: false, requiresAcknowledgement: false, isRead: false, isAcknowledged: false,
  createdAt: "2026-10-22T01:00:00Z", viewCount: 0, acknowledgedCount: 0, inAppRecipientCount: 0, lineEligibleRecipientCount: 0,
  lineQueuedCount: 0, lineSentCount: 0, lineFailedCount: 0,
  targets: [], files: [], images: [], notifyInApp: true, notifyViaLine: false, notifyViaLineGroup: true,
  lineGroupIds: ["a"], lineGroups: [groups[0]] };

describe("Announcement LINE group selection", () => {
  beforeEach(() => {
    vi.resetAllMocks(); state.canSend = true; state.canView = false;
    vi.mocked(api.getAnnouncementCategories).mockResolvedValue([]);
    vi.mocked(api.getAnnouncementLineGroupOptions).mockResolvedValue(groups);
    vi.mocked(api.getAnnouncementLineGroupDeliveries).mockResolvedValue([]);
    vi.mocked(api.createAnnouncement).mockResolvedValue(detail);
    vi.mocked(api.updateAnnouncement).mockResolvedValue(detail);
  });
  const renderNew = () => renderFleet(<AdminAnnouncementFormPage />, "/admin/announcements/new", "/admin/announcements/new");

  it("keeps group notifications off by default and requires a group when switched on", async () => {
    renderNew();
    const toggle = screen.getByRole("checkbox", { name: "LINE กลุ่ม (หมอพร้อม)" });
    expect(toggle).not.toBeChecked();
    fireEvent.click(toggle);
    fireEvent.click(screen.getByRole("button", { name: "บันทึกข้อมูล" }));
    expect(state.showError).toHaveBeenCalledWith("กรุณาเลือกกลุ่ม LINE หมอพร้อมอย่างน้อยหนึ่งกลุ่ม");
    expect(api.createAnnouncement).not.toHaveBeenCalled();
  });

  it("selects multiple groups independently from personal LINE and includes only IDs in the draft payload", async () => {
    renderNew();
    fireEvent.change(screen.getByLabelText(/หัวข้อประกาศ/), { target: { value: "ข่าวใหม่" } });
    fireEvent.change(screen.getByLabelText(/เนื้อหาประกาศ/), { target: { value: "เนื้อหาข่าว" } });
    fireEvent.click(screen.getByRole("checkbox", { name: "LINE กลุ่ม (หมอพร้อม)" }));
    const input = screen.getByRole("combobox", { name: /กลุ่ม LINE หมอพร้อมที่ต้องการแจ้ง/ });
    fireEvent.mouseDown(input);
    fireEvent.click(await screen.findByRole("option", { name: "กลุ่ม A" }));
    fireEvent.click(await screen.findByRole("option", { name: "กลุ่ม B" }));
    fireEvent.keyDown(input, { key: "Escape" });
    fireEvent.click(screen.getByRole("button", { name: "บันทึกข้อมูล" }));
    await waitFor(() => expect(api.createAnnouncement).toHaveBeenCalledWith(expect.objectContaining({ notifyViaLineGroup: true,
      lineGroupIds: ["a", "b"], notifyViaLine: false, notifyInApp: true })));
    expect(api.getAnnouncementLineGroupDeliveries).not.toHaveBeenCalled();
  });

  it("restores the groups selected on an existing announcement", async () => {
    vi.mocked(api.getAdminAnnouncementDetail).mockResolvedValue(detail);
    renderFleet(<AdminAnnouncementFormPage />, "/admin/announcements/news/edit", "/admin/announcements/:id/edit");
    expect(await screen.findByRole("checkbox", { name: "LINE กลุ่ม (หมอพร้อม)" })).toBeChecked();
    expect(screen.getByText("กลุ่ม A")).toBeInTheDocument();
  });

  it("warns that a restricted announcement summary will be visible to every group member", async () => {
    vi.mocked(api.getAdminAnnouncementDetail).mockResolvedValue({ ...detail, targets: [{ targetType: "Role", targetValue: "Staff" }] });
    renderFleet(<AdminAnnouncementFormPage />, "/admin/announcements/news/edit", "/admin/announcements/:id/edit");
    expect(await screen.findByText(/สมาชิกทุกคนในกลุ่ม LINE จะเห็นชื่อและสรุปประกาศ/)).toBeInTheDocument();
  });

  it("does not fetch group destinations when the editor lacks permission to send LINE", () => {
    state.canSend = false; renderNew();
    expect(screen.getByRole("checkbox", { name: "LINE กลุ่ม (หมอพร้อม)" })).toBeDisabled();
    expect(api.getAnnouncementLineGroupOptions).not.toHaveBeenCalled();
  });

  it("shows per-group delivery results only to permitted users", async () => {
    state.canView = true;
    vi.mocked(api.getAnnouncementLineGroupDeliveries).mockResolvedValue([{ id: "delivery", destinationId: "a", displayName: "กลุ่ม A", status: "Sent",
      attemptCount: 1, createdAt: "2026-10-22T01:00:00Z", sentAt: "2026-10-22T01:01:00Z" }]);
    renderFleet(<AnnouncementGroupDeliveryPanel announcementId="news" />);
    expect(await screen.findByText("กลุ่ม A")).toBeInTheDocument();
    expect(screen.getByText("ส่งสำเร็จ")).toBeInTheDocument();
  });
});
