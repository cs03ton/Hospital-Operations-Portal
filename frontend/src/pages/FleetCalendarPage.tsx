import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ArrowForwardIosOutlinedIcon from "@mui/icons-material/ArrowForwardIosOutlined";
import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import BuildCircleOutlinedIcon from "@mui/icons-material/BuildCircleOutlined";
import CalendarTodayOutlinedIcon from "@mui/icons-material/CalendarTodayOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import DirectionsCarFilledOutlinedIcon from "@mui/icons-material/DirectionsCarFilledOutlined";
import EventNoteOutlinedIcon from "@mui/icons-material/EventNoteOutlined";
import FilterAltOutlinedIcon from "@mui/icons-material/FilterAltOutlined";
import PersonOffOutlinedIcon from "@mui/icons-material/PersonOffOutlined";
import ScheduleOutlinedIcon from "@mui/icons-material/ScheduleOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  IconButton,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { alpha } from "@mui/material/styles";
import dayjs, { type Dayjs } from "dayjs";
import "dayjs/locale/th";
import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getFleetCalendar, type FleetCalendarEvent } from "../api/fleetApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";
import { brandColors } from "../theme/theme";

dayjs.locale("th");

type CalendarView = "week" | "month" | "agenda";

const eventTypes = [
  "REQUEST",
  "ASSIGNMENT",
  "TRIP",
  "VEHICLE_UNAVAILABILITY",
  "DRIVER_UNAVAILABILITY",
  "MAINTENANCE",
  "VEHICLE_DOCUMENT_EXPIRY",
  "CANCELLATION_REQUEST",
] as const;

const eventTypeLabels: Record<string, string> = {
  REQUEST: "คำขอใช้รถ",
  ASSIGNMENT: "การจัดรถและคนขับ",
  TRIP: "การเดินทาง",
  VEHICLE_UNAVAILABILITY: "รถไม่พร้อมใช้งาน",
  DRIVER_UNAVAILABILITY: "คนขับไม่พร้อมใช้งาน",
  MAINTENANCE: "งานบำรุงรักษา",
  VEHICLE_DOCUMENT_EXPIRY: "เอกสารรถใกล้หมดอายุ",
  CANCELLATION_REQUEST: "คำขอยกเลิก",
};

const startHour = 6;
const endHour = 19;
const hourHeight = 48;
const emptyCalendarEvents: FleetCalendarEvent[] = [];

