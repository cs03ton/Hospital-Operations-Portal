import { expect, test, type Page } from "@playwright/test";

const username = process.env.FLEET_QA_USERNAME ?? "";
const password = process.env.FLEET_QA_PASSWORD ?? "";

test.skip(!username || !password, "Set FLEET_QA_USERNAME and FLEET_QA_PASSWORD for Fleet CSRF E2E.");

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("ชื่อผู้ใช้").fill(username);
  await page.getByLabel("รหัสผ่าน").fill(password);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  await expect(page).not.toHaveURL(/\/login/);
}

test("draft and submit use the shared client once without a CSRF failure", async ({ browser }) => {
  const marker = `CSRF-E2E-${Date.now()}`;
  const context = await browser.newContext();
  const page = await context.newPage();
  const unsafeCalls: Array<{ method: string; url: string }> = [];
  page.on("request", request => {
    if (["POST", "PUT", "PATCH", "DELETE"].includes(request.method()) && request.url().includes("/api/fleet/requests")) {
      unsafeCalls.push({ method: request.method(), url: request.url() });
    }
  });

  await login(page);
  await page.goto("/fleet/requests/create");
  await page.getByLabel("วัตถุประสงค์/ภารกิจ").fill(marker);
  await page.getByLabel("ปลายทาง").fill("โรงพยาบาลน่าน");
  await page.getByLabel("หมายเลขโทรศัพท์ผู้ประสานงาน").fill("0812345678");

  const start = new Date(Date.now() + 48 * 60 * 60 * 1000);
  const end = new Date(start.getTime() + 4 * 60 * 60 * 1000);
  const localInput = (value: Date) => {
    const offset = value.getTimezoneOffset() * 60_000;
    return new Date(value.getTime() - offset).toISOString().slice(0, 16);
  };
  await page.getByLabel("ออกเดินทาง").fill(localInput(start));
  await page.getByLabel("คาดว่าจะกลับ").fill(localInput(end));

  const draftResponsePromise = page.waitForResponse(response =>
    response.request().method() === "POST" && /\/api\/fleet\/requests$/.test(new URL(response.url()).pathname),
  );
  await page.getByRole("button", { name: "บันทึกร่าง" }).click();
  const draftResponse = await draftResponsePromise;
  const draftBody = await draftResponse.json();
  expect(draftResponse.status(), JSON.stringify(draftBody)).toBe(201);
  const requestId = draftBody.data.id as string;
  await expect(page).toHaveURL(new RegExp(`/fleet/requests/${requestId}$`));

  await page.goto(`/fleet/requests/${requestId}/edit`);
  await page.getByLabel("ปลายทาง").fill("โรงพยาบาลน่าน (แก้ไข)");
  const updateResponsePromise = page.waitForResponse(response =>
    response.request().method() === "PUT" && new URL(response.url()).pathname === `/api/fleet/requests/${requestId}`,
  );
  const submitResponsePromise = page.waitForResponse(response =>
    response.request().method() === "POST" && new URL(response.url()).pathname === `/api/fleet/requests/${requestId}/submit`,
  );
  await page.getByRole("button", { name: "ส่งคำขอ" }).click();
  const updateResponse = await updateResponsePromise;
  const submitResponse = await submitResponsePromise;
  const updateBody = await updateResponse.json();
  const submitBody = await submitResponse.json();
  expect(updateResponse.status(), JSON.stringify(updateBody)).toBe(200);
  expect(submitResponse.status(), JSON.stringify(submitBody)).toBe(200);
  expect(submitBody.data.status).toBe("PENDING_DISPATCH");

  expect(unsafeCalls.filter(call => call.method === "POST" && /\/api\/fleet\/requests$/.test(new URL(call.url).pathname))).toHaveLength(1);
  expect(await page.getByText("Invalid CSRF token").count()).toBe(0);
  await context.close();

  const freshContext = await browser.newContext();
  const freshPage = await freshContext.newPage();
  await login(freshPage);
  await freshPage.goto(`/fleet/requests/${requestId}`);
  await expect(freshPage.getByText(marker)).toBeVisible();
  await freshContext.close();
});
