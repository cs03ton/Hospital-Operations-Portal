import { fireEvent, screen, waitFor } from "@testing-library/react";
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
});