export function FleetCalendarPage() {
  const [params, setParams] = useSearchParams();
  const initialDate = dayjs(params.get("date") || undefined).isValid() ? dayjs(params.get("date") || undefined) : dayjs();
  const [anchorDate, setAnchorDate] = useState(initialDate.startOf("day"));
  const [view, setView] = useState<CalendarView>((params.get("view") as CalendarView) || "week");
  const [eventType, setEventType] = useState("");
  const [status, setStatus] = useState("");
  const [keyword, setKeyword] = useState("");
  const [showFilters, setShowFilters] = useState(true);
  const [selectedEvent, setSelectedEvent] = useState<FleetCalendarEvent | null>(null);

  const period = useMemo(() => resolvePeriod(anchorDate, view), [anchorDate, view]);
  const queryParams = useMemo(() => {
    const query = new URLSearchParams();
    query.set("start", period.start.format("YYYY-MM-DD"));
    query.set("end", period.end.format("YYYY-MM-DD"));
    if (eventType) query.set("eventTypes", eventType);
    if (status) query.set("statuses", status);
    return query;
  }, [eventType, period, status]);
  const calendarQuery = useQuery({
    queryKey: ["fleet-calendar", queryParams.toString()],
    queryFn: () => getFleetCalendar(queryParams),
    retry: false,
  });
  const events = calendarQuery.data ?? emptyCalendarEvents;
  const filteredEvents = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    return events.filter((event) => {
      const searchable = [event.title, eventTypeLabels[event.eventType], getFleetStatusLabel(event.status)]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return !normalizedKeyword || searchable.includes(normalizedKeyword);
    });
  }, [events, keyword]);
  const displayEvents = useMemo(() => deduplicateRequestEvents(filteredEvents), [filteredEvents]);
  const statusOptions = useMemo(() => Array.from(new Set(events.map((event) => event.status))).sort(), [events]);

  const changePeriod = (direction: -1 | 1) => {
    const next = view === "month" ? anchorDate.add(direction, "month") : anchorDate.add(direction * 7, "day");
    updateDate(next);
  };
  const updateDate = (date: Dayjs) => {
    const normalized = date.startOf("day");
    setAnchorDate(normalized);
    setParams((current) => {
      current.set("date", normalized.format("YYYY-MM-DD"));
      current.set("view", view);
      return current;
    }, { replace: true });
  };
  const updateView = (nextView: CalendarView) => {
    setView(nextView);
    setParams((current) => {
      current.set("date", anchorDate.format("YYYY-MM-DD"));
      current.set("view", nextView);
      return current;
    }, { replace: true });
  };

  return (
    <Box>
      <Stack direction={{ xs: "column", lg: "row" }} justifyContent="space-between" spacing={1.5} sx={{ mb: 2 }}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Tooltip title="ช่วงก่อนหน้า"><IconButton onClick={() => changePeriod(-1)}><ChevronLeftOutlinedIcon /></IconButton></Tooltip>
          <Button variant="outlined" onClick={() => updateDate(dayjs())}>วันนี้</Button>
          <Tooltip title="ช่วงถัดไป"><IconButton onClick={() => changePeriod(1)}><ChevronRightOutlinedIcon /></IconButton></Tooltip>
          <Typography variant="h6" color="primary" fontWeight={900}>{formatPeriodLabel(period.start, period.end, view)}</Typography>
          <CalendarTodayOutlinedIcon color="action" fontSize="small" />
        </Stack>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          <ToggleButtonGroup exclusive size="small" value={view} onChange={(_, value: CalendarView | null) => value && updateView(value)}>
            <ToggleButton value="week">สัปดาห์</ToggleButton>
            <ToggleButton value="month">เดือน</ToggleButton>
            <ToggleButton value="agenda">รายการ</ToggleButton>
          </ToggleButtonGroup>
          <Button variant={showFilters ? "contained" : "outlined"} startIcon={<FilterAltOutlinedIcon />} onClick={() => setShowFilters((value) => !value)}>ตัวกรอง</Button>
          <Button component={Link} to="/fleet/requests/create" variant="contained" startIcon={<AddOutlinedIcon />}>สร้างคำขอ</Button>
        </Stack>
      </Stack>

      {showFilters && (
        <Card sx={{ mb: 2 }}>
          <CardContent sx={{ py: 2 }}>
            <Grid container spacing={1.5} alignItems="center">
              <Grid item xs={12} sm={6} md={3}>
                <TextField select fullWidth size="small" label="ประเภทกิจกรรม" value={eventType} onChange={(event) => setEventType(event.target.value)}>
                  <MenuItem value="">ทุกประเภท</MenuItem>
                  {eventTypes.map((value) => <MenuItem key={value} value={value}>{eventTypeLabels[value]}</MenuItem>)}
                </TextField>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <TextField select fullWidth size="small" label="สถานะ" value={status} onChange={(event) => setStatus(event.target.value)}>
                  <MenuItem value="">ทุกสถานะ</MenuItem>
                  {statusOptions.map((value) => <MenuItem key={value} value={value}>{getFleetStatusLabel(value)}</MenuItem>)}
                </TextField>
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  size="small"
                  label="ค้นหา"
                  placeholder="ค้นหาเลขคำขอ ปลายทาง รถ คนขับ หรือกิจกรรม"
                  value={keyword}
                  onChange={(event) => setKeyword(event.target.value)}
                  InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment> }}
                />
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {calendarQuery.isError && <Alert severity="error" sx={{ mb: 2 }} action={<Button onClick={() => void calendarQuery.refetch()}>ลองใหม่</Button>}>โหลดปฏิทินรถไม่สำเร็จ</Alert>}

      <Grid container spacing={2} alignItems="flex-start">
        <Grid item xs={12} lg={selectedEvent ? 9 : 12}>
          <Card>
            <CardContent sx={{ p: { xs: 1, md: 1.5 }, "&:last-child": { pb: { xs: 1, md: 1.5 } } }}>
              {calendarQuery.isLoading ? (
                <LoadingState message="กำลังโหลดปฏิทินรถ..." />
              ) : (
                <Stack spacing={1.5}>
                  {filteredEvents.length === 0 && view !== "agenda" && (
                    <Alert severity="info">ไม่มีกิจกรรมในช่วงนี้ ตารางยังพร้อมสำหรับตรวจสอบช่วงเวลาอื่น</Alert>
                  )}
                  {view === "week" ? (
                    <WeekCalendar start={period.start} events={displayEvents} selectedId={selectedEvent?.id} onSelect={setSelectedEvent} />
                  ) : view === "month" ? (
                    <MonthCalendar month={anchorDate} events={displayEvents} selectedId={selectedEvent?.id} onSelect={setSelectedEvent} />
                  ) : filteredEvents.length ? (
                    <AgendaCalendar events={displayEvents} selectedId={selectedEvent?.id} onSelect={setSelectedEvent} />
                  ) : (
                    <EmptyState icon={CalendarTodayOutlinedIcon} title="ไม่มีกิจกรรมในช่วงนี้" description="ลองเปลี่ยนช่วงเวลา หรือล้างตัวกรองเพื่อดูรายการอื่น" />
                  )}
                </Stack>
              )}
            </CardContent>
          </Card>
          <CalendarLegend />
        </Grid>
        {selectedEvent && (
          <Grid item xs={12} lg={3}>
            <EventDetail event={selectedEvent} onClose={() => setSelectedEvent(null)} />
            <WeekSummary events={displayEvents} />
          </Grid>
        )}
      </Grid>
    </Box>
  );
}

