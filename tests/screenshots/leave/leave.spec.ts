import { test } from "@playwright/test";
import { recordCapture } from "../helpers/captureRegistry";
import { loadScreenshotConfig } from "../helpers/screenshotConfig";
import { loginAs, logout, navigateAndCapture, prepareBrowser } from "../helpers/screenshotActions";
import { leaveDetail, routes } from "../pages/routes";

test.describe("HOP user leave screenshots", () => {
  test("user leave flow pages", async ({ page }) => {
    const config = loadScreenshotConfig();
    await prepareBrowser(page);
    await loginAs(page, config, "user");

    await navigateAndCapture(page, config, routes.leaveList, {
      role: "user",
      module: "leave",
      step: "Leave list",
      file: "leave/user-leave-01-list.png",
    });
    await navigateAndCapture(page, config, routes.leaveCreate, {
      role: "user",
      module: "leave",
      step: "Create leave form",
      file: "leave/user-leave-02-create.png",
      manual: "docs/manuals/phase1/03-Leave-User-Guide.md",
    });
    await navigateAndCapture(page, config, routes.leaveCreate, {
      role: "user",
      module: "leave",
      step: "Leave type field",
      file: "leave/user-leave-03-type.png",
    });
    await navigateAndCapture(page, config, routes.leaveCreate, {
      role: "user",
      module: "leave",
      step: "Leave date fields",
      file: "leave/user-leave-04-date.png",
    });

    if (config.seed.leaveDraftId) {
      await navigateAndCapture(page, config, leaveDetail(config.seed.leaveDraftId), {
        role: "user",
        module: "leave",
        step: "Upload attachment",
        file: "leave/user-leave-05-upload.png",
        manual: "docs/manuals/phase1/03-Leave-User-Guide.md",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leaveDraftId), {
        role: "user",
        module: "leave",
        step: "Review leave draft",
        file: "leave/user-leave-06-review.png",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leaveDraftId), {
        role: "user",
        module: "leave",
        step: "Submit leave",
        file: "leave/user-leave-07-submit.png",
      });
      await navigateAndCapture(page, config, leaveDetail(config.seed.leaveDraftId), {
        role: "user",
        module: "leave",
        step: "Submit success",
        file: "leave/user-leave-08-success.png",
      });
    } else {
      markSeedRequired("user", "leave", "Draft leave-dependent steps", "leave/user-leave-05-upload.png");
    }

    const detailId = config.seed.leavePendingHeadId || config.seed.leaveDraftId;
    if (detailId) {
      await navigateAndCapture(page, config, leaveDetail(detailId), {
        role: "user",
        module: "leave",
        step: "Pending leave",
        file: "leave/user-leave-09-pending.png",
      });
      await navigateAndCapture(page, config, leaveDetail(detailId), {
        role: "user",
        module: "leave",
        step: "Leave detail",
        file: "leave/user-leave-10-detail.png",
        manual: "docs/manuals/phase1/03-Leave-User-Guide.md",
      });
      await navigateAndCapture(page, config, leaveDetail(detailId), {
        role: "user",
        module: "leave",
        step: "Leave PDF",
        file: "leave/user-leave-11-pdf.png",
        manual: "docs/manuals/phase1/03-Leave-User-Guide.md",
      });
    } else {
      markSeedRequired("user", "leave", "Pending/detail/PDF leave steps", "leave/user-leave-09-pending.png");
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
    message: "Seed Required: configure a safe test leave id in screenshot-users.json.",
    startedAt: now,
    completedAt: now,
    durationMs: 0,
  });
}
