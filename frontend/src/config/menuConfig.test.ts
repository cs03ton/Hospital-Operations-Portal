import { describe, expect, it } from "vitest";
import { leaveNavigationBadgeCount } from "./menuConfig";

describe("Leave task navigation badge", () => {
  it("shows pending approval count only on the actionable approval menu", () => {
    expect(leaveNavigationBadgeCount("/leave/pending-approvals", 4)).toBe(4);
    expect(leaveNavigationBadgeCount("/leave", 4)).toBe(0);
    expect(leaveNavigationBadgeCount("/leave/cancellations", 4)).toBe(0);
  });

  it("uses zero until the count query has loaded and preserves values above 99", () => {
    expect(leaveNavigationBadgeCount("/leave/pending-approvals")).toBe(0);
    expect(leaveNavigationBadgeCount("/leave/pending-approvals", 120)).toBe(120);
  });
});