function WeekCalendar({ start, events, selectedId, onSelect }: { start: Dayjs; events: FleetCalendarEvent[]; selectedId?: string; onSelect: (event: FleetCalendarEvent) => void }) {
  const days = Array.from({ length: 7 }, (_, index) => start.add(index, "day"));
  const timedEvents = deduplicateRequestEvents(events.filter((event) => !event.isAllDay));
  const allDayEvents = events.filter((event) => event.isAllDay);
  return (
    <Box sx={{ overflowX: "auto" }}>
      <Box sx={{ minWidth: 920 }}>
        <Box sx={{ display: "grid", gridTemplateColumns: "62px repeat(7, minmax(120px, 1fr))", borderBottom: 1, borderColor: "divider" }}>
          <Box />
          {days.map((day) => {
            const isToday = day.isSame(dayjs(), "day");
            return <Box key={day.format("YYYY-MM-DD")} aria-current={isToday ? "date" : undefined} sx={{ py: 1, textAlign: "center", borderLeft: 1, borderColor: isToday ? brandColors.accent : "divider", background: isToday ? `linear-gradient(135deg, ${alpha(brandColors.accentSoft, 0.5)} 0%, ${alpha(brandColors.accent, 0.22)} 100%)` : "transparent", boxShadow: isToday ? `inset 0 3px 0 ${brandColors.accent}, 0 3px 12px ${alpha(brandColors.accent, 0.2)}` : "none" }}><Stack direction="row" spacing={0.75} justifyContent="center" alignItems="center"><Typography fontWeight={isToday ? 900 : 700} color={isToday ? "primary.dark" : "text.primary"}>{day.format("ddd D MMM")}</Typography>{isToday && <Chip label="วันนี้" size="small" color="primary" sx={{ height: 22, fontWeight: 800, boxShadow: `0 2px 6px ${alpha(brandColors.primaryDark, 0.2)}`, "& .MuiChip-label": { px: 0.8 } }} />}</Stack></Box>;
          })}
        </Box>
        <Box sx={{ display: "grid", gridTemplateColumns: "62px repeat(7, minmax(120px, 1fr))", minHeight: 42, borderBottom: 1, borderColor: "divider" }}>
          <Typography variant="caption" sx={{ p: 1 }}>ทั้งวัน</Typography>
          {days.map((day) => { const isToday = day.isSame(dayjs(), "day"); return <Stack key={day.format("YYYY-MM-DD")} aria-current={isToday ? "date" : undefined} spacing={0.5} sx={{ p: 0.5, borderLeft: 1, borderColor: isToday ? brandColors.accent : "divider", bgcolor: isToday ? alpha(brandColors.accentSoft, 0.18) : "transparent" }}>{allDayEvents.filter((event) => eventOccursOn(event, day)).map((event) => <MiniEvent key={event.id} event={event} selected={event.id === selectedId} onSelect={onSelect} />)}</Stack>; })}
        </Box>
        <Box sx={{ display: "grid", gridTemplateColumns: "62px repeat(7, minmax(120px, 1fr))" }}>
          <Box sx={{ position: "relative", height: (endHour - startHour) * hourHeight }}>
            {Array.from({ length: endHour - startHour + 1 }, (_, index) => <Typography key={index} variant="caption" color="text.secondary" sx={{ position: "absolute", top: index * hourHeight - 8, right: 8 }}>{String(startHour + index).padStart(2, "0")}:00</Typography>)}
          </Box>
          {days.map((day) => {
            const isToday = day.isSame(dayjs(), "day");
            return (
            <Box key={day.format("YYYY-MM-DD")} aria-current={isToday ? "date" : undefined} sx={(theme) => ({ position: "relative", height: (endHour - startHour) * hourHeight, borderLeft: 1, borderColor: isToday ? brandColors.accent : "divider", bgcolor: isToday ? alpha(brandColors.accentSoft, 0.14) : "transparent", backgroundImage: `repeating-linear-gradient(to bottom, transparent 0, transparent ${hourHeight - 1}px, ${theme.palette.divider} ${hourHeight}px)`, boxShadow: isToday ? `inset 1px 0 0 ${alpha(brandColors.accent, 0.35)}, inset -1px 0 0 ${alpha(brandColors.accent, 0.35)}` : "none" })}>
              {groupOverlappingEvents(timedEvents.filter((event) => eventOccursOn(event, day)), day).map((group) => (
                <TimedEventGroup key={group.map((event) => event.id).join("-")} events={group} day={day} selectedId={selectedId} onSelect={onSelect} />
              ))}
              {isToday && dayjs().hour() >= startHour && dayjs().hour() <= endHour && <Box sx={{ position: "absolute", zIndex: 3, left: 0, right: 0, top: ((dayjs().hour() - startHour) * 60 + dayjs().minute()) / 60 * hourHeight, borderTop: "2px solid", borderColor: "error.main" }} />}
            </Box>
          ); })}
        </Box>
      </Box>
    </Box>
  );
}

