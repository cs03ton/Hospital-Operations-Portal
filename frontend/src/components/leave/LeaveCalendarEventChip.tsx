import { Box, Chip, Stack, Typography } from "@mui/material";
import type { LeaveCalendarItem } from "../../api/leaveApi";
import { getLeaveTypeColor, getLeaveTypeWithDurationLabel, isHalfDayLeave } from "../../utils/leaveLabels";
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
        bgcolor: getLeaveTypeColor(item.leaveTypeName),
        border: "1px solid",
        borderColor: getLeaveTypeColor(item.leaveTypeName),
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
