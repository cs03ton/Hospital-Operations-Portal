import { test } from "@playwright/test";
import { loadScreenshotConfig, type RoleName } from "../helpers/screenshotConfig";
import { loginAs, logout, navigateAndCapture, prepareBrowser } from "../helpers/screenshotActions";
import { routes } from "../pages/routes";

const targets: { role: RoleName; file: string; manual?: string }[] = [
  { role: "user", file: "dashboard/user-dashboard-01-home.png", manual: "docs/manuals/phase1/02-Dashboard-User-Guide.md" },
  { role: "head", file: "dashboard/head-dashboard-01-home.png", manual: "docs/manuals/phase1/02-Dashboard-User-Guide.md" },
  { role: "director", file: "dashboard/director-dashboard-01-home.png", manual: "docs/manuals/phase1/07-Executive-User-Guide.md" },
  { role: "hr", file: "dashboard/hr-dashboard-01-home.png" },
  { role: "superadmin", file: "dashboard/superadmin-dashboard-01-home.png" },
];

test.describe("HOP dashboard screenshots", () => {
  for (const target of targets) {
    test(`${target.role} dashboard`, async ({ page }) => {
      const config = loadScreenshotConfig();
      await prepareBrowser(page);
      await loginAs(page, config, target.role);
      await navigateAndCapture(page, config, routes.dashboard, {
        role: target.role,
        module: "dashboard",
        step: `${target.role} dashboard home`,
        file: target.file,
        manual: target.manual,
      });
      await logout(page, config);
    });
  }
});
