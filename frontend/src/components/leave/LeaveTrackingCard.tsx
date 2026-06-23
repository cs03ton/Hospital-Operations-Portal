import AssignmentTurnedInOutlinedIcon from "@mui/icons-material/AssignmentTurnedInOutlined";
import DoneAllOutlinedIcon from "@mui/icons-material/DoneAllOutlined";
import PersonSearchOutlinedIcon from "@mui/icons-material/PersonSearchOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import UpdateOutlinedIcon from "@mui/icons-material/UpdateOutlined";
import { Chip, Grid, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";
import type { LeaveRequest } from "../../api/leaveApi";
import { InfoCard } from "../common/InfoCard";
import { formatThaiDateTime } from "../../utils/dateFormat";
import { getLeaveStatusColor } from "../../utils/leaveLabels";
import { getLeaveRequestCode, getTrackingMessage, getTrackingStatusLabel, getTrackingStepLabel } from "../../utils/leaveTrackingLabels";

type LeaveTrackingCardProps = {
  request: LeaveRequest;
};

export function LeaveTrackingCard({ request }: LeaveTrackingCardProps) {
  return (
    <InfoCard title="สถานะเอกสาร" subtitle={getTrackingMessage(request)}>
      <Grid container spacing={2}>
        <Grid item xs={12} md={4}>
          <TrackingItem icon={<AssignmentTurnedInOutlinedIcon />} label="เลขที่คำขอ" value={getLeaveRequestCode(request.requestNumber, request.id)} />
        </Grid>
        <Grid item xs={12} md={4}>
          <Stack spacing={0.75}>
            <Typography variant="caption" color="text.secondary" fontWeight={700}>
              สถานะปัจจุบัน
            </Typography>
            <Chip size="small" color={getLeaveStatusColor(request.status)} label={getTrackingStatusLabel(request)} sx={{ alignSelf: "flex-start" }} />
          </Stack>
        </Grid>
        <Grid item xs={12} md={4}>
          <TrackingItem icon={<PersonOutlineOutlinedIcon />} label="ผู้ขออนุมัติ" value={request.fullname ?? "-"} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TrackingItem icon={<PersonSearchOutlinedIcon />} label="ผู้อนุมัติปัจจุบัน" value={request.currentApproverName ?? "-"} helper={request.currentApproverName ? "ชื่อ-นามสกุลผู้อนุมัติ" : undefined} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TrackingItem icon={<UpdateOutlinedIcon />} label="ขั้นตอนปัจจุบัน" value={getTrackingStepLabel(request)} helper={`อัปเดตล่าสุด: ${formatThaiDateTime(request.latestActionAt ?? request.updatedAt ?? request.createdAt)}`} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TrackingItem icon={<DoneAllOutlinedIcon />} label="สิ้นสุดใบงาน" value={getCompletedAtLabel(request)} />
        </Grid>
        <Grid item xs={12} md={6}>
          <TrackingItem label="วันที่ส่งคำขอ" value={formatThaiDateTime(request.submittedAt)} />
        </Grid>
        <Grid item xs={12} md={6}>
          <TrackingItem label="ข้อความติดตาม" value={getTrackingMessage(request)} />
        </Grid>
      </Grid>
    </InfoCard>
  );
}

function getCompletedAtLabel(request: LeaveRequest) {
  if (!["Approved", "Rejected", "Cancelled"].includes(request.status)) {
    return "-";
  }

  return formatThaiDateTime(request.latestActionAt ?? request.updatedAt);
}

function TrackingItem({ icon, label, value, helper }: { icon?: ReactNode; label: string; value: string; helper?: string }) {
  return (
    <Stack direction="row" spacing={1.25} alignItems="flex-start">
      {icon}
      <Stack spacing={0.25} sx={{ minWidth: 0 }}>
        <Typography variant="caption" color="text.secondary" fontWeight={700}>
          {label}
        </Typography>
        <Typography fontWeight={700} sx={{ overflowWrap: "anywhere" }}>
          {value}
        </Typography>
        {helper && (
          <Typography variant="caption" color="text.secondary">
            {helper}
          </Typography>
        )}
      </Stack>
    </Stack>
  );
}
