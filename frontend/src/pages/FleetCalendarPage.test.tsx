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
  it("renders all event legends", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([]);
    renderFleet(<FleetCalendarPage />);
    for (const label of [
      "คำขอ",
      "Assignment",
      "Trip",
      "รถไม่พร้อม",
      "คนขับไม่พร้อม",
      "Maintenance",
      "เอกสารหมดอายุ",
      "ยกเลิกคำขอ",
    ])
      expect(screen.getByText(label)).toBeInTheDocument();
  });
  it("renders assignment trip and unavailability", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([
      event("ASSIGNMENT"),
      event("TRIP"),
      event("VEHICLE_UNAVAILABILITY"),
    ]);
    renderFleet(<FleetCalendarPage />);
    expect(await screen.findByText("ASSIGNMENT event")).toBeInTheDocument();
    expect(screen.getByText("TRIP event")).toBeInTheDocument();
  });
  it("renders all-day document expiry", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([
      event("VEHICLE_DOCUMENT_EXPIRY", true),
    ]);
    renderFleet(<FleetCalendarPage />);
    expect(await screen.findByText(/ทั้งวัน/)).toBeInTheDocument();
  });
  it("filters event types through URL query", async () => {
    vi.mocked(api.getFleetCalendar).mockResolvedValue([]);
    renderFleet(<FleetCalendarPage />);
    fireEvent.click(screen.getByLabelText("Assignment"));
    await waitFor(() => expect(api.getFleetCalendar).toHaveBeenCalledTimes(2));
  });
  it("shows API error and retry", async () => {
    vi.mocked(api.getFleetCalendar)
      .mockRejectedValueOnce(new Error())
      .mockResolvedValue([]);
    renderFleet(<FleetCalendarPage />);
    fireEvent.click(await screen.findByText("ลองใหม่"));
    await waitFor(() => expect(api.getFleetCalendar).toHaveBeenCalledTimes(2));
  });
});
