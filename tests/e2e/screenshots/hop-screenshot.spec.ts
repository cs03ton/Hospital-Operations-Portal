import { expect, test, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

type RoleName = "user" | "head" | "director" | "hr" | "superadmin";

type ScreenshotUser = {
  username: string;
  password: string;
};

type ScreenshotConfig = {
  baseUrl: string;
  users: Record<RoleName, ScreenshotUser>;
};

type CaptureTarget = {
  role: RoleName;
  module: string;
  page: string;
  route: string;
  file: string;
  denied?: boolean;
};

const specDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(specDir, "..", "..", "..");
const configPath = path.join(specDir, "screenshot-users.json");
const exampleConfigPath = path.join(specDir, "screenshot-users.example.json");
const outputRoot = path.join(repoRoot, "docs", "manuals", "assets", "screenshots");

const staticTargets: CaptureTarget[] = [
  { role: "user", module: "dashboard", page: "Dashboard", route: "/dashboard", file: "dashboard/dashboard-user.png" },
  { role: "user", module: "common", page: "My Profile", route: "/profile", file: "common/profile-user.png" },
  { role: "user", module: "leave", page: "Leave List", route: "/leave", file: "leave/leave-list-user.png" },
  { role: "user", module: "leave", page: "Create Leave", route: "/leave/create", file: "leave/leave-create.png" },
  { role: "user", module: "leave", page: "Leave Calendar", route: "/leave/calendar", file: "leave/leave-calendar-user.png" },
  { role: "user", module: "leave", page: "Leave Balance", route: "/leave/balances", file: "leave/leave-balance-user.png" },
  { role: "user", module: "common", page: "Notification", route: "/notifications", file: "common/notification-user.png" },

  { role: "head", module: "dashboard", page: "Dashboard", route: "/dashboard", file: "dashboard/dashboard-head.png" },
  { role: "head", module: "approval", page: "Pending Approval", route: "/leave/pending-approvals", file: "approval/approval-pending-list.png" },
  { role: "head", module: "leave", page: "Leave List Department", route: "/leave", file: "leave/leave-list-head.png" },
  { role: "head", module: "approval", page: "Team Leave Calendar", route: "/leave/calendar", file: "approval/team-leave-calendar.png" },

  { role: "director", module: "dashboard", page: "Dashboard", route: "/dashboard", file: "dashboard/dashboard-director.png" },
  { role: "director", module: "approval", page: "Pending Approval", route: "/leave/pending-approvals", file: "approval/approval-pending-list-director.png" },

  { role: "hr", module: "dashboard", page: "Dashboard", route: "/dashboard", file: "dashboard/dashboard-hr.png" },
  { role: "hr", module: "hr", page: "Leave List Department", route: "/leave", file: "hr/leave-list-hr.png" },
  { role: "hr", module: "hr", page: "Leave Balance Management", route: "/admin/leave-balances", file: "hr/leave-balance.png" },
  { role: "hr", module: "hr", page: "Holiday Management", route: "/admin/leave-holidays", file: "hr/holiday-list.png" },
  { role: "hr", module: "hr", page: "Employee Leave History", route: "/leave", file: "hr/employee-leave-history.png" },
  { role: "hr", module: "hr", page: "Leave Type Management", route: "/leave/types", file: "hr/leave-types.png" },
  { role: "hr", module: "hr", page: "Approval Chain", route: "/admin/approval-chains", file: "hr/approval-chain.png" },

  { role: "superadmin", module: "dashboard", page: "Dashboard", route: "/dashboard", file: "dashboard/dashboard-superadmin.png" },
  { role: "superadmin", module: "user-management", page: "User List", route: "/admin/users", file: "user-management/user-list.png" },
  { role: "superadmin", module: "user-management", page: "Create User", route: "/admin/users/create", file: "user-management/create-user.png" },
  { role: "superadmin", module: "user-management", page: "Department List", route: "/admin/departments", file: "user-management/department-list.png" },
  { role: "superadmin", module: "user-management", page: "Role List", route: "/admin/roles", file: "user-management/role-list.png" },
  { role: "superadmin", module: "superadmin", page: "Approval Chain", route: "/admin/approval-chains", file: "superadmin/approval-chain.png" },
  { role: "superadmin", module: "superadmin", page: "Holiday List", route: "/admin/leave-holidays", file: "superadmin/holiday-list.png" },
  { role: "superadmin", module: "superadmin", page: "Audit Log", route: "/admin/audit-logs", file: "superadmin/audit-log.png" },
  { role: "superadmin", module: "superadmin", page: "System Setting", route: "/admin/system-settings", file: "superadmin/system-setting.png" },
  { role: "superadmin", module: "superadmin", page: "Notification Setting", route: "/admin/line-settings", file: "superadmin/notification-setting.png" },
  { role: "superadmin", module: "superadmin", page: "LINE Users", route: "/admin/line-users", file: "superadmin/line-users.png" },
  { role: "superadmin", module: "common", page: "Profile", route: "/profile", file: "common/profile-superadmin.png" },
  { role: "superadmin", module: "superadmin", page: "Create Leave Denied", route: "/leave/create", file: "superadmin/leave-create-denied.png", denied: true },
];

test.use({
  channel: "chrome",
  viewport: { width: 1920, height: 1080 },
  ignoreHTTPSErrors: true,
  colorScheme: "light",
});

test.describe("HOP screenshot catalog capture", () => {
  test.skip(!fs.existsSync(configPath), `Missing ${configPath}. Copy ${exampleConfigPath} and fill test credentials only.`);

  const config = readConfig();

  test("capture login page without credentials", async ({ page }) => {
    await preparePage(page);
    await page.goto(`${config.baseUrl}/login`, { waitUntil: "networkidle" });
    await capture(page, "login/login-page.png");
  });

  for (const role of uniqueRoles(staticTargets)) {
    test(`capture accessible pages for ${role}`, async ({ page }) => {
      test.setTimeout(180000);
      await preparePage(page);
      await login(page, config.baseUrl, config.users[role], role);

      for (const target of staticTargets.filter((item) => item.role === role)) {
        await page.goto(`${config.baseUrl}${target.route}`, { waitUntil: "networkidle" });
        await page.waitForTimeout(500);

        if (target.denied) {
          await expect(page).toHaveURL(/unauthorized|login|dashboard|leave\/create/);
          const unauthorizedVisible = await page.getByText(/ไม่มีสิทธิ|Unauthorized|ไม่ได้รับอนุญาต/i).first().isVisible().catch(() => false);
          if (!unauthorizedVisible && page.url().includes(target.route)) {
            throw new Error(`Expected denied/unauthorized state for ${role} ${target.route}, but route still appears accessible.`);
          }
        } else {
          await expect(page).not.toHaveURL(/\/login/);
          await expect(page).not.toHaveURL(/\/unauthorized/);
        }

        await capture(page, target.file);
      }
    });
  }
});

function readConfig(): ScreenshotConfig {
  if (!fs.existsSync(configPath)) {
    return {
      baseUrl: "http://localhost:5173",
      users: {
        user: { username: "", password: "" },
        head: { username: "", password: "" },
        director: { username: "", password: "" },
        hr: { username: "", password: "" },
        superadmin: { username: "", password: "" },
      },
    };
  }

  return JSON.parse(fs.readFileSync(configPath, "utf8")) as ScreenshotConfig;
}

async function preparePage(page: Page) {
  await page.addInitScript(() => {
    window.localStorage.setItem("hop-theme", "light");
  });
  await page.emulateMedia({ colorScheme: "light", reducedMotion: "reduce" });
}

async function login(page: Page, baseUrl: string, user: ScreenshotUser, role: RoleName) {
  if (!user?.username || !user.password || user.password === "CHANGE_ME") {
    throw new Error(`Missing safe test credentials for role '${role}'. Update screenshot-users.json locally; do not commit it.`);
  }

  await page.goto(`${baseUrl}/login`, { waitUntil: "networkidle" });
  await page.getByLabel(/ชื่อผู้ใช้|username/i).fill(user.username);
  await page.getByLabel(/รหัสผ่าน|password/i).fill(user.password);
  await page.getByRole("button", { name: /เข้าสู่ระบบ|login/i }).click();
  await page.waitForURL(/\/dashboard/, { timeout: 20000 });
}

async function capture(page: Page, relativeFile: string) {
  await disableAnimations(page);
  await maskSensitiveData(page);
  const outputPath = path.join(outputRoot, relativeFile);
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  await page.screenshot({ path: outputPath, fullPage: true });
}

async function disableAnimations(page: Page) {
  await page.addStyleTag({
    content: `
      *, *::before, *::after {
        animation-duration: 0s !important;
        animation-delay: 0s !important;
        transition-duration: 0s !important;
        transition-delay: 0s !important;
        caret-color: transparent !important;
      }
    `,
  }).catch(() => undefined);
}

async function maskSensitiveData(page: Page) {
  await page.locator('input[type="password"]').evaluateAll((items) => {
    for (const item of items) {
      (item as HTMLInputElement).value = "";
    }
  }).catch(() => undefined);

  await page.addStyleTag({
    content: `
      [data-sensitive="true"],
      [data-testid*="token"],
      [data-testid*="secret"],
      [data-testid*="password"] {
        filter: blur(8px) !important;
      }
    `,
  }).catch(() => undefined);
}

function uniqueRoles(targets: CaptureTarget[]): RoleName[] {
  return Array.from(new Set(targets.map((item) => item.role)));
}
