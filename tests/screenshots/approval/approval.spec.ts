import { test } from "@playwright/test";
import { recordCapture } from "../helpers/captureRegistry";
import { loadScreenshotConfig } from "../helpers/screenshotConfig";
import { loginAs, logout, navigateAndCapture, prepareBrowser } from "../helpers/screenshotActions";
import { leaveDetail, routes } from "../pages/routes";

test.describe("HOP approval screenshots", () => {
  test("head approval flow", async ({ page }) => {
    const config = loadScreenshotConfig();
    await prepareBrowser(page);
    await loginAs(page, config, "head");
    await navigateAndCapture(page, config, routes.pendingApprovals, {
      role: "head",
      module: "approval",
      step: "Pending approval list",
      file: "approval/head-approval-01-list.png",
      manual: "docs/manuals/phase1/04-Approval-User-Guide.md",
    });

    if (config.seed.leavePendingHeadId) {
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingHeadId), {
        role: "head",
        module: "approval",
        step: "Approval detail",
        file: "approval/head-approval-02-detail.png",
        manual: "docs/manuals/phase1/04-Approval-User-Guide.md",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingHeadId), {
        role: "head",
        module: "approval",
        step: "Approve action",
        file: "approval/head-approval-03-approve.png",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingHeadId), {
        role: "head",
        module: "approval",
        step: "Approve success",
        file: "approval/head-approval-04-success.png",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingHeadId), {
        role: "head",
        module: "approval",
        step: "Approval history",
        file: "approval/head-approval-05-history.png",
        manual: "docs/manuals/phase1/04-Approval-User-Guide.md",
      });
    } else {
      markSeedRequired("head", "approval", "Head approval detail/action/history", "approval/head-approval-02-detail.png");
    }
    await logout(page, config);
  });

  test("director approval flow", async ({ page }) => {
    const config = loadScreenshotConfig();
    await prepareBrowser(page);
    await loginAs(page, config, "director");
    await navigateAndCapture(page, config, routes.pendingApprovals, {
      role: "director",
      module: "approval",
      step: "Pending approval list",
      file: "approval/director-approval-01-list.png",
    });

    if (config.seed.leavePendingDirectorId) {
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingDirectorId), {
        role: "director",
        module: "approval",
        step: "Approval detail",
        file: "approval/director-approval-02-detail.png",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingDirectorId), {
        role: "director",
        module: "approval",
        step: "Final approve action",
        file: "approval/director-approval-03-final-approve.png",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leavePendingDirectorId), {
        role: "director",
        module: "approval",
        step: "Approval history",
        file: "approval/director-approval-04-history.png",
      });
    } else {
      markSeedRequired("director", "approval", "Director approval detail/action/history", "approval/director-approval-02-detail.png");
    }
    await logout(page, config);
  });
});

function markSeedRequired(role: string, module: string, step: string, file: string) {
  const now = new Date().toISOString();
  recordCapture({
    role,
    module,
    step,
    file,
    status: "Skipped",
    message: "Seed Required: configure a safe pending leave id in screenshot-users.json.",
    startedAt: now,
    completedAt: now,
    durationMs: 0,
  });
}