function TimedEventGroup({ events, day, selectedId, onSelect }: { events: FleetCalendarEvent[]; day: Dayjs; selectedId?: string; onSelect: (event: FleetCalendarEvent) => void }) {
  const dayStart = day.startOf("day").hour(startHour);
  const dayEnd = day.startOf("day").hour(endHour);
  const visibleStart = events.reduce((earliest, event) => {
    const start = dayjs(event.startAt).isBefore(dayStart) ? dayStart : dayjs(event.startAt);
    return start.isBefore(earliest) ? start : earliest;
  }, dayEnd);
  const visibleEnd = events.reduce((latest, event) => {
    const end = dayjs(event.endAt).isAfter(dayEnd) ? dayEnd : dayjs(event.endAt);
    return end.isAfter(latest) ? end : latest;
  }, dayStart);
  const top = Math.max(0, (visibleStart.hour() - startHour) * hourHeight + visibleStart.minute() / 60 * hourHeight);
  const height = Math.max(34, visibleEnd.diff(visibleStart, "minute") / 60 * hourHeight);
  const isGroup = events.length > 1;
  return (
    <Box sx={{ position: "absolute", zIndex: events.some((event) => event.id === selectedId) ? 4 : 2, top, left: 4, right: 4, height, minHeight: 34, overflow: "hidden", border: "1px solid", borderColor: events.some((event) => event.id === selectedId) ? "primary.main" : "divider", borderRadius: 1.5, bgcolor: "background.paper", boxShadow: events.some((event) => event.id === selectedId) ? 3 : 1, display: "flex", flexDirection: "column" }}>
      {isGroup && <Typography variant="caption" fontWeight={800} color="primary.dark" sx={{ px: 0.75, py: 0.35, bgcolor: "primary.50", borderBottom: 1, borderColor: "divider" }}>{events.length} รายการในช่วงเวลานี้</Typography>}
      <Stack spacing={0.4} sx={{ p: 0.45, overflowY: "auto", minHeight: 0 }}>
        {events.map((event) => {
          const colors = eventColors(event);
          const selected = event.id === selectedId;
          return (
            <Box key={event.id} component="button" type="button" onClick={() => onSelect(event)} sx={{ flexShrink: 0, width: "100%", border: "1px solid", borderColor: selected ? "primary.main" : colors.border, borderRadius: 1, bgcolor: colors.background, px: 0.65, py: 0.5, textAlign: "left", cursor: "pointer", font: "inherit", boxShadow: selected ? 2 : "none", "&:hover": { filter: "brightness(0.98)", borderColor: "primary.main" } }}>
              <Typography variant="caption" color="text.secondary" display="block">{dayjs(event.startAt).format("HH:mm")} - {dayjs(event.endAt).format("HH:mm")}</Typography>
              <Typography variant="caption" fontWeight={900} color="primary.dark" display="block" sx={{ overflowWrap: "anywhere" }}>{event.title}</Typography>
              {(isGroup || height >= 65) && <Typography variant="caption" color="text.secondary" display="block">{getFleetStatusLabel(event.status)}</Typography>}
            </Box>
          );
        })}
      </Stack>
    </Box>
  );
}

