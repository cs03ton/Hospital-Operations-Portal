import { describe, expect, it } from "vitest";
import { getDashboardHubMetrics, getDashboardModule, getDashboardModuleMetricLabel, getVisibleDashboardModules } from "./dashboardModules";
import type { DashboardSummary } from "../api/adminApi";
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

  it("shows only approval queues to a Director on both Hub cards", () => {
    const director = user("Director", ["LeaveRequest.ViewPendingApproval", "LeaveCancellation.ApproveCurrentStep", "FleetDirector.Approve"]);
    const summary = {
      pendingLeaveApprovals: 2,
      pendingLeaveCancellationApprovals: 3,
      pendingFleetDirectorRequests: 4,
      pendingFleetReviewRequests: 0,
      myLeaveRequestsPending: 8,
      myLeaveCancellationRequestsPending: 9,
      myActiveFleetRequests: 10,
    } as DashboardSummary;

    expect(getDashboardHubMetrics(getDashboardModule("leave")!, director, summary).map(metric => metric.value)).toEqual([2, 3]);
    expect(getDashboardHubMetrics(getDashboardModule("vehicle")!, director, summary).map(metric => metric.value)).toEqual([4]);
  });

  it("shows both fleet queues and own active requests to a non-Director approver", () => {
    const reviewer = user("Admin", ["FleetAdminReview.Approve", "FleetDirector.Approve", "FleetRequest.ViewOwn", "LeaveRequest.ViewPendingApproval", "LeaveCancellation.ApproveCurrentStep", "LeaveRequest.ViewOwn", "LeaveCancellation.ViewOwn"]);
    const summary = {
      pendingLeaveApprovals: 1,
      pendingLeaveCancellationApprovals: 2,
      pendingFleetReviewRequests: 3,
      pendingFleetDirectorRequests: 4,
      myLeaveRequestsPending: 5,
      myLeaveCancellationRequestsPending: 6,
      myActiveFleetRequests: 7,
    } as DashboardSummary;

    expect(getDashboardHubMetrics(getDashboardModule("leave")!, reviewer, summary).map(metric => metric.value)).toEqual([1, 2, 5, 6]);
    expect(getDashboardHubMetrics(getDashboardModule("vehicle")!, reviewer, summary).map(metric => metric.value)).toEqual([7, 7]);
    expect(getDashboardHubMetrics(getDashboardModule("vehicle")!, reviewer, summary)[0].to).toBe("/fleet/approvals");
  });
});
