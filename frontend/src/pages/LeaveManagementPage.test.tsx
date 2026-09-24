import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import type { LeaveRequest } from "../api/leaveApi";
import { LeaveMobileRequestCard } from "./LeaveManagementPage";

const request = {
  id: "request-1",
  requestNumber: "LV-202609-008",
  userId: "user-1",
  fullname: "ผู้ขอลา",
  leaveTypeId: "type-1",
  leaveTypeName: "พักผ่อน",
  startDate: "2026-09-18",
  endDate: "2026-09-18",
  durationType: "FULL_DAY",
  totalDays: 1,
  reason: "พักผ่อน",
  status: "Approved",
  currentApproverName: "หัวหน้างาน",
  currentStatusLabel: "อนุมัติแล้ว",
  trackingMessage: "",
  createdAt: "2026-09-14T07:55:39Z",
  revisionCount: 0,
} satisfies LeaveRequest;

describe("mobile leave request card", () => {
  it.each(["Draft", "ReturnedForRevision", "Approved"])("keeps the detail action reachable for %s requests", (status) => {
    render(<MemoryRouter><LeaveMobileRequestCard item={{ ...request, status }} /></MemoryRouter>);

    expect(screen.getByText("LV-202609-008")).toBeInTheDocument();
    expect(screen.getByText(/หัวหน้างาน/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "จัดการ / ดูรายละเอียด" })).toHaveAttribute("href", "/leave/request-1");
  });
});
