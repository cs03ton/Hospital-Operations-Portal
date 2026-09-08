import { describe, expect, it } from "vitest";
import { fleetNavigationBadgeCount, visibleFleetNavigationItems } from "./fleetNavigation";

const labels = (permissions: string[], role?: string) => visibleFleetNavigationItems(permissions, role).map(item => item.label);

describe("Fleet permission-based navigation", () => {
  it("shows the fleet calendar to every authenticated user", () => {
    expect(labels([])).toEqual(["ปฏิทินรถ"]);
  });

  it("shows requester navigation without operational menus", () => {
    expect(labels(["FleetRequest.ViewOwn", "FleetCalendar.View"])).toEqual(["Dashboard รถ", "คำขอใช้รถ", "ปฏิทินรถ"]);
  });

  it("hides requester navigation for a fleet driver even when the base staff role grants requester permissions", () => {
    expect(labels(
      ["FleetRequest.ViewOwn", "FleetRequest.Create", "FleetDriver.ViewOwnJobs", "FleetCalendar.View"],
      "พนักงานขับรถ",
    )).toEqual(["Dashboard รถ", "งานขับรถของฉัน", "ปฏิทินรถ"]);
  });

  it("unions dispatcher and driver capabilities without duplicates", () => {
    const result = labels(["FleetDispatch.View", "FleetDriver.ViewOwnJobs", "FleetCalendar.View"]);
    expect(result).toEqual(["Dashboard รถ", "งานขับรถของฉัน", "คิวจัดรถ", "ปฏิทินรถ"]);
    expect(new Set(result).size).toBe(result.length);
  });

  it("keeps the standard order for fleet administrators", () => {
    expect(labels(["FleetRequest.ViewOwn", "FleetDriver.ViewOwnJobs", "FleetDispatch.View", "FleetAdminReview.Approve", "FleetDirector.Approve", "FleetCalendar.View", "FleetReport.View", "FleetSettings.Manage"])).toEqual([
      "Dashboard รถ", "คำขอใช้รถ", "งานขับรถของฉัน", "คิวจัดรถ", "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", "ปฏิทินรถ", "รายงาน", "ตั้งค่า",
    ]);
  });

  it("maps scoped dashboard counts to task menus without a separate badge source", () => {
    const badges = { dispatchQueue: 4, reviewQueue: 2, approvalQueue: 3, myDriverJobs: 7 };
    expect(fleetNavigationBadgeCount("/fleet/my-trips", badges)).toBe(7);
    expect(fleetNavigationBadgeCount("/fleet/dispatch", badges)).toBe(4);
    expect(fleetNavigationBadgeCount("/fleet/requests/review", badges)).toBe(4);
    expect(fleetNavigationBadgeCount("/fleet/approvals", badges)).toBe(5);
    expect(fleetNavigationBadgeCount("/fleet/calendar", badges)).toBe(0);
  });

  it("exposes a delegated Fleet permission through the same navigation union", () => {
    expect(labels(["FleetDirector.Approve"])).toEqual(["Dashboard รถ", "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", "ปฏิทินรถ"]);
  });
});
