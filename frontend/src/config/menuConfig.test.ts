import { describe, expect, it } from "vitest";
import { leaveNavigationBadgeCount, navigationModules } from "./menuConfig";
import { getPageTitle, getPageBreadcrumbs } from "./pageTitleConfig";

describe("repair navigation", () => {
  it("places repairs immediately after vehicle booking", () => {
    const index = navigationModules.findIndex(
      (m) => m.moduleId === "VehicleBooking",
    );
    expect(index).toBeGreaterThanOrEqual(0);
    expect(navigationModules[index + 1].moduleId).toBe("RepairManagement");
  });
  it("keeps the dashboard URL and new label when Fleet is absent", () => {
    const modules = navigationModules.filter(
      (m) => m.moduleId !== "VehicleBooking",
    );
    const repairs = modules.find((m) => m.moduleId === "RepairManagement");
    expect(
      repairs?.children.find((c) => c.path === "/dashboard/repair")?.label,
    ).toBe("Dashboard แจ้งซ่อม");
    expect(getPageTitle("/dashboard/repair").title).toBe("Dashboard แจ้งซ่อม");
    expect(getPageBreadcrumbs("/dashboard/repair").slice(-1)[0]?.label).toBe(
      "Dashboard แจ้งซ่อม",
    );
  });
});

describe("Leave task navigation badge", () => {
  it("shows pending approval count only on the actionable approval menu", () => {
    expect(leaveNavigationBadgeCount("/leave/pending-approvals", 4)).toBe(4);
    expect(leaveNavigationBadgeCount("/leave", 4)).toBe(0);
    expect(leaveNavigationBadgeCount("/leave/cancellations", 4)).toBe(0);
  });

  it("uses zero until the count query has loaded and preserves values above 99", () => {
    expect(leaveNavigationBadgeCount("/leave/pending-approvals")).toBe(0);
    expect(leaveNavigationBadgeCount("/leave/pending-approvals", 120)).toBe(
      120,
    );
  });
});
