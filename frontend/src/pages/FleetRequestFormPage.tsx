import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { Alert, Autocomplete, Box, Button, Card, CardContent, Chip, FormControlLabel, Grid, IconButton, MenuItem, Popover, Stack, Switch, TextField, Typography } from "@mui/material";
import { DateCalendar, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import dayjs, { type Dayjs } from "dayjs";
import { isAxiosError } from "axios";
import "dayjs/locale/th";
import { createFleetRequest, getFleetPersonnelOptions, getFleetRequest, getFleetVehicleTypes, transitionFleetRequest, updateFleetRequest, type FleetPassenger, type FleetPersonnelOption, type SaveFleetRequest } from "../api/fleetApi";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";

type FormState = {
  purpose: string; missionType: string; requestedVehicleTypeId: string; destination: string;
  contactPersonName: string; contactPhone: string; departureAt: string; expectedReturnAt: string;
  specialRequirement: string; isUrgent: boolean; urgentReason: string; requesterTravels: boolean;
};

const missionTypes = ["ทั่วไป", "ประชุม/อบรม", "รับ-ส่งเอกสารหรือสิ่งของ", "ราชการนอกสถานที่", "อื่น ๆ"];
const thaiMonths = ["มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];

const toBangkokLocalValue = (iso?: string) => {
  if (!iso) return "";
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Bangkok", year: "numeric", month: "2-digit", day: "2-digit",
    hour: "2-digit", minute: "2-digit", hourCycle: "h23",
  }).formatToParts(new Date(iso));
  const value = (type: Intl.DateTimeFormatPartTypes) => parts.find(part => part.type === type)?.value ?? "";
  return `${value("year")}-${value("month")}-${value("day")}T${value("hour")}:${value("minute")}`;
};

const toUtcIso = (bangkokLocal: string) => new Date(`${bangkokLocal}:00+07:00`).toISOString();

function ThaiDateTimeField({ label, value, onChange, error = false, helperText }: { label: string; value: string; onChange: (value: string) => void; error?: boolean; helperText?: string }) {
  const [datePart = "", timePart = ""] = value.split("T");
  const selectedDate = datePart ? dayjs(datePart) : null;
  const [hour = "", minute = ""] = timePart.split(":");
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [calendarMonth, setCalendarMonth] = useState<Dayjs>(() => selectedDate ?? dayjs());

  const updateDate = (nextDate: Dayjs | null) => {
    if (!nextDate) return;
    const nextTime = timePart || "00:00";
    onChange(`${nextDate.format("YYYY-MM-DD")}T${nextTime}`);
    setCalendarMonth(nextDate);
    setAnchorEl(null);
  };
  const updateTime = (nextHour: string, nextMinute: string) => {
    const baseDate = selectedDate ?? dayjs();
    onChange(`${baseDate.format("YYYY-MM-DD")}T${nextHour || "00"}:${nextMinute || "00"}`);
  };
  const dateLabel = selectedDate
    ? `${selectedDate.format("DD/MM")}/${selectedDate.year() + 543}`
    : "เลือกวันที่";

  return <Box component="fieldset" aria-invalid={error} sx={{ m: 0, px: 1.5, pb: 1.25, pt: 0.75, border: 1, borderColor: error ? "error.main" : "divider", borderRadius: 1 }}>
    <Typography component="legend" variant="caption" sx={{ px: 0.5, color: error ? "error.main" : "text.secondary" }}>{label} *</Typography>
    <Grid container spacing={1} alignItems="center">
      <Grid item xs={12} sm={6}>
        <Button fullWidth variant="outlined" startIcon={<CalendarMonthOutlinedIcon />} onClick={event => { setCalendarMonth(selectedDate ?? dayjs()); setAnchorEl(event.currentTarget); }} sx={{ height: 40, justifyContent: "flex-start", color: selectedDate ? "text.primary" : "text.secondary", borderColor: "divider" }}>{dateLabel}</Button>
        <Popover open={Boolean(anchorEl)} anchorEl={anchorEl} onClose={() => setAnchorEl(null)} anchorOrigin={{ vertical: "bottom", horizontal: "left" }}>
          <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="th">
            <Box sx={{ p: 1 }}>
              <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ px: 1, pt: 0.5 }}>
                <IconButton size="small" aria-label="เดือนก่อนหน้า" onClick={() => setCalendarMonth(current => current.subtract(1, "month"))}><ChevronLeftIcon /></IconButton>
                <Typography fontWeight={800}>{thaiMonths[calendarMonth.month()]} {calendarMonth.year() + 543}</Typography>
                <IconButton size="small" aria-label="เดือนถัดไป" onClick={() => setCalendarMonth(current => current.add(1, "month"))}><ChevronRightIcon /></IconButton>
              </Stack>
              <DateCalendar key={calendarMonth.format("YYYY-MM")} value={selectedDate} referenceDate={calendarMonth} onChange={updateDate} slots={{ calendarHeader: () => null }} dayOfWeekFormatter={date => date.format("dd")} />
            </Box>
          </LocalizationProvider>
        </Popover>
      </Grid>
      <Grid item xs={6} sm={3}><TextField select fullWidth size="small" label="ชั่วโมง" value={hour} onChange={event => updateTime(event.target.value, minute)}>{Array.from({ length: 24 }, (_, index) => String(index).padStart(2, "0")).map(item => <MenuItem key={item} value={item}>{item}</MenuItem>)}</TextField></Grid>
      <Grid item xs={6} sm={3}><TextField select fullWidth size="small" label="นาที" value={minute} onChange={event => updateTime(hour, event.target.value)}>{Array.from({ length: 60 }, (_, index) => String(index).padStart(2, "0")).map(item => <MenuItem key={item} value={item}>{item}</MenuItem>)}</TextField></Grid>
    </Grid>
    <Typography variant="caption" color={error ? "error.main" : "text.secondary"}>{helperText ?? "ปฏิทิน พ.ศ. · เวลา 24 ชั่วโมง"}</Typography>
  </Box>;
}

