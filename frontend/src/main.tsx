import React from "react";
import ReactDOM from "react-dom/client";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { ErrorBoundary } from "./components/common/ErrorBoundary";
import { appName, hospitalName } from "./config/appConfig";
import { AuthProvider } from "./context/AuthContext";
import { PermissionProvider } from "./context/PermissionContext";
import { NotificationProvider } from "./contexts/NotificationContext";
import { theme } from "./theme/theme";

const queryClient = new QueryClient();

document.title = `${appName} | ${hospitalName}`;

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <NotificationProvider>
            <AuthProvider>
              <PermissionProvider>
                <ErrorBoundary>
                  <App />
                </ErrorBoundary>
              </PermissionProvider>
            </AuthProvider>
          </NotificationProvider>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
