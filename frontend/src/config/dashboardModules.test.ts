import { describe, expect, it } from "vitest";
import { getDashboardModule, getDashboardModuleMetricLabel, getVisibleDashboardModules } from "./dashboardModules";
import type { AuthUser } from "../types/auth";

function user(role: string, permissions: string[] = []): AuthUser {
  return { id: crypto.randomUUID(), username: role.toLowerCase(), fullname: role, role, permissions } as AuthUser;
}

describe("Dashboard Hub module visibility", () => {
  it.each(["Staff", "FleetDriver", "Director", "Admin", "SuperAdmin"])("shows Fleet card to %s", (role) => {
    expect(getVisibleDashboardModules(user(role)).map(item => item.key)).toContain("vehicle");
  });

  it("shows Fleet card to an authenticated user without Fleet permissions", () => {
    expect(getVisibleDashboardModules(user("GeneralUser", [])).map(item => item.key)).toContain("vehicle");
  });

  it("does not expose modules before authentication", () => {
    expect(getVisibleDashboardModules(null)).toEqual([]);
  });

  it("labels the Fleet metric as the current driver's active jobs", () => {
    const fleetModule = getDashboardModule("vehicle")!;
    const driver = user("FleetDriver", ["FleetDriver.ViewOwnJobs"]);

    expect(getDashboardModuleMetricLabel(fleetModule, driver)).toBe("งานขับรถของฉันที่ยังดำเนินการอยู่");
  });

  it("keeps the general Fleet metric label for non-drivers", () => {
    const fleetModule = getDashboardModule("vehicle")!;

    expect(getDashboardModuleMetricLabel(fleetModule, user("Staff"))).toBe("คำขอใช้รถของฉันที่ยังดำเนินการอยู่");
  });
});