export function FleetRequestFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [personnelSearch, setPersonnelSearch] = useState("");
  const [selectedPersonnel, setSelectedPersonnel] = useState<FleetPersonnelOption[]>([]);
  const [preservedExternal, setPreservedExternal] = useState<FleetPassenger[]>([]);
  const [submitAfterSave, setSubmitAfterSave] = useState(false);
  const [form, setForm] = useState<FormState>({
    purpose: "", missionType: "ทั่วไป", requestedVehicleTypeId: "", destination: "",
    contactPersonName: user?.fullname ?? "", contactPhone: "", departureAt: "", expectedReturnAt: "",
    specialRequirement: "", isUrgent: false, urgentReason: "", requesterTravels: true,
  });

  const request = useQuery({ queryKey: ["fleet", "request-form", id], queryFn: () => getFleetRequest(id!), enabled: Boolean(id) });
  const vehicleTypes = useQuery({ queryKey: ["fleet", "vehicle-types"], queryFn: getFleetVehicleTypes });
  const personnel = useQuery({ queryKey: ["fleet", "personnel-options", personnelSearch], queryFn: () => getFleetPersonnelOptions(personnelSearch), staleTime: 30_000 });

  useEffect(() => {
    const data = request.data;
    if (!data) return;
    setForm({
      purpose: data.purpose, missionType: data.missionType, requestedVehicleTypeId: data.requestedVehicleTypeId ?? "",
      destination: data.destination, contactPersonName: data.contactPersonName, contactPhone: data.contactPhone,
      departureAt: toBangkokLocalValue(data.departureAt), expectedReturnAt: toBangkokLocalValue(data.expectedReturnAt),
      specialRequirement: data.specialRequirement ?? "", isUrgent: data.isUrgent,
      urgentReason: data.urgentReason ?? "", requesterTravels: data.passengers.some(x => x.isRequester),
    });
    setSelectedPersonnel(data.passengers.filter(x => x.passengerType === "EMPLOYEE" && !x.isRequester && x.userId).map(x => ({ id: x.userId!, fullName: x.fullName })));
    setPreservedExternal(data.passengers.filter(x => x.passengerType === "EXTERNAL"));
  }, [request.data]);

  const options = useMemo(() => {
    const map = new Map<string, FleetPersonnelOption>();
    [...selectedPersonnel, ...(personnel.data ?? [])].forEach(item => map.set(item.id, item));
    if (user) map.delete(user.id);
    return [...map.values()];
  }, [personnel.data, selectedPersonnel, user]);

  const passengerCount = selectedPersonnel.length + (form.requesterTravels && user ? 1 : 0) + preservedExternal.length;
  const hasInvalidTripTime = Boolean(form.departureAt && form.expectedReturnAt && form.expectedReturnAt <= form.departureAt);
  const tripTimeErrorMessage = "วันและเวลากลับต้องอยู่หลังวันและเวลาออกเดินทาง กรุณาตรวจสอบวันที่และเวลาอีกครั้ง";
  const hasRequiredFields = Boolean(form.purpose.trim() && form.missionType && form.destination.trim() && form.contactPersonName.trim() && form.contactPhone.trim() && form.departureAt && form.expectedReturnAt && passengerCount > 0 && (!form.isUrgent || form.urgentReason.trim()) && !hasInvalidTripTime);

  function buildPayload(): SaveFleetRequest {
    const employees: FleetPassenger[] = selectedPersonnel.map((item, index) => ({ userId: item.id, fullName: item.fullName, positionOrOrganization: item.departmentName, passengerType: "EMPLOYEE", isRequester: false, sortOrder: index + 1 }));
    const requesterPassenger: FleetPassenger[] = form.requesterTravels && user ? [{ userId: user.id, fullName: user.fullname, positionOrOrganization: user.department, passengerType: "EMPLOYEE", isRequester: true, sortOrder: 0 }] : [];
    const passengers = [...requesterPassenger, ...employees, ...preservedExternal.map((item, index) => ({ ...item, sortOrder: requesterPassenger.length + employees.length + index }))];
    return {
      purpose: form.purpose.trim(), missionType: form.missionType, requestedVehicleTypeId: form.requestedVehicleTypeId || null,
      destination: form.destination.trim(), contactPersonName: form.contactPersonName.trim(), contactPhone: form.contactPhone.trim(),
      departureAt: toUtcIso(form.departureAt), expectedReturnAt: toUtcIso(form.expectedReturnAt),
      passengerCount: passengers.length, specialRequirement: form.specialRequirement.trim(), isUrgent: form.isUrgent,
      urgentReason: form.isUrgent ? form.urgentReason.trim() : "", concurrencyToken: request.data?.concurrencyToken, passengers,
    };
  }

  const save = useMutation({
    mutationFn: async (andSubmit: boolean) => {
      const saved = id ? await updateFleetRequest(id, buildPayload()) : await createFleetRequest(buildPayload());
      return andSubmit ? transitionFleetRequest(saved.id, "submit", saved.concurrencyToken) : saved;
    },
    onMutate: andSubmit => setSubmitAfterSave(andSubmit),
    onSuccess: result => navigate(`/fleet/requests/${result.id}`),
  });

  const field = (key: keyof FormState) => (event: React.ChangeEvent<HTMLInputElement>) => setForm(current => ({ ...current, [key]: event.target.value }));

  return (
    <Stack spacing={2.5}>
      <PageHeader title={id ? "แก้ไขคำขอใช้รถ" : "สร้างคำขอใช้รถ"} subtitle="ผู้ขอไม่สามารถเลือกรถหรือคนขับได้ งานยานพาหนะจะเป็นผู้จัดรถให้ตามความเหมาะสม" />
      {request.isError && <Alert severity="error">โหลดข้อมูลคำขอไม่สำเร็จ กรุณากลับไปยังรายการคำขอแล้วลองใหม่</Alert>}
      {save.isError && <Alert severity="error">{getSaveErrorMessage(save.error)}</Alert>}

      <Card><CardContent sx={{ p: { xs: 2, md: 3 } }}>
        <Typography variant="h6" fontWeight={900} sx={{ mb: 2 }}>1. รายละเอียดภารกิจ</Typography>
        <Grid container spacing={2}>
          <Grid item xs={12}><TextField required fullWidth label="วัตถุประสงค์/ภารกิจ" placeholder="ระบุวัตถุประสงค์หรือภารกิจ" value={form.purpose} onChange={field("purpose")} /></Grid>
          <Grid item xs={12} md={6}><TextField select required fullWidth label="ประเภทการเดินทาง" value={form.missionType} onChange={field("missionType")}>{missionTypes.map(item => <MenuItem key={item} value={item}>{item}</MenuItem>)}</TextField></Grid>
          <Grid item xs={12} md={6}><TextField required fullWidth label="ปลายทาง" placeholder="ระบุปลายทาง" value={form.destination} onChange={field("destination")} /></Grid>
          <Grid item xs={12} md={6}><TextField required fullWidth label="ผู้ประสานงาน" value={form.contactPersonName} onChange={field("contactPersonName")} /></Grid>
          <Grid item xs={12} md={6}><TextField required fullWidth label="หมายเลขโทรศัพท์ผู้ประสานงาน" value={form.contactPhone} onChange={field("contactPhone")} /></Grid>
          <Grid item xs={12} md={6}><ThaiDateTimeField label="ออกเดินทาง" value={form.departureAt} onChange={value => setForm(current => ({ ...current, departureAt: value }))} /></Grid>
          <Grid item xs={12} md={6}><ThaiDateTimeField label="คาดว่าจะกลับ" value={form.expectedReturnAt} error={hasInvalidTripTime} helperText={hasInvalidTripTime ? "กรุณาระบุวันและเวลากลับให้หลังเวลาออกเดินทาง" : undefined} onChange={value => setForm(current => ({ ...current, expectedReturnAt: value }))} /></Grid>
          {hasInvalidTripTime && <Grid item xs={12}><Alert severity="warning">{tripTimeErrorMessage}</Alert></Grid>}
          <Grid item xs={12} md={6}><TextField select fullWidth label="ประเภทรถที่ต้องการ (ถ้ามี)" value={form.requestedVehicleTypeId} onChange={field("requestedVehicleTypeId")}><MenuItem value="">ไม่ระบุ</MenuItem>{(vehicleTypes.data ?? []).filter(item => item.isActive).map(item => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}</TextField></Grid>
          <Grid item xs={12} md={6}><Stack direction="row" alignItems="center" sx={{ height: "100%" }}><FormControlLabel control={<Switch checked={form.isUrgent} onChange={(_, checked) => setForm(current => ({ ...current, isUrgent: checked, urgentReason: checked ? current.urgentReason : "" }))} />} label="เร่งด่วน" /></Stack></Grid>
          <Grid item xs={12} md={form.isUrgent ? 6 : 12}><TextField fullWidth multiline minRows={3} label="ความต้องการพิเศษ (ถ้ามี)" placeholder="เช่น ต้องการรถตู้ มีสัมภาระจำนวนมาก หรือมีอุปกรณ์พิเศษ" value={form.specialRequirement} onChange={field("specialRequirement")} /></Grid>
          {form.isUrgent && <Grid item xs={12} md={6}><TextField required fullWidth multiline minRows={3} label="เหตุผลความเร่งด่วน" value={form.urgentReason} onChange={field("urgentReason")} /></Grid>}
        </Grid>

        <Box sx={{ borderTop: 1, borderColor: "divider", mt: 3, pt: 3 }}>
          <Typography variant="h6" fontWeight={900}>2. ผู้ร่วมเดินทาง (ไม่รวมคนขับ)</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>เลือกได้เฉพาะบุคลากรที่มีบัญชีผู้ใช้ในระบบ HOP</Typography>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={9} sx={{ alignSelf: "flex-start" }}><Autocomplete multiple filterSelectedOptions options={options} value={selectedPersonnel} loading={personnel.isFetching} isOptionEqualToValue={(option, value) => option.id === value.id} getOptionLabel={option => option.fullName} onInputChange={(_, value, reason) => reason === "input" && setPersonnelSearch(value)} onChange={(_, value) => setSelectedPersonnel(value)} renderOption={(props, option) => <li {...props} key={option.id}><Box><Typography variant="body2" fontWeight={700}>{option.fullName}</Typography><Typography variant="caption" color="text.secondary">{[option.employeeCode, option.departmentName].filter(Boolean).join(" · ") || "บุคลากร HOP"}</Typography></Box></li>} renderTags={(value, getTagProps) => value.map((option, index) => <Chip label={option.fullName} {...getTagProps({ index })} key={option.id} />)} renderInput={params => <TextField {...params} label="เพิ่มผู้ร่วมเดินทาง" placeholder="ค้นหาชื่อ รหัสพนักงาน หรือหน่วยงาน" helperText="เลือกบุคลากรได้หลายคน" sx={{ "& .MuiOutlinedInput-root": { minHeight: 56 } }} />} /></Grid>
            <Grid item xs={12} md={3} sx={{ alignSelf: "flex-start" }}><TextField disabled fullWidth label="จำนวนผู้ร่วมเดินทาง" value={`${passengerCount} คน`} helperText="คำนวณอัตโนมัติ · ไม่รวมคนขับ" sx={{ "& .MuiOutlinedInput-root": { minHeight: 56 }, "& .MuiInputBase-input.Mui-disabled": { WebkitTextFillColor: "text.primary", fontWeight: 700 } }} /></Grid>
            <Grid item xs={12}><FormControlLabel control={<Switch checked={form.requesterTravels} onChange={(_, checked) => setForm(current => ({ ...current, requesterTravels: checked }))} />} label="ผู้ขอร่วมเดินทางด้วย" /></Grid>
          </Grid>
          {preservedExternal.length > 0 && <Alert severity="info" sx={{ mt: 2 }}>คำขอเดิมมีบุคคลภายนอก {preservedExternal.length} คน ระบบจะเก็บข้อมูลเดิมไว้ แต่ไม่สามารถเพิ่มบุคคลภายนอกใหม่จากหน้านี้ได้</Alert>}
        </Box>
      </CardContent>

      <Box sx={{ borderTop: 1, borderColor: "divider", bgcolor: "background.paper", px: { xs: 2, md: 3 }, py: 2, position: "sticky", bottom: 0, zIndex: 2 }}>
        <Stack direction={{ xs: "column-reverse", sm: "row" }} justifyContent="space-between" spacing={1.5}>
          <Button variant="outlined" onClick={() => navigate(-1)} disabled={save.isPending}>ยกเลิก</Button>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
            <Button variant="outlined" startIcon={<SaveOutlinedIcon />} disabled={!hasRequiredFields || save.isPending} onClick={() => save.mutate(false)}>{save.isPending && !submitAfterSave ? "กำลังบันทึก..." : "บันทึกร่าง"}</Button>
            <Button variant="contained" startIcon={<SendOutlinedIcon />} disabled={!hasRequiredFields || save.isPending} onClick={() => save.mutate(true)}>{save.isPending && submitAfterSave ? "กำลังส่งคำขอ..." : "ส่งคำขอ"}</Button>
          </Stack>
        </Stack>
      </Box></Card>
    </Stack>
  );
}

function getSaveErrorMessage(error: unknown) {
  if (isAxiosError<{ message?: string }>(error)) {
    const message = error.response?.data?.message;
    if (message?.includes("Expected return time must be after departure time")) {
      return "วันและเวลากลับต้องอยู่หลังวันและเวลาออกเดินทาง กรุณาตรวจสอบวันที่และเวลาอีกครั้ง";
    }
    if (message) return message;
  }
  return "บันทึกคำขอไม่สำเร็จ กรุณาตรวจข้อมูลและลองใหม่อีกครั้ง";
}
