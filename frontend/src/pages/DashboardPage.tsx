import { Alert, Box } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getDashboardSummary } from "../api/adminApi";
import { normalizeDashboardRole, RoleBasedDashboard } from "../components/dashboard/RoleBasedDashboard";
import { PageHeader } from "../components/PageHeader";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";

export function DashboardPage() {
  const { user } = useAuth();
  const role = normalizeDashboardRole(user?.role);
  const { data, isError, isLoading } = useQuery({
    queryKey: ["dashboard-summary", role],
    queryFn: getDashboardSummary,
  });

  return (
    <Box>
      <PageHeader
        title="แดชบอร์ด"
        subtitle={`ยินดีต้อนรับ ${user?.fullname ?? "ผู้ใช้งาน"} เข้าสู่ระบบบริหารงาน${hospitalName}`}
      />
      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ไม่สามารถโหลดข้อมูลแดชบอร์ดได้ในขณะนี้ ระบบจะแสดงค่าเริ่มต้นเป็น 0
        </Alert>
      )}
      <RoleBasedDashboard data={data} isLoading={isLoading} role={role} userName={user?.fullname ?? "ผู้ใช้งาน"} />
    </Box>
  );
}
