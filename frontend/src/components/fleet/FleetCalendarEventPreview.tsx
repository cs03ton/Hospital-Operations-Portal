import type { ReactElement } from "react";
import { Box, Chip, Stack, Tooltip, Typography } from "@mui/material";
import type { FleetCalendarEvent } from "../../api/fleetApi";
import { formatThaiDateTime } from "../../utils/dateFormat";
import { getFleetStatusLabel } from "../../utils/fleetLabels";
import { brandColors } from "../../theme/theme";

/** Read-only preview; touch users keep the calendar's existing tap-to-select action. */
export function FleetCalendarEventPreview({ event, children }: { event: FleetCalendarEvent; children: ReactElement }) {
  const metadata = Object.fromEntries(Object.entries(event.metadata ?? {}).map(([key, value]) =>
    [key.replace(/[_\s-]/g, "").toLowerCase(), value]));
  const text = (key: string) => {
    const value = metadata[key];
    return typeof value === "string" && value.trim() ? value : undefined;
  };
  const isRequest = ["REQUEST", "ASSIGNMENT", "TRIP"].includes(event.eventType);
  const vehicle = [text("vehicle"), text("registrationnumber")].filter(Boolean).join(" · ");
  const rows = isRequest ? [
    ["ผู้ขอ", text("requester") ?? text("requestedby") ?? "-"],
    ["หน่วยงาน", text("department") ?? "-"],
    ["ปลายทาง", text("destination") ?? "-"],
    ["วัตถุประสงค์", text("purpose") ?? "-"],
    ["รถ / ทะเบียน", vehicle || "ยังไม่จัดรถ"],
    ["คนขับ", text("driver") ?? "ยังไม่มอบหมาย"],
    ["ผู้ร่วมเดินทาง", typeof metadata.passengercount === "number" ? `${metadata.passengercount.toLocaleString("th-TH")} คน` : "-"],
  ] : text("reason") ? [["รายละเอียด", text("reason")!]] : [];

  return <Tooltip
    describeChild arrow placement="right" enterDelay={250} enterNextDelay={250} leaveDelay={100} disableTouchListener
    PopperProps={{ modifiers: [{ name: "preventOverflow", options: { padding: 12 } }] }}
    componentsProps={{
      tooltip: { sx: { bgcolor: "background.paper", color: "text.primary", maxWidth: "min(360px, calc(100vw - 32px))", width: 320, p: 0, border: "1px solid", borderColor: "divider", borderRadius: 2, boxShadow: "0 8px 28px rgba(16, 67, 56, 0.18)", overflow: "hidden" } },
      arrow: { sx: { color: "background.paper", "&::before": { border: "1px solid", borderColor: "divider" } } },
    }}
    title={<Box>
      <Box sx={{ px: 1.75, py: 1.25, bgcolor: brandColors.accentSoft, borderTop: `3px solid ${brandColors.accent}` }}>
        <Typography variant="subtitle2" fontWeight={900} color="primary.dark" sx={{ overflowWrap: "anywhere" }}>{text("requestno") ?? event.title}</Typography>
        <Chip size="small" label={getFleetStatusLabel(event.status)} sx={{ mt: 0.5, height: 22, fontWeight: 700 }} />
      </Box>
      <Stack spacing={0.7} sx={{ px: 1.75, py: 1.25 }}>
        <PreviewRow label={isRequest ? "ออกเดินทาง" : "เริ่มต้น"} value={formatThaiDateTime(event.startAt)} />
        <PreviewRow label={isRequest ? "คาดว่าจะกลับ" : "สิ้นสุด"} value={formatThaiDateTime(event.endAt)} />
        {rows.map(([label, value]) => <PreviewRow key={label} label={label} value={value} />)}
        {event.eventType === "TRIP" && event.status === "IN_PROGRESS" && !metadata.actualendat && !metadata.abortedat &&
          <Typography variant="caption" color="warning.dark" fontWeight={700}>ยังไม่บันทึกจบทริป</Typography>}
        <Typography variant="caption" color="text.secondary" sx={{ pt: 0.4 }}>คลิกเพื่อดูรายละเอียด</Typography>
      </Stack>
    </Box>}
  >{children}</Tooltip>;
}

function PreviewRow({ label, value }: { label: string; value: string }) {
  return <Box sx={{ display: "grid", gridTemplateColumns: "86px minmax(0, 1fr)", columnGap: 1 }}>
    <Typography variant="caption" color="text.secondary">{label}</Typography>
    <Typography variant="caption" fontWeight={600} sx={{ overflowWrap: "anywhere", display: "-webkit-box", WebkitLineClamp: 3, WebkitBoxOrient: "vertical", overflow: "hidden" }}>{value}</Typography>
  </Box>;
}
