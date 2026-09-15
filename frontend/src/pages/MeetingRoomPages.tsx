import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  Tabs,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import AccessTimeOutlinedIcon from "@mui/icons-material/AccessTimeOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import MeetingRoomOutlinedIcon from "@mui/icons-material/MeetingRoomOutlined";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import InsertLinkOutlinedIcon from "@mui/icons-material/InsertLinkOutlined";
import NotesOutlinedIcon from "@mui/icons-material/NotesOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import { PageHeader } from "../components/PageHeader";
import { DataTableCard } from "../components/common/DataTableCard";
import { ListPagination } from "../components/common/ListPagination";
import { PageToolbar } from "../components/common/PageToolbar";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { AppTimePicker } from "../components/common/AppTimePicker";
import { useAuth } from "../context/AuthContext";
import { usePermission } from "../context/PermissionContext";
import {
  cancelMeetingBooking,
  createMeetingBooking,
  getMeetingBooking,
  getMeetingBookings,
  getMeetingCalendar,
  getMeetingRooms,
  meetingAttachmentUrl,
  meetingRoomPermissions,
  saveMeetingRoom,
  uploadMeetingAttachments,
  type BookingInput,
  type MeetingBooking,
  type MeetingRoom,
} from "../api/meetingRoomApi";
import { dashboardPollingOptions } from "../config/queryPolling";

const queryKeys = {
  rooms: ["meeting-rooms", "rooms"] as const,
  calendar: (a: string, b: string, r: string) =>
    ["meeting-rooms", "calendar", a, b, r] as const,
  bookings: (
    scope: string,
    page: number,
    pageSize: number,
    search: string,
    status: string,
  ) =>
    [
      "meeting-rooms",
      "bookings",
      scope,
      page,
      pageSize,
      search,
      status,
    ] as const,
};
const thDate = (value: string) =>
  new Intl.DateTimeFormat("th-TH", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Bangkok",
  }).format(new Date(value));
const dateOnly = (date: Date) =>
  date.toLocaleDateString("en-CA", { timeZone: "Asia/Bangkok" });
const panelSx = {
  border: "1px solid",
  borderColor: "divider",
  borderRadius: 2,
  borderTop: "4px solid",
  borderTopColor: "secondary.main",
  boxShadow: "0 10px 28px rgba(25,75,60,.06)",
  bgcolor: "background.paper",
};

function Heading({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle: string;
  action?: React.ReactNode;
}) {
  return (
    <Stack gap={2} mb={3}>
      <PageHeader title={title} subtitle={subtitle} />
      {action && (
        <Stack
          direction="row"
          justifyContent="flex-end"
          flexWrap="wrap"
          gap={1}
          sx={{ "& .MuiButton-root": { minHeight: 42 } }}
        >
          {action}
        </Stack>
      )}
    </Stack>
  );
}
function Status({ value }: { value: string }) {
  return (
    <Chip
      size="small"
      color={value === "Confirmed" ? "success" : "default"}
      label={value === "Confirmed" ? "ยืนยันแล้ว" : "ยกเลิกแล้ว"}
    />
  );
}
function BookingCard({ item }: { item: MeetingBooking }) {
  return (
    <Card
      sx={{
        ...panelSx,
        p: 2.25,
        transition: "transform .15s, box-shadow .15s",
        "&:hover": {
          transform: "translateY(-1px)",
          boxShadow: "0 12px 30px rgba(25,75,60,.12)",
        },
      }}
    >
      <Stack gap={1.25}>
        <Stack direction="row" justifyContent="space-between" gap={1}>
          <Typography fontWeight={800} color="primary.main">
            MR-{String(item.number).padStart(6, "0")}
          </Typography>
          <Status value={item.status} />
        </Stack>
        <Typography variant="h6" fontWeight={750}>
          {item.subject}
        </Typography>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          gap={1.5}
          color="text.secondary"
        >
          <Stack direction="row" gap={0.5}>
            <MeetingRoomOutlinedIcon fontSize="small" />
            <Typography variant="body2">{item.roomName}</Typography>
          </Stack>
          <Stack direction="row" gap={0.5}>
            <AccessTimeOutlinedIcon fontSize="small" />
            <Typography variant="body2">
              {thDate(item.startAt)} -{" "}
              {new Date(item.endAt).toLocaleTimeString("th-TH", {
                hour: "2-digit",
                minute: "2-digit",
                timeZone: "Asia/Bangkok",
              })}
            </Typography>
          </Stack>
        </Stack>
        <Divider />
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          alignItems={{ sm: "center" }}
          gap={1}
        >
          <Typography variant="body2">
            {item.bookerName} · {item.departmentName || "ไม่ระบุหน่วยงาน"}
          </Typography>
          <Button
            component={Link}
            to={`/meeting-rooms/bookings/${item.id}`}
            endIcon={<OpenInNewOutlinedIcon />}
          >
            ดูรายละเอียด
          </Button>
        </Stack>
      </Stack>
    </Card>
  );
}

