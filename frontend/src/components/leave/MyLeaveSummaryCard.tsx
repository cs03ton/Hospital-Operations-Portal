import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import { Box, Button, Card, CardContent, Chip, Divider, LinearProgress, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { Link as RouterLink } from "react-router-dom";
import type { DashboardLeaveRequestGroup, DashboardLeaveRequestItem } from "../../api/adminApi";
import { brandColors } from "../../theme/theme";
import { formatThaiDate } from "../../utils/dateFormat";
import { getLeaveStatusColor, getLeaveStatusLabel, getLeaveTypeLabel } from "../../utils/leaveLabels";
import { getLeaveRequestCode } from "../../utils/leaveTrackingLabels";

type MyLeaveSummaryCardProps = {
  total: number;
  draft: number;
  pending: number;
  returnedForRevision: number;
  approved: number;
  rejected: number;
  cancelled: number;
  cancellationPending?: number;
  recentRequests?: DashboardLeaveRequestGroup;
  isLoading?: boolean;
};

export function MyLeaveSummaryCard({
  total,
  draft,
  pending,
  returnedForRevision,
  approved,
  rejected,
  cancelled,
  cancellationPending = 0,
  recentRequests,
  isLoading,
}: MyLeaveSummaryCardProps) {
  const items = [
    { label: "ทั้งหมด", value: total, color: "primary.main" },
    { label: "แบบร่าง", value: draft, color: "text.secondary" },
    { label: "รออนุมัติ", value: pending, color: "warning.main" },
    { label: "ขอยกเลิก", value: cancellationPending, color: "warning.dark" },
    { label: "ตีกลับรอแก้ไข", value: returnedForRevision, color: "warning.dark" },
    { label: "อนุมัติแล้ว", value: approved, color: "success.main" },
    { label: "ไม่อนุมัติ", value: rejected, color: "error.main" },
    { label: "ยกเลิกแล้ว", value: cancelled, color: "text.secondary" },
  ];
  const recentItems = recentRequests?.items.slice(0, 3) ?? [];
  const finalizedTotal = approved + rejected;
  const approvalRate = finalizedTotal > 0 ? Math.round((approved / finalizedTotal) * 100) : 0;
  const rejectionRate = finalizedTotal > 0 ? Math.round((rejected / finalizedTotal) * 100) : 0;

  return (
    <Card sx={(theme) => ({ borderTop: "4px solid", borderTopColor: brandColors.accent, bgcolor: alpha(theme.palette.background.paper, 0.98) })}>
      <CardContent sx={{ p: { xs: 2, md: 2.5 } }}>
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
            gridTemplateColumns: "repeat(auto-fit, minmax(128px, 1fr))",
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

        <Divider sx={{ my: 2 }} />

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "minmax(0, 1.25fr) minmax(280px, 0.75fr)" },
            gap: 2,
            alignItems: "stretch",
          }}
        >
          <Stack spacing={1}>
            <Typography variant="body2" fontWeight={800}>
              รายการคำขอล่าสุด
            </Typography>
            {isLoading ? (
              <Box sx={(theme) => ({ height: 82, borderRadius: 2, bgcolor: alpha(theme.palette.primary.main, 0.08) })} />
            ) : recentItems.length ? (
              recentItems.map((item) => <RecentLeaveRequestItem key={item.id} item={item} />)
            ) : (
              <Box
                sx={(theme) => ({
                  border: `1px dashed ${alpha(theme.palette.primary.main, 0.24)}`,
                  borderRadius: 2,
                  p: 2,
                  color: "text.secondary",
                  bgcolor: alpha(theme.palette.background.default, 0.55),
                })}
              >
                ยังไม่มีคำขอลา
              </Box>
            )}
          </Stack>
          <Stack
            spacing={1.25}
            sx={(theme) => ({
              border: `1px solid ${alpha(theme.palette.primary.main, 0.12)}`,
              borderRadius: 2.5,
              p: 1.5,
              bgcolor: alpha(theme.palette.background.default, 0.55),
              minHeight: 148,
            })}
          >
            <Typography variant="body2" fontWeight={800}>
              ภาพรวมผลคำขอลา
            </Typography>
            <MetricLine label="Approval Rate" value={approvalRate} color="success.main" isLoading={isLoading} />
            <MetricLine label="Rejection Rate" value={rejectionRate} color="error.main" isLoading={isLoading} />
            <Typography variant="caption" color="text.secondary">
              คิดจากคำขอที่อนุมัติแล้วและไม่อนุมัติเท่านั้น
            </Typography>
          </Stack>
        </Box>
      </CardContent>
    </Card>
  );
}

function RecentLeaveRequestItem({ item }: { item: DashboardLeaveRequestItem }) {
  return (
    <Box
      component={RouterLink}
      to={item.detailPath || `/leave/${item.id}`}
      sx={(theme) => ({
        display: "block",
        border: `1px solid ${alpha(theme.palette.primary.main, 0.12)}`,
        borderRadius: 2,
        p: 1.25,
        color: "inherit",
        textDecoration: "none",
        transition: theme.transitions.create(["border-color", "background-color", "transform"], { duration: theme.transitions.duration.shortest }),
        "&:hover": {
          bgcolor: alpha(theme.palette.primary.main, 0.035),
          borderColor: alpha(theme.palette.primary.main, 0.28),
          transform: "translateY(-1px)",
        },
      })}
    >
      <Stack spacing={0.75}>
        <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={0.75}>
          <Box sx={{ minWidth: 0 }}>
            <Typography fontWeight={800} noWrap>
              {getLeaveRequestCode(item.requestNumber, item.id)}
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap>
              {getLeaveTypeLabel(item.leaveTypeName ?? "-")} · {formatDays(item.totalDays)} วัน
            </Typography>
          </Box>
          <Chip size="small" label={getLeaveStatusLabel(item.status)} color={getLeaveStatusColor(item.status)} sx={{ alignSelf: { xs: "flex-start", sm: "center" } }} />
        </Stack>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={{ xs: 0.25, sm: 1.5 }} color="text.secondary">
          <Typography variant="caption">{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</Typography>
          <Typography variant="caption">ผู้อนุมัติปัจจุบัน: {item.currentApproverName || "-"}</Typography>
        </Stack>
      </Stack>
    </Box>
  );
}

function MetricLine({ label, value, color, isLoading }: { label: string; value: number; color: string; isLoading?: boolean }) {
  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" spacing={1}>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="body2" fontWeight={800}>
          {isLoading ? "-" : value}
        </Typography>
      </Stack>
      <LinearProgress
        variant="determinate"
        value={isLoading ? 0 : Math.min(value, 100)}
        sx={{
          height: 8,
          borderRadius: 99,
          bgcolor: "action.hover",
          "& .MuiLinearProgress-bar": {
            bgcolor: color,
          },
        }}
      />
    </Box>
  );
}

function formatDays(value: number) {
  return Number.isInteger(value) ? value.toLocaleString("th-TH") : value.toLocaleString("th-TH", { maximumFractionDigits: 1 });
}