function groupOverlappingEvents(events: FleetCalendarEvent[], day: Dayjs) {
  const dayStart = day.startOf("day").hour(startHour);
  const dayEnd = day.startOf("day").hour(endHour);
  const sorted = [...events].sort((left, right) => dayjs(left.startAt).valueOf() - dayjs(right.startAt).valueOf());
  const groups: FleetCalendarEvent[][] = [];
  let currentEnd: Dayjs | null = null;

  sorted.forEach((event) => {
    const visibleStart = dayjs(event.startAt).isBefore(dayStart) ? dayStart : dayjs(event.startAt);
    const visibleEnd = dayjs(event.endAt).isAfter(dayEnd) ? dayEnd : dayjs(event.endAt);
    if (!currentEnd || !visibleStart.isBefore(currentEnd)) {
      groups.push([event]);
      currentEnd = visibleEnd;
      return;
    }
    groups[groups.length - 1].push(event);
    if (visibleEnd.isAfter(currentEnd)) currentEnd = visibleEnd;
  });

  return groups;
}

function deduplicateRequestEvents(events: FleetCalendarEvent[]) {
  const eventPriority: Record<string, number> = { REQUEST: 4, TRIP: 3, ASSIGNMENT: 2, CANCELLATION_REQUEST: 1 };
  const requestEvents = new Map<string, FleetCalendarEvent>();
  const standaloneEvents: FleetCalendarEvent[] = [];

  events.forEach((event) => {
    if (!event.requestId) {
      standaloneEvents.push(event);
      return;
    }
    const current = requestEvents.get(event.requestId);
    if (!current || (eventPriority[event.eventType] ?? 0) > (eventPriority[current.eventType] ?? 0)) requestEvents.set(event.requestId, event);
  });

  return [...requestEvents.values(), ...standaloneEvents];
}

function MiniEvent({ event, selected, onSelect }: { event: FleetCalendarEvent; selected: boolean; onSelect: (event: FleetCalendarEvent) => void }) {
  const colors = eventColors(event);
  return <Box component="button" type="button" onClick={() => onSelect(event)} sx={{ border: "1px solid", borderColor: selected ? "primary.main" : colors.border, bgcolor: colors.background, borderRadius: 1, px: 0.75, py: 0.35, textAlign: "left", cursor: "pointer" }}><Typography variant="caption" noWrap>{event.title}</Typography></Box>;
}

function MonthCalendar({ month, events, selectedId, onSelect }: { month: Dayjs; events: FleetCalendarEvent[]; selectedId?: string; onSelect: (event: FleetCalendarEvent) => void }) {
  const gridStart = month.startOf("month").startOf("week");
  const days = Array.from({ length: 42 }, (_, index) => gridStart.add(index, "day"));
  return <Box sx={{ minWidth: 720, overflowX: "auto" }}><Box sx={{ display: "grid", gridTemplateColumns: "repeat(7, 1fr)" }}>{["อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส."].map((label) => <Typography key={label} textAlign="center" fontWeight={800} sx={{ py: 1, borderBottom: 1, borderColor: "divider" }}>{label}</Typography>)}{days.map((day) => { const dayEvents = events.filter((event) => eventOccursOn(event, day)); const isToday = day.isSame(dayjs(), "day"); return <Box key={day.format("YYYY-MM-DD")} aria-current={isToday ? "date" : undefined} sx={{ minHeight: 118, p: 0.75, borderRight: 1, borderBottom: 1, borderColor: isToday ? brandColors.accent : "divider", opacity: day.month() === month.month() ? 1 : 0.45, background: isToday ? `linear-gradient(145deg, ${alpha(brandColors.accentSoft, 0.34)} 0%, ${alpha(brandColors.accent, 0.12)} 100%)` : "transparent", boxShadow: isToday ? `inset 0 3px 0 ${brandColors.accent}` : "none" }}><Stack direction="row" spacing={0.75} alignItems="center"><Box sx={{ minWidth: 28, height: 28, px: 0.5, borderRadius: 999, display: "grid", placeItems: "center", bgcolor: isToday ? brandColors.primary : "transparent", color: isToday ? "#fff" : brandColors.textPrimary, fontWeight: isToday ? 900 : 600, boxShadow: isToday ? `0 2px 7px ${alpha(brandColors.primaryDark, 0.24)}` : "none" }}>{day.date()}</Box>{isToday && <Typography variant="caption" color="primary" fontWeight={900}>วันนี้</Typography>}</Stack><Stack spacing={0.4} sx={{ mt: 0.5 }}>{dayEvents.slice(0, 3).map((event) => <MiniEvent key={event.id} event={event} selected={event.id === selectedId} onSelect={onSelect} />)}{dayEvents.length > 3 && <Typography variant="caption" color="primary">+{dayEvents.length - 3} รายการ</Typography>}</Stack></Box>; })}</Box></Box>;
}

