import { test } from "@playwright/test";
import { recordCapture } from "../helpers/captureRegistry";
import { loadScreenshotConfig } from "../helpers/screenshotConfig";
import { loginAs, logout, navigateAndCapture, prepareBrowser } from "../helpers/screenshotActions";
import { departmentEdit, rolePermissions, routes, userEdit } from "../pages/routes";

test.describe("HOP SuperAdmin screenshots", () => {
  test("superadmin management pages", async ({ page }) => {
    const config = loadScreenshotConfig();
    await prepareBrowser(page);
    await loginAs(page, config, "superadmin");
    await navigateAndCapture(page, config, routes.dashboard, {
      role: "superadmin",
      module: "superadmin",
      step: "Dashboard",
      file: "superadmin/superadmin-dashboard-01-home.png",
    });
    await navigateAndCapture(page, config, routes.userList, {
      role: "superadmin",
      module: "superadmin",
      step: "User list",
      file: "superadmin/superadmin-user-01-list.png",
      manual: "docs/manuals/phase1/06-Admin-User-Guide.md",
    });
    await navigateAndCapture(page, config, routes.userCreate, {
      role: "superadmin",
      module: "superadmin",
      step: "Create user",
      file: "superadmin/superadmin-user-02-create.png",
    });
    if (config.seed.testUserId) {
      await navigateAndCapture(page, config, userEdit(config.seed.testUserId), {
        role: "superadmin",
        module: "superadmin",
        step: "Edit user",
        file: "superadmin/superadmin-user-03-edit.png",
      });
    } else {
      markSeedRequired("Edit user", "superadmin/superadmin-user-03-edit.png");
    }
    await navigateAndCapture(page, config, routes.roles, {
      role: "superadmin",
      module: "superadmin",
      step: "Role list",
      file: "superadmin/superadmin-role-01-list.png",
    });
    if (config.seed.testRoleId) {
      await navigateAndCapture(page, config, rolePermissions(config.seed.testRoleId), {
        role: "superadmin",
        module: "superadmin",
        step: "Permission list",
        file: "superadmin/superadmin-permission-01-list.png",
      });
    } else {
      markSeedRequired("Permission list", "superadmin/superadmin-permission-01-list.png");
    }
    await navigateAndCapture(page, config, routes.departments, {
      role: "superadmin",
      module: "superadmin",
      step: "Department list",
      file: "superadmin/superadmin-department-01-list.png",
      manual: "docs/manuals/phase1/06-Admin-User-Guide.md",
    });
    if (config.seed.testDepartmentId) {
      await navigateAndCapture(page, config, departmentEdit(config.seed.testDepartmentId), {
        role: "superadmin",
        module: "superadmin",
        step: "Department detail",
        file: "superadmin/superadmin-department-02-detail.png",
      });
    }
    await navigateAndCapture(page, config, routes.auditLogs, {
      role: "superadmin",
      module: "superadmin",
      step: "Audit log",
      file: "superadmin/superadmin-audit-01-list.png",
      manual: "docs/manuals/phase1/06-Admin-User-Guide.md",
    });
    await navigateAndCapture(page, config, routes.systemSettings, {
      role: "superadmin",
      module: "superadmin",
      step: "System setting",
      file: "superadmin/superadmin-setting-01-system.png",
    });
    await logout(page, config);
  });
});

function markSeedRequired(step: string, file: string) {
  const now = new Date().toISOString();
  recordCapture({
    role: "superadmin",
    module: "superadmin",
    step,
    file,
    status: "Skipped",
    message: "Seed Required: configure safe id in screenshot-users.json.",
    startedAt: now,
    completedAt: now,
    durationMs: 0,
  });
}
