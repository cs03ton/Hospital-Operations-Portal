import { useEffect, useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Alert, Box, Button, Card, Chip, CircularProgress, Grid, IconButton, Stack, Typography } from "@mui/material";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import PeopleOutlineIcon from "@mui/icons-material/PeopleOutline";
import MeetingRoomOutlinedIcon from "@mui/icons-material/MeetingRoomOutlined";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import AccessTimeOutlinedIcon from "@mui/icons-material/AccessTimeOutlined";
import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { getMeetingCalendar, getMeetingRooms, type MeetingBooking, type MeetingRoom } from "../api/meetingRoomApi";
import { bangkokDayjs } from "../utils/dateFormat";

import { MeetingRoomImage } from "../components/MeetingRoomImage";
import { usePermission } from "../context/PermissionContext";
import { PageHeader } from "../components/PageHeader";

type RoomState = "busy" | "soon" | "free" | "inactive";
const labels: Record<RoomState, string> = { busy: "กำลังใช้งาน", soon: "ใกล้ถึงใช้งาน", free: "ว่าง", inactive: "ไม่พร้อมใช้งาน" };
const colors: Record<RoomState, string> = { busy: "#16a765", soon: "#f0ad26", free: "#3b79e7", inactive: "#dd5260" };

export function roomStatus(room: MeetingRoom, bookings: MeetingBooking[], now: number): RoomState {
  if (!room.isActive) return "inactive";
  const confirmed = bookings.filter((booking) => booking.roomId === room.id && booking.status === "Confirmed");
  if (confirmed.some((booking) => Date.parse(booking.startAt) <= now && now < Date.parse(booking.endAt))) return "busy";
  if (confirmed.some((booking) => now < Date.parse(booking.startAt) && Date.parse(booking.startAt) - now <= 30 * 60_000)) return "soon";
  return "free";
}

const thaiTime = (value: string) => bangkokDayjs(value).format("HH:mm");
const thaiDate = (value: string) => bangkokDayjs(value).format("D/M/") + (bangkokDayjs(value).year() + 543);

