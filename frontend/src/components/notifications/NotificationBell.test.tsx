// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { getDashboardActionItems } from "../../api/dashboardActionApi";
import { getNotificationBadge, getNotificationCenterItems } from "../../services/notificationService";
import { NotificationBell } from "./NotificationBell";

vi.mock("../../api/dashboardActionApi", () => ({ getDashboardActionItems: vi.fn() }));
vi.mock("../../services/notificationService", () => ({ getNotificationBadge: vi.fn(), getNotificationCenterItems: vi.fn() }));

describe("NotificationBell", () => {
  it("separates live work from general updates and keeps the unread badge", async () => {
    vi.mocked(getDashboardActionItems).mockResolvedValue({
      leaveCount: 1, fleetCount: 0, repairCount: 0, meetingTodayCount: 1,
      items: [{ id: "leave-1", module: "leave", title: "คำขอลารออนุมัติ", detail: "LV-1 · สมชาย", path: "/leave/1", createdAt: "2026-10-20T01:00:00Z" }],
      meetingBookings: [],
    });
    vi.mocked(getNotificationBadge).mockResolvedValue(2);
    vi.mocked(getNotificationCenterItems).mockResolvedValue({
      items: [{ id: "update-1", type: "Information", title: "จองห้องประชุมสำเร็จ", message: "ยืนยันแล้ว", createdAt: "2026-10-20T01:00:00Z", unread: true, path: "/meeting-rooms/bookings/1", category: "MeetingRoom", priority: "Information", notificationType: "Information" }],
      totalItems: 1, page: 1, pageSize: 12, totalPages: 1,
    });
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(<QueryClientProvider client={client}><MemoryRouter><NotificationBell /></MemoryRouter></QueryClientProvider>);
    fireEvent.click(screen.getByRole("button", { name: "เปิดรายการแจ้งเตือน" }));
    await waitFor(() => expect(screen.getByText("คำขอลารออนุมัติ")).toBeInTheDocument());
    expect(screen.queryByText("จองห้องประชุมสำเร็จ")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("tab", { name: /อัปเดตทั่วไป/ }));
    await waitFor(() => expect(screen.getByText("จองห้องประชุมสำเร็จ")).toBeInTheDocument());
    expect(screen.queryByText("คำขอลารออนุมัติ")).not.toBeInTheDocument();
    expect(getNotificationCenterItems).toHaveBeenCalledWith({ filter: "hub-updates", pageSize: 12 });
  });
});
