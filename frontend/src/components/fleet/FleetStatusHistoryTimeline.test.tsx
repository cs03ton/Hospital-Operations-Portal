import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FleetStatusHistoryTimeline } from "./FleetStatusHistoryTimeline";
import type { FleetHistory } from "../../api/fleetApi";

const history = (id: string, createdAt: string, toStatus = "DRAFT", action = "Fleet.RequestCreated"): FleetHistory =>
  ({ id, createdAt, toStatus, action, actorUserId: id, actorName: `ผู้ทำ ${id}` });

describe("Fleet status history timeline", () => {
  it("sorts newest first, preserves ties and never mutates the API array", () => {
    const rows = [history("old", "2026-10-04T01:00:00Z"), history("tie1", "2026-10-04T02:00:00Z"), history("tie2", "2026-10-04T02:00:00Z")];
    render(<FleetStatusHistoryTimeline histories={rows} />);
    const items = screen.getAllByRole("listitem");
    expect(items.map(item => within(item).getByText(/ผู้ทำ/).textContent)).toEqual(["ผู้ทำ tie1", "ผู้ทำ tie2", "ผู้ทำ old"]);
    expect(rows.map(row => row.id)).toEqual(["old", "tie1", "tie2"]);
    expect(within(items[0]).getByText("04/10/2569")).toBeInTheDocument();
    expect(within(items[0]).getByText("09:00 น.")).toBeInTheDocument();
    expect(screen.getByRole("list").tagName).toBe("OL");
  });
  it("shows all statuses, actual reasons and missing actors without inventing roles", () => {
    const statuses = ["PENDING_DIRECTOR", "READY", "IN_PROGRESS", "COMPLETED", "RETURNED", "REJECTED", "CANCELLED", "ABORTED", "APPROVED"];
    const reason = "เหตุผลยาวมาก".repeat(20);
    render(<FleetStatusHistoryTimeline histories={statuses.map((status, index) => ({ ...history(String(index), "2026-10-04T02:00:00Z", status, "Future.Unknown"), reason: index === 0 ? reason : null, actorName: index === 0 ? " " : `ผู้ทำ ${index}` }))} />);
    expect(screen.getAllByRole("listitem")).toHaveLength(statuses.length);
    for (const label of ["รอผู้อำนวยการอนุมัติ", "พร้อมเดินทาง", "กำลังปฏิบัติงาน", "เสร็จสิ้น", "ส่งกลับแก้ไข", "ไม่รับคำขอ", "ยกเลิก", "ยุติการเดินทาง", "อนุมัติแล้ว"]) expect(screen.getByText(label)).toBeInTheDocument();
    expect(screen.getAllByText("บันทึกการเปลี่ยนสถานะ")).toHaveLength(statuses.length);
    expect(screen.getByText(reason)).toBeInTheDocument();
    expect(screen.getByText("ไม่ระบุผู้ดำเนินการ")).toBeInTheDocument();
  });
  it.each([["Fleet.RequestCreated", "สร้างคำขอใช้รถ"], ["Fleet.Assigned", "จัดรถและคนขับ"], ["Fleet.DIRECTOR_APPROVE", "ผู้อำนวยการอนุมัติ"], ["Fleet.DRIVER_ACCEPT", "คนขับรับทราบ"], ["Fleet.TRIP_START", "เริ่มเดินทาง"], ["Fleet.TRIP_COMPLETE", "จบการเดินทาง"]])("labels %s from its real action", (action, label) => {
    render(<FleetStatusHistoryTimeline histories={[history("one", "2026-10-04T02:00:00Z", "READY", action)]} />);
    expect(screen.getByText(label)).toBeInTheDocument();
    expect(screen.getByRole("listitem").querySelectorAll('svg:not([aria-hidden="true"])')).toHaveLength(0);
  });
  it("keeps the empty state", () => {
    render(<FleetStatusHistoryTimeline histories={[]} />);
    expect(screen.getByText("ยังไม่มีประวัติสถานะ")).toBeInTheDocument(); expect(screen.queryByRole("list")).not.toBeInTheDocument();
  });
});
