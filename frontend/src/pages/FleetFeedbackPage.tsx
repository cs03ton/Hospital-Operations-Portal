import AccessTimeOutlinedIcon from "@mui/icons-material/AccessTimeOutlined";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import ChatBubbleOutlineOutlinedIcon from "@mui/icons-material/ChatBubbleOutlineOutlined";
import CleaningServicesOutlinedIcon from "@mui/icons-material/CleaningServicesOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import HealthAndSafetyOutlinedIcon from "@mui/icons-material/HealthAndSafetyOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import ReportProblemOutlinedIcon from "@mui/icons-material/ReportProblemOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import StarOutlineRoundedIcon from "@mui/icons-material/StarOutlineRounded";
import VolunteerActivismOutlinedIcon from "@mui/icons-material/VolunteerActivismOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, FormControl, FormControlLabel, FormLabel, MenuItem, Radio, RadioGroup, Rating, Stack, TextField, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useMutation, useQuery } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getFleetFeedbackContext, submitFleetFeedback, type SubmitFleetFeedback } from "../api/fleetApi";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDateTime } from "../utils/dateFormat";

const incidentCategories = [
  ["DRIVING", "การขับขี่"], ["PUNCTUALITY", "ความตรงต่อเวลา"], ["SERVICE", "การให้บริการ"],
  ["VEHICLE_CONDITION", "สภาพรถ"], ["CLEANLINESS", "ความสะอาด"], ["OTHER", "อื่น ๆ"],
] as const;

