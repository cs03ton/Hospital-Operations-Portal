import type { Page } from "@playwright/test";
import { expect, test } from "./fixtures/fleetQa";

async function openDashboard(page: Page) {
  await page.goto("/fleet/dashboard");
  await expect(page.getByRole("heading", { name: "Dashboard รถ" })).toBeVisible();
}

async function expectMenu(page: Page, label: string, visible: boolean) {
  const link = page.getByRole("link", { name: new RegExp(`^${label}(?:\\s+\\d+|\\s+99\\+)?$`) });
  if (visible) await expect(link).toBeVisible(); else await expect(link).toHaveCount(0);
}

test("@fleet-m6 requester sees own widgets and requester navigation only", async ({ requesterPage: page }) => {
  await openDashboard(page);
  await expect(page.getByText("คำขอใช้รถของฉัน")).toBeVisible();
  await expect(page.getByText("งานจัดรถ")).toHaveCount(0);
  await expectMenu(page, "คำขอใช้รถ", true);
  await expectMenu(page, "คิวจัดรถ", false);
  await expectMenu(page, "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", false);
});

test("@fleet-m6 driver sees only own driver widgets and navigation", async ({ driverPage: page }) => {
  await openDashboard(page);
  await expect(page.getByText("งานขับรถของฉัน").last()).toBeVisible();
  await expect(page.getByText("คำขอใช้รถของฉัน")).toHaveCount(0);
  await expectMenu(page, "งานขับรถของฉัน", true);
  await expectMenu(page, "คิวจัดรถ", false);
});

test("@fleet-m6 dispatcher sees dispatch widgets without approval widgets", async ({ dispatcherPage: page }) => {
  await openDashboard(page);
  await expect(page.getByText("งานจัดรถ")).toBeVisible();
  await expectMenu(page, "คิวจัดรถ", true);
  await expectMenu(page, "ตรวจคำขอรถ", false);
  await expectMenu(page, "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", false);
});

test("@fleet-m6 reviewer sees review widgets and guarded approval deep link", async ({ reviewerPage: page }) => {
  await openDashboard(page);
  await expect(page.getByRole("heading", { name: "ตรวจคำขอ", exact: true })).toBeVisible();
  await expectMenu(page, "คำขอใช้รถ", true);
  await expectMenu(page, "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", true);
  await page.getByRole("link", { name: "เปิดตรวจคำขอ" }).click();
  await expect(page).toHaveURL(/\/fleet\/review$/);
  await expect(page.getByRole("heading", { name: "ตรวจคำขอใช้รถ" })).toBeVisible();
});

test("@fleet-m6 director sees director widgets and guarded approval deep link", async ({ directorPage: page }) => {
  await openDashboard(page);
  await expect(page.getByRole("heading", { name: "อนุมัติคำขอ", exact: true })).toBeVisible();
  await expectMenu(page, "คำขอใช้รถ", true);
  await expectMenu(page, "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", true);
  await page.getByRole("link", { name: "เปิดอนุมัติคำขอ" }).click();
  await expect(page).toHaveURL(/\/fleet\/approvals$/);
  await expect(page.getByRole("heading", { name: "งานรอตรวจสอบและอนุมัติคำขอใช้รถ" })).toBeVisible();
});

test("@fleet-m6 admin composes all sections once and caps sidebar badges", async ({ adminPage: page }) => {
  await page.route("**/api/fleet/dashboard", async route => {
    const response = await route.fetch();
    const body = await response.json();
    body.data.badges.dispatchQueue = 120;
    body.data.badges.reviewQueue = 60;
    body.data.badges.approvalQueue = 60;
    await route.fulfill({ response, json: body });
  });
  await openDashboard(page);
  for (const title of ["คำขอใช้รถของฉัน", "งานขับรถของฉัน", "งานจัดรถ", "ตรวจคำขอ", "อนุมัติคำขอ", "ภาพรวมผู้ดูแล Fleet"]) {
    await expect(page.getByText(title).last()).toBeVisible();
  }
  await expect(page.getByText("99+").first()).toBeVisible();
  await expectMenu(page, "รายงาน", true);
  await expectMenu(page, "ตั้งค่า", true);
});

test("@fleet-m6 dashboard is responsive without horizontal overflow", async ({ requesterPage: page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await openDashboard(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBeTruthy();
});
