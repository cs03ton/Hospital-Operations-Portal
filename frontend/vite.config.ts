import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const apiProxyTarget = process.env.VITE_DEV_PROXY_TARGET ?? "http://localhost:5000";

export default defineConfig({
  plugins: [react()],
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
