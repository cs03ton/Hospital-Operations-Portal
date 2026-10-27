import { Box, Chip, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import AddCircleOutline from "@mui/icons-material/AddCircleOutline";
import CheckCircleOutline from "@mui/icons-material/CheckCircleOutline";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import DirectionsCarOutlined from "@mui/icons-material/DirectionsCarOutlined";
import FlagOutlined from "@mui/icons-material/FlagOutlined";
import HistoryOutlined from "@mui/icons-material/HistoryOutlined";
import HourglassEmptyOutlined from "@mui/icons-material/HourglassEmptyOutlined";
import PersonOutline from "@mui/icons-material/PersonOutline";
import PlayCircleOutline from "@mui/icons-material/PlayCircleOutline";
import ReplyOutlined from "@mui/icons-material/ReplyOutlined";
import CancelOutlined from "@mui/icons-material/CancelOutlined";
import type { FleetHistory } from "../../api/fleetApi";
import { bangkokDayjs, formatThaiDate, formatThaiDateTime } from "../../utils/dateFormat";
import { getFleetStatusLabel } from "../../utils/fleetLabels";

const actions: Record<string, string> = {
  "Fleet.RequestCreated": "สร้างคำขอใช้รถ", "Fleet.RequestSubmitted": "ส่งคำขอใช้รถ", "Fleet.Assigned": "จัดรถและคนขับ",
  "Fleet.AssignmentChanged": "เปลี่ยนรถหรือคนขับ", "Fleet.AssignmentReplaced": "เปลี่ยนรถหรือคนขับ",
  "Fleet.ADMIN_APPROVE": "หัวหน้าฝ่ายบริหารตรวจสอบและอนุมัติ", "Fleet.DIRECTOR_APPROVE": "ผู้อำนวยการอนุมัติ",
  "Fleet.DRIVER_ACCEPT": "คนขับรับทราบ", "Fleet.DRIVER_DECLINE": "คนขับปฏิเสธงาน",
  "Fleet.TRIP_START": "เริ่มเดินทาง", "Fleet.TRIP_COMPLETE": "จบการเดินทาง",
  "Fleet.TripStarted": "เริ่มเดินทาง", "Fleet.TripCompleted": "จบการเดินทาง",
  "Fleet.RequestCancelled": "ยกเลิกคำขอ", "Fleet.TripClosedByAdmin": "ยุติการเดินทางโดยผู้ดูแล",
  "Fleet.ReferAutoApproved": "อนุมัติอัตโนมัติสำหรับภารกิจส่งต่อผู้ป่วย", "Fleet.CancellationRequested": "ขอยกเลิกคำขอใช้รถ",
  "Fleet.ADMIN_RETURN": "ส่งกลับแก้ไขโดยหัวหน้าฝ่ายบริหาร", "Fleet.DIRECTOR_RETURN": "ส่งกลับแก้ไขโดยผู้อำนวยการ",
  "Fleet.ADMIN_REJECT": "ไม่อนุมัติโดยหัวหน้าฝ่ายบริหาร", "Fleet.DIRECTOR_REJECT": "ไม่อนุมัติโดยผู้อำนวยการ",
};

function appearance(status: string) {
  if (status === "COMPLETED") return { color: "#26733C", Icon: FlagOutlined };
  if (status === "APPROVED") return { color: "#26733C", Icon: CheckCircleOutline };
  if (status === "READY") return { color: "#1E6656", Icon: DirectionsCarOutlined };
  if (status === "IN_PROGRESS") return { color: "#176FA5", Icon: PlayCircleOutline };
  if (status === "RETURNED") return { color: "#93600F", Icon: ReplyOutlined };
  if (status === "REJECTED") return { color: "#AD3535", Icon: CancelOutlined };
  if (status === "CANCELLED" || status === "ABORTED") return { color: "#665A68", Icon: CancelOutlined };
  if (status.startsWith("PENDING_") || status === "CANCELLATION_PENDING") return { color: "#806022", Icon: HourglassEmptyOutlined };
  return { color: "#536977", Icon: DescriptionOutlined };
}

export function FleetStatusHistoryTimeline({ histories }: { histories: readonly FleetHistory[] }) {
  const ordered = histories.map((history, index) => ({ history, index }))
    .sort((a, b) => (bangkokDayjs(b.history.createdAt).valueOf() - bangkokDayjs(a.history.createdAt).valueOf()) || a.index - b.index);
  return <Box component="section" aria-labelledby="fleet-history-heading">
    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
      <HistoryOutlined aria-hidden="true" color="primary" />
      <Typography id="fleet-history-heading" variant="h6" fontWeight={800}>ประวัติสถานะ</Typography>
      {ordered.length > 0 && <Typography variant="caption" color="text.secondary">เรียงจากล่าสุด</Typography>}
    </Stack>
    {!ordered.length ? <Typography color="text.secondary">ยังไม่มีประวัติสถานะ</Typography> :
      <Box component="ol" sx={{ listStyle: "none", p: 0, m: 0 }}>
        {ordered.map(({ history: item }, index) => {
          const { color, Icon } = appearance(item.toStatus);
          const action = actions[item.action] ?? "บันทึกการเปลี่ยนสถานะ";
          const ActionIcon = item.action === "Fleet.RequestCreated" ? AddCircleOutline : Icon;
          return <Box component="li" key={item.id} sx={{ position: "relative", display: "grid", columnGap: { xs: 1.5, md: 2 },
            gridTemplateColumns: { xs: "20px minmax(0, 1fr)", md: "128px 20px minmax(140px, 1fr) minmax(0, 1.6fr) minmax(0, 1fr)" },
            alignItems: "start", px: { xs: 1, md: 1.5 }, py: 1.75, borderRadius: 2,
            bgcolor: index % 2 === 0 ? alpha("#1E6656", 0.035) : "transparent" }}>
            <Box component="time" dateTime={item.createdAt} title={formatThaiDateTime(item.createdAt)} sx={{ gridColumn: { xs: 2, md: 1 }, gridRow: 1, mb: { xs: 1, md: 0 } }}>
              <Typography component="span" display="block" variant="body2" fontWeight={700}>{formatThaiDate(item.createdAt)}</Typography>
              <Typography component="span" variant="caption" color="text.secondary">{bangkokDayjs(item.createdAt).format("HH:mm")} น.</Typography>
            </Box>
            <Box aria-hidden="true" sx={{ gridColumn: { xs: 1, md: 2 }, gridRow: { xs: "1 / span 4", md: 1 }, alignSelf: "stretch", position: "relative", display: "flex", justifyContent: "center" }}>
              {index !== ordered.length - 1 && <Box sx={{ position: "absolute", top: 12, bottom: -28, borderLeft: "2px solid", borderColor: alpha("#1E6656", 0.18) }} />}
              <Box sx={{ zIndex: 1, mt: 0.6, width: 11, height: 11, borderRadius: "50%", bgcolor: color, boxShadow: `0 0 0 4px ${alpha(color, 0.1)}` }} />
            </Box>
            <Box sx={{ gridColumn: { xs: 2, md: 3 }, gridRow: { xs: 2, md: 1 }, minWidth: 0, mb: { xs: 1, md: 0 } }}>
              <Chip icon={<Icon aria-hidden="true" sx={{ fontSize: 17 }} />} label={getFleetStatusLabel(item.toStatus)} size="small"
                sx={{ maxWidth: "100%", height: "auto", py: 0.5, color, bgcolor: alpha(color, 0.1), fontWeight: 700,
                  "& .MuiChip-icon": { color }, "& .MuiChip-label": { whiteSpace: "normal", overflowWrap: "anywhere" } }} />
            </Box>
            <Stack direction="row" spacing={1} sx={{ gridColumn: { xs: 2, md: 4 }, gridRow: { xs: 3, md: 1 }, minWidth: 0, mb: { xs: 1, md: 0 } }}>
              <ActionIcon aria-hidden="true" sx={{ color, fontSize: 20, mt: 0.2, flexShrink: 0 }} />
              <Box minWidth={0} sx={{ overflowWrap: "anywhere" }}><Typography variant="body2" fontWeight={700}>{action}</Typography>
                {item.reason?.trim() && <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "pre-wrap", mt: 0.25 }}>{item.reason}</Typography>}
              </Box>
            </Stack>
            <Stack direction="row" spacing={1} sx={{ gridColumn: { xs: 2, md: 5 }, gridRow: { xs: 4, md: 1 }, minWidth: 0 }}>
              <PersonOutline aria-hidden="true" sx={{ fontSize: 20, color: "text.secondary", flexShrink: 0 }} />
              <Typography variant="body2" sx={{ overflowWrap: "anywhere" }}>{item.actorName?.trim() || "ไม่ระบุผู้ดำเนินการ"}</Typography>
            </Stack>
          </Box>;
        })}
      </Box>}
  </Box>;
}