export function MeetingRoomCalendarPage() {
  const [cursor, setCursor] = useState(new Date());
  const [view, setView] = useState("month");
  const [roomId, setRoomId] = useState("");
  const [selected, setSelected] = useState<MeetingBooking | null>(null);
  const first = new Date(cursor.getFullYear(), cursor.getMonth(), 1),
    last = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0);
  const start = dateOnly(
    new Date(first.getFullYear(), first.getMonth(), 1 - first.getDay()),
  );
  const end = dateOnly(
    new Date(
      last.getFullYear(),
      last.getMonth(),
      last.getDate() + 6 - last.getDay(),
    ),
  );
  const rooms = useQuery({
    queryKey: queryKeys.rooms,
    queryFn: () => getMeetingRooms(),
    ...dashboardPollingOptions,
  });
  const calendar = useQuery({
    queryKey: queryKeys.calendar(start, end, roomId),
    queryFn: () => getMeetingCalendar(start, end, roomId),
    ...dashboardPollingOptions,
    staleTime: 0,
  });
  const days = Array.from({ length: 42 }, (_, i) => {
    const d = new Date(`${start}T12:00:00`);
    d.setDate(d.getDate() + i);
    return d;
  });
  const byDay = (d: Date) =>
    (calendar.data || []).filter(
      (x) => dateOnly(new Date(x.startAt)) === dateOnly(d),
    );
  return (
    <Box>
      <Heading
        title="ปฏิทินห้องประชุม"
        subtitle="ตรวจสอบตารางการใช้ห้องของโรงพยาบาล"
        action={
          <Stack
            direction={{ xs: "column", sm: "row" }}
            gap={1}
            sx={{
              width: { xs: "100%", sm: "auto" },
              "& .MuiButton-root": {
                width: { xs: "100%", sm: 148 },
                height: 44,
              },
            }}
          >
            <Button
              variant="outlined"
              startIcon={<RefreshOutlinedIcon />}
              onClick={() => calendar.refetch()}
            >
              Refresh
            </Button>
            <Button
              variant="contained"
              component={Link}
              to="/meeting-rooms/new"
              startIcon={<AddOutlinedIcon />}
            >
              จองห้อง
            </Button>
          </Stack>
        }
      />
      <Card sx={{ ...panelSx, p: 2, mb: 2 }}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          gap={1}
          alignItems={{ md: "center" }}
        >
          <Button
            onClick={() =>
              setCursor(
                new Date(cursor.getFullYear(), cursor.getMonth() - 1, 1),
              )
            }
          >
            ก่อนหน้า
          </Button>
          <Typography
            fontWeight={800}
            sx={{
              minWidth: 180,
              textAlign: "center",
              px: 2,
              py: 1.1,
              borderRadius: 2,
              bgcolor: "secondary.main",
              color: "secondary.contrastText",
              boxShadow: "0 3px 10px rgba(139,105,47,.18)",
            }}
          >
            {cursor.toLocaleDateString("th-TH", {
              month: "long",
              year: "numeric",
            })}
          </Typography>
          <Button
            onClick={() =>
              setCursor(
                new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1),
              )
            }
          >
            ถัดไป
          </Button>
          <Tabs value={view} onChange={(_, v) => setView(v)}>
            <Tab value="month" label="เดือน" />
            <Tab value="week" label="สัปดาห์" />
            <Tab value="list" label="รายการ" />
          </Tabs>
          <FormControl size="small" sx={{ minWidth: 220, ml: { md: "auto" } }}>
            <InputLabel>ห้องประชุม</InputLabel>
            <Select
              value={roomId}
              label="ห้องประชุม"
              onChange={(e) => setRoomId(e.target.value)}
            >
              <MenuItem value="">ทุกห้อง</MenuItem>
              {rooms.data?.map((r) => (
                <MenuItem key={r.id} value={r.id}>
                  {r.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Stack>
      </Card>
      {calendar.isError && <Alert severity="error">โหลดปฏิทินไม่สำเร็จ</Alert>}
      {view === "month" ? (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: "repeat(7,minmax(0,1fr))",
            borderTop: "1px solid",
            borderLeft: "1px solid",
            borderColor: "divider",
            overflow: "hidden",
            borderRadius: 2,
          }}
        >
          {[
            "วันอาทิตย์",
            "วันจันทร์",
            "วันอังคาร",
            "วันพุธ",
            "วันพฤหัสบดี",
            "วันศุกร์",
            "วันเสาร์",
          ].map((name, index) => (
            <Box
              key={name}
              sx={{
                py: 1.25,
                px: 0.5,
                textAlign: "center",
                bgcolor:
                  index === 0 || index === 6
                    ? "rgba(201,164,91,.16)"
                    : "primary.main",
                color:
                  index === 0 || index === 6
                    ? "text.primary"
                    : "primary.contrastText",
                borderRight: "1px solid",
                borderBottom: "1px solid",
                borderColor: "divider",
              }}
            >
              <Typography fontWeight={800} fontSize={{ xs: 11, sm: 14 }}>
                {name}
              </Typography>
            </Box>
          ))}
          {days.map((d) => (
            <Box
              key={d.toISOString()}
              sx={{
                minHeight: { xs: 92, md: 132 },
                p: { xs: 0.5, sm: 1 },
                borderRight: "1px solid",
                borderBottom: "1px solid",
                borderColor: "divider",
                bgcolor:
                  d.getMonth() === cursor.getMonth()
                    ? "background.paper"
                    : "action.hover",
              }}
            >
              <Typography
                variant="caption"
                fontWeight={800}
                color={
                  dateOnly(d) === dateOnly(new Date())
                    ? "primary.main"
                    : "text.primary"
                }
              >
                {d.getDate()}
              </Typography>
              {byDay(d)
                .slice(0, 3)
                .map((x) => (
                  <Tooltip
                    key={x.id}
                    arrow
                    placement="top"
                    enterDelay={250}
                    title={
                      <Stack spacing={0.5} sx={{ py: 0.5 }}>
                        <Typography variant="subtitle2" fontWeight={800}>
                          {x.subject}
                        </Typography>
                        <Typography variant="caption">
                          ผู้จอง: {x.bookerName}
                        </Typography>
                        <Typography variant="caption">
                          เวลา:{" "}
                          {new Date(x.startAt).toLocaleTimeString("th-TH", {
                            hour: "2-digit",
                            minute: "2-digit",
                            hour12: false,
                            timeZone: "Asia/Bangkok",
                          })}{" "}
                          -{" "}
                          {new Date(x.endAt).toLocaleTimeString("th-TH", {
                            hour: "2-digit",
                            minute: "2-digit",
                            hour12: false,
                            timeZone: "Asia/Bangkok",
                          })}{" "}
                          น.
                        </Typography>
                      </Stack>
                    }
                  >
                    <Box
                      component="button"
                      type="button"
                      onClick={() => setSelected(x)}
                      sx={{
                        display: "block",
                        width: "100%",
                        border: 0,
                        textAlign: "left",
                        cursor: "pointer",
                        mt: 0.5,
                        p: 0.65,
                        bgcolor:
                          x.status === "Cancelled"
                            ? "grey.200"
                            : "rgba(31,101,83,.11)",
                        color: "primary.dark",
                        borderLeft: "3px solid",
                        borderColor:
                          x.status === "Cancelled"
                            ? "grey.500"
                            : "secondary.main",
                        borderRadius: 1,
                        fontFamily: "inherit",
                        fontSize: 12,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                        "&:hover": { bgcolor: "rgba(201,164,91,.22)" },
                      }}
                    >
                      {new Date(x.startAt).toLocaleTimeString("th-TH", {
                        hour: "2-digit",
                        minute: "2-digit",
                      })}{" "}
                      {x.roomName} · {x.subject}
                    </Box>
                  </Tooltip>
                ))}
            </Box>
          ))}
        </Box>
      ) : (
        <Stack gap={1}>
          {(calendar.data || []).map((x) => (
            <Box
              key={x.id}
              onClick={() => setSelected(x)}
              sx={{ cursor: "pointer" }}
            >
              <BookingCard item={x} />
            </Box>
          ))}
          {calendar.data?.length === 0 && (
            <Alert severity="info">ไม่พบรายการในช่วงนี้</Alert>
          )}
        </Stack>
      )}
      <Dialog
        open={!!selected}
        onClose={() => setSelected(null)}
        fullWidth
        maxWidth="sm"
        PaperProps={{
          sx: {
            borderRadius: 2,
            borderTop: "5px solid",
            borderTopColor: "secondary.main",
          },
        }}
      >
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" justifyContent="space-between" gap={1}>
            <Box>
              <Typography
                variant="overline"
                color="primary.main"
                fontWeight={800}
              >
                รายละเอียดการประชุม
              </Typography>
              <Typography variant="h6" fontWeight={800}>
                {selected?.subject}
              </Typography>
            </Box>
            {selected && <Status value={selected.status} />}
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack gap={1.5} sx={{ pt: 1 }}>
            <Stack direction="row" gap={1}>
              <MeetingRoomOutlinedIcon color="primary" />
              <Typography>
                <b>{selected?.roomName}</b>
              </Typography>
            </Stack>
            <Stack direction="row" gap={1}>
              <AccessTimeOutlinedIcon color="primary" />
              <Typography>
                {selected && thDate(selected.startAt)} -{" "}
                {selected &&
                  new Date(selected.endAt).toLocaleTimeString("th-TH", {
                    hour: "2-digit",
                    minute: "2-digit",
                    timeZone: "Asia/Bangkok",
                  })}
              </Typography>
            </Stack>
            <Divider />
            <Typography>
              <b>ผู้จอง:</b> {selected?.bookerName}
            </Typography>
            <Typography>
              <b>กลุ่มงาน:</b> {selected?.departmentName || "ไม่ระบุหน่วยงาน"}
            </Typography>
            <Typography>
              <b>จำนวนผู้เข้าร่วม:</b> {selected?.attendeeCount} คน
            </Typography>
            <Typography>
              <b>วัตถุประสงค์:</b> {selected?.purpose}
            </Typography>
            {selected?.additionalRequest && (
              <Typography>
                <b>คำขอเพิ่มเติม:</b> {selected.additionalRequest}
              </Typography>
            )}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setSelected(null)}>ปิด</Button>
          {selected && (
            <Button
              variant="contained"
              component={Link}
              to={`/meeting-rooms/bookings/${selected.id}`}
            >
              ดูรายละเอียดทั้งหมด
            </Button>
          )}
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export function MeetingRoomCreatePage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const rooms = useQuery({
    queryKey: queryKeys.rooms,
    queryFn: () => getMeetingRooms(),
  });
  const [form, setForm] = useState<BookingInput>({
    roomId: "",
    date: dateOnly(new Date()),
    startTime: "09:00",
    endTime: "10:00",
    subject: "",
    purpose: "",
    attendeeCount: 1,
    meetingLink: "",
    additionalRequest: "",
  });
  const [files, setFiles] = useState<File[]>([]);
  const room = rooms.data?.find((x) => x.id === form.roomId);
  const capacityError = !!room && form.attendeeCount > room.capacity;
  const mutation = useMutation({
    mutationFn: async () => {
      const booking = await createMeetingBooking(form);
      if (files.length) await uploadMeetingAttachments(booking.id, files);
      return booking;
    },
    onSuccess: async (x) => {
      await qc.invalidateQueries({ queryKey: ["meeting-rooms"] });
      navigate(`/meeting-rooms/bookings/${x.id}`);
    },
  });
  const set = (key: keyof BookingInput, value: string | number) =>
    setForm((x) => ({ ...x, [key]: value }));
  return (
    <Box>
      <Heading
        title="จองห้องประชุม"
        subtitle="รายการจะได้รับการยืนยันทันทีเมื่อห้องว่าง"
      />
      <Card sx={{ ...panelSx, p: { xs: 2, md: 3 } }}>
        <Box
          component="form"
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate();
          }}
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" },
            gap: 2,
          }}
        >
          <TextField label="ชื่อผู้จอง" value={user?.fullname || ""} disabled />
          <TextField
            label="กลุ่มงาน"
            value={user?.department || "ไม่ระบุหน่วยงาน"}
            disabled
          />
          <TextField
            select
            required
            label="ห้องประชุม"
            value={form.roomId}
            onChange={(e) => set("roomId", e.target.value)}
          >
            {rooms.data?.map((r) => (
              <MenuItem key={r.id} value={r.id}>
                {r.name} ({r.capacity} คน)
              </MenuItem>
            ))}
          </TextField>
          <AppDatePicker
            label="วันที่ประชุม"
            value={form.date}
            onChange={(value) => set("date", value)}
            size="medium"
          />
          <AppTimePicker
            label="เวลาเริ่ม"
            value={form.startTime}
            onChange={(value) => set("startTime", value)}
          />
          <AppTimePicker
            label="เวลาสิ้นสุด"
            value={form.endTime}
            onChange={(value) => set("endTime", value)}
          />
          <TextField
            required
            label="หัวข้อประชุม"
            value={form.subject}
            onChange={(e) => set("subject", e.target.value)}
            sx={{ gridColumn: { md: "1/-1" } }}
          />
          <TextField
            required
            multiline
            minRows={3}
            label="วัตถุประสงค์"
            value={form.purpose}
            onChange={(e) => set("purpose", e.target.value)}
          />
          <TextField
            multiline
            minRows={3}
            label="คำขอเพิ่มเติม เช่น อุปกรณ์ อาหารว่าง น้ำ"
            value={form.additionalRequest}
            onChange={(e) => set("additionalRequest", e.target.value)}
          />
          <TextField
            required
            type="number"
            label="จำนวนผู้เข้าประชุม"
            value={form.attendeeCount}
            onChange={(e) => set("attendeeCount", Number(e.target.value))}
            error={capacityError}
            helperText={
              capacityError
                ? `ห้องรองรับได้สูงสุด ${room?.capacity} คน`
                : room
                  ? `ความจุ ${room.capacity} คน`
                  : ""
            }
          />
          <TextField
            type="url"
            label="Link ประชุม (ถ้ามี)"
            value={form.meetingLink}
            onChange={(e) => set("meetingLink", e.target.value)}
            helperText="ต้องเป็น HTTPS"
          />
          <Button component="label" variant="outlined">
            เลือกเอกสาร (สูงสุด 5 ไฟล์)
            <input
              hidden
              multiple
              type="file"
              accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx"
              onChange={(e) =>
                setFiles(Array.from(e.target.files || []).slice(0, 5))
              }
            />
          </Button>
          <Typography alignSelf="center" color="text.secondary">
            {files.length ? `${files.length} ไฟล์` : "ยังไม่ได้เลือกไฟล์"}
          </Typography>
          {mutation.isError && (
            <Alert severity="error" sx={{ gridColumn: { md: "1/-1" } }}>
              {mutation.error instanceof Error
                ? mutation.error.message
                : "บันทึกไม่สำเร็จ"}
            </Alert>
          )}
          <Stack
            direction="row"
            justifyContent="flex-end"
            gap={1}
            sx={{ gridColumn: { md: "1/-1" } }}
          >
            <Button onClick={() => navigate(-1)}>กลับ</Button>
            <Button
              type="submit"
              variant="contained"
              disabled={
                mutation.isPending ||
                capacityError ||
                files.some((f) => f.size > 10 * 1024 * 1024)
              }
            >
              ยืนยันการจอง
            </Button>
          </Stack>
        </Box>
      </Card>
    </Box>
  );
}

