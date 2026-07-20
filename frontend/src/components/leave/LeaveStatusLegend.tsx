/* eslint-disable react-refresh/only-export-components */
import { Chip, Stack, Typography } from "@mui/material";
import { getLeaveStatusColor, getLeaveStatusLabel } from "../../utils/leaveLabels";

const visibleStatuses = ["Approved", "Pending", "Cancelled", "Rejected"];

export function getLeaveStatus(status: string) {
  return {
    label: getLeaveStatusLabel(status),
    color: getLeaveStatusColor(status),
  };
}

export function LeaveStatusLegend() {
  return (
    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
      <Typography variant="caption" color="text.secondary" fontWeight={700}>
        คำอธิบายสี:
      </Typography>
      <Chip size="small" label="วันหยุดประจำปี" color="info" variant="filled" />
      {visibleStatuses.map((status) => (
        <Chip
          key={status}
          size="small"
          label={getLeaveStatusLabel(status)}
          color={getLeaveStatusColor(status)}
          variant={status === "Cancelled" ? "outlined" : "filled"}
        />
      ))}
    </Stack>
  );
}
