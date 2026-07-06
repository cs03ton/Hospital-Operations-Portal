import { defineConfig, devices } from "@playwright/test";
import path from "node:path";

export default defineConfig({
  testDir: "..",
  globalSetup: require.resolve("../utils/globalSetup"),
  globalTeardown: require.resolve("../utils/globalTeardown"),
  outputDir: path.resolve(__dirname, "../../../tmp/screenshots-test-results", String(Date.now())),
  timeout: 120_000,
  expect: {
    timeout: 15_000,
  },
  fullyParallel: false,
  workers: 1,
  reporter: [["list"]],
  use: {
    ...devices["Desktop Chrome"],
    channel: "chrome",
    viewport: { width: 1920, height: 1080 },
    colorScheme: "light",
    ignoreHTTPSErrors: true,
    screenshot: "only-on-failure",
    trace: "retain-on-failure",
    actionTimeout: 20_000,
    navigationTimeout: 45_000,
  },
});