export function MeetingRoomBookingsPage({
  manage = false,
}: {
  manage?: boolean;
}) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const scope = manage ? "all" : "mine";
  const query = useQuery({
    queryKey: queryKeys.bookings(scope, page, pageSize, search, status),
    queryFn: () =>
      getMeetingBookings({
        scope,
        page,
        pageSize,
        search,
        status: status || undefined,
      }),
    ...dashboardPollingOptions,
    staleTime: 0,
  });
  const items = query.data?.items ?? [];
  const reset = () => {
    setSearch("");
    setStatus("");
    setPage(1);
  };
  return (
    <Box>
      <Heading
        title={manage ? "จัดการรายการจอง" : "รายการจองของฉัน"}
        subtitle={
          manage
            ? "ตรวจสอบและจัดการรายการจองทั้งหมด"
            : "ติดตามวันเวลา ห้อง และสถานะรายการจองของคุณ"
        }
        action={
          <Button
            variant="contained"
            component={Link}
            to="/meeting-rooms/new"
            startIcon={<AddOutlinedIcon />}
            sx={{ minWidth: 148 }}
          >
            จองห้องประชุม
          </Button>
        }
      />
      <PageToolbar>
        <Stack
          direction={{ xs: "column", md: "row" }}
          gap={1.5}
          sx={{ width: "100%" }}
        >
          <TextField
            size="small"
            fullWidth
            label="ค้นหาหัวข้อหรือวัตถุประสงค์"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <TextField
            select
            size="small"
            label="สถานะรายการ"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
            sx={{ minWidth: { xs: "100%", md: 220 } }}
          >
            <MenuItem value="">ทุกสถานะ</MenuItem>
            <MenuItem value="Confirmed">ยืนยันแล้ว</MenuItem>
            <MenuItem value="Cancelled">ยกเลิกแล้ว</MenuItem>
          </TextField>
          <Button
            variant="text"
            onClick={reset}
            disabled={!search && !status}
            sx={{ whiteSpace: "nowrap", minWidth: 110 }}
          >
            ล้างตัวกรอง
          </Button>
        </Stack>
      </PageToolbar>
      {query.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ไม่สามารถโหลดรายการจองได้ กรุณาลองใหม่
        </Alert>
      )}
      <Box sx={{ display: { xs: "none", md: "block" } }}>
        <DataTableCard
          title="รายการจองห้องประชุม"
          subtitle={`พบ ${query.data?.total ?? 0} รายการ`}
          minTableWidth={920}
        >
          <TableHead>
            <TableRow>
              <TableCell>เลขที่ / หัวข้อ</TableCell>
              <TableCell>ห้องประชุม</TableCell>
              <TableCell>วันและเวลา</TableCell>
              {manage && <TableCell>ผู้จอง / กลุ่มงาน</TableCell>}
              <TableCell align="center">จำนวน</TableCell>
              <TableCell>สถานะ</TableCell>
              <TableCell align="right">จัดการ</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((item) => (
              <TableRow key={item.id} hover>
                <TableCell>
                  <Typography fontWeight={800} color="primary.main">
                    MR-{String(item.number).padStart(6, "0")}
                  </Typography>
                  <Typography variant="body2">{item.subject}</Typography>
                </TableCell>
                <TableCell>{item.roomName}</TableCell>
                <TableCell>
                  {thDate(item.startAt)}
                  <Typography
                    variant="caption"
                    display="block"
                    color="text.secondary"
                  >
                    ถึง{" "}
                    {new Date(item.endAt).toLocaleTimeString("th-TH", {
                      hour: "2-digit",
                      minute: "2-digit",
                      timeZone: "Asia/Bangkok",
                    })}
                  </Typography>
                </TableCell>
                {manage && (
                  <TableCell>
                    {item.bookerName}
                    <Typography
                      variant="caption"
                      display="block"
                      color="text.secondary"
                    >
                      {item.departmentName || "ไม่ระบุหน่วยงาน"}
                    </Typography>
                  </TableCell>
                )}
                <TableCell align="center">{item.attendeeCount}</TableCell>
                <TableCell>
                  <Status value={item.status} />
                </TableCell>
                <TableCell align="right">
                  <Button
                    component={Link}
                    to={`/meeting-rooms/bookings/${item.id}`}
                    endIcon={<OpenInNewOutlinedIcon />}
                  >
                    รายละเอียด
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </DataTableCard>
      </Box>
      <Stack gap={1.5} sx={{ display: { xs: "flex", md: "none" } }}>
        {items.map((item) => (
          <BookingCard key={item.id} item={item} />
        ))}
      </Stack>
      {!query.isLoading && !items.length && !query.isError && (
        <Alert severity="info" sx={{ mt: 2 }}>
          ไม่พบรายการจองตามตัวกรอง
        </Alert>
      )}
      <Card sx={{ mt: 2, p: { xs: 1, sm: 1.5 } }}>
        <ListPagination
          page={page}
          pageSize={pageSize}
          totalItems={query.data?.total ?? 0}
          disabled={query.isFetching}
          onPageChange={setPage}
          onPageSizeChange={(value) => {
            setPageSize(value);
            setPage(1);
          }}
        />
      </Card>
    </Box>
  );
}

