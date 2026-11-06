import { useEffect, useRef, useState } from "react";
import { Alert, Autocomplete, Box, Button, Card, CardContent, MenuItem, Stack, TextField, Typography } from "@mui/material";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { isAxiosError } from "axios";
import { getCho11Options, renderCho11, type Cho11Form, type Cho11Options } from "../api/correspondenceApi";
import { PageHeader } from "../components/PageHeader";
import { MemoPdfPreview } from "../components/correspondence/MemoPdfPreview";
import { confirmSystem } from "../components/common/SystemDialogHost";
import { useMemoLeaveGuard } from "../hooks/useMemoLeaveGuard";
import { isValidApiDate, bangkokDayjs } from "../utils/dateFormat";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { focusMemoElement } from "../utils/memoValidation";

const months = ["มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];
export function monthIndex(value: string) {
  const match = /^(\d{4})-(0[1-9]|1[0-2])$/.exec(value);
  return match && +match[1] >= 1900 && +match[1] <= 2200 ? +match[1] * 12 + +match[2] - 1 : NaN;
}
export function validateCho11(form: Cho11Form, options: Cho11Options) {
  const errors: Record<string, string> = {};
  for (const field of ["startMonth", "endMonth"] as const) if (!Number.isFinite(monthIndex(form[field]))) errors[field] = "กรุณาเลือกเดือนและปี พ.ศ. ให้ครบถ้วน";
  const count = monthIndex(form.endMonth) - monthIndex(form.startMonth) + 1;
  if (count < 1 || count > 12) errors.endMonth = "ช่วงเอกสารต้องเรียงตามเวลาและไม่เกิน 12 เดือน";
  if (!isValidApiDate(form.employmentDate)) errors.employmentDate = "กรุณาระบุวันเริ่มงานให้ถูกต้อง";
  else if (monthIndex(form.employmentDate.slice(0, 7)) > monthIndex(form.startMonth)) errors.employmentDate = "วันเริ่มงานต้องไม่อยู่หลังเดือนเอกสาร";
  if (!form.hospital.trim() || form.hospital.length > 120) errors.hospital = "กรุณาระบุโรงพยาบาล ไม่เกิน 120 ตัวอักษร";
  if (!form.province.trim() || form.province.length > 80) errors.province = "กรุณาระบุจังหวัด ไม่เกิน 80 ตัวอักษร";
  if (!options.reviewers.some(p => p.id === form.reviewerUserId && p.position?.trim())) errors.reviewerUserId = "กรุณาเลือกผู้ตรวจสอบที่มีตำแหน่งครบถ้วน";
  const author = (options.people ?? [options.author]).find(p => p.id === (form.authorUserId ?? options.author.id));
  if (!author?.name.trim()) errors.authorUserId = "กรุณาเลือกผู้ขอจากบุคลากรในระบบ";
  const position = form.authorPosition ?? author?.position ?? "";
  if (!position.trim() || position.length > 300) errors.authorPosition = "กรุณากรอกตำแหน่งผู้ขอ ไม่เกิน 300 ตัวอักษร";
  return errors;
}
function MonthFields({ label, value, error, onChange }: { label: string; value: string; error?: string; onChange: (value: string) => void }) {
  const [year, month] = value.split("-");
  const [yearText, setYearText] = useState(year ? String(+year + 543) : "");
  useEffect(() => { if (Number.isFinite(monthIndex(value))) setYearText(String(+value.slice(0, 4) + 543)); else if (!value) setYearText(""); }, [value]);
  return <Stack spacing={1} sx={{ flex: 1, minWidth: 0 }}><Typography fontWeight={600}>{label}</Typography><Stack direction="row" spacing={1}>
    <TextField select fullWidth label={`เดือน${label}`} value={month || ""} error={!!error} onChange={e => onChange(`${year || ""}-${e.target.value}`)}>
      {months.map((m, i) => <MenuItem key={m} value={String(i + 1).padStart(2, "0")}>{m}</MenuItem>)}
    </TextField>
    <TextField fullWidth label={`ปี พ.ศ.${label}`} value={yearText} error={!!error} inputProps={{ inputMode: "numeric" }} onChange={e => { const y = e.target.value.replace(/\D/g, "").slice(0, 4); setYearText(y); onChange(`${y.length === 4 && +y >= 2443 && +y <= 2743 ? +y - 543 : ""}-${month || ""}`); }} />
  </Stack>{error && <Typography color="error" variant="caption">{error}</Typography>}</Stack>;
}
type Preview = { pdf: string; docx: string; name: string };
export function Cho11Page() {
  const [options, setOptions] = useState<Cho11Options | null>(null), [form, setForm] = useState<Cho11Form | null>(null);
  const [error, setError] = useState(""), [fields, setFields] = useState<Record<string, string>>({}), [busy, setBusy] = useState(false), [dirty, setDirty] = useState(false), [preview, setPreview] = useState<Preview | null>(null);
  const files = useRef<Preview | null>(null), request = useRef<AbortController | null>(null), root = useRef<HTMLDivElement>(null);
  useMemoLeaveGuard(dirty);
  function initial(data: Cho11Options): Cho11Form {
    const heads = data.heads.filter(h => !!data.author.departmentId && h.departmentId === data.author.departmentId && data.reviewers.some(r => r.id === h.id));
    const month = bangkokDayjs().format("YYYY-MM");
    return { authorUserId: data.author.id, authorPosition: data.author.position ?? "", startMonth: month, endMonth: month, employmentDate: "", hospital: data.hospital, province: data.province, reviewerUserId: heads.length === 1 ? heads[0].id : "" };
  }
  function invalidate() {
    request.current?.abort(); request.current = null; setBusy(false);
    if (files.current) { URL.revokeObjectURL(files.current.pdf); URL.revokeObjectURL(files.current.docx); }
    files.current = null; setPreview(null);
  }
  useEffect(() => {
    const controller = new AbortController();
    getCho11Options(controller.signal).then(data => { if (!controller.signal.aborted) { setOptions(data); setForm(initial(data)); } }).catch(() => { if (!controller.signal.aborted) setError("โหลดข้อมูล ฉ.11 ไม่สำเร็จ กรุณาโหลดหน้าใหม่"); });
    return () => { controller.abort(); request.current?.abort(); if (files.current) { URL.revokeObjectURL(files.current.pdf); URL.revokeObjectURL(files.current.docx); } };
  }, []);
  function change(patch: Partial<Cho11Form>) { invalidate(); setForm(current => current && { ...current, ...patch }); setDirty(true); setFields({}); setError(""); }
  async function makePreview() {
    if (!form || !options) return;
    const issues = validateCho11(form, options); setFields(issues);
    if (Object.keys(issues).length) { setError(Object.values(issues)[0]); const field = root.current?.querySelector<HTMLElement>(`[data-field="${Object.keys(issues)[0]}"]`); focusMemoElement(field?.querySelector<HTMLElement>("input, [role=combobox]") ?? field); return; }
    invalidate(); setBusy(true); setError(""); const controller = new AbortController(); request.current = controller; const created: string[] = [];
    try {
      const result = await renderCho11(form, controller.signal); if (controller.signal.aborted) return;
      const url = (base64: string, type: string) => { const value = URL.createObjectURL(new Blob([Uint8Array.from(atob(base64), c => c.charCodeAt(0))], { type })); created.push(value); return value; };
      const next = { pdf: url(result.pdfBase64, "application/pdf"), docx: url(result.docxBase64, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"), name: result.fileName };
      files.current = next; setPreview(next); setDirty(true);
    } catch (e) { created.forEach(URL.revokeObjectURL); if (!controller.signal.aborted) setError(isAxiosError(e) ? e.response?.data?.message ?? "สร้างเอกสาร ฉ.11 ไม่สำเร็จ" : "สร้างเอกสาร ฉ.11 ไม่สำเร็จ"); }
    finally { if (!controller.signal.aborted) setBusy(false); }
  }
  function download(type: "pdf" | "docx") { if (!preview) return; const a = document.createElement("a"); a.href = preview[type]; a.download = `${preview.name}.${type}`; a.click(); setDirty(false); }
  if (!options || !form) return error ? <Alert severity="error">{error}</Alert> : <Typography role="status">กำลังโหลดข้อมูล ฉ.11...</Typography>;
  const pages = monthIndex(form.endMonth) - monthIndex(form.startMonth) + 1;
  return <Stack ref={root} spacing={3}>
    <PageHeader title="สร้างเอกสาร ฉ.11" subtitle="ใบขอรับเงินค่าตอบแทนเบี้ยเลี้ยงเหมาจ่าย สร้างหนึ่งหน้าต่อเดือน" />
    <Alert severity="info">เอกสารไม่ถูกเก็บบนเซิร์ฟเวอร์ กรุณาดาวน์โหลดก่อนปิดหรือรีเฟรชหน้า ชื่อผู้ตรวจสอบเป็นการเตรียมเอกสาร ไม่ใช่การอนุมัติในระบบ</Alert>
    <Card><CardContent><Stack spacing={3}>
      <Typography variant="h6">ข้อมูลผู้ขอและหน่วยบริการ</Typography>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <Autocomplete data-field="authorUserId" fullWidth options={options.people ?? [options.author]} value={(options.people ?? [options.author]).find(p => p.id === form.authorUserId) ?? null} getOptionLabel={p => p.name} isOptionEqualToValue={(a, b) => a.id === b.id} onChange={(_, person) => change({ authorUserId: person?.id ?? "", authorPosition: person?.position ?? "" })} renderOption={(props, person) => <li {...props} key={person.id}><Box sx={{ overflowWrap: "anywhere" }}>{person.name}<Typography variant="caption" display="block" color="text.secondary">{person.position || "ยังไม่ระบุตำแหน่ง"}</Typography></Box></li>} renderInput={params => <TextField {...params} required label="ผู้ขอจาก HOP" error={!!fields.authorUserId} helperText={fields.authorUserId ?? "ค้นหาและเลือกบุคลากร ค่าเริ่มต้นเป็นเจ้าของบัญชี"} />} />
        <TextField data-field="authorPosition" fullWidth required label="ตำแหน่งผู้ขอ" value={form.authorPosition ?? ""} onChange={e => change({ authorPosition: e.target.value })} error={!!fields.authorPosition} helperText={fields.authorPosition ?? "แก้ไขเฉพาะเอกสารนี้ ไม่เปลี่ยนข้อมูล Profile"} />
      </Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}><TextField data-field="hospital" fullWidth required label="โรงพยาบาล" value={form.hospital} error={!!fields.hospital} helperText={fields.hospital} onChange={e => change({ hospital: e.target.value })} /><TextField data-field="province" fullWidth required label="จังหวัด" value={form.province} error={!!fields.province} helperText={fields.province} onChange={e => change({ province: e.target.value })} /></Stack>
      <Typography color="text.secondary">ระดับ/กลุ่ม: {options.areaLevel}</Typography>
      <TextField data-field="reviewerUserId" select required label="ผู้ตรวจสอบรับรอง" value={form.reviewerUserId} error={!!fields.reviewerUserId} helperText={fields.reviewerUserId || "เลือกผู้อนุมัติได้ทุกหน่วยงาน ค่าเริ่มต้นเป็นหัวหน้าหน่วยงานของตนเอง"} onChange={e => change({ reviewerUserId: e.target.value })}>{options.reviewers.map(p => <MenuItem key={p.id} value={p.id} sx={{ whiteSpace: "normal" }}>{p.name} · {p.position || "ไม่ระบุตำแหน่ง"}</MenuItem>)}</TextField>
    </Stack></CardContent></Card>
    <Card><CardContent><Stack spacing={3}>
      <Typography variant="h6">ช่วงเดือนเอกสารและวันเริ่มงาน</Typography>
      <Stack direction={{ xs: "column", md: "row" }} spacing={3}>
        <Box data-field="startMonth" flex={1}><MonthFields label="เอกสารเริ่มต้น" value={form.startMonth} error={fields.startMonth} onChange={value => change({ startMonth: value })} /></Box>
        <Box data-field="endMonth" flex={1}><MonthFields label="เอกสารสิ้นสุด" value={form.endMonth} error={fields.endMonth} onChange={value => change({ endMonth: value })} /></Box>
      </Stack>
      <Box data-field="employmentDate"><AppDatePicker label="วันเริ่มงาน" required value={form.employmentDate} error={!!fields.employmentDate} helperText={fields.employmentDate} onChange={value => change({ employmentDate: value })} /></Box>
      <Alert severity="info">คำนวณอายุงานจากวันเริ่มงานถึงสิ้นเดือนของแต่ละหน้า รวมวันสุดท้าย แสดงปี เดือน และวัน โดยไม่เปลี่ยนข้อมูล Profile{pages >= 1 && pages <= 12 ? ` • จำนวนเอกสาร ${pages} หน้า` : ""}</Alert>
    </Stack></CardContent></Card>
    {error && <Alert severity="error">{error}</Alert>}
    <Stack direction={{ xs: "column", sm: "row" }} spacing={1} justifyContent="flex-end"><Button variant="contained" startIcon={<VisibilityOutlinedIcon />} disabled={busy} onClick={() => void makePreview()}>{busy ? "กำลังสร้างเอกสาร..." : "สร้าง Preview"}</Button><Button variant="outlined" onClick={async () => { if (dirty && !await confirmSystem("ล้างข้อมูล ฉ.11 ทั้งหมดหรือไม่?")) return; invalidate(); setForm(initial(options)); setDirty(false); setError(""); setFields({}); }}>ล้างฟอร์ม</Button></Stack>
    {preview && <Card><CardContent><Stack spacing={2}><Stack direction={{ xs: "column", sm: "row" }} spacing={1}><Button variant="contained" onClick={() => download("docx")}>ดาวน์โหลด DOCX</Button><Button variant="outlined" onClick={() => download("pdf")}>ดาวน์โหลด PDF</Button><Button component="a" href={preview.pdf} target="_blank" rel="noopener noreferrer" startIcon={<OpenInNewIcon />} variant="outlined">ดูเต็มหน้า</Button></Stack><MemoPdfPreview url={preview.pdf} documentLabel="เอกสาร ฉ.11" /></Stack></CardContent></Card>}
  </Stack>;
}