export function MeetingRoomDashboardPage() {
  const { hasPermission } = usePermission();
  const [selectedDate, setSelectedDate] = useState(() => bangkokDayjs().format("YYYY-MM-DD"));
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => { const timer = window.setInterval(() => setNow(Date.now()), 30_000); return () => window.clearInterval(timer); }, []);
  const rooms = useQuery({ queryKey: ["meeting-rooms", "rooms", "all"], queryFn: () => getMeetingRooms(true), refetchInterval: 30_000 });
  const day = useQuery({ queryKey: ["meeting-rooms", "dashboard", selectedDate], queryFn: () => getMeetingCalendar(selectedDate, selectedDate), refetchInterval: 30_000 });
  const today = bangkokDayjs(now).format("YYYY-MM-DD");
  const futureEnd = bangkokDayjs(now).add(365, "day").format("YYYY-MM-DD");
  const current = useQuery({ queryKey: ["meeting-rooms", "dashboard", "current", today, futureEnd], queryFn: () => getMeetingCalendar(today, futureEnd), refetchInterval: 30_000 });
  const allRooms = rooms.data ?? [];
  const confirmedDay = (day.data ?? []).filter((booking) => booking.status === "Confirmed");
  const confirmedCurrent = (current.data ?? []).filter((booking) => booking.status === "Confirmed");
  const statuses = useMemo(() => new Map(allRooms.map((room) => [room.id, roomStatus(room, confirmedCurrent, now)])), [allRooms, confirmedCurrent, now]);
  const counts = { busy: 0, soon: 0, free: 0, inactive: 0 };
  statuses.forEach((status) => { counts[status]++; });
  const upcoming = confirmedCurrent.filter((booking) => Date.parse(booking.startAt) > now).sort((a, b) => Date.parse(a.startAt) - Date.parse(b.startAt)).slice(0, 5);
  const hours = Array.from({ length: 10 }, (_, i) => i + 8);
  for (const booking of confirmedDay) {
    const start = bangkokDayjs(booking.startAt).hour();
    const end = bangkokDayjs(booking.endAt).hour() + (bangkokDayjs(booking.endAt).minute() > 0 ? 1 : 0);
    for (let hour = Math.min(8, start); hour < Math.max(18, end); hour++) if (!hours.includes(hour)) hours.push(hour);
  }
  hours.sort((a, b) => a - b);
  const bookedPerHour = hours.map((hour) => confirmedDay.filter((booking) => {
    const slotStart = bangkokDayjs(`${selectedDate}T00:00:00+07:00`).hour(hour).valueOf();
    return Date.parse(booking.startAt) < slotStart + 3_600_000 && Date.parse(booking.endAt) > slotStart;
  }).length);
  const changeDay = (offset: number) => setSelectedDate(bangkokDayjs(`${selectedDate}T12:00:00+07:00`).add(offset, "day").format("YYYY-MM-DD"));

  return <Box sx={{ pb: 4 }}>
    <PageHeader title="Dashboard ห้องประชุม" subtitle="ตรวจสอบสถานะและตารางการใช้ห้องประชุมของโรงพยาบาล" />
    <Card sx={{ p: { xs: 2, md: 3 }, mb: 2, borderRadius: 3, background: "linear-gradient(110deg,#fff,#fffaf0)", border: "1px solid #e9ddc9" }}>
      <Stack direction={{ xs: "column", lg: "row" }} justifyContent="space-between" alignItems={{ lg: "center" }} gap={2}>
        <Box sx={{ minWidth: 0 }}><Typography variant="h5" fontWeight={900} color="primary.main">ภาพรวมการใช้งานห้องประชุม</Typography><Typography color="text.secondary">สถานะห้องปัจจุบันและตารางการจองของวันที่เลือก</Typography></Box>
        <Stack direction={{ xs: "column", sm: "row" }} alignItems={{ sm: "center" }} gap={1} sx={{ width: { xs: "100%", lg: "auto" }, flexShrink: 0 }}>
          <Stack direction="row" alignItems="center" gap={0.5} sx={{ width: { xs: "100%", sm: 330 }, minWidth: 0 }}>
            <IconButton onClick={() => changeDay(-1)} aria-label="วันก่อนหน้า" sx={{ flexShrink: 0, border: "1px solid", borderColor: "divider", borderRadius: 1.5 }}><ChevronLeftOutlinedIcon /></IconButton>
            <Box sx={{ flex: 1, minWidth: 0 }}><AppDatePicker label="วันที่แสดงตาราง" value={selectedDate} onChange={(value) => value && setSelectedDate(value)} /></Box>
            <IconButton onClick={() => changeDay(1)} aria-label="วันถัดไป" sx={{ flexShrink: 0, border: "1px solid", borderColor: "divider", borderRadius: 1.5 }}><ChevronRightOutlinedIcon /></IconButton>
          </Stack>
          {hasPermission("MeetingRoom.Booking.Create") && <Button component={RouterLink} to="/meeting-rooms/new" variant="contained" startIcon={<AddOutlinedIcon />} sx={{ minHeight: 40, whiteSpace: "nowrap", alignSelf: { xs: "stretch", sm: "center" } }}>จองห้องประชุม</Button>}
        </Stack>
      </Stack>
    </Card>
    {(rooms.isError || day.isError || current.isError) && <Alert severity="error" sx={{ mb: 2 }}>โหลดข้อมูลห้องประชุมไม่สำเร็จ <Button onClick={() => { rooms.refetch(); day.refetch(); current.refetch(); }}>ลองใหม่</Button></Alert>}
    {(rooms.isLoading || day.isLoading || current.isLoading) && <Stack alignItems="center" py={3}><CircularProgress/></Stack>}
    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "repeat(2, minmax(0, 1fr))", sm: "repeat(3, minmax(0, 1fr))", lg: "repeat(5, minmax(0, 1fr))" }, gap: 1.5, mb: 2 }}>
      {([
        { label: "ห้องประชุมทั้งหมด", count: allRooms.length, color: "#215d4c", tint: "#e2f1eb", icon: MeetingRoomOutlinedIcon },
        { label: "กำลังใช้งาน", count: counts.busy, color: colors.busy, tint: "#e2f8e9", icon: GroupsOutlinedIcon },
        { label: "ว่างอยู่", count: counts.free, color: colors.free, tint: "#e7efff", icon: CheckCircleOutlineIcon },
        { label: "ใกล้ถึงใช้งาน", count: counts.soon, color: colors.soon, tint: "#fff1d6", icon: AccessTimeOutlinedIcon },
        { label: "ไม่พร้อมใช้งาน", count: counts.inactive, color: colors.inactive, tint: "#ffe8eb", icon: BuildOutlinedIcon },
      ]).map(({ label, count, color, tint, icon: Icon }) => <Card key={label} sx={{ p: { xs: 1.5, md: 2 }, borderRadius: 2.5, border: "1px solid #e8e5df", height: "100%", minWidth: 0 }}>
        <Stack direction="row" alignItems="center" gap={1.5}>
          <Box sx={{ width: 46, height: 46, flexShrink: 0, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: tint, color }}><Icon /></Box>
          <Box sx={{ minWidth: 0 }}><Typography variant="body2" color="text.secondary" fontWeight={700}>{label}</Typography><Typography variant="h4" fontWeight={900} sx={{ color, lineHeight: 1.15 }}>{count} <Typography component="span" variant="body2">ห้อง</Typography></Typography></Box>
        </Stack>
      </Card>)}
    </Box>
    <Grid container spacing={2}><Grid item xs={12} lg={7}><Card sx={{ p: 2, borderRadius: 3, border: "1px solid #e8e5df" }}><Typography variant="h6" fontWeight={800} mb={2}>สถานะห้องประชุม <Typography component="span" variant="caption" color="text.secondary">ณ เวลาปัจจุบัน</Typography></Typography><Grid container spacing={1.5}>{allRooms.map((room) => { const status = statuses.get(room.id) ?? "free"; return <Grid item key={room.id} xs={12} sm={6}><Card variant="outlined" sx={{ overflow: "hidden", borderRadius: 2, height: "100%" }}><MeetingRoomImage photoUrl={room.photoUrl} alt={room.photoUrl ? `รูป ${room.name}` : "ภาพประกอบห้องประชุม"} /><Box p={1.5}><Stack direction="row" justifyContent="space-between"><Typography fontWeight={800}>{room.name}</Typography><Chip size="small" label={labels[status]} sx={{ bgcolor: colors[status], color: "white", fontWeight: 700 }}/></Stack><Typography variant="body2" color="text.secondary">{room.location} · <PeopleOutlineIcon sx={{ fontSize: 14, verticalAlign: "middle" }}/> {room.capacity} คน</Typography><Button size="small" component={RouterLink} to={`/meeting-rooms/calendar?roomId=${room.id}`}>ดูรายละเอียด</Button></Box></Card></Grid>; })}</Grid>{allRooms.length === 0 && <Typography color="text.secondary">ยังไม่มีห้องประชุม</Typography>}</Card></Grid>
    <Grid item xs={12} lg={5}><Stack spacing={2}><Card sx={{ p: 2, borderRadius: 3, border: "1px solid #e8e5df", overflowX: "auto" }}><Typography variant="h6" fontWeight={800}>ตารางการใช้งานวันที่ {thaiDate(`${selectedDate}T12:00:00+07:00`)}</Typography><Box sx={{ minWidth: 540, mt: 2 }}><Stack direction="row" pl="120px" mb={1}>{hours.map((hour) => <Typography key={hour} variant="caption" sx={{ flex: 1, textAlign: "center" }}>{String(hour).padStart(2,"0")}:00</Typography>)}</Stack>{allRooms.map((room) => <Stack key={room.id} direction="row" alignItems="center" mb={0.75}><Typography variant="caption" noWrap sx={{ width: 120, fontWeight: 700 }}>{room.name}</Typography><Box sx={{ position: "relative", flex: 1, height: 31, bgcolor: "#eff3f5", borderRadius: 1, backgroundImage: "linear-gradient(to right,#dfe6e8 1px,transparent 1px)", backgroundSize: `${100 / hours.length}% 100%` }}>{confirmedDay.filter((booking) => booking.roomId === room.id).map((booking) => { const origin = bangkokDayjs(`${selectedDate}T00:00:00+07:00`).hour(hours[0]).valueOf(); const begin = Math.max(0, (Date.parse(booking.startAt) - origin) / 3_600_000); const finish = Math.min(hours.length, (Date.parse(booking.endAt) - origin) / 3_600_000); return <Box key={booking.id} title={`${booking.subject} ${thaiTime(booking.startAt)}–${thaiTime(booking.endAt)}`} sx={{ position: "absolute", left: `${begin / hours.length * 100}%`, width: `${Math.max(0, finish - begin) / hours.length * 100}%`, height: "100%", bgcolor: "#a6e3bc", borderRadius: 1, overflow: "hidden", px: .5, fontSize: 10, fontWeight: 700, whiteSpace: "nowrap" }}>{booking.subject}</Box>; })}</Box></Stack>)}</Box></Card>
    <Card sx={{ p: 2, borderRadius: 3, border: "1px solid #e8e5df" }}><Typography variant="h6" fontWeight={800}>การจองที่จะมาถึง</Typography>{upcoming.length === 0 && <Typography color="text.secondary">ไม่มีรายการจองที่กำลังจะมาถึง</Typography>}{upcoming.map((booking) => <Button key={booking.id} component={RouterLink} to={`/meeting-rooms/bookings/${booking.id}`} fullWidth sx={{ justifyContent: "space-between", textAlign: "left", borderBottom: "1px solid #eee", py: 1.5 }}><Box><Typography fontWeight={700}>{booking.subject}</Typography><Typography variant="caption">{booking.roomName} · {thaiDate(booking.startAt)}</Typography></Box><Typography variant="caption">{thaiTime(booking.startAt)}–{thaiTime(booking.endAt)}</Typography></Button>)}</Card></Stack></Grid></Grid>
    <Grid container spacing={2} mt={.25}><Grid item xs={12} md={7}><Card sx={{ p: 2, borderRadius: 3, border: "1px solid #e8e5df" }}><Typography variant="h6" fontWeight={800}>การใช้งานรายชั่วโมง</Typography><Stack direction="row" alignItems="end" gap={.5} height={130} mt={2}>{hours.map((hour, index) => <Box key={hour} sx={{ flex: 1, minWidth: 20, textAlign: "center" }}><Box title={`${bookedPerHour[index]} ห้อง`} sx={{ height: `${Math.max(4, bookedPerHour[index] / Math.max(1, allRooms.length) * 110)}px`, bgcolor: "#64c995", borderRadius: "4px 4px 0 0" }}/><Typography variant="caption">{hour}:00</Typography></Box>)}</Stack></Card></Grid><Grid item xs={12} md={5}><Card sx={{ p: 2, borderRadius: 3, border: "1px solid #e8e5df" }}><Typography variant="h6" fontWeight={800}>สัดส่วนสถานะห้องปัจจุบัน</Typography><Stack direction="row" alignItems="center" gap={3} mt={2}><Box sx={{ width: 110, height: 110, borderRadius: "50%", background: `conic-gradient(${colors.busy} 0 ${counts.busy / Math.max(1, allRooms.length) * 100}%, ${colors.free} 0 ${(counts.busy + counts.free) / Math.max(1, allRooms.length) * 100}%, ${colors.soon} 0 ${(counts.busy + counts.free + counts.soon) / Math.max(1, allRooms.length) * 100}%, ${colors.inactive} 0)` }}/><Stack>{(["busy", "free", "soon", "inactive"] as RoomState[]).map((status) => <Typography key={status} variant="body2" sx={{ color: colors[status] }}>{labels[status]} {counts[status]} ห้อง</Typography>)}</Stack></Stack></Card></Grid></Grid>
    <Button onClick={() => { setNow(Date.now()); current.refetch(); day.refetch(); }} sx={{ mt: 2 }}>อัปเดตสถานะ</Button>
  </Box>;
}


