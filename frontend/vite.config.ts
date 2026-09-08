import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// Keep the local proxy on plain HTTP. Using the ASP.NET development HTTPS
// certificate here makes Node/Vite fail the TLS handshake on some Windows
// machines, which surfaces to the UI as a misleading 500/404 API error.
const apiProxyTarget = process.env.VITE_DEV_PROXY_TARGET ?? "http://127.0.0.1:5000";

export default defineConfig({
  plugins: [react()],
  test: {
    include: ["src/**/*.test.{ts,tsx}"],
    exclude: ["e2e/**", "node_modules/**", "dist/**"],
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    coverage: { provider: "v8", reporter: ["text", "json-summary", "html"], include: ["src/pages/FleetDashboardPage.tsx", "src/pages/FleetCalendarPage.tsx", "src/pages/FleetMaintenanceFormPage.tsx", "src/pages/FleetMaintenanceDetailPage.tsx", "src/pages/FleetVehicleDocumentsPage.tsx", "src/pages/FleetCapabilityFormPage.tsx", "src/pages/FleetVehicleCapabilitiesPage.tsx", "src/pages/FleetEmergencyReviewPage.tsx", "src/pages/FleetDriverTripPage.tsx"], thresholds: { statements: 35, branches: 25, functions: 30, lines: 35 } },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: apiProxyTarget,
        changeOrigin: true,
        secure: false,
      },
      "/health": {
        target: apiProxyTarget,
        changeOrigin: true,
        secure: false,
      },
      "/healthz": {
        target: apiProxyTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
