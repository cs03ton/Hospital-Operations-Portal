import { expect, test, type Page } from "@playwright/test";

const viewports = [
  { name: "mobile-375", width: 375, height: 812 },
  { name: "mobile-430", width: 430, height: 932 },
  { name: "tablet", width: 768, height: 1024 },
  { name: "desktop-1366", width: 1366, height: 768 },
  { name: "desktop-1920", width: 1920, height: 1080 },
] as const;

const roles = [
  { key: "STAFF", routes: ["/dashboard", "/dashboard/leave", "/announcements", "/leave", "/leave/balances", "/leave/calendar", "/fleet/dashboard", "/fleet/requests"] },
  { key: "HEAD", routes: ["/dashboard", "/dashboard/leave", "/leave/pending-approvals", "/leave/calendar", "/fleet/dashboard", "/fleet/approvals"] },
  { key: "DIRECTOR", routes: ["/dashboard", "/dashboard/executive", "/dashboard/leave", "/reports/leave-analytics", "/reports/leaves", "/fleet/dashboard", "/fleet/approvals", "/fleet/reports"] },
  { key: "ADMIN", routes: ["/dashboard", "/admin/dashboard", "/admin/users", "/admin/departments", "/admin/announcements", "/notifications", "/admin/leave-balances", "/fleet/dashboard", "/fleet/settings"] },
  { key: "SUPERADMIN", routes: ["/dashboard", "/admin/dashboard", "/admin/users", "/admin/roles", "/admin/health", "/admin/diagnostics", "/admin/backup", "/admin/announcements", "/fleet/dashboard", "/fleet/health", "/fleet/admin/line-groups"] },
] as const;

async function login(page: Page, role: string) {
  const username = process.env[`RESPONSIVE_QA_${role}_USERNAME`];
  const password = process.env[`RESPONSIVE_QA_${role}_PASSWORD`];
  if (!username || !password) return false;
  await page.goto("/login");
  await page.getByLabel(/ชื่อผู้ใช้|username/i).fill(username);
  await page.getByLabel(/รหัสผ่าน|password/i).fill(password);
  await page.getByRole("button", { name: /เข้าสู่ระบบ|login/i }).click();
  await expect(page).not.toHaveURL(/\/login/);
  return true;
}

for (const viewport of viewports) {
  for (const role of roles) {
    test(`${role.key} ${viewport.name} has complete accessible actions`, async ({ page }) => {
      test.skip(!(await login(page, role.key)), `Set RESPONSIVE_QA_${role.key}_USERNAME and RESPONSIVE_QA_${role.key}_PASSWORD`);
      await page.setViewportSize(viewport);

      for (const route of role.routes) {
        await page.goto(route);
        await page.waitForLoadState("networkidle").catch(() => undefined);
        await expect(page).not.toHaveURL(/\/unauthorized/);
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
        expect(overflow, `${role.key} ${route} causes page-level horizontal overflow`).toBeLessThanOrEqual(1);

        const visibleActions = page.locator("button:visible, a:visible");
        const count = await visibleActions.count();
        for (let index = 0; index < count; index += 1) {
          const box = await visibleActions.nth(index).boundingBox();
          if (!box) continue;
          expect(box.x + box.width, `${route} has an action outside the right viewport`).toBeLessThanOrEqual(viewport.width + 1);
          expect(box.x, `${route} has an action outside the left viewport`).toBeGreaterThanOrEqual(-1);
        }
      }

      if (viewport.width < 900) {
        await page.goto("/dashboard");
        await page.getByRole("button", { name: "เปิดเมนูผู้ใช้งาน" }).click();
        await expect(page.getByText("ข้อมูลส่วนตัวของฉัน")).toBeVisible();
        await expect(page.getByText("เปลี่ยนรหัสผ่าน")).toBeVisible();
        await expect(page.getByText("ออกจากระบบ").last()).toBeVisible();
      }
    });
  }
}
