import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import { Alert, Box, Button, Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getDashboardSummary } from "../api/adminApi";
import { normalizeDashboardRole, RoleBasedDashboard } from "../components/dashboard/RoleBasedDashboard";
import { PageHeader } from "../components/PageHeader";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";
import { Link as RouterLink } from "react-router-dom";
import { PermissionGuard } from "../context/PermissionContext";

export function DashboardPage() {
  const { user } = useAuth();
  const role = normalizeDashboardRole(user?.role);
  const { data, isError, isLoading } = useQuery({
    queryKey: ["dashboard-summary", "leave", role],
    queryFn: getDashboardSummary,
  });

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
      </Stack>
      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ไม่สามารถโหลดข้อมูลแดชบอร์ดได้ในขณะนี้ ระบบจะแสดงค่าเริ่มต้นเป็น 0
        </Alert>
      )}
      <RoleBasedDashboard data={data} isLoading={isLoading} role={role} userName={user?.fullname ?? "ผู้ใช้งาน"} />
    </Box>
  );
}
