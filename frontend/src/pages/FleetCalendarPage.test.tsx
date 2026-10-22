import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FleetCalendarPage } from "./FleetCalendarPage";
import { renderFleet } from "../test/renderFleet";
import * as api from "../api/fleetApi";
vi.mock("../api/fleetApi", () => ({ getFleetCalendar: vi.fn() }));
const event = (type: string, allDay = false) => ({
  id: `${type}:1`,
  eventType: type,
  title: `${type} event`,
  startAt: "2026-08-01T00:00:00Z",
  endAt: "2026-08-01T01:00:00Z",
  isAllDay: allDay,
  status: "ACTIVE",
  severity: "info",
  detailUrl: "/fleet/requests/1",
  metadata: {},
});
describe("FleetCalendarPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });
  it("renders all event type filters", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([]);
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-08-01", "/fleet/calendar");
    fireEvent.mouseDown(screen.getByLabelText("ประเภทกิจกรรม"));
    for (const label of [
      "คำขอใช้รถ",
      "การจัดรถและคนขับ",
      "การเดินทาง",
      "รถไม่พร้อมใช้งาน",
      "คนขับไม่พร้อมใช้งาน",
      "งานบำรุงรักษา",
      "เอกสารรถใกล้หมดอายุ",
      "คำขอยกเลิก",
    ])
      expect(await screen.findByRole("option", { name: label })).toBeInTheDocument();
  });
  it("renders assignment trip and unavailability", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([
      event("ASSIGNMENT"),
      event("TRIP"),
      event("VEHICLE_UNAVAILABILITY"),
    ]);
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-08-01", "/fleet/calendar");
    expect(await screen.findByText("ASSIGNMENT event")).toBeInTheDocument();
    expect(screen.getByText("TRIP event")).toBeInTheDocument();
  });
  it("renders all-day document expiry", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([
      event("VEHICLE_DOCUMENT_EXPIRY", true),
    ]);
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-08-01", "/fleet/calendar");
    expect(await screen.findByText(/ทั้งวัน/)).toBeInTheDocument();
  });
  it("filters event types through URL query", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([]);
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-08-01", "/fleet/calendar");
    fireEvent.mouseDown(screen.getByLabelText("ประเภทกิจกรรม"));
    fireEvent.click(await screen.findByRole("option", { name: "การจัดรถและคนขับ" }));
    await waitFor(() => expect(api.getFleetCalendar).toHaveBeenCalledTimes(2));
  });
  it("shows API error and retry", async () => {
    vi.mocked(api.getFleetCalendar).mockRejectedValue(new Error());
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-08-01", "/fleet/calendar");
    const retry = await screen.findByText("ลองใหม่", {}, { timeout: 4000 });
    vi.mocked(api.getFleetCalendar).mockResolvedValue([]);
    fireEvent.click(retry);
    await waitFor(() => expect(api.getFleetCalendar).toHaveBeenCalledTimes(2));
  });

  it.each(["week", "month", "agenda"])("shows an unfinished trip on its scheduled day in %s view", async (view) => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([{
      ...event("TRIP"), status: "IN_PROGRESS",
      startAt: "2026-10-04T02:00:00Z", endAt: "2026-10-04T06:00:00Z",
      metadata: { actualStartAt: "2026-10-04T02:14:16Z", actualEndAt: null, startMileage: 89286, endMileage: null },
    }]);
    renderFleet(<FleetCalendarPage />, `/fleet/calendar?date=2026-10-04&view=${view}`, "/fleet/calendar");
    const tripButtons = await screen.findAllByRole("button", { name: /TRIP event/ });
    expect(tripButtons).toHaveLength(1);
    fireEvent.click(tripButtons[0]);
    expect(screen.getByText("กำหนดเดินทางตามคำขอ")).toBeInTheDocument();
    expect(screen.getByText("เวลาเริ่มจริง")).toBeInTheDocument();
    expect(screen.getByText("ยังไม่บันทึกจบทริป")).toBeInTheDocument();
    expect(screen.getByText(/04\/10\/2569 09:14/)).toBeInTheDocument();
  });

  it.each(["week", "month"])("places a three-day trip on all three scheduled days in %s view", async (view) => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([{
      ...event("TRIP"), status: "IN_PROGRESS",
      startAt: "2026-10-05T01:00:00Z", endAt: "2026-10-07T10:00:00Z",
    }]);
    renderFleet(<FleetCalendarPage />, `/fleet/calendar?date=2026-10-05&view=${view}`, "/fleet/calendar");
    expect(await screen.findAllByRole("button", { name: /TRIP event/ })).toHaveLength(3);
  });

  it("keeps late actual completion separate from the scheduled calendar dates", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([{
      ...event("TRIP"), status: "COMPLETED",
      startAt: "2026-10-04T02:00:00Z", endAt: "2026-10-04T06:00:00Z",
      metadata: { ActualStartAt: "2026-10-04T02:14:16Z", ActualEndAt: "2026-10-22T07:00:00Z" },
    }]);
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-10-04", "/fleet/calendar");
    fireEvent.click(await screen.findByRole("button", { name: /TRIP event/ }));
    expect(screen.getByText(/04\/10\/2569 09:00 ถึง 04\/10\/2569 13:00/)).toBeInTheDocument();
    expect(screen.getByText(/22\/10\/2569 14:00/)).toBeInTheDocument();
    expect(screen.queryByText("ยังไม่บันทึกจบทริป")).not.toBeInTheDocument();
  });

  it.each(["week", "month", "agenda"])("previews who is travelling where on hover in %s view", async (view) => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([{
      ...event("REQUEST"), status: "PENDING_DRIVER_ACK",
      metadata: { requestNo: "VH-202610-0043", Requester: "ผู้ขอทดสอบ", Department: "กลุ่มงานการพยาบาล",
        destination: "โรงพยาบาลน่าน", purpose: "ประชุมประสานงาน", vehicle: "รถตู้ 01", registrationNumber: "กข 1234",
        driver: "คนขับทดสอบ", passengerCount: 3, patientName: "ชื่อผู้ป่วยที่ไม่แสดงในปฏิทิน" },
    }]);
    renderFleet(<FleetCalendarPage />, `/fleet/calendar?date=2026-08-01&view=${view}`, "/fleet/calendar");
    const button = await screen.findByRole("button", { name: /REQUEST event/ });
    fireEvent.mouseOver(button);
    const preview = within(await screen.findByRole("tooltip"));
    for (const value of ["VH-202610-0043", "ผู้ขอทดสอบ", "กลุ่มงานการพยาบาล", "โรงพยาบาลน่าน", "ประชุมประสานงาน",
      "รถตู้ 01 · กข 1234", "คนขับทดสอบ", "3 คน"]) expect(preview.getByText(value)).toBeInTheDocument();
    expect(preview.queryByText("ชื่อผู้ป่วยที่ไม่แสดงในปฏิทิน")).not.toBeInTheDocument();
    expect(api.getFleetCalendar).toHaveBeenCalledTimes(1);
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("tooltip")).not.toBeInTheDocument());
    fireEvent.click(button);
    expect(screen.getByText("รายละเอียด")).toBeInTheDocument();
  });

  it("previews unassigned requests on keyboard focus and keeps touch selection working", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([event("REQUEST")]);
    renderFleet(<FleetCalendarPage />, "/fleet/calendar?date=2026-08-01", "/fleet/calendar");
    const button = await screen.findByRole("button", { name: /REQUEST event/ });
    fireEvent.keyDown(document, { key: "Tab" });
    act(() => button.focus());
    const preview = within(await screen.findByRole("tooltip"));
    expect(preview.getByText("ยังไม่จัดรถ")).toBeInTheDocument();
    expect(preview.getByText("ยังไม่มอบหมาย")).toBeInTheDocument();
    act(() => button.blur());
    await waitFor(() => expect(screen.queryByRole("tooltip")).not.toBeInTheDocument());
    fireEvent.touchStart(button);
    fireEvent.touchEnd(button);
    fireEvent.click(button);
    expect(screen.getByText("รายละเอียด")).toBeInTheDocument();
  });
});
