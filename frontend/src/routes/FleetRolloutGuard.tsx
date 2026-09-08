import { Alert, CircularProgress, Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { getFleetRolloutAccess } from "../api/fleetApi";

export function FleetRolloutGuard({ children }: { children: ReactNode }) {
  const query = useQuery({ queryKey: ["fleet-rollout-access"], queryFn: getFleetRolloutAccess, staleTime: 60_000, retry: 1 });
  if (query.isLoading) return <Stack alignItems="center" sx={{ py: 6 }}><CircularProgress /></Stack>;
  if (query.isError) return <Alert severity="error">ตรวจสอบสถานะ Fleet rollout ไม่สำเร็จ กรุณาลองใหม่</Alert>;
  if (!query.data?.isAllowed) return <Alert severity="warning">Fleet Module ยังไม่เปิดสำหรับบัญชีนี้ ({query.data?.mode})</Alert>;
  return <>{children}</>;
}
