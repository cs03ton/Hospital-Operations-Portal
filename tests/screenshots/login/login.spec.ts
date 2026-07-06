import { test } from "@playwright/test";
import { loadScreenshotConfig } from "../helpers/screenshotConfig";
import { loginAs, logout, navigateAndCapture, prepareBrowser } from "../helpers/screenshotActions";
import { routes } from "../pages/routes";

test.describe("HOP login screenshots", () => {
  test("login page and logout flow", async ({ page }) => {
    const config = loadScreenshotConfig();
    await prepareBrowser(page);
    await navigateAndCapture(page, config, routes.login, {
      role: "user",
      module: "login",
      step: "Login Page",
      file: "login/login-01-page.png",
      manual: "docs/manuals/phase1/01-Getting-Started.md",
    });
    await loginAs(page, config, "user");
    await logout(page, config);
    await navigateAndCapture(page, config, routes.login, {
      role: "user",
      module: "login",
      step: "Logout",
      file: "login/login-02-logout.png",
      manual: "docs/manuals/phase1/01-Getting-Started.md",
    });
  });
});