function AgendaCalendar({ events, selectedId, onSelect }: { events: FleetCalendarEvent[]; selectedId?: string; onSelect: (event: FleetCalendarEvent) => void }) {
  return (
    <Stack spacing={1.25}>
      {events.map((event) => {
        const colors = eventColors(event);
        const selected = event.id === selectedId;
        return (
          <Card
            key={event.id}
            component="button"
            type="button"
            variant="outlined"
            onClick={() => onSelect(event)}
            sx={{
              width: "100%",
              p: 0,
              color: "text.primary",
              font: "inherit",
              textAlign: "left",
              cursor: "pointer",
              overflow: "hidden",
              borderLeft: "6px solid",
              borderLeftColor: colors.border,
              borderTopColor: selected ? colors.border : "divider",
              borderRightColor: selected ? colors.border : "divider",
              borderBottomColor: selected ? colors.border : "divider",
              background: `linear-gradient(105deg, ${alpha(colors.background, 0.9)} 0%, #FFFFFF 42%)`,
              boxShadow: selected ? `0 12px 28px ${alpha(colors.border, 0.2)}` : `0 4px 14px ${alpha(brandColors.primaryDark, 0.06)}`,
              transform: selected ? "translateY(-2px)" : "none",
              transition: "transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease",
              "&:hover": {
                transform: "translateY(-3px)",
                boxShadow: `0 14px 30px ${alpha(colors.border, 0.18)}`,
                borderColor: colors.border,
              },
              "&:focus-visible": {
                outline: `3px solid ${alpha(colors.border, 0.32)}`,
                outlineOffset: 2,
              },
            }}
          >
            <CardContent sx={{ py: 1.5, px: { xs: 1.5, sm: 2 }, "&:last-child": { pb: 1.5 } }}>
              <Stack direction="row" spacing={{ xs: 1.25, sm: 1.75 }} alignItems="center">
                <Box sx={{ width: 48, height: 48, flexShrink: 0, borderRadius: 2.25, display: "grid", placeItems: "center", color: colors.border, bgcolor: colors.background, border: "1px solid", borderColor: alpha(colors.border, 0.28), boxShadow: `inset 0 0 0 1px ${alpha("#FFFFFF", 0.7)}` }}>
                  {agendaEventIcon(event.eventType)}
                </Box>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography fontWeight={900} color="primary.dark" sx={{ fontSize: { xs: "0.98rem", sm: "1.08rem" }, overflowWrap: "anywhere" }}>
                    {event.title}
                  </Typography>
                  <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap sx={{ mt: 0.45 }}>
                    <Chip size="small" label={eventTypeLabels[event.eventType] ?? "กิจกรรมรถ"} sx={{ height: 23, bgcolor: alpha(colors.border, 0.1), color: "text.secondary", fontWeight: 700 }} />
                    <Stack direction="row" spacing={0.45} alignItems="center" color="text.secondary">
                      <ScheduleOutlinedIcon sx={{ fontSize: 16 }} />
                      <Typography variant="caption" fontWeight={600}>{formatThaiDateTime(event.startAt)}</Typography>
                    </Stack>
                  </Stack>
                </Box>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ flexShrink: 0 }}>
                  <Chip size="small" label={getFleetStatusLabel(event.status)} sx={{ display: { xs: "none", sm: "inline-flex" }, border: "1px solid", borderColor: alpha(colors.border, 0.42), bgcolor: colors.background, color: brandColors.textPrimary, fontWeight: 800 }} />
                  <Box sx={{ width: 30, height: 30, borderRadius: "50%", display: "grid", placeItems: "center", color: selected ? "primary.contrastText" : "primary.main", bgcolor: selected ? "primary.main" : alpha(brandColors.primary, 0.08), transition: "transform 180ms ease", ".MuiCard-root:hover &": { transform: "translateX(2px)" } }}>
                    <ArrowForwardIosOutlinedIcon sx={{ fontSize: 15 }} />
                  </Box>
                </Stack>
              </Stack>
              <Chip size="small" label={getFleetStatusLabel(event.status)} sx={{ display: { xs: "inline-flex", sm: "none" }, mt: 1, ml: 7.75, border: "1px solid", borderColor: alpha(colors.border, 0.42), bgcolor: colors.background, fontWeight: 800 }} />
            </CardContent>
          </Card>
        );
      })}
    </Stack>
  );
}

