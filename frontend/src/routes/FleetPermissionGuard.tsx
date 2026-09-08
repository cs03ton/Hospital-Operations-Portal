import { CircularProgress, Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { FLEET_DASHBOARD_QUERY_KEY, getFleetDashboard } from "../api/fleetApi";
import { usePermission } from "../context/PermissionContext";
import { fleetCapabilitiesAllowAny } from "../config/fleetNavigation";

export function FleetPermissionGuard({ children, permissions, denyMode = "redirect" }: { children: ReactNode; permissions: readonly string[]; denyMode?: "redirect" | "hide" }) {
  const { hasAnyPermission } = usePermission();
  const directlyAllowed = hasAnyPermission([...permissions]);
  const dashboard = useQuery({
    queryKey: FLEET_DASHBOARD_QUERY_KEY,
    queryFn: getFleetDashboard,
    enabled: !directlyAllowed,
    staleTime: 30_000,
    refetchInterval: 60_000,
    refetchOnWindowFocus: true,
    retry: false,
  });

  if (directlyAllowed) return <>{children}</>;
  if (dashboard.isLoading) return <Stack alignItems="center" sx={{ py: 6 }}><CircularProgress /></Stack>;
  if (fleetCapabilitiesAllowAny(permissions, dashboard.data?.capabilities)) return <>{children}</>;
  return denyMode === "hide" ? null : <Navigate to="/unauthorized" replace />;
}
