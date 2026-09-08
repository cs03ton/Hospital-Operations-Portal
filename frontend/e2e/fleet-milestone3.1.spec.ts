import { expect, test } from "@playwright/test";

const frontendUrl = process.env.FLEET_FRONTEND_URL ?? "http://localhost:5173";
const username = process.env.FLEET_QA_USERNAME ?? "";
const password = process.env.FLEET_QA_PASSWORD ?? "";

test.skip(!username || !password, "Set FLEET_QA_USERNAME and FLEET_QA_PASSWORD for Fleet 3.1 E2E.");

test("hardening routes remain permission guarded and navigation-independent", async ({ page }) => {
  await page.goto(`${frontendUrl}/login`);
  await page.getByLabel("ชื่อผู้ใช้").fill(username);
  await page.getByLabel("รหัสผ่าน").fill(password);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  for (const path of ["/fleet/delegations", "/fleet/driver", "/fleet/dashboard"]) {
    await page.goto(`${frontendUrl}${path}`);
    await expect(page).not.toHaveURL(/\/login$/);
    await expect(page.locator("body")).toContainText(/Fleet|งานขับรถ|ไม่มีสิทธิ์/);
  }
});
