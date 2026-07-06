import { expect, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { recordCapture } from "./captureRegistry";
import type { RoleName, ScreenshotConfig } from "./screenshotConfig";
import { screenshotRoot } from "./screenshotConfig";

export type CaptureOptions = {
  role: RoleName;
  module: string;
  step: string;
  file: string;
  manual?: string;
};

export async function prepareBrowser(page: Page) {
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.emulateMedia({ colorScheme: "light", reducedMotion: "reduce" });
  await page.addInitScript(() => {
    window.localStorage.setItem("hop-theme", "light");
  });
}

export async function loginAs(page: Page, config: ScreenshotConfig, role: RoleName) {
  const user = config.roles[role];
  await page.goto(`${config.baseUrl}/login`, { waitUntil: "networkidle" });
  await waitForStableUi(page);
  await page.getByLabel(/ชื่อผู้ใช้|username/i).fill(user.username);
  await page.getByLabel(/รหัสผ่าน|password/i).fill(user.password);
  await page.getByRole("button", { name: /เข้าสู่ระบบ|login/i }).click();
  await page.waitForURL(/\/dashboard/, { timeout: 30_000 });
  await waitForStableUi(page);
}

export async function logout(page: Page, config: ScreenshotConfig) {
  const logoutButton = page.getByRole("button", { name: /ออกจากระบบ|logout/i }).first();
  if (await logoutButton.isVisible().catch(() => false)) {
    await logoutButton.click();
    await page.waitForLoadState("networkidle").catch(() => undefined);
    return;
  }

  const accountButton = page.locator('[aria-label*="account" i], [aria-label*="profile" i], button:has-text("ออกจากระบบ")').first();
  if (await accountButton.isVisible().catch(() => false)) {
    await accountButton.click();
    const menuLogout = page.getByRole("menuitem", { name: /ออกจากระบบ|logout/i }).first();
    if (await menuLogout.isVisible().catch(() => false)) {
      await menuLogout.click();
      await page.waitForLoadState("networkidle").catch(() => undefined);
      return;
    }
  }

  await page.goto(`${config.baseUrl}/login`, { waitUntil: "networkidle" });
}

export async function navigateAndCapture(page: Page, config: ScreenshotConfig, route: string, options: CaptureOptions) {
  const startedAt = Date.now();
  const startedIso = new Date().toISOString();
  try {
    await page.goto(`${config.baseUrl}${route}`, { waitUntil: "networkidle" });
    await waitForStableUi(page);
    await expandSidebar(page);
    await maskSensitiveFields(page);
    await capturePng(page, options.file);
    recordCapture({
      ...options,
      status: "Captured",
      startedAt: startedIso,
      completedAt: new Date().toISOString(),
      durationMs: Date.now() - startedAt,
    });
  } catch (error) {
    recordCapture({
      ...options,
      status: "Failed",
      message: error instanceof Error ? error.message : String(error),
      startedAt: startedIso,
      completedAt: new Date().toISOString(),
      durationMs: Date.now() - startedAt,
    });
    throw error;
  }
}

export async function assertDeniedAndCapture(page: Page, config: ScreenshotConfig, route: string, options: CaptureOptions) {
  await page.goto(`${config.baseUrl}${route}`, { waitUntil: "networkidle" });
  await waitForStableUi(page);
  const denied = page.url().includes("/unauthorized") || (await page.getByText(/ไม่มีสิทธิ|Unauthorized|ไม่ได้รับอนุญาต/i).first().isVisible().catch(() => false));
  expect(denied, `${options.role} should not access ${route}`).toBeTruthy();
  await capturePng(page, options.file);
  recordCapture({
    ...options,
    status: "Captured",
    startedAt: new Date().toISOString(),
    completedAt: new Date().toISOString(),
    durationMs: 0,
    message: "Denied state captured from real app.",
  });
}

export async function waitForStableUi(page: Page) {
  await page.waitForLoadState("domcontentloaded").catch(() => undefined);
  await page.waitForLoadState("networkidle").catch(() => undefined);
  await page.waitForTimeout(500);
  await page
    .locator('[role="progressbar"], .MuiSkeleton-root, text=/กำลังโหลด|Loading/i')
    .first()
    .waitFor({ state: "hidden", timeout: 10_000 })
    .catch(() => undefined);
  await page
    .locator('.MuiSnackbar-root, [role="alert"]')
    .first()
    .waitFor({ state: "hidden", timeout: 5_000 })
    .catch(() => undefined);
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

export async function expandSidebar(page: Page) {
  const collapsedButton = page.getByRole("button", { name: /ขยายเมนู|open menu|menu/i }).first();
  if (await collapsedButton.isVisible().catch(() => false)) {
    await collapsedButton.click().catch(() => undefined);
    await page.waitForTimeout(250);
  }
}

async function capturePng(page: Page, relativeFile: string) {
  const outputPath = path.join(screenshotRoot, relativeFile);
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  await page.screenshot({ path: outputPath, fullPage: false, type: "png" });
}

async function maskSensitiveFields(page: Page) {
  await page.locator('input[type="password"]').evaluateAll((items) => {
    for (const item of items) {
      (item as HTMLInputElement).value = "";
    }
  }).catch(() => undefined);
  await page.addStyleTag({
    content: `
      [data-sensitive="true"],
      [data-testid*="password"],
      [data-testid*="secret"],
      [data-testid*="token"] {
        filter: blur(10px) !important;
      }
    `,
  }).catch(() => undefined);
}
