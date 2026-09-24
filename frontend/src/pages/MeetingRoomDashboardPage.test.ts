import { describe, expect, it } from "vitest";
import { roomStatus } from "./MeetingRoomDashboardPage";
import type { MeetingBooking, MeetingRoom } from "../api/meetingRoomApi";

const now = Date.parse("2026-09-24T02:00:00Z");
const room = { id: "room-1", isActive: true } as MeetingRoom;
const booking = (startAt: string, endAt: string, status: MeetingBooking["status"] = "Confirmed") => ({ roomId: room.id, startAt, endAt, status }) as MeetingBooking;

describe("roomStatus", () => {
  it("prioritizes inactive rooms and ignores cancelled bookings", () => {
    const current = booking("2026-09-24T01:00:00Z", "2026-09-24T03:00:00Z");
    expect(roomStatus({ ...room, isActive: false }, [current], now)).toBe("inactive");
    expect(roomStatus(room, [{ ...current, status: "Cancelled" }], now)).toBe("free");
  });
  it("uses inclusive start and exclusive end", () => {
    expect(roomStatus(room, [booking("2026-09-24T02:00:00Z", "2026-09-24T03:00:00Z")], now)).toBe("busy");
    expect(roomStatus(room, [booking("2026-09-24T01:00:00Z", "2026-09-24T02:00:00Z")], now)).toBe("free");
  });
  it("marks a room as starting soon within 30 minutes", () => {
    expect(roomStatus(room, [booking("2026-09-24T02:30:00Z", "2026-09-24T03:00:00Z")], now)).toBe("soon");
    expect(roomStatus(room, [booking("2026-09-24T02:30:01Z", "2026-09-24T03:00:00Z")], now)).toBe("free");
  });
});
