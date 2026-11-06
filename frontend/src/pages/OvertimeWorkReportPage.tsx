import { useEffect, useRef, useState } from "react";
import { Alert, Box, Button, Card, CardContent, Chip, MenuItem, Stack, Tab, Tabs, TextField, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import ScheduleOutlinedIcon from "@mui/icons-material/ScheduleOutlined";
import CalculateOutlinedIcon from "@mui/icons-material/CalculateOutlined";
import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import { isAxiosError } from "axios";
import { getMemoOptions, renderOvertimeBundle, type MemoFiles, type MemoOptions, type OvertimePaymentInput, type OvertimeReportForm, type OvertimeReportEntry, type OvertimeReportTask } from "../api/correspondenceApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { AppTimePicker } from "../components/common/AppTimePicker";
import { confirmSystem } from "../components/common/SystemDialogHost";
import { MemoPdfPreview } from "../components/correspondence/MemoPdfPreview";
import { PageHeader } from "../components/PageHeader";
import { useMemoLeaveGuard } from "../hooks/useMemoLeaveGuard";
import { bangkokDayjs } from "../utils/dateFormat";
import { focusMemoElement } from "../utils/memoValidation";
import { defaultOvertimeSupervisor, overtimeApprovers, overtimeWholeHours, validateOvertimeReport } from "../utils/overtimeReport";
import { MemoTemplateSelect, overtimeReportTemplateCode } from "../components/correspondence/MemoTemplateSelect";
import { OvertimePaymentPanel } from "../components/correspondence/OvertimePaymentPanel";

type TaskDraft = OvertimeReportTask & { id: number };
type EntryDraft = Omit<OvertimeReportEntry, "hours" | "tasks"> & { id: number; manualEndDate: boolean; tasks: TaskDraft[] };
type Draft = Omit<OvertimeReportForm, "entries"> & { entries: EntryDraft[] };
type Preview = { pdf: string; docx: string; name: string };
type BundlePreview = { workReport: Preview; paymentEvidence: Preview };
const title = "รายงานผลการปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ";
function fileUrl(base64: string, type: string) {
  return URL.createObjectURL(new Blob([Uint8Array.from(atob(base64), char => char.charCodeAt(0))], { type }));
}
export function OvertimeWorkReportPage({ memoOptions, initialDepartmentId, initialSupervisorUserId, onSelectTemplate }: { memoOptions?: MemoOptions; initialDepartmentId?: string; initialSupervisorUserId?: string | null; onSelectTemplate?: (code: string) => void }) {
  const [options, setOptions] = useState<MemoOptions | null>(null), [form, setForm] = useState<Draft | null>(null);
  const [error, setError] = useState(""), [fields, setFields] = useState<Record<string, string>>({});
  const [dirty, setDirty] = useState(false), [rendering, setRendering] = useState(false), [preview, setPreview] = useState<BundlePreview | null>(null);
  const [paymentMonth, setPaymentMonth] = useState(() => bangkokDayjs().format("YYYY-MM")), [payment, setPayment] = useState<OvertimePaymentInput | null>(null), [paymentKey, setPaymentKey] = useState(0), [tab, setTab] = useState<keyof BundlePreview>("workReport");
  const downloaded = useRef(new Set<keyof BundlePreview>());
  const root = useRef<HTMLDivElement>(null), sequence = useRef(0), revision = useRef(0), files = useRef<BundlePreview | null>(null), request = useRef<AbortController | null>(null);
  useMemoLeaveGuard(dirty);
  const task = (): TaskDraft => ({ id: ++sequence.current, description: "", quantity: "" });
  const entry = (): EntryDraft => ({ id: ++sequence.current, date: bangkokDayjs().format("YYYY-MM-DD"), endDate: bangkokDayjs().format("YYYY-MM-DD"), startTime: "", endTime: "", remarks: "", manualEndDate: false, tasks: [task()] });
  const initial = (data: MemoOptions): Draft => {
    const department = data.departments.find(d => d.id === (initialDepartmentId ?? data.proposer.departmentId));
    return { departmentId: department?.id ?? "", projectName: department?.name ?? "", supervisorUserId: defaultOvertimeSupervisor(data, initialSupervisorUserId), entries: [entry()] };
  };
  function invalidate() {
    revision.current++; request.current?.abort(); request.current = null; setRendering(false);
    if (files.current) for (const file of Object.values(files.current)) { URL.revokeObjectURL(file.pdf); URL.revokeObjectURL(file.docx); }
    downloaded.current.clear();
    files.current = null; setPreview(null);
  }
  useEffect(() => {
    if (memoOptions) { setOptions(memoOptions); setForm(initial(memoOptions)); return; }
    const controller = new AbortController();
    getMemoOptions(controller.signal).then(data => { if (!controller.signal.aborted) { setOptions(data); setForm(initial(data)); } })
      .catch(() => { if (!controller.signal.aborted) setError("โหลดข้อมูลประกอบรายงานไม่สำเร็จ กรุณาลองโหลดหน้าใหม่"); });
    return () => controller.abort();
  }, [memoOptions]);
  useEffect(() => () => { request.current?.abort(); if (files.current) for (const file of Object.values(files.current)) { URL.revokeObjectURL(file.pdf); URL.revokeObjectURL(file.docx); } }, []);
  function paymentEdit() { invalidate(); setPayment(null); setDirty(true); setFields({}); setError(""); }
  function change(update: (draft: Draft) => Draft) { paymentEdit(); setForm(current => current && update(current)); }
  function changeEntry(index: number, patch: Partial<EntryDraft>) { change(current => ({ ...current, entries: current.entries.map((item, i) => i === index ? { ...item, ...patch } : item) })); }
  function serialize(draft: Draft): OvertimeReportForm {
    return { departmentId: draft.departmentId, projectName: draft.projectName, supervisorUserId: draft.supervisorUserId, entries: draft.entries.map(item => ({ date: item.date, endDate: item.endDate, startTime: item.startTime, endTime: item.endTime, hours: overtimeWholeHours(item), remarks: item.remarks, tasks: item.tasks.map(({ description, quantity }) => ({ description, quantity })) })) };
  }
  async function makePreview() {
    if (!options || !form) return;
    const payload = serialize(form), issues = validateOvertimeReport(payload, options);
    if (!payment) issues.push({ field: "payment", message: "กรุณาตรวจและยืนยันชั่วโมงเบิกก่อนสร้างเอกสารทั้งสองชุด" });
    if (issues.length) {
      setFields(Object.fromEntries(issues.map(issue => [issue.field, issue.message]))); setError(issues[0].message);
      const field = root.current?.querySelector<HTMLElement>(`[data-report-field="${issues[0].field}"]`);
      focusMemoElement(field?.querySelector<HTMLElement>(issues[0].field === "payment" ? "button" : "input, textarea, [role=combobox]") ?? field); return;
    }
    invalidate(); setError(""); setFields({}); setRendering(true);
    const current = revision.current, controller = new AbortController(); request.current = controller; const created: string[] = [];
    try {
      const result = await renderOvertimeBundle(payload, payment!, controller.signal); if (controller.signal.aborted || current !== revision.current) return;
      const prepare = (file: MemoFiles): Preview => {
        const pdf = fileUrl(file.pdfBase64, "application/pdf"); created.push(pdf);
        const docx = fileUrl(file.docxBase64, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"); created.push(docx);
        return { pdf, docx, name: file.fileName };
      };
      const next = { workReport: prepare(result.workReport), paymentEvidence: prepare(result.paymentEvidence) };
      files.current = next; setPreview(next); setTab("workReport");
    } catch (e) { created.forEach(url => URL.revokeObjectURL(url)); if (!controller.signal.aborted) setError(isAxiosError(e) ? e.response?.data?.message ?? "สร้างเอกสารทั้งสองชุดไม่สำเร็จ กรุณาลองใหม่" : "สร้างเอกสารทั้งสองชุดไม่สำเร็จ กรุณาลองใหม่"); }
    finally { if (current === revision.current && !controller.signal.aborted) setRendering(false); }
  }
  async function clear() {
    if (!options || dirty && !await confirmSystem("ล้างข้อมูลรายงานทั้งหมดหรือไม่?")) return;
    invalidate(); setForm(initial(options)); setPayment(null); setPaymentMonth(bangkokDayjs().format("YYYY-MM")); setPaymentKey(n => n + 1); setDirty(false); setError(""); setFields({});
  }
  function download(type: "pdf" | "docx") {
    if (!preview) return; const file = preview[tab]; const anchor = document.createElement("a"); anchor.href = file[type]; anchor.download = `${file.name}.${type}`; anchor.click(); downloaded.current.add(tab); if (downloaded.current.size === 2) setDirty(false);
  }
  if (!options || !form) return error ? <Alert severity="error">{error}</Alert> : <Typography role="status">กำลังโหลดข้อมูลรายงาน...</Typography>;
  const approvers = overtimeApprovers(options);
  return <Stack ref={root} spacing={3}>
    <PageHeader title={title} subtitle="กรอกวันทำงาน ลักษณะงาน ปริมาณ และเวลา ตรวจตัวอย่าง แล้วดาวน์โหลด Word / PDF" />
    {onSelectTemplate && <Card><CardContent><Stack spacing={1.5}><Typography fontWeight={800}>เทมเพลตเอกสาร</Typography><MemoTemplateSelect templates={options.contentTemplates} value={overtimeReportTemplateCode} onChange={code => { if (code !== overtimeReportTemplateCode) void (async () => { if (dirty && !await confirmSystem("เปลี่ยนเทมเพลตและล้างข้อมูลรายงานที่ยังกรอกอยู่หรือไม่?")) return; onSelectTemplate(code); })(); }} /></Stack></CardContent></Card>}
    <Alert severity="info">เอกสารไม่ถูกเก็บบนเซิร์ฟเวอร์ ข้อมูลอยู่เฉพาะแท็บนี้ กรุณาดาวน์โหลดก่อนปิดหรือโหลดหน้าใหม่</Alert>
    <Card><CardContent><Stack spacing={2}>
      <Typography variant="h6" fontWeight={800}>ข้อมูลรายงานและผู้ลงชื่อ</Typography>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", md: "repeat(2,minmax(0,1fr))" }, gap: 2 }}>
        <TextField data-report-field="departmentId" label="หน่วยงาน" required select value={form.departmentId} error={!!fields.departmentId} helperText={fields.departmentId} onChange={e => change(current => { const selected = options.departments.find(d => d.id === e.target.value); return { ...current, departmentId: e.target.value, projectName: selected?.name ?? "" }; })}>{options.departments.map(d => <MenuItem key={d.id} value={d.id}>{d.name}</MenuItem>)}</TextField>
        <TextField data-report-field="projectName" label="โครงการ/กิจกรรมปฏิบัติงาน" required value={form.projectName} inputProps={{ maxLength: 500 }} error={!!fields.projectName} helperText={fields.projectName} onChange={e => change(current => ({ ...current, projectName: e.target.value }))} />
        <Box data-report-field="reporter"><TextField label="ผู้รายงานจาก HOP" fullWidth value={options.proposer.name} InputProps={{ readOnly: true }} error={!!fields.reporter} helperText={fields.reporter} /></Box>
        <TextField label="ตำแหน่งผู้รายงาน" value={options.proposer.position ?? ""} InputProps={{ readOnly: true }} />
        <TextField data-report-field="supervisorUserId" label="ผู้ควบคุม" required select value={form.supervisorUserId} error={!!fields.supervisorUserId} helperText={fields.supervisorUserId ?? (approvers.length ? "ค่าเริ่มต้นเป็นหัวหน้าหน่วยงานของคุณ สามารถเลือกผู้อนุมัติจากทุกหน่วยงานได้" : "ไม่พบผู้อนุมัติที่เปิดใช้งาน กรุณาติดต่อผู้ดูแล")} sx={{ gridColumn: "1 / -1" }} onChange={e => change(current => ({ ...current, supervisorUserId: e.target.value }))}><MenuItem value="">เลือกผู้ควบคุม</MenuItem>{approvers.map(h => <MenuItem value={h.id} key={h.id} sx={{ whiteSpace: "normal" }}>{h.name} · {h.position ?? "ยังไม่มีตำแหน่ง"}</MenuItem>)}</TextField>
      </Box>
      <Typography variant="caption" color="text.secondary">ส่วนลงชื่อเป็นการเตรียมเอกสาร ไม่ถือเป็นการลงนามหรืออนุมัติในระบบ</Typography>
    </Stack></CardContent></Card>
    <Stack data-report-field="entries" spacing={2}>
      {form.entries.map((item, index) => {
        const path = `entries.${index}`;
        return <Card key={item.id} component="section" aria-label={`วันปฏิบัติงาน รายการ ${index + 1}`} sx={{ borderRadius: 3, border: "1px solid #dce5df", borderTop: "3px solid #205f52", boxShadow: "0 6px 24px rgba(31,79,65,.05)", overflow: "visible" }}>
          <CardContent sx={{ p: { xs: 2, sm: 3 }, "&:last-child": { pb: { xs: 2, sm: 3 } } }}><Stack spacing={2.5}>
            <Stack direction="row" spacing={1.5} alignItems="center" justifyContent="space-between" sx={{ flexWrap: "wrap", rowGap: 1 }}>
              <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
                <Box aria-hidden="true" sx={{ display: "grid", placeItems: "center", width: 42, height: 42, flexShrink: 0, borderRadius: 2, bgcolor: "#edf5f1", color: "#205f52", fontWeight: 800 }}>{String(index + 1).padStart(2, "0")}</Box>
                <Box sx={{ minWidth: 0 }}><Typography variant="h6" fontWeight={800} sx={{ fontSize: { xs: "1.05rem", sm: "1.15rem" } }}>วันปฏิบัติงาน รายการ {index + 1}</Typography><Typography variant="body2" color="text.secondary">ระบุช่วงเวลาและงานที่ปฏิบัติในวันนี้</Typography></Box>
              </Stack>
              <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} aria-label={`ลบวันปฏิบัติงาน รายการ ${index + 1}`} disabled={form.entries.length === 1} onClick={() => change(current => ({ ...current, entries: current.entries.filter((_, i) => i !== index) }))}>ลบรายการ</Button>
            </Stack>
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", md: "repeat(2,minmax(0,1fr))" }, gap: 2 }}>
              <Box sx={{ p: { xs: 1.75, sm: 2 }, borderRadius: 2.5, bgcolor: "#f5f9f7", border: "1px solid #dce8e1", minWidth: 0 }}>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}><CalendarMonthOutlinedIcon fontSize="small" sx={{ color: "#205f52" }} aria-hidden="true" /><Typography variant="subtitle2" fontWeight={800} color="primary.main">เริ่มปฏิบัติงาน</Typography></Stack>
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", sm: "repeat(2,minmax(0,1fr))", md: "minmax(0,1fr)", lg: "repeat(2,minmax(0,1fr))" }, gap: 1.5, "& .MuiInputBase-root": { bgcolor: "white" } }}>
                  <Box data-report-field={`${path}.date`}><AppDatePicker size="small" required label={`วันที่ปฏิบัติงาน รายการ ${index + 1}`} value={item.date} error={!!fields[`${path}.date`]} helperText={fields[`${path}.date`] ?? " "} onChange={value => changeEntry(index, { date: value, ...(!item.manualEndDate ? { endDate: value } : {}) })} /></Box>
                  <Box data-report-field={`${path}.startTime`}><AppTimePicker size="small" minuteStep={1} label={`เวลาเริ่ม รายการ ${index + 1}`} value={item.startTime} error={!!fields[`${path}.startTime`]} helperText={fields[`${path}.startTime`] ?? " "} onChange={value => changeEntry(index, { startTime: value })} /></Box>
                </Box>
              </Box>
              <Box sx={{ p: { xs: 1.75, sm: 2 }, borderRadius: 2.5, bgcolor: "#fbf8f0", border: "1px solid #e9dfc9", minWidth: 0 }}>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}><ScheduleOutlinedIcon fontSize="small" sx={{ color: "#917132" }} aria-hidden="true" /><Typography variant="subtitle2" fontWeight={800}>สิ้นสุดการปฏิบัติงาน</Typography></Stack>
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", sm: "repeat(2,minmax(0,1fr))", md: "minmax(0,1fr)", lg: "repeat(2,minmax(0,1fr))" }, gap: 1.5, "& .MuiInputBase-root": { bgcolor: "white" } }}>
                  <Box data-report-field={`${path}.endDate`}><AppDatePicker size="small" required label={`วันสิ้นสุด รายการ ${index + 1}`} value={item.endDate} error={!!fields[`${path}.endDate`]} helperText={fields[`${path}.endDate`] ?? " "} onChange={value => changeEntry(index, { endDate: value, manualEndDate: true })} /></Box>
                  <Box data-report-field={`${path}.endTime`}><AppTimePicker size="small" minuteStep={1} label={`เวลาสิ้นสุด รายการ ${index + 1}`} value={item.endTime} error={!!fields[`${path}.endTime`]} helperText={fields[`${path}.endTime`] ?? " "} onChange={value => changeEntry(index, { endTime: value })} /></Box>
                </Box>
              </Box>
            </Box>
            <Box sx={{ p: 2, borderRadius: 2.5, bgcolor: "#edf5f1", border: "1px solid #cfe3da", display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", sm: "200px minmax(0,1fr)" }, gap: 2, alignItems: "center" }}>
              <TextField data-report-field={`${path}.hours`} label={`ชั่วโมงทำงานจริง รายการ ${index + 1}`} value={overtimeWholeHours(item) ?? ""} type="number" disabled size="small" sx={{ "& .MuiInputBase-root": { bgcolor: "white" }, "& .MuiInputBase-input.Mui-disabled": { WebkitTextFillColor: "#205f52", fontSize: "1.65rem", fontWeight: 800, py: 1 }, "& .MuiInputLabel-root.Mui-disabled": { color: "#456b60" }, "& .MuiOutlinedInput-root.Mui-disabled fieldset": { borderColor: "#c4dacf" } }} />
              <Stack spacing={.5}><Stack direction="row" alignItems="center" spacing={.75}><CalculateOutlinedIcon fontSize="small" sx={{ color: "#205f52" }} aria-hidden="true" /><Typography variant="body2" fontWeight={800} color="primary.main">คำนวณชั่วโมงให้อัตโนมัติ</Typography></Stack><Typography variant="caption" color="text.secondary">ใช้เวลาแบบ 24 ชั่วโมง เลือกนาทีได้ 00–59 และตัดเศษชั่วโมงทิ้ง</Typography></Stack>
            </Box>
            <Stack data-report-field={`${path}.tasks`} spacing={1.5}>
              <Stack direction="row" spacing={1} alignItems="center"><AssignmentOutlinedIcon fontSize="small" color="primary" aria-hidden="true" /><Typography fontWeight={800}>รายละเอียดงานที่ปฏิบัติ</Typography><Chip label={`${item.tasks.length} งาน`} size="small" sx={{ height: 24, bgcolor: "#f3efdf", color: "#725e2f", fontWeight: 700 }} /></Stack>
              {item.tasks.map((work, taskIndex) => <Box key={work.id} component="section" aria-label={`งาน ${taskIndex + 1} ของรายการ ${index + 1}`} sx={{ p: { xs: 1.75, sm: 2 }, borderRadius: 2.5, bgcolor: "#fff", border: "1px solid #e5e7e2", minWidth: 0 }}><Stack spacing={2}>
                <Stack direction="row" alignItems="center" justifyContent="space-between"><Stack direction="row" alignItems="center" spacing={1}><Box aria-hidden="true" sx={{ display: "grid", placeItems: "center", width: 26, height: 26, borderRadius: 1, bgcolor: "#f8f3e7", color: "#917132", fontSize: ".8rem", fontWeight: 800 }}>{taskIndex + 1}</Box><Typography variant="subtitle2" fontWeight={800}>งาน {taskIndex + 1}</Typography></Stack><Button size="small" color="error" startIcon={<DeleteOutlineIcon fontSize="small" />} disabled={item.tasks.length === 1} aria-label={`ลบงาน ${taskIndex + 1} รายการ ${index + 1}`} onClick={() => changeEntry(index, { tasks: item.tasks.filter((_, i) => i !== taskIndex) })}>ลบงาน</Button></Stack>
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", md: "minmax(0,2fr) minmax(0,1fr)" }, gap: 2, alignItems: "start", "& .MuiInputBase-root": { bgcolor: "#fdfdfb" } }}>
                  <TextField data-report-field={`${path}.tasks.${taskIndex}.description`} required label="ลักษณะงานที่ปฏิบัติ" placeholder="อธิบายงานหรือผลการปฏิบัติงาน" multiline minRows={3} value={work.description} inputProps={{ maxLength: 2000, "aria-label": `ลักษณะงาน รายการ ${index + 1} งาน ${taskIndex + 1}` }} error={!!fields[`${path}.tasks.${taskIndex}.description`]} helperText={fields[`${path}.tasks.${taskIndex}.description`] ?? " "} onChange={e => changeEntry(index, { tasks: item.tasks.map((w, i) => i === taskIndex ? { ...w, description: e.target.value } : w) })} />
                  <TextField data-report-field={`${path}.tasks.${taskIndex}.quantity`} required label="ปริมาณ / หน่วยนับ" placeholder="เช่น จำนวน 159 ราย" multiline minRows={3} value={work.quantity} inputProps={{ maxLength: 200, "aria-label": `ปริมาณ/หน่วยนับ รายการ ${index + 1} งาน ${taskIndex + 1}` }} error={!!fields[`${path}.tasks.${taskIndex}.quantity`]} helperText={fields[`${path}.tasks.${taskIndex}.quantity`] ?? " "} onChange={e => changeEntry(index, { tasks: item.tasks.map((w, i) => i === taskIndex ? { ...w, quantity: e.target.value } : w) })} />
                </Box>
              </Stack></Box>)}
              <Button startIcon={<AddIcon />} variant="outlined" sx={{ alignSelf: { xs: "stretch", sm: "flex-start" }, borderStyle: "dashed", bgcolor: "#f8fbf9" }} disabled={item.tasks.length >= 20} onClick={() => changeEntry(index, { tasks: [...item.tasks, task()] })}>เพิ่มงานในรายการ {index + 1}</Button>
            </Stack>
            <TextField data-report-field={`${path}.remarks`} label={`หมายเหตุ รายการ ${index + 1}`} placeholder="ข้อมูลเพิ่มเติม (ถ้ามี)" multiline minRows={2} value={item.remarks} inputProps={{ maxLength: 1000 }} onChange={e => changeEntry(index, { remarks: e.target.value })} />
          </Stack></CardContent>
        </Card>;
      })}
      <Button variant="outlined" startIcon={<AddIcon />} disabled={form.entries.length >= 50} sx={{ alignSelf: "flex-start" }} onClick={() => change(current => ({ ...current, entries: [...current.entries, entry()] }))}>เพิ่มวันปฏิบัติงาน</Button>
    </Stack>
    <OvertimePaymentPanel key={paymentKey} month={paymentMonth} entries={serialize(form).entries} confirmed={!!payment} error={fields.payment} onMonthChange={month => { paymentEdit(); setPaymentMonth(month); }} onEdit={paymentEdit} onConfirm={input => { invalidate(); setPayment(input); setDirty(true); setError(""); setFields({}); }} />
    {error && <Alert severity="error">{error}</Alert>}
    <Card><CardContent><Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} justifyContent="flex-end">
      <Button variant="contained" startIcon={<VisibilityOutlinedIcon />} disabled={rendering} onClick={() => void makePreview()}>{rendering ? "กำลังสร้างรายงาน..." : "สร้าง Preview"}</Button>
      <Button variant="outlined" onClick={() => void clear()}>ล้างฟอร์ม</Button>
    </Stack></CardContent></Card>
    {preview && <Card><CardContent><Stack spacing={2}>
      <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable" scrollButtons="auto" aria-label="ตัวอย่างเอกสารสองรายงาน"><Tab id="work-report-tab" aria-controls="work-report-panel" value="workReport" label="รายงานผลการปฏิบัติงาน" /><Tab id="payment-evidence-tab" aria-controls="payment-evidence-panel" value="paymentEvidence" label="หลักฐานการเบิกจ่าย" /></Tabs>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}><Button variant="contained" onClick={() => download("docx")}>ดาวน์โหลด DOCX</Button><Button variant="outlined" onClick={() => download("pdf")}>ดาวน์โหลด PDF</Button><Button component="a" variant="outlined" startIcon={<OpenInNewIcon />} href={preview[tab].pdf} target="_blank" rel="noopener noreferrer" aria-label="ดูเต็มหน้า เปิด PDF ในแท็บใหม่">ดูเต็มหน้า</Button></Stack>
      <Box role="tabpanel" id={tab === "workReport" ? "work-report-panel" : "payment-evidence-panel"} aria-labelledby={tab === "workReport" ? "work-report-tab" : "payment-evidence-tab"}><MemoPdfPreview key={tab} url={preview[tab].pdf} documentLabel={tab === "workReport" ? "รายงานผลการปฏิบัติงาน" : "หลักฐานการเบิกจ่าย"} /></Box>
    </Stack></CardContent></Card>}
  </Stack>;
}
