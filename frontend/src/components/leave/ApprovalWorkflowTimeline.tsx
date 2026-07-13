import AccessTimeOutlinedIcon from "@mui/icons-material/AccessTimeOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import HighlightOffOutlinedIcon from "@mui/icons-material/HighlightOffOutlined";
import RadioButtonUncheckedOutlinedIcon from "@mui/icons-material/RadioButtonUncheckedOutlined";
import SkipNextOutlinedIcon from "@mui/icons-material/SkipNextOutlined";
import { Box, Chip, Stack, Typography, useMediaQuery } from "@mui/material";
import { alpha, useTheme, type Theme } from "@mui/material/styles";
import type { LeaveApproval } from "../../api/leaveApi";
import { EmptyState } from "../common/EmptyState";
import { formatThaiDateTime } from "../../utils/dateFormat";
import {
  getApprovalStatusColor,
  getApprovalStatusIcon,
  getApprovalStatusLabel,
  normalizeApprovalStatus,
  type ApprovalStatusIcon,
} from "../../utils/approvalLabels";
import { brandColors } from "../../theme/theme";

type ApprovalWorkflowTimelineProps = {
  approvals: LeaveApproval[];
};

export function ApprovalWorkflowTimeline({ approvals }: ApprovalWorkflowTimelineProps) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));
  const sortedApprovals = [...approvals].sort((a, b) => a.stepOrder - b.stepOrder);
  const currentPendingId = sortedApprovals.find((item) => normalizeApprovalStatus(item.status) === "pending")?.id;

  if (!sortedApprovals.length) {
    return <EmptyState message="ยังไม่มีข้อมูลสายการอนุมัติ" />;
  }

  return (
    <Box sx={{ overflowX: isMobile ? "visible" : "auto", pb: isMobile ? 0 : 0.5 }}>
      <Stack direction={isMobile ? "column" : "row"} spacing={isMobile ? 0 : 0} alignItems={isMobile ? "stretch" : "flex-start"} sx={{ minWidth: isMobile ? "auto" : "max-content" }}>
        {sortedApprovals.map((approval, index) => {
          const isCurrent = approval.id === currentPendingId;
          const isLast = index === sortedApprovals.length - 1;

          return (
            <Stack key={approval.id} direction={isMobile ? "column" : "row"} alignItems={isMobile ? "flex-start" : "center"} sx={{ flexShrink: 0 }}>
              <ApprovalStepCard approval={approval} isCurrent={isCurrent} />
              {!isLast && <ApprovalConnector isMobile={isMobile} status={approval.status} />}
            </Stack>
          );
        })}
      </Stack>
    </Box>
  );
}

function ApprovalStepCard({ approval, isCurrent }: { approval: LeaveApproval; isCurrent: boolean }) {
  const theme = useTheme();
  const tone = getApprovalTone(approval.status, theme);
  const icon = getApprovalStatusIcon(approval.status);

  return (
    <Box
      sx={{
        width: { xs: "100%", md: 260 },
        border: 1,
        borderColor: isCurrent ? tone.main : "divider",
        borderLeft: "5px solid",
        borderLeftColor: tone.main,
        borderRadius: 3,
        bgcolor: isCurrent ? alpha(tone.main, 0.08) : "background.paper",
        p: 2,
        boxShadow: isCurrent ? `0 12px 28px ${alpha(tone.main, 0.16)}` : "none",
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1.25} alignItems="flex-start">
          <Box
            sx={{
              width: 38,
              height: 38,
              borderRadius: "50%",
              display: "grid",
              placeItems: "center",
              flexShrink: 0,
              color: tone.contrastText,
              bgcolor: tone.main,
              opacity: normalizeApprovalStatus(approval.status) ? 1 : 0.65,
            }}
          >
            <ApprovalIcon icon={icon} />
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="subtitle2" fontWeight={800}>
              ขั้นที่ {approval.stepOrder}: {approval.stepName || "ผู้อนุมัติ"}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ overflowWrap: "anywhere" }}>
              {approval.approverName || "-"}
            </Typography>
          </Box>
        </Stack>

        <Chip size="small" color={getApprovalStatusColor(approval.status)} label={getApprovalStatusLabel(approval.status)} sx={{ alignSelf: "flex-start" }} />

        <Stack spacing={0.75}>
          <ApprovalMeta label="ผู้อนุมัติ" value={approval.approverName || "-"} />
          <ApprovalMeta label="สถานะ" value={getApprovalStatusLabel(approval.status)} />
          <ApprovalMeta label="วันที่ดำเนินการ" value={formatThaiDateTime(approval.actionAt)} />
          <ApprovalMeta label="หมายเหตุ" value={approval.remark || "-"} />
          {approval.returnedAt && <ApprovalMeta label="ตีกลับเมื่อ" value={formatThaiDateTime(approval.returnedAt)} />}
          {approval.returnReason && <ApprovalMeta label="เหตุผลตีกลับ" value={approval.returnReason} />}
        </Stack>
      </Stack>
    </Box>
  );
}

function ApprovalConnector({ isMobile, status }: { isMobile: boolean; status?: string | null }) {
  const theme = useTheme();
  const tone = getApprovalTone(status, theme);

  return (
    <Box
      sx={{
        width: isMobile ? 2 : 56,
        height: isMobile ? 28 : 2,
        ml: isMobile ? 2.3 : 1.5,
        mr: isMobile ? 0 : 1.5,
        my: isMobile ? 0.75 : 0,
        bgcolor: alpha(tone.main, 0.42),
        borderRadius: 999,
      }}
    />
  );
}

function ApprovalMeta({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" fontWeight={700}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ mt: 0.2, overflowWrap: "anywhere" }}>
        {value}
      </Typography>
    </Box>
  );
}

function ApprovalIcon({ icon }: { icon: ApprovalStatusIcon }) {
  switch (icon) {
    case "check":
      return <CheckCircleOutlineOutlinedIcon fontSize="small" />;
    case "clock":
      return <AccessTimeOutlinedIcon fontSize="small" />;
    case "close":
      return <HighlightOffOutlinedIcon fontSize="small" />;
    case "cancel":
      return <BlockOutlinedIcon fontSize="small" />;
    case "skip":
      return <SkipNextOutlinedIcon fontSize="small" />;
    case "pending":
    default:
      return <RadioButtonUncheckedOutlinedIcon fontSize="small" />;
  }
}

function getApprovalTone(status: string | null | undefined, theme: Theme) {
  switch (normalizeApprovalStatus(status)) {
    case "approved":
    case "approve":
      return { main: theme.palette.success.main, contrastText: theme.palette.success.contrastText };
    case "pending":
    case "returnedforrevision":
      return { main: brandColors.accent, contrastText: brandColors.primaryDark };
    case "rejected":
    case "reject":
      return { main: theme.palette.error.main, contrastText: theme.palette.error.contrastText };
    case "cancelled":
    case "canceled":
    case "skipped":
    case "waiting":
    case "draft":
    default:
      return { main: theme.palette.grey[500], contrastText: theme.palette.common.white };
  }
}
