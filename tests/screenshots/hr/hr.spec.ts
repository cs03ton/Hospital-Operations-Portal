import { test } from "@playwright/test";
import { loadScreenshotConfig } from "../helpers/screenshotConfig";
import { loginAs, logout, navigateAndCapture, prepareBrowser } from "../helpers/screenshotActions";
import { routes } from "../pages/routes";

test.describe("HOP HR screenshots", () => {
  test("hr dashboard, report, export, and holiday pages", async ({ page }) => {
    const config = loadScreenshotConfig();
    await prepareBrowser(page);
    await loginAs(page, config, "hr");
    await navigateAndCapture(page, config, routes.dashboard, {
      role: "hr",
      module: "hr",
      step: "HR dashboard",
      file: "hr/hr-dashboard-01-home.png",
    });
    await navigateAndCapture(page, config, routes.leaveList, {
      role: "hr",
      module: "hr",
      step: "Report dashboard/list",
      file: "hr/hr-report-01-dashboard.png",
      manual: "docs/manuals/phase1/05-HR-User-Guide.md",
    });
    await navigateAndCapture(page, config, routes.leaveReports, {
      role: "hr",
      module: "hr",
      step: "Leave report",
      file: "hr/hr-report-02-leave-report.png",
      manual: "docs/manuals/phase1/05-HR-User-Guide.md",
    });
    await navigateAndCapture(page, config, routes.leaveReports, {
      role: "hr",
      module: "hr",
      step: "Export report",
      file: "hr/hr-report-03-export.png",
    });
    await navigateAndCapture(page, config, routes.leaveHolidays, {
      role: "hr",
      module: "hr",
      step: "Holiday list",
      file: "hr/hr-holiday-01-list.png",
      manual: "docs/manuals/phase1/05-HR-User-Guide.md",
    });
    await logout(page, config);
  });
});
