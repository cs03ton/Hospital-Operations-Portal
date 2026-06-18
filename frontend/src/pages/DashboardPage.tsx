import { Box, Card, CardContent, Grid, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getDashboardSummary } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";

export function DashboardPage() {
  const { user } = useAuth();
  const { data, isLoading } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: getDashboardSummary,
  });

  const cards = [
    { label: "ผู้ใช้งาน", value: data?.totalUsers ?? 0, note: "จำนวนผู้ใช้งานที่เปิดใช้งาน" },
    { label: "หน่วยงาน", value: data?.totalDepartments ?? 0, note: "จำนวนหน่วยงานที่เปิดใช้งาน" },
    { label: "รออนุมัติ", value: data?.pendingApprovals ?? 0, note: "รายการรออนุมัติ" },
    { label: "แจ้งซ่อม", value: data?.openRepairRequests ?? 0, note: "รายการแจ้งซ่อมที่เปิดอยู่" },
    { label: "ยืมอุปกรณ์", value: data?.activeBorrowRequests ?? 0, note: "รายการยืมที่กำลังใช้งาน" },
    { label: "Inventory", value: data?.inventoryItems ?? 0, note: "รายการทรัพย์สิน/ครุภัณฑ์" },
  ];

  return (
    <Box>
      <PageHeader
        title="แดชบอร์ด"
        subtitle={`ยินดีต้อนรับ ${user?.fullname ?? "ผู้ใช้งาน"} เข้าสู่ระบบบริหารงาน${hospitalName}`}
      />
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
