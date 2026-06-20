import { Alert, Box, Button, Card, CardContent, Grid, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { getDashboardSummary } from "../api/adminApi";
import { MyLeaveSummaryCard } from "../components/leave/MyLeaveSummaryCard";
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
    { label: "งานรออนุมัติของฉัน", value: data?.pendingApprovals ?? 0, note: "แสดงเฉพาะคำขอลาที่ถึงคิวผู้ใช้งานปัจจุบัน", color: "warning.main", action: true },
    { label: "เจ้าหน้าที่ลาวันนี้", value: data?.staffOnLeaveToday ?? 0, note: "คำขอลาที่อนุมัติและครอบคลุมวันนี้", color: "success.main" },
    { label: "เจ้าหน้าที่ลาสัปดาห์นี้", value: data?.staffOnLeaveThisWeek ?? 0, note: "คำขอลาที่อนุมัติในสัปดาห์นี้", color: "info.main" },
    { label: "เจ้าหน้าที่ลาเดือนนี้", value: data?.staffOnLeaveThisMonth ?? 0, note: "คำขอลาที่อนุมัติในเดือนนี้", color: "secondary.main" },
    { label: "วันลาคงเหลือของฉัน", value: data?.myRemainingLeaveDays ?? 0, note: "ยอดรวมวันลาคงเหลือของปีปัจจุบัน", color: "primary.main" },
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
      <Box sx={{ mb: 2 }}>
        <MyLeaveSummaryCard
          total={data?.myLeaveRequestsTotal ?? 0}
          pending={data?.myLeaveRequestsPending ?? 0}
          approved={data?.myLeaveRequestsApproved ?? 0}
          rejected={data?.myLeaveRequestsRejected ?? 0}
          cancelled={data?.myLeaveRequestsCancelled ?? 0}
          isLoading={isLoading}
        />
      </Box>
      <Grid container spacing={2}>
        {cards.map((card) => (
          <Grid item xs={12} sm={6} lg={4} key={card.label}>
            <Card
              sx={(theme) => ({
                borderTop: "4px solid",
                borderTopColor: card.color,
                bgcolor: alpha(theme.palette.background.paper, 0.98),
              })}
            >
              <CardContent>
                <Typography color="text.secondary" variant="body2">
                  {card.label}
                </Typography>
                <Typography variant="h3" sx={{ my: 1, color: card.color }}>
                  {isLoading ? "-" : card.value.toLocaleString("th-TH")}
                </Typography>
                <Stack spacing={1.5}>
                  <Typography color="text.secondary" variant="body2">
                    {card.note}
                  </Typography>
                  {card.action && (
                    <Box>
                      <Button component={RouterLink} to="/leave" size="small" variant="outlined">
                        ดูทั้งหมด
                      </Button>
                    </Box>
                  )}
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
