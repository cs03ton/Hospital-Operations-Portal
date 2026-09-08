import { expect, test } from "@playwright/test";

const frontendUrl = process.env.FLEET_FRONTEND_URL ?? "http://localhost:5173";
const username = process.env.FLEET_QA_USERNAME ?? "";
const password = process.env.FLEET_QA_PASSWORD ?? "";

test.skip(!username || !password, "Set FLEET_QA_USERNAME and FLEET_QA_PASSWORD for Fleet E2E.");

test("requester and dispatcher routes are permission guarded", async ({ page }) => {
  await page.goto(`${frontendUrl}/login`);
  await page.getByLabel("ชื่อผู้ใช้").fill(username);
  await page.getByLabel("รหัสผ่าน").fill(password);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  await page.goto(`${frontendUrl}/fleet/requests`);
  await expect(page.getByText(/คำขอใช้รถของฉัน|ไม่มีสิทธิ์/)).toBeVisible();
  await page.goto(`${frontendUrl}/fleet/dispatch`);
  await expect(page.getByText(/คิวรอจัดรถ|ไม่มีสิทธิ์/)).toBeVisible();
});