export function MeetingRoomDetailPage() {
  const { id = "" } = useParams();
  const { hasPermission } = usePermission();
  const qc = useQueryClient();
  const [cancelOpen, setCancelOpen] = useState(false),
    [reason, setReason] = useState("");
  const query = useQuery({
    queryKey: ["meeting-rooms", "booking", id],
    queryFn: () => getMeetingBooking(id),
    ...dashboardPollingOptions,
    staleTime: 0,
  });
  const cancel = useMutation({
    mutationFn: () =>
      cancelMeetingBooking(id, query.data!.booking.concurrencyToken, reason),
    onSuccess: async () => {
      setCancelOpen(false);
      await qc.invalidateQueries({ queryKey: ["meeting-rooms"] });
    },
  });
  const b = query.data?.booking;
  if (query.isLoading && !b) return <Typography>กำลังโหลด...</Typography>;
  if (!b) return <Alert severity="error">ไม่พบรายการจอง</Alert>;
  return (
    <Box>
      <Heading
        title={`MR-${String(b.number).padStart(6, "0")}`}
        subtitle={b.subject}
        action={
          <Stack
            direction={{ xs: "column", sm: "row" }}
            gap={1}
            sx={{ width: { xs: "100%", sm: "auto" } }}
          >
            <Button
              component={Link}
              to="/meeting-rooms/calendar"
              variant="outlined"
              startIcon={<ArrowBackOutlinedIcon />}
              sx={{ minWidth: 148 }}
            >
              ปฏิทิน
            </Button>
            {hasPermission(meetingRoomPermissions.manage) &&
              b.status !== "Cancelled" && (
                <Button
                  color="error"
                  variant="outlined"
                  onClick={() => setCancelOpen(true)}
                >
                  ยกเลิกรายการ
                </Button>
              )}
          </Stack>
        }
      />
      <Card sx={{ ...panelSx, p: { xs: 2, md: 3 } }}>
        <Stack gap={3}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="space-between"
            alignItems={{ sm: "center" }}
            gap={1.5}
            sx={{ pb: 2, borderBottom: 1, borderColor: "divider" }}
          >
            <Stack direction="row" alignItems="center" gap={1.25}>
              <Box
                sx={{
                  display: "grid",
                  placeItems: "center",
                  width: 44,
                  height: 44,
                  borderRadius: 2,
                  bgcolor: "rgba(31,101,83,.10)",
                  color: "primary.main",
                }}
              >
                <MeetingRoomOutlinedIcon />
              </Box>
              <Box>
                <Typography variant="h6" fontWeight={800}>
                  รายละเอียดการประชุม
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  ข้อมูลการใช้ห้องและผู้เข้าร่วม
                </Typography>
              </Box>
            </Stack>
            <Status value={b.status} />
          </Stack>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "repeat(2,minmax(0,1fr))" },
              gap: 1.5,
            }}
          >
            {[
              {
                icon: <MeetingRoomOutlinedIcon />,
                label: "ห้องประชุม",
                value: b.roomName,
              },
              {
                icon: <AccessTimeOutlinedIcon />,
                label: "วันและเวลา",
                value: `${thDate(b.startAt)} ถึง ${thDate(b.endAt)}`,
              },
              {
                icon: <PersonOutlineOutlinedIcon />,
                label: "ผู้จอง",
                value: b.bookerName,
              },
              {
                icon: <BusinessOutlinedIcon />,
                label: "กลุ่มงาน",
                value: b.departmentName || "ไม่ระบุหน่วยงาน",
              },
              {
                icon: <GroupsOutlinedIcon />,
                label: "จำนวนผู้เข้าประชุม",
                value: `${b.attendeeCount} คน`,
              },
            ].map((item) => (
              <Box
                key={item.label}
                sx={{
                  display: "flex",
                  gap: 1.25,
                  p: 2,
                  borderRadius: 2,
                  bgcolor: "rgba(31,101,83,.045)",
                  border: "1px solid",
                  borderColor: "divider",
                }}
              >
                <Box sx={{ color: "primary.main", mt: 0.25 }}>{item.icon}</Box>
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="caption" color="text.secondary">
                    {item.label}
                  </Typography>
                  <Typography
                    fontWeight={700}
                    sx={{ overflowWrap: "anywhere" }}
                  >
                    {item.value}
                  </Typography>
                </Box>
              </Box>
            ))}
          </Box>
          <Box>
            <Stack direction="row" alignItems="center" gap={1} mb={1}>
              <NotesOutlinedIcon color="primary" />
              <Typography variant="h6" fontWeight={800}>
                รายละเอียดการประชุม
              </Typography>
            </Stack>
            <Box
              sx={{
                p: 2,
                borderRadius: 2,
                bgcolor: "rgba(201,164,91,.10)",
                borderLeft: "4px solid",
                borderColor: "secondary.main",
              }}
            >
              <Typography variant="caption" color="text.secondary">
                วัตถุประสงค์
              </Typography>
              <Typography sx={{ whiteSpace: "pre-wrap" }}>
                {b.purpose}
              </Typography>
              {b.additionalRequest && (
                <>
                  <Divider sx={{ my: 1.5 }} />
                  <Typography variant="caption" color="text.secondary">
                    คำขอเพิ่มเติม
                  </Typography>
                  <Typography sx={{ whiteSpace: "pre-wrap" }}>
                    {b.additionalRequest}
                  </Typography>
                </>
              )}
            </Box>
          </Box>
          {b.meetingLink && (
            <Button
              component="a"
              href={b.meetingLink}
              target="_blank"
              rel="noreferrer"
              variant="outlined"
              startIcon={<InsertLinkOutlinedIcon />}
              endIcon={<OpenInNewOutlinedIcon />}
              sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
            >
              เปิด Link ประชุม
            </Button>
          )}
          {b.cancellationReason && (
            <Alert severity="warning">
              เหตุผลยกเลิก: {b.cancellationReason}
            </Alert>
          )}
          <Box>
            <Stack direction="row" alignItems="center" gap={1} mb={1}>
              <DescriptionOutlinedIcon color="primary" />
              <Typography variant="h6" fontWeight={800}>
                เอกสารประกอบ
              </Typography>
            </Stack>
            <Stack gap={1}>
              {query.data?.attachments.map((a) => (
                <Button
                  key={a.id}
                  component="a"
                  href={meetingAttachmentUrl(a.id)}
                  target="_blank"
                  variant="outlined"
                  startIcon={<DescriptionOutlinedIcon />}
                  endIcon={<OpenInNewOutlinedIcon />}
                  sx={{ justifyContent: "space-between", maxWidth: 520 }}
                >
                  {a.originalFileName}
                </Button>
              ))}
              {!query.data?.attachments.length && (
                <Box sx={{ p: 2, borderRadius: 2, bgcolor: "action.hover" }}>
                  <Typography color="text.secondary">
                    ไม่มีเอกสารประกอบการประชุม
                  </Typography>
                </Box>
              )}
            </Stack>
          </Box>
        </Stack>
      </Card>
      <Dialog open={cancelOpen} onClose={() => setCancelOpen(false)}>
        <DialogTitle>ยกเลิกรายการจอง</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            multiline
            minRows={3}
            label="เหตุผลยกเลิก"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelOpen(false)}>กลับ</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!reason.trim() || cancel.isPending}
            onClick={() => cancel.mutate()}
          >
            ยืนยันยกเลิก
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export function MeetingRoomManagePage() {
  const [tab, setTab] = useState(0);
  const qc = useQueryClient();
  const rooms = useQuery({
    queryKey: [...queryKeys.rooms, "all"],
    queryFn: () => getMeetingRooms(true),
  });
  const [editing, setEditing] = useState<Partial<MeetingRoom> | null>(null);
  const save = useMutation({
    mutationFn: () =>
      saveMeetingRoom({
        id: editing?.id,
        concurrencyToken: editing?.concurrencyToken,
        code: editing?.code ?? "",
        name: editing?.name ?? "",
        location: editing?.location ?? "",
        capacity: editing?.capacity ?? 1,
        isActive: editing?.isActive ?? true,
      }),
    onSuccess: async () => {
      setEditing(null);
      await qc.invalidateQueries({ queryKey: ["meeting-rooms", "rooms"] });
    },
  });
  return (
    <Box>
      <Heading
        title="จัดการห้องประชุม"
        subtitle="จัดการรายการจองและทะเบียนห้อง"
      />
      <Box
        sx={{
          display: "flex",
          minWidth: 0,
          width: "100%",
          p: 0.75,
          mb: 2.5,
          border: "1px solid",
          borderColor: "rgba(200,169,107,.55)",
          borderRadius: 1,
          bgcolor: "#FFF9EB",
          boxShadow: "0 4px 14px rgba(111,85,57,.07)",
        }}
      >
        <Tabs
          value={tab}
          onChange={(_, v) => setTab(v)}
          variant="scrollable"
          scrollButtons="auto"
          allowScrollButtonsMobile
          aria-label="จัดการห้องประชุม"
          sx={{
            minWidth: 0,
            flex: 1,
            minHeight: 48,
            "& .MuiTabs-indicator": {
              height: 3,
              borderRadius: "3px 3px 0 0",
              bgcolor: "secondary.main",
            },
            "& .MuiTab-root": {
              minHeight: 48,
              minWidth: { xs: 150, sm: 190 },
              borderRadius: 1,
              color: "text.secondary",
              fontWeight: 700,
              transition: "background-color 160ms ease, color 160ms ease",
            },
            "& .MuiTab-root:hover": {
              bgcolor: "rgba(200,169,107,.16)",
              color: "primary.main",
            },
            "& .MuiTab-root.Mui-selected": {
              bgcolor: "primary.main",
              color: "primary.contrastText",
            },
          }}
        >
          <Tab
            icon={<EventAvailableOutlinedIcon />}
            iconPosition="start"
            label="รายการจองทั้งหมด"
          />
          <Tab
            icon={<MeetingRoomOutlinedIcon />}
            iconPosition="start"
            label="ทะเบียนห้อง"
          />
        </Tabs>
      </Box>
      {tab === 0 ? (
        <MeetingRoomBookingsPage manage />
      ) : (
        <>
          <Stack alignItems="flex-end" mb={2}>
            <Button
              variant="contained"
              startIcon={<AddOutlinedIcon />}
              onClick={() =>
                setEditing({
                  code: "",
                  name: "",
                  location: "",
                  capacity: 1,
                  isActive: true,
                })
              }
            >
              เพิ่มห้อง
            </Button>
          </Stack>
          <Stack gap={1}>
            {rooms.data?.map((r) => (
              <Card key={r.id} sx={{ ...panelSx, p: 2 }}>
                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  justifyContent="space-between"
                  alignItems={{ sm: "center" }}
                  gap={1}
                >
                  <Box>
                    <Typography fontWeight={800}>
                      {r.name} ({r.code})
                    </Typography>
                    <Typography color="text.secondary">
                      {r.location} · {r.capacity} คน
                    </Typography>
                  </Box>
                  <Stack direction="row" gap={1}>
                    <Chip
                      color={r.isActive ? "success" : "default"}
                      label={r.isActive ? "เปิดใช้งาน" : "ปิดใช้งาน"}
                    />
                    <Button onClick={() => setEditing(r)}>แก้ไข</Button>
                  </Stack>
                </Stack>
              </Card>
            ))}
          </Stack>
        </>
      )}
      <Dialog
        open={!!editing}
        onClose={() => setEditing(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>
          {editing?.id ? "แก้ไขห้อง" : "เพิ่มห้องประชุม"}
        </DialogTitle>
        <DialogContent>
          <Stack gap={2} mt={1}>
            <TextField
              label="รหัสห้อง"
              value={editing?.code || ""}
              onChange={(e) =>
                setEditing((x) => ({ ...x!, code: e.target.value }))
              }
            />
            <TextField
              label="ชื่อห้อง"
              value={editing?.name || ""}
              onChange={(e) =>
                setEditing((x) => ({ ...x!, name: e.target.value }))
              }
            />
            <TextField
              label="สถานที่"
              value={editing?.location || ""}
              onChange={(e) =>
                setEditing((x) => ({ ...x!, location: e.target.value }))
              }
            />
            <TextField
              type="number"
              label="ความจุ"
              value={editing?.capacity || 1}
              onChange={(e) =>
                setEditing((x) => ({ ...x!, capacity: Number(e.target.value) }))
              }
            />
            <TextField
              select
              label="สถานะ"
              value={editing?.isActive ? "active" : "inactive"}
              onChange={(e) =>
                setEditing((x) => ({
                  ...x!,
                  isActive: e.target.value === "active",
                }))
              }
            >
              <MenuItem value="active">เปิดใช้งาน</MenuItem>
              <MenuItem value="inactive">ปิดใช้งาน</MenuItem>
            </TextField>
            {save.isError && <Alert severity="error">บันทึกไม่สำเร็จ</Alert>}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditing(null)}>กลับ</Button>
          <Button
            variant="contained"
            disabled={!editing?.code || !editing?.name || save.isPending}
            onClick={() => save.mutate()}
          >
            บันทึก
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
