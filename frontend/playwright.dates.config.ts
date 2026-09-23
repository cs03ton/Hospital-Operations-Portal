import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e", testMatch: "calendar-input.spec.ts", retries: 0,
  use: { baseURL: "http://127.0.0.1:5188", headless: true, channel: "msedge" },
  webServer: { command: "npm run dev -- --port 5188", url: "http://127.0.0.1:5188", reuseExistingServer: false },
  projects: [
    { name: "thai-bangkok", use: { locale: "th-TH", timezoneId: "Asia/Bangkok" } },
    { name: "english-utc", use: { locale: "en-US", timezoneId: "UTC" } },
    { name: "english-america", use: { locale: "en-US", timezoneId: "America/Los_Angeles" } },
  ],
});
