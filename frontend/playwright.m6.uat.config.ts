import { defineConfig } from "@playwright/test";

const required = ["FLEET_QA_ADMIN_PASSWORD", "FLEET_QA_DRIVER_PASSWORD", "FLEET_QA_REQUESTER_PASSWORD", "FLEET_QA_DISPATCHER_PASSWORD", "FLEET_QA_REVIEWER_PASSWORD", "FLEET_QA_DIRECTOR_PASSWORD", "FLEET_QA_FIXTURE_PATH"];
for (const key of required) if (!process.env[key]) throw new Error(`M6 UAT preflight failed: ${key} is required`);

export default defineConfig({
  testDir: "./e2e",
  timeout: 45_000,
  retries: 0,
  workers: 1,
  forbidOnly: true,
  use: { baseURL: process.env.FLEET_QA_BASE_URL ?? "http://127.0.0.1:5173", trace: "retain-on-failure", screenshot: "only-on-failure", video: "retain-on-failure" },
  reporter: [["list"], ["json", { outputFile: "../tmp/fleet-dashboard-m6-playwright.json" }]],
});
