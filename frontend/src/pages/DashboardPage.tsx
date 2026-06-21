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
                      <Button component={RouterLink} to="/leave/pending-approvals" size="small" variant="outlined">
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
      <Grid container spacing={2} sx={{ mt: 0.5 }}>
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                ภาพรวมสถานะคำขอลาของฉัน
              </Typography>
              <Stack spacing={1.5}>
                <MetricBar label="รออนุมัติ" value={data?.myLeaveRequestsPending ?? 0} total={data?.myLeaveRequestsTotal ?? 0} color="warning.main" />
                <MetricBar label="อนุมัติแล้ว" value={data?.myLeaveRequestsApproved ?? 0} total={data?.myLeaveRequestsTotal ?? 0} color="success.main" />
                <MetricBar label="ไม่อนุมัติ" value={data?.myLeaveRequestsRejected ?? 0} total={data?.myLeaveRequestsTotal ?? 0} color="error.main" />
                <MetricBar label="ยกเลิกแล้ว" value={data?.myLeaveRequestsCancelled ?? 0} total={data?.myLeaveRequestsTotal ?? 0} color="text.secondary" />
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                แนวโน้มเจ้าหน้าที่ลางาน
              </Typography>
              <Stack spacing={1.5}>
                <MetricBar label="วันนี้" value={data?.staffOnLeaveToday ?? 0} total={Math.max(data?.staffOnLeaveThisMonth ?? 0, 1)} color="success.main" />
                <MetricBar label="สัปดาห์นี้" value={data?.staffOnLeaveThisWeek ?? 0} total={Math.max(data?.staffOnLeaveThisMonth ?? 0, 1)} color="info.main" />
                <MetricBar label="เดือนนี้" value={data?.staffOnLeaveThisMonth ?? 0} total={Math.max(data?.staffOnLeaveThisMonth ?? 0, 1)} color="secondary.main" />
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}

function MetricBar({ label, value, total, color }: { label: string; value: number; total: number; color: string }) {
  const percent = total > 0 ? Math.min(100, Math.round((value / total) * 100)) : 0;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.75 }}>
        <Typography variant="body2" color="text.secondary">{label}</Typography>
        <Typography variant="body2" fontWeight={700}>{value.toLocaleString("th-TH")}</Typography>
      </Stack>
      <Box sx={(theme) => ({ height: 10, borderRadius: 999, bgcolor: alpha(theme.palette.primary.main, 0.08), overflow: "hidden" })}>
        <Box sx={{ width: `${percent}%`, height: "100%", bgcolor: color, borderRadius: 999 }} />
      </Box>
    </Box>
  );
}