function agendaEventIcon(eventType: string) {
  if (eventType === "ASSIGNMENT") return <AssignmentOutlinedIcon />;
  if (eventType === "TRIP") return <DirectionsCarFilledOutlinedIcon />;
  if (eventType === "VEHICLE_UNAVAILABILITY" || eventType === "MAINTENANCE" || eventType === "VEHICLE_DOCUMENT_EXPIRY") return <BuildCircleOutlinedIcon />;
  if (eventType === "DRIVER_UNAVAILABILITY") return <PersonOffOutlinedIcon />;
  if (eventType === "CANCELLATION_REQUEST") return <CancelOutlinedIcon />;
  if (eventType === "REQUEST") return <EventNoteOutlinedIcon />;
  return <CalendarTodayOutlinedIcon />;
}

function EventDetail({ event, onClose }: { event: FleetCalendarEvent; onClose: () => void }) {
  const metadata = event.metadata || {};
  return <Card sx={{ mb: 2 }}><CardContent><Stack spacing={1.5}><Stack direction="row" justifyContent="space-between" alignItems="center"><Typography variant="h6" color="primary">รายละเอียด</Typography><Button size="small" onClick={onClose}>ปิด</Button></Stack><Chip size="small" sx={{ alignSelf: "flex-start" }} label={getFleetStatusLabel(event.status)} /><Typography fontWeight={900}>{event.title}</Typography><Detail label="ประเภท" value={eventTypeLabels[event.eventType] ?? "กิจกรรมอื่น"} /><Detail label="ช่วงเวลา" value={`${formatThaiDateTime(event.startAt)} ถึง ${formatThaiDateTime(event.endAt)}`} />{Object.entries(metadata).slice(0, 6).map(([key, value]) => <Detail key={key} label={metadataLabel(key)} value={formatMetadata(key, value)} />)}<Button component={Link} to={event.detailUrl} variant="outlined" fullWidth>ดูรายละเอียด</Button></Stack></CardContent></Card>;
}

function WeekSummary({ events }: { events: FleetCalendarEvent[] }) {
  const statusCounts = new Map<string, number>();
  events.forEach((event) => statusCounts.set(event.status, (statusCounts.get(event.status) ?? 0) + 1));
  return <Card><CardContent><Typography fontWeight={900} sx={{ mb: 1.5 }}>สรุปรายการในช่วงเวลานี้</Typography><Stack spacing={1}><Stack direction="row" justifyContent="space-between"><Typography variant="body2" color="text.secondary">รายการทั้งหมด</Typography><Typography variant="body2" fontWeight={900}>{events.length.toLocaleString("th-TH")} รายการ</Typography></Stack>{Array.from(statusCounts.entries()).slice(0, 6).map(([status, count]) => <Stack key={status} direction="row" justifyContent="space-between"><Typography variant="body2" color="text.secondary">{getFleetStatusLabel(status)}</Typography><Typography variant="body2" fontWeight={800}>{count.toLocaleString("th-TH")} รายการ</Typography></Stack>)}</Stack></CardContent></Card>;
}

