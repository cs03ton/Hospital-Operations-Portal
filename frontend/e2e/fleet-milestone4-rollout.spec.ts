import { expect, test } from "@playwright/test";

const frontendUrl = process.env.FLEET_FRONTEND_URL ?? "http://localhost:5173";
const username = process.env.FLEET_QA_USERNAME ?? "";
const password = process.env.FLEET_QA_PASSWORD ?? "";

test.skip(!username || !password, "Set Fleet UAT credentials and rollout mode before running Milestone 4 scenarios.");

test("controlled rollout and Fleet Health are enforced", async ({ page }) => {
  await page.goto(`${frontendUrl}/login`);
  await page.getByLabel("ชื่อผู้ใช้").fill(username);
  await page.getByLabel("รหัสผ่าน").fill(password);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  await page.goto(`${frontendUrl}/fleet/health`);
  await expect(page.getByText(/Fleet Health Center|Fleet Module ยังไม่เปิด|ไม่มีสิทธิ์/)).toBeVisible();
});