export function FleetFeedbackPage() {
  const { tripId } = useParams();
  const navigate = useNavigate();
  const { showSuccess } = useNotification();
  const [form, setForm] = useState<SubmitFleetFeedback>({ punctualityRating: 0, safetyRating: 0, serviceRating: 0, overallRating: 0, vehicleConditionRating: 0, vehicleCleanlinessRating: 0, hasIncident: false, incidentCategory: null, comment: "" });
  const [validationError, setValidationError] = useState("");
  const context = useQuery({ queryKey: ["fleet-feedback-context", tripId], queryFn: () => getFleetFeedbackContext(tripId!), enabled: Boolean(tripId), retry: false });
  const submit = useMutation({
    mutationFn: () => submitFleetFeedback(tripId!, form),
    onSuccess: () => { showSuccess("ส่ง Feedback การเดินทางเรียบร้อยแล้ว ขอบคุณสำหรับความคิดเห็น"); navigate("/fleet/dashboard"); },
  });
  const setRating = (key: keyof SubmitFleetFeedback) => (_: unknown, value: number | null) => setForm(current => ({ ...current, [key]: value ?? 0 }));
  const validateAndSubmit = () => {
    if ([form.punctualityRating, form.safetyRating, form.serviceRating, form.overallRating, form.vehicleConditionRating, form.vehicleCleanlinessRating].some(value => value < 1)) return setValidationError("กรุณาให้คะแนนครบทุกหัวข้อ");
    if (form.hasIncident && (!form.incidentCategory || !form.comment?.trim())) return setValidationError("กรุณาเลือกประเภทและระบุรายละเอียดเหตุการณ์");
    setValidationError(""); submit.mutate();
  };

  if (context.isLoading) return <Stack alignItems="center" spacing={2} sx={{ py: 8 }}><CircularProgress /><Typography>กำลังโหลดข้อมูลการเดินทาง...</Typography></Stack>;
  if (context.isError || !context.data) return <><PageHeader title="Feedback การเดินทาง" subtitle="แสดงความคิดเห็นหลังการเดินทาง" /><Alert severity="error">{apiMessage(context.error, "คุณไม่มีสิทธิ์เปิด Feedback สำหรับการเดินทางนี้")}</Alert></>;
  const trip = context.data;
  if (!trip.canSubmitFeedback) return <><PageHeader title="Feedback การเดินทาง" subtitle="แสดงความคิดเห็นหลังการเดินทาง" /><Alert severity={trip.feedbackStatus === "SUBMITTED" ? "success" : "info"}>{trip.feedbackStatus === "SUBMITTED" ? "คุณส่ง Feedback สำหรับการเดินทางนี้แล้ว" : trip.feedbackStatus === "EXPIRED" ? "หมดระยะเวลาให้ Feedback การเดินทางแล้ว" : "การเดินทางนี้ยังไม่เปิดให้ Feedback"}</Alert></>;

  return <Box sx={{ pb: "calc(88px + env(safe-area-inset-bottom))" }}>
    <PageHeader title="Feedback การเดินทาง" subtitle="ความคิดเห็นเป็นทางเลือกและไม่มีผลต่อสถานะการเดินทาง" />
    <Stack spacing={2}>
      <Card variant="outlined" sx={{ overflow: "hidden", borderColor: "primary.light", background: theme => `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.09)}, ${alpha(theme.palette.warning.main, 0.08)})` }}><CardContent sx={{ p: { xs: 2, sm: 2.5 } }}><Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}><Box sx={{ width: 52, height: 52, borderRadius: 3, display: "grid", placeItems: "center", bgcolor: "primary.main", color: "primary.contrastText", boxShadow: 3, flexShrink: 0 }}><DirectionsCarOutlinedIcon sx={{ fontSize: 30 }} /></Box><Box sx={{ flex: 1 }}><Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}><Typography variant="h6" fontWeight={900} color="primary.dark">{trip.requestNo}</Typography><Chip size="small" color="success" variant="outlined" label="พร้อมให้ประเมิน" sx={{ alignSelf: { xs: "flex-start", sm: "center" } }} /></Stack><Typography fontWeight={700} sx={{ mt: 0.5 }}>{trip.destination}</Typography><Stack direction={{ xs: "column", sm: "row" }} spacing={{ xs: 0.25, sm: 2 }} sx={{ mt: 0.75 }}><Typography variant="body2" color="text.secondary">เดินทาง {formatThaiDateTime(trip.tripDate)}</Typography><Typography variant="body2" color="text.secondary">รถ {trip.vehicleDisplay}</Typography><Typography variant="body2" color="text.secondary">คนขับ {trip.driverDisplay}</Typography></Stack></Box><Box sx={{ px: 1.5, py: 1, borderRadius: 2, bgcolor: alpha("#fff", 0.68), border: 1, borderColor: "warning.light" }}><Typography variant="caption" color="text.secondary">ประเมินได้ถึง</Typography><Typography variant="body2" fontWeight={800} color="warning.dark">{formatThaiDateTime(trip.feedbackDeadline)}</Typography></Box></Stack></CardContent></Card>
      {validationError && <Alert severity="warning">{validationError}</Alert>}
      {submit.isError && <Alert severity="error">{apiMessage(submit.error, "ส่ง Feedback ไม่สำเร็จ กรุณาลองใหม่")}</Alert>}
      <RatingSection title="การให้บริการของคนขับ" subtitle="แตะดาวเพื่อให้คะแนนในแต่ละหัวข้อ" icon={<PersonOutlineOutlinedIcon />} rows={[
        ["ความตรงต่อเวลา", "punctualityRating", <AccessTimeOutlinedIcon />], ["ความปลอดภัยในการขับขี่", "safetyRating", <HealthAndSafetyOutlinedIcon />], ["มารยาทและการให้บริการ", "serviceRating", <VolunteerActivismOutlinedIcon />], ["ความพึงพอใจโดยรวม", "overallRating", <AutoAwesomeOutlinedIcon />],
      ]} form={form} setRating={setRating} />
      <RatingSection title="สภาพและความพร้อมของรถ" subtitle="ช่วยให้เราดูแลรถให้สะอาดและพร้อมใช้งาน" icon={<DirectionsCarOutlinedIcon />} rows={[["สภาพรถ", "vehicleConditionRating", <DirectionsCarOutlinedIcon />], ["ความสะอาด", "vehicleCleanlinessRating", <CleaningServicesOutlinedIcon />]]} form={form} setRating={setRating} />
      <Card variant="outlined" sx={{ borderColor: form.hasIncident ? "warning.main" : "divider" }}><CardContent sx={{ p: { xs: 2, sm: 2.5 } }}><Stack spacing={2.25}><SectionHeading icon={<ChatBubbleOutlineOutlinedIcon />} title="เหตุการณ์และข้อเสนอแนะ" subtitle="ข้อมูลของคุณช่วยให้เราปรับปรุงบริการให้ดียิ่งขึ้น" /><Box sx={{ p: 1.5, borderRadius: 2.5, bgcolor: theme => alpha(theme.palette.primary.main, 0.045) }}><FormControl><FormLabel sx={{ color: "text.primary", fontWeight: 700 }}>มีเหตุการณ์ที่ต้องการแจ้งหรือไม่</FormLabel><RadioGroup row value={form.hasIncident ? "yes" : "no"} onChange={event => setForm(current => ({ ...current, hasIncident: event.target.value === "yes", incidentCategory: event.target.value === "yes" ? current.incidentCategory : null }))}><FormControlLabel value="no" control={<Radio color="success" />} label="ไม่มี ทุกอย่างเรียบร้อย" /><FormControlLabel value="yes" control={<Radio color="warning" />} label="มีเหตุการณ์ที่ต้องแจ้ง" /></RadioGroup></FormControl></Box>{form.hasIncident && <Stack spacing={2} sx={{ p: { xs: 1.5, sm: 2 }, borderRadius: 2.5, bgcolor: theme => alpha(theme.palette.warning.main, 0.07), border: 1, borderColor: "warning.light" }}><Stack direction="row" spacing={1} alignItems="center"><ReportProblemOutlinedIcon color="warning" /><Typography fontWeight={800}>รายละเอียดเหตุการณ์</Typography></Stack><TextField select fullWidth label="ประเภทเหตุการณ์" value={form.incidentCategory ?? ""} onChange={event => setForm(current => ({ ...current, incidentCategory: event.target.value }))}>{incidentCategories.map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}</TextField><TextField fullWidth multiline minRows={4} label="เล่าเหตุการณ์หรือข้อเสนอแนะ" placeholder="ระบุรายละเอียดที่จำเป็น โดยไม่ต้องใส่ข้อมูลผู้ป่วยหรือข้อมูลสุขภาพ" inputProps={{ maxLength: 2000 }} value={form.comment ?? ""} onChange={event => setForm(current => ({ ...current, comment: event.target.value }))} helperText={`${form.comment?.length ?? 0}/2000 ตัวอักษร`}/></Stack>}</Stack></CardContent></Card>
    </Stack>
    <Stack direction="row" spacing={1.5} sx={{ position: "fixed", left: { xs: 8, sm: "auto" }, right: 8, bottom: "calc(8px + env(safe-area-inset-bottom))", zIndex: 10, bgcolor: "background.paper", p: 1.25, borderRadius: 2, boxShadow: 6 }}><Button disabled={submit.isPending} onClick={() => navigate("/fleet/dashboard")}>ยกเลิก</Button><Button variant="contained" startIcon={submit.isPending ? <CircularProgress size={18} color="inherit" /> : <SendOutlinedIcon />} disabled={submit.isPending} onClick={validateAndSubmit}>{submit.isPending ? "กำลังส่ง..." : "ส่ง Feedback"}</Button></Stack>
  </Box>;
}

