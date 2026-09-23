import { expect, test } from "@playwright/test";

test("calendar input and Bangkok instants are independent of browser locale", async ({ page }) => {
  await page.goto("/e2e/fixtures/calendar-input.html");
  const date = page.getByRole("textbox", { name: "วันที่ลา", exact: true });
  await expect(date).toHaveValue(/24.*09.*2569/);
  await expect(page.getByTestId("date")).toHaveText("2026-09-24");
  for (const input of ["25/09/2569", "25/09/2026"]) {
    await date.fill(input);
    await date.press("Tab");
    await expect(page.getByTestId("date")).toHaveText("2026-09-25");
  }
  await date.fill("29/02/2567");
  await date.press("Tab");
  await expect(page.getByTestId("date")).toHaveText("2024-02-29");
  await page.getByRole("button").first().click();
  await page.getByRole("gridcell", { name: "28", exact: true }).click();
  await expect(page.getByTestId("date")).toHaveText("2024-02-28");
  await expect(page.getByTestId("instant")).toHaveText("2026-09-24T01:00:00.000Z");
  await expect(page.getByText("24/09/2569 01:00", { exact: true })).toBeVisible();
  await page.screenshot({ path: test.info().outputPath("calendar-input.png"), fullPage: true });
});
