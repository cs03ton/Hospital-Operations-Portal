import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  testMatch: "repairs-responsive.spec.ts",
  timeout: 30000,
  workers: 2,
  reporter: "list",
  use: { baseURL: "http://127.0.0.1:5193", screenshot: "only-on-failure", trace: "retain-on-failure" },
  webServer: {
    command: "npm run dev -- --host 127.0.0.1 --port 5193 --strictPort",
    url: "http://127.0.0.1:5193",
    reuseExistingServer: false,
    env: { VITE_API_BASE_URL: "/", VITE_AUTH_TOKEN_STORAGE_MODE: "localStorage" },
  },
});
