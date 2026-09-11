import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { Alert, Box, Button, CircularProgress, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getAdminDashboard, getDashboardSummary, getExecutiveDashboard } from "../api/adminApi";
import { getLeaveCalendar, getMyNotifications } from "../api/leaveApi";
import { normalizeDashboardRole, RoleBasedDashboard } from "../components/dashboard/RoleBasedDashboard";
import { PageHeader } from "../components/PageHeader";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";
import { Link as RouterLink } from "react-router-dom";
import { PermissionGuard } from "../context/PermissionContext";
import { dashboardPollingOptions } from "../config/queryPolling";

export function DashboardPage() {
  const { user } = useAuth();
  const role = normalizeDashboardRole(user?.role);
  const summaryQuery = useQuery({
    queryKey: ["dashboard-summary", "leave", role],
    queryFn: getDashboardSummary,
    ...dashboardPollingOptions,
  });
  const now = new Date();
  const notificationQuery = useQuery({
    queryKey: ["notifications", "dashboard"],
    queryFn: getMyNotifications,
    ...dashboardPollingOptions,
  });
  const calendarQuery = useQuery({
    queryKey: ["leave-calendar", "dashboard", now.getFullYear(), now.getMonth() + 1],
    queryFn: () => getLeaveCalendar({ year: now.getFullYear(), month: now.getMonth() + 1 }),
    ...dashboardPollingOptions,
  });
  const executiveQuery = useQuery({
    queryKey: ["executive-dashboard", "leave-dashboard", now.getFullYear(), now.getMonth() + 1],
    queryFn: () => getExecutiveDashboard({ trendYear: now.getFullYear() }),
    enabled: role === "Director",
    ...dashboardPollingOptions,
  });
  const adminQuery = useQuery({
    queryKey: ["admin-dashboard", "leave-dashboard"],
    queryFn: getAdminDashboard,
    enabled: role === "Admin" || role === "SuperAdmin",
    ...dashboardPollingOptions,
  });
  const data = summaryQuery.data;
  const isLoading = summaryQuery.isLoading
    || notificationQuery.isLoading
    || calendarQuery.isLoading
    || (role === "Director" && executiveQuery.isLoading)
    || ((role === "Admin" || role === "SuperAdmin") && adminQuery.isLoading);
  const isError = summaryQuery.isError;
  const isFetching = summaryQuery.isFetching || notificationQuery.isFetching || calendarQuery.isFetching || executiveQuery.isFetching || adminQuery.isFetching;
  const refreshAll = async () => {
    const requests: Promise<unknown>[] = [summaryQuery.refetch(), notificationQuery.refetch(), calendarQuery.refetch()];
    if (role === "Director") requests.push(executiveQuery.refetch());
    if (role === "Admin" || role === "SuperAdmin") requests.push(adminQuery.refetch());
    await Promise.all(requests);
  };
  const updatedAt = data?.generatedAtUtc ? new Date(data.generatedAtUtc) : summaryQuery.dataUpdatedAt ? new Date(summaryQuery.dataUpdatedAt) : null;

  return (
    <Box>
      <PageHeader
        title="แดชบอร์ดระบบลา"
        subtitle={`ภาพรวมระบบลาและงานที่ต้องติดตามของ${hospitalName}`}
      />
      <Stack direction="row" spacing={1.5} flexWrap="wrap" useFlexGap sx={{ mb: 2 }}>
        <Button component={RouterLink} to="/dashboard" variant="outlined" startIcon={<ArrowBackOutlinedIcon />}>
          กลับไป Dashboard Hub
        </Button>
        <PermissionGuard permission="LeaveRequest.Create">
          <Button component={RouterLink} to="/leave/create" variant="contained">
            สร้างคำขอลา
          </Button>
        </PermissionGuard>
        <PermissionGuard permission="LeaveRequest.ViewPendingApproval">
          <Button component={RouterLink} to="/leave/pending-approvals" variant="outlined">
            งานรออนุมัติของฉัน
          </Button>
        </PermissionGuard>
        <Button variant="outlined" startIcon={isFetching ? <CircularProgress size={16} /> : <RefreshOutlinedIcon />} onClick={() => void refreshAll()} disabled={isFetching}>
          อัปเดตข้อมูล
        </Button>
        {updatedAt && (
          <Typography variant="caption" color="text.secondary" sx={{ alignSelf: "center" }}>
            อัปเดตล่าสุด {updatedAt.toLocaleTimeString("th-TH", { hour: "2-digit", minute: "2-digit", second: "2-digit" })}
          </Typography>
        )}
      </Stack>
      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ไม่สามารถโหลดข้อมูลแดชบอร์ดได้ในขณะนี้ ระบบจะแสดงค่าเริ่มต้นเป็น 0
        </Alert>
      )}
      <RoleBasedDashboard
        data={data}
        isLoading={isLoading}
        role={role}
        userName={user?.fullname ?? "ผู้ใช้งาน"}
        notifications={notificationQuery.data ?? []}
        calendarItems={calendarQuery.data ?? []}
        executive={executiveQuery.data}
        admin={adminQuery.data}
      />
    </Box>
  );
}
