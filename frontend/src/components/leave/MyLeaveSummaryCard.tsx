import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import { Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { Link as RouterLink } from "react-router-dom";
import { brandColors } from "../../theme/theme";

type MyLeaveSummaryCardProps = {
  total: number;
  draft: number;
  pending: number;
  returnedForRevision: number;
  approved: number;
  rejected: number;
  cancelled: number;
  isLoading?: boolean;
};

export function MyLeaveSummaryCard({ total, draft, pending, returnedForRevision, approved, rejected, cancelled, isLoading }: MyLeaveSummaryCardProps) {
  const items = [
    { label: "ทั้งหมด", value: total, color: "primary.main" },
    { label: "แบบร่าง", value: draft, color: "text.secondary" },
    { label: "รออนุมัติ", value: pending, color: "warning.main" },
    { label: "ตีกลับรอแก้ไข", value: returnedForRevision, color: "warning.dark" },
    { label: "อนุมัติแล้ว", value: approved, color: "success.main" },
    { label: "ไม่อนุมัติ", value: rejected, color: "error.main" },
    { label: "ยกเลิกแล้ว", value: cancelled, color: "text.secondary" },
  ];

  return (
    <Card sx={(theme) => ({ borderTop: "4px solid", borderTopColor: brandColors.accent, bgcolor: alpha(theme.palette.background.paper, 0.98) })}>
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
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "repeat(2, minmax(0, 1fr))",
              sm: "repeat(3, minmax(0, 1fr))",
              md: "repeat(4, minmax(0, 1fr))",
              lg: "repeat(7, minmax(0, 1fr))",
            },
            gap: 1.25,
            mt: 2,
            alignItems: "stretch",
          }}
        >
          {items.map((item) => (
            <Box
              key={item.label}
              sx={(theme) => ({
                minHeight: 104,
                p: 1.25,
                border: `1px solid ${alpha(theme.palette.primary.main, 0.12)}`,
                borderRadius: 2.25,
                bgcolor: alpha(theme.palette.background.default, 0.72),
                display: "flex",
                flexDirection: "column",
                justifyContent: "space-between",
                boxShadow: `0 10px 22px ${alpha(theme.palette.primary.dark, 0.04)}`,
              })}
            >
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{
                  minHeight: 34,
                  lineHeight: 1.35,
                  display: "flex",
                  alignItems: "flex-start",
                  wordBreak: "keep-all",
                }}
              >
                {item.label}
              </Typography>
              <Typography
                variant="h5"
                sx={{
                  color: item.color,
                  fontWeight: 900,
                  lineHeight: 1,
                  fontVariantNumeric: "tabular-nums",
                }}
              >
                {isLoading ? "-" : item.value.toLocaleString("th-TH")}
              </Typography>
            </Box>
          ))}
        </Box>
      </CardContent>
    </Card>
  );
}