function RatingSection({ title, subtitle, icon, rows, form, setRating }: { title: string; subtitle: string; icon: React.ReactNode; rows: readonly (readonly [string, keyof SubmitFleetFeedback, React.ReactNode])[]; form: SubmitFleetFeedback; setRating: (key: keyof SubmitFleetFeedback) => (_: unknown, value: number | null) => void }) {
  return <Card variant="outlined" sx={{ overflow: "hidden" }}><Box sx={{ height: 4, background: theme => `linear-gradient(90deg, ${theme.palette.primary.main}, ${theme.palette.warning.main})` }} /><CardContent sx={{ p: { xs: 2, sm: 2.5 } }}><Stack spacing={2}><SectionHeading icon={icon} title={title} subtitle={subtitle} /><Stack spacing={1.25}>{rows.map(([label, key, rowIcon]) => {
    const value = Number(form[key]);
    return <Stack key={key} direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "center" }} spacing={1} sx={{ p: { xs: 1.5, sm: 1.75 }, borderRadius: 2.5, bgcolor: theme => value ? alpha(theme.palette.warning.main, 0.075) : alpha(theme.palette.primary.main, 0.035), border: 1, borderColor: value ? "warning.light" : "divider", transition: "all .2s ease", "&:hover": { transform: "translateY(-1px)", boxShadow: 1 } }}><Stack direction="row" spacing={1.25} alignItems="center"><Box sx={{ width: 36, height: 36, borderRadius: 2, display: "grid", placeItems: "center", color: "primary.main", bgcolor: theme => alpha(theme.palette.primary.main, 0.1), "& svg": { fontSize: 21 } }}>{rowIcon}</Box><Box><Typography component="label" htmlFor={`rating-${key}`} fontWeight={750}>{label}</Typography><Typography variant="caption" color={value ? "warning.dark" : "text.secondary"}>{ratingLabel(value)}</Typography></Box></Stack><Rating id={`rating-${key}`} name={key} value={value} onChange={setRating(key)} size="large" emptyIcon={<StarOutlineRoundedIcon fontSize="inherit" />} sx={{ alignSelf: { xs: "flex-start", sm: "center" }, color: "warning.main", "& .MuiRating-icon": { mx: { xs: 0.1, sm: 0.2 } } }} aria-label={label} /></Stack>;
  })}</Stack></Stack></CardContent></Card>;
}
function SectionHeading({ icon, title, subtitle }: { icon: React.ReactNode; title: string; subtitle: string }) { return <Stack direction="row" spacing={1.25} alignItems="center"><Box sx={{ width: 42, height: 42, borderRadius: 2.5, display: "grid", placeItems: "center", bgcolor: theme => alpha(theme.palette.primary.main, 0.11), color: "primary.main", flexShrink: 0 }}>{icon}</Box><Box><Typography variant="h6" fontWeight={900}>{title}</Typography><Typography variant="body2" color="text.secondary">{subtitle}</Typography></Box></Stack>; }
function ratingLabel(value: number) { return value === 5 ? "ดีเยี่ยม" : value === 4 ? "ดีมาก" : value === 3 ? "พอใช้" : value === 2 ? "ควรปรับปรุง" : value === 1 ? "ควรปรับปรุงอย่างมาก" : "ยังไม่ได้ให้คะแนน"; }
function apiMessage(error: unknown, fallback: string) { return isAxiosError<{ message?: string }>(error) ? error.response?.data?.message ?? fallback : fallback; }
