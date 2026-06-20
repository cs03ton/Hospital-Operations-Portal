import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import { Box, Button, Card, CardContent, Grid, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { Link as RouterLink } from "react-router-dom";

type MyLeaveSummaryCardProps = {
  total: number;
  pending: number;
  approved: number;
  rejected: number;
  cancelled: number;
  isLoading?: boolean;
};

export function MyLeaveSummaryCard({ total, pending, approved, rejected, cancelled, isLoading }: MyLeaveSummaryCardProps) {
  const items = [
    { label: "ทั้งหมด", value: total, color: "primary.main" },
    { label: "รออนุมัติ", value: pending, color: "warning.main" },
    { label: "อนุมัติแล้ว", value: approved, color: "success.main" },
    { label: "ไม่อนุมัติ", value: rejected, color: "error.main" },
    { label: "ยกเลิกแล้ว", value: cancelled, color: "text.secondary" },
  ];

  return (
    <Card sx={(theme) => ({ borderTop: "4px solid", borderTopColor: "primary.main", bgcolor: alpha(theme.palette.background.paper, 0.98) })}>
      <CardContent>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "flex-start" }}>
          <Stack direction="row" spacing={1.25} alignItems="flex-start">
            <AssignmentOutlinedIcon color="primary" />
            <Box>
              <Typography color="text.secondary" variant="body2">
                คำขอลาของฉัน
              </Typography>
              <Typography variant="h5" fontWeight={800}>
                ติดตามสถานะคำขอลา
              </Typography>
            </Box>
          </Stack>
          <Button component={RouterLink} to="/leave" size="small" variant="outlined">
            ดูรายละเอียด
          </Button>
        </Stack>
        <Grid container spacing={1.5} sx={{ mt: 1 }}>
          {items.map((item) => (
            <Grid item xs={6} sm={4} md={2.4} key={item.label}>
              <Box sx={{ p: 1.25, border: 1, borderColor: "divider", borderRadius: 2, bgcolor: "background.default" }}>
                <Typography variant="caption" color="text.secondary">
                  {item.label}
                </Typography>
                <Typography variant="h5" sx={{ color: item.color, fontWeight: 800 }}>
                  {isLoading ? "-" : item.value.toLocaleString("th-TH")}
                </Typography>
              </Box>
            </Grid>
          ))}
        </Grid>
      </CardContent>
    </Card>
  );
}