function CalendarLegend() {
  const items = [{ label: "คำขอ/รอจัดรถ", color: "#D8A52D" }, { label: "จัดรถแล้ว", color: "#1E8E4A" }, { label: "รออนุมัติ", color: "#7E3FC5" }, { label: "กำลังเดินทาง/เสร็จสิ้น", color: "#2288B8" }, { label: "ไม่พร้อม/บำรุงรักษา", color: "#E2574C" }];
  return <Card sx={{ mt: 2 }}><CardContent sx={{ py: 1.5, "&:last-child": { pb: 1.5 } }}><Stack direction="row" spacing={3} flexWrap="wrap" useFlexGap>{items.map((item) => <Stack key={item.label} direction="row" spacing={0.75} alignItems="center"><Box sx={{ width: 9, height: 9, borderRadius: "50%", bgcolor: item.color }} /><Typography variant="body2" color="text.secondary">{item.label}</Typography></Stack>)}</Stack></CardContent></Card>;
}

function Detail({ label, value }: { label: string; value: string }) { return <Box><Typography variant="caption" color="text.secondary">{label}</Typography><Typography variant="body2" fontWeight={700}>{value}</Typography></Box>; }
function resolvePeriod(date: Dayjs, view: CalendarView) { if (view === "month") return { start: date.startOf("month").startOf("week"), end: date.endOf("month").endOf("week") }; if (view === "agenda") return { start: date.startOf("month"), end: date.endOf("month") }; return { start: date.startOf("week"), end: date.endOf("week") }; }
function formatPeriodLabel(start: Dayjs, end: Dayjs, view: CalendarView) { if (view === "month" || view === "agenda") return `${start.format("MMMM")} ${start.year() + 543}`; return `${start.format("D")} – ${end.format("D MMMM")} ${end.year() + 543}`; }
function eventOccursOn(event: FleetCalendarEvent, day: Dayjs) { return dayjs(event.startAt).isBefore(day.endOf("day")) && dayjs(event.endAt).isAfter(day.startOf("day")); }
function eventColors(event: FleetCalendarEvent) { if (event.severity === "danger") return { background: "#FDECEA", border: "#E2574C" }; if (event.severity === "warning") return { background: "#FFF5DE", border: "#D8A52D" }; if (event.severity === "success") return { background: "#E8F5EC", border: "#1E8E4A" }; if (event.status.includes("PENDING")) return { background: "#F2EAFB", border: "#7E3FC5" }; return { background: "#E8F3F8", border: "#2288B8" }; }
function normalizeMetadataKey(key: string) { return key.replace(/[_\s-]/g, "").toLowerCase(); }
function metadataLabel(key: string) {
  const labels: Record<string, string> = { requestno: "เลขคำขอ", priority: "ความเร่งด่วน", department: "หน่วยงาน", destination: "ปลายทาง", passengercount: "จำนวนผู้ร่วมเดินทาง", reason: "เหตุผล", type: "ประเภท", missiontype: "ประเภทภารกิจ", startmileage: "เลขไมล์เริ่มต้น", endmileage: "เลขไมล์สิ้นสุด", requestedby: "ผู้ขอ", requester: "ผู้ขอ", vehicle: "รถ", driver: "พนักงานขับรถ", registrationnumber: "ทะเบียนรถ", note: "หมายเหตุ" };
  return labels[normalizeMetadataKey(key)] ?? "ข้อมูลเพิ่มเติม";
}
function formatMetadata(key: string, value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "boolean") return value ? "ใช่" : "ไม่ใช่";
  if (typeof value === "object") return "ข้อมูลประกอบ";
  const text = String(value);
  const normalizedKey = normalizeMetadataKey(key);
  if (normalizedKey === "priority") return ({ NORMAL: "ปกติ", URGENT: "เร่งด่วน", EMERGENCY: "ฉุกเฉิน" } as Record<string, string>)[text.toUpperCase()] ?? "ปกติ";
  if (normalizedKey === "department") return ({ "Information Technology": "งานเทคโนโลยีสารสนเทศ", Administration: "ฝ่ายบริหาร", Pharmacy: "งานเภสัชกรรม", Nursing: "กลุ่มงานการพยาบาล" } as Record<string, string>)[text] ?? text;
  if (normalizedKey === "type" || normalizedKey === "missiontype") return ({ REQUEST: "คำขอใช้รถ", OFFICIAL: "ราชการ", TRAINING: "อบรม", MEETING: "ประชุม", PATIENT_TRANSFER: "รับ-ส่งผู้ป่วย", OTHER: "อื่น ๆ" } as Record<string, string>)[text.toUpperCase()] ?? text;
  if (normalizedKey === "passengercount") return `${Number(text).toLocaleString("th-TH")} คน`;
  return text;
}
