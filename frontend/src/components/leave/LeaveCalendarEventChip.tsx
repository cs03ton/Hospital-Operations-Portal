import { Box, Chip, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import type { LeaveCalendarItem } from "../../api/leaveApi";
import { getLeaveTypeWithDurationLabel, isHalfDayLeave } from "../../utils/leaveLabels";
import { getLeaveStatus } from "./LeaveStatusLegend";

type LeaveCalendarEventChipProps = {
  item: LeaveCalendarItem;
  compact?: boolean;
};

export function LeaveCalendarEventChip({ item, compact = false }: LeaveCalendarEventChipProps) {
  const status = getLeaveStatus(item.status);
  const isHalfDay = isHalfDayLeave(item.durationType);

  return (
    <Box
      sx={(theme) => ({
        borderRadius: 1.5,
        px: 1,
        py: 0.75,
        bgcolor: getStatusBackground(item.status, theme),
        border: "1px solid",
        borderColor: status.color === "default" ? "divider" : `${status.color}.light`,
        borderStyle: isHalfDay ? "dashed" : "solid",
        boxShadow: isHalfDay ? `inset 3px 0 0 ${theme.palette.warning.main}` : "none",
        overflow: "hidden",
      })}
    >
      <Stack direction="row" spacing={0.75} alignItems="center" justifyContent="space-between">
        <Typography variant="caption" fontWeight={800} noWrap>
          {item.fullname ?? "-"}
        </Typography>
        <Chip size="small" color={status.color} label={status.label} sx={{ height: 20, fontSize: 11, flexShrink: 0 }} />
      </Stack>
      {!compact && (
        <Typography variant="caption" color="text.secondary" noWrap sx={{ display: "block", mt: 0.25 }}>
          {getLeaveTypeWithDurationLabel(item.leaveTypeName, item.durationType)}
        </Typography>
      )}
    </Box>
  );
}

function getStatusBackground(status: string, theme: import("@mui/material/styles").Theme) {
  switch (status) {
    case "Approved":
      return alpha(theme.palette.success.main, 0.08);
    case "Pending":
      return alpha(theme.palette.warning.main, 0.1);
    case "Rejected":
      return alpha(theme.palette.error.main, 0.07);
    case "Cancelled":
      return alpha(theme.palette.text.secondary, 0.06);
    default:
      return theme.palette.action.hover;
  }
}
