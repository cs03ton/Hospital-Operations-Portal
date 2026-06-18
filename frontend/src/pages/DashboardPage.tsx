import { Alert, Box, Card, CardContent, Grid, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getDashboardSummary } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";

export function DashboardPage() {
  const { user } = useAuth();
  const { data, isError, isLoading } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: getDashboardSummary,
  });

  const cards = [
    { label: "คำขอลารออนุมัติ", value: data?.pendingApprovals ?? 0, note: "รายการที่รอผู้ใช้ปัจจุบันอนุมัติ" },
    { label: "เจ้าหน้าที่ลาวันนี้", value: data?.staffOnLeaveToday ?? 0, note: "คำขอลาที่อนุมัติและครอบคลุมวันนี้" },
    { label: "เจ้าหน้าที่ลาสัปดาห์นี้", value: data?.staffOnLeaveThisWeek ?? 0, note: "คำขอลาที่อนุมัติในสัปดาห์นี้" },
    { label: "เจ้าหน้าที่ลาเดือนนี้", value: data?.staffOnLeaveThisMonth ?? 0, note: "คำขอลาที่อนุมัติในเดือนนี้" },
    { label: "วันลาคงเหลือของฉัน", value: data?.myRemainingLeaveDays ?? 0, note: "ยอดรวมวันลาคงเหลือของปีปัจจุบัน" },
  ];

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
      <Grid container spacing={2}>
        {cards.map((card) => (
          <Grid item xs={12} sm={6} lg={4} key={card.label}>
            <Card>
              <CardContent>
                <Typography color="text.secondary" variant="body2">
                  {card.label}
                </Typography>
                <Typography variant="h3" color="primary" sx={{ my: 1 }}>
                  {isLoading ? "-" : card.value.toLocaleString("th-TH")}
                </Typography>
                <Typography color="text.secondary" variant="body2">
                  {card.note}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
