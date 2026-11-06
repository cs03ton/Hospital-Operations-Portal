import { confirmSystem } from "../components/common/SystemDialogHost";
import { useEffect, useRef, useState } from "react";
import { Alert, Autocomplete, Box, Button, Card, CardContent, Checkbox, Divider, FormControlLabel, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import PersonOutlineIcon from "@mui/icons-material/PersonOutline";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import { isAxiosError } from "axios";
import { getMemoOptions, renderMemo, type MemoForm, type MemoOptions } from "../api/correspondenceApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { MemoBodyEditor } from "../components/correspondence/MemoBodyEditor";
import { MemoPdfPreview } from "../components/correspondence/MemoPdfPreview";
import { MemoContentTemplatePicker, type MemoTemplateFocusHandle } from "../components/correspondence/MemoContentTemplatePicker";
import { unresolvedMemoVariables } from "../utils/memoContentTemplates";
import { PageHeader } from "../components/PageHeader";
import { bangkokDayjs } from "../utils/dateFormat";
import { focusMemoElement, validateMemo, type MemoValidationIssue } from "../utils/memoValidation";
import { useMemoLeaveGuard } from "../hooks/useMemoLeaveGuard";
import { OvertimeWorkReportPage } from "./OvertimeWorkReportPage";
import { defaultMemoHead, memoApprovers } from "../utils/memoPeople";

function initial(options: MemoOptions): MemoForm {
  const departmentId = options.proposer.departmentId && options.departments.some(d => d.id === options.proposer.departmentId) ? options.proposer.departmentId : "";
  return { departmentId, extension: "", documentNumber: "", documentDate: bangkokDayjs().format("YYYY-MM-DD"), subject: "", recipient: "ผู้อำนวยการโรงพยาบาลนาหมื่น", body: [{ runs: [{ text: "" }], align: "justify", indent: 1, ordered: false }], closing: options.closings[0] ?? "จึงเรียนมาเพื่อโปรดทราบ",
    includeHead: true, headUserId: defaultMemoHead(options) || null, headComment: options.headComments[0] ?? "- เห็นสมควรอนุมัติ", includeDirector: true, directorUserId: options.directors.length === 1 ? options.directors[0].id : null, directorComment: "" };
}
function blobUrl(base64: string, type: string) {
  const bytes = atob(base64); const buffer = new Uint8Array(bytes.length); for (let i = 0; i < bytes.length; i++) buffer[i] = bytes.charCodeAt(i);
  return URL.createObjectURL(new Blob([buffer], { type }));
}
function FormSectionTitle({ step, title, description }: { step: string; title: string; description: string }) {
  return <Stack direction="row" spacing={1.5} alignItems="center">
    <Box aria-hidden="true" sx={{ width: 40, height: 40, flexShrink: 0, display: "grid", placeItems: "center", borderRadius: 2, bgcolor: "#edf5f1", color: "primary.main", fontWeight: 800 }}>{step}</Box>
    <Box><Typography variant="h6" fontWeight={800}>{title}</Typography><Typography variant="body2" color="text.secondary">{description}</Typography></Box>
  </Stack>;
}
type Preview = { pdf: string; docx: string; name: string };
export function CorrespondenceMemoPage() {
  const [options, setOptions] = useState<MemoOptions | null>(null); const [form, setForm] = useState<MemoForm | null>(null);
  const [unitNumber, setUnitNumber] = useState(""); const [outgoingNumber, setOutgoingNumber] = useState("");
  const [templateKey, setTemplateKey] = useState(0);
  const [reportMode, setReportMode] = useState(false), [selectedMemoCode, setSelectedMemoCode] = useState("");
  const [error, setError] = useState(""); const [loading, setLoading] = useState(true); const [rendering, setRendering] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const pageRoot = useRef<HTMLDivElement>(null), templateRef = useRef<MemoTemplateFocusHandle>(null);
  const [preview, setPreview] = useState<Preview | null>(null); const fileRef = useRef<Preview | null>(null);
  const requestRef = useRef<AbortController | null>(null); const version = useRef(0); const [editorKey, setEditorKey] = useState(0); const [dirty, setDirty] = useState(false);
  useMemoLeaveGuard(dirty && !reportMode);
  const invalidate = () => { version.current++; requestRef.current?.abort(); requestRef.current = null; setRendering(false); if (fileRef.current) { URL.revokeObjectURL(fileRef.current.pdf); URL.revokeObjectURL(fileRef.current.docx); } fileRef.current = null; setPreview(null); };
  useEffect(() => {
    const controller = new AbortController();
    getMemoOptions(controller.signal).then(data => { if (!controller.signal.aborted) { setOptions(data); setForm(initial(data)); setLoading(false); } }).catch(() => { if (!controller.signal.aborted) { setError("โหลดข้อมูลประกอบเอกสารไม่สำเร็จ กรุณารีเฟรชหน้า"); setLoading(false); } });
    return () => { controller.abort(); requestRef.current?.abort(); if (fileRef.current) { URL.revokeObjectURL(fileRef.current.pdf); URL.revokeObjectURL(fileRef.current.docx); } };
  }, []);
  function change<K extends keyof MemoForm>(field: K, value: MemoForm[K]) { invalidate(); setDirty(true); setError(""); setFieldErrors({}); setForm(current => current && { ...current, [field]: value }); }
  function focusIssue(issues: MemoValidationIssue[]) {
    const first = issues[0];
    if (first.variable) {
      let needsApply = false; let unknownVariable: string | undefined;
      for (const issue of issues.filter(issue => issue.variable)) {
        const result = templateRef.current?.focusVariable(issue.variable!);
        if (result === "input") return;
        if (result === "apply") needsApply = true;
        if (!result) unknownVariable ??= issue.variable;
      }
      if (needsApply && !unknownVariable) {
        setError("กรุณากดเติมข้อความจากเทมเพลตอีกครั้งเพื่อใช้ข้อมูลที่กรอกแล้ว ก่อนสร้าง Preview");
        focusMemoElement(pageRoot.current?.querySelector<HTMLElement>("[data-memo-apply-template]")); return;
      }
      const token = `{${unknownVariable ?? first.variable}}`;
      const target = [...(pageRoot.current?.querySelectorAll<HTMLElement>("[contenteditable=true], input, textarea") ?? [])].find(el => (el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement ? el.value : el.textContent)?.includes(token));
      focusMemoElement(target); return;
    }
    const field = pageRoot.current?.querySelector<HTMLElement>(`[data-memo-field="${first.field}"]`);
    if (first.field === "body") { focusMemoElement(field?.querySelector<HTMLElement>("[contenteditable=true]")); return; }
    const targets = [...(field?.querySelectorAll<HTMLElement>("input:not([type=hidden]), textarea, [contenteditable=true], [role=combobox]") ?? [])];
    focusMemoElement(first.field === "proposer" ? targets.find(el => el instanceof HTMLInputElement && !el.value.trim()) ?? targets[0] : targets[0] ?? field);
  }
  const makePreview = async () => {
    if (!form || !options) return;
    const issues = validateMemo(form, options, unitNumber, outgoingNumber);
    if (issues.length) {
      setFieldErrors(Object.fromEntries(issues.map(issue => [issue.field, issue.message]))); setError(issues[0].message); focusIssue(issues); return;
    }
    setFieldErrors({});
    invalidate(); const revision = version.current; const controller = new AbortController(); requestRef.current = controller; setError(""); setRendering(true);
    let pdf: string | null = null;
    try {
      const files = await renderMemo(form, controller.signal); if (controller.signal.aborted || version.current !== revision) return;
      pdf = blobUrl(files.pdfBase64, "application/pdf"); const docx = blobUrl(files.docxBase64, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
      const next = { pdf, docx, name: files.fileName }; fileRef.current = next; setPreview(next);
    } catch (e) { if (pdf) URL.revokeObjectURL(pdf); if (!controller.signal.aborted) setError(isAxiosError(e) ? e.response?.data?.message ?? "สร้างไฟล์ไม่สำเร็จ กรุณาลองใหม่" : "สร้างไฟล์ไม่สำเร็จ กรุณาลองใหม่"); }
    finally { if (version.current === revision && !controller.signal.aborted) setRendering(false); }
  };
  const clear = async () => { if (dirty && !(await confirmSystem("ล้างข้อมูลทั้งหมดที่กรอกหรือไม่?"))) return; invalidate(); setForm(initial(options!)); setUnitNumber(""); setOutgoingNumber(""); setSelectedMemoCode(""); setEditorKey(n => n + 1); setTemplateKey(n => n + 1); setDirty(false); setError(""); setFieldErrors({}); };
  const changeNumber = (unit: string, outgoing: string) => { setUnitNumber(unit); setOutgoingNumber(outgoing); change("documentNumber", unit && outgoing ? `นน 0033.${unit}/${outgoing}` : ""); };
  const download = (type: "docx" | "pdf") => { if (!preview) return; const a = document.createElement("a"); a.href = preview[type]; a.download = `${preview.name}.${type}`; a.click(); setDirty(false); };
  if (loading) return <Typography role="status">กำลังโหลดข้อมูลสำหรับบันทึกข้อความ...</Typography>;
  if (!options || !form) return <Alert severity="error">{error}</Alert>;
  if (reportMode) return <OvertimeWorkReportPage memoOptions={options} initialDepartmentId={form.departmentId} initialSupervisorUserId={form.headUserId} onSelectTemplate={code => { setSelectedMemoCode(code); setReportMode(false); }} />;
  const approvers = memoApprovers(options);
  const missingProfile = !options.proposer.name.trim() || !options.proposer.position?.trim();
  const length = form.body.reduce((sum, p) => sum + p.runs.reduce((n, r) => n + r.text.length, 0), 0);
  const unresolved = unresolvedMemoVariables(form.subject, form.body, form.closing);
  return <Stack ref={pageRoot} spacing={3} sx={{ width: "100%", maxWidth: 1200, mx: "auto", "& [data-memo-field], & [data-template-field]": { scrollMarginTop: "100px" }, "& .MuiCard-root": { borderRadius: 3, boxShadow: "0 6px 24px rgba(28, 67, 54, 0.04)" }, "& .MuiCardContent-root": { p: { xs: 2, sm: 3.5 }, "&:last-child": { pb: { xs: 2, sm: 3.5 } } }, "& .MuiInputBase-root": { bgcolor: "white" } }}>
    <PageHeader title="สร้างบันทึกข้อความ" subtitle="กรอกข้อมูล ตรวจตัวอย่าง และดาวน์โหลด Word / PDF" />
    <Alert severity="info">เอกสารไม่ถูกเก็บบนเซิร์ฟเวอร์ ปิดหรือรีเฟรชหน้าแล้วข้อมูลจะถูกล้าง กรุณาดาวน์โหลดไฟล์ไว้ในเครื่อง</Alert>
    {missingProfile && <Alert severity="warning">ชื่อหรือตำแหน่งใน Profile ไม่ครบ กรุณาติดต่อผู้ดูแลระบบก่อนสร้างไฟล์</Alert>}
    <Card sx={{ borderTop: "3px solid", borderTopColor: "primary.main" }}><CardContent><Stack spacing={3}>
      <FormSectionTitle step="01" title="ข้อมูลบันทึกข้อความ" description="ระบุหน่วยงาน เลขหนังสือ และผู้รับ" />
      <Box>
        <Typography variant="subtitle2" fontWeight={800} sx={{ mb: 1.5 }}>หน่วยงานและการติดต่อ</Typography>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(2, minmax(0, 1fr))", md: "minmax(0, 2fr) repeat(2, minmax(0, 1fr))" }, gap: 2 }}>
          <TextField data-memo-field="departmentId" error={!!fieldErrors.departmentId} helperText={fieldErrors.departmentId} select size="small" required fullWidth label="หน่วยงาน" InputLabelProps={{ shrink: true }} sx={{ gridColumn: { sm: "1 / -1", md: "auto" } }} value={form.departmentId} onChange={e => change("departmentId", e.target.value)}>{options.departments.map(d => <MenuItem key={d.id} value={d.id}>{d.name}</MenuItem>)}</TextField>
          <TextField size="small" label="โทรศัพท์" fullWidth value="054-719361" InputProps={{ readOnly: true }} helperText="เบอร์โรงพยาบาล" />
          <TextField data-memo-field="extension" error={!!fieldErrors.extension} helperText={fieldErrors.extension} size="small" label="เบอร์ต่อ" required fullWidth value={form.extension} InputLabelProps={{ shrink: true }} placeholder="เช่น 103" onChange={e => change("extension", e.target.value.replace(/\D/g, ""))} inputProps={{ maxLength: 12, inputMode: "numeric" }} />
        </Box>
      </Box>
      <Box sx={{ p: { xs: 2, sm: 2.5 }, borderRadius: 2.5, bgcolor: "#faf8f2", border: "1px solid #e9e3d5" }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}><DescriptionOutlinedIcon fontSize="small" sx={{ color: "primary.main" }} aria-hidden="true" /><Typography variant="subtitle2" fontWeight={800}>เลขหนังสือและวันที่</Typography></Stack>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(2, minmax(0, 1fr))", md: "repeat(3, minmax(0, 1fr))" }, gap: 2, alignItems: "start" }}>
          <TextField data-memo-field="unitNumber" error={!!fieldErrors.unitNumber} helperText={fieldErrors.unitNumber} size="small" required fullWidth label="เลขที่หน่วยงาน" placeholder="312" InputLabelProps={{ shrink: true }} value={unitNumber} onChange={e => changeNumber(e.target.value.replace(/\D/g, ""), outgoingNumber)} inputProps={{ maxLength: 10, inputMode: "numeric" }} />
          <TextField data-memo-field="outgoingNumber" error={!!fieldErrors.outgoingNumber} helperText={fieldErrors.outgoingNumber} size="small" required fullWidth label="เลขทะเบียนส่ง" placeholder="29" InputLabelProps={{ shrink: true }} value={outgoingNumber} onChange={e => changeNumber(unitNumber, e.target.value.replace(/[^\d.]/g, ""))} inputProps={{ maxLength: 20, inputMode: "decimal" }} />
          <Box data-memo-field="documentDate" sx={{ gridColumn: { sm: "1 / -1", md: "auto" } }}><AppDatePicker required label="วันที่ออกหนังสือ" value={form.documentDate} onChange={v => change("documentDate", v)} error={!!fieldErrors.documentDate} helperText={fieldErrors.documentDate} /></Box>
        </Box>
        <Box sx={{ mt: 2, p: 1.5, borderRadius: 2, bgcolor: "#edf5f1", border: "1px solid #d5e5dc" }}>
          <TextField size="small" fullWidth label="เลขที่หนังสือ" value={form.documentNumber} placeholder="นน 0033.312/29" InputLabelProps={{ shrink: true }} InputProps={{ readOnly: true }} sx={{ "& .MuiInputBase-root": { bgcolor: "transparent", fontWeight: 800, color: "primary.main" }, "& fieldset": { border: 0 } }} />
          <Typography variant="caption" color="text.secondary" sx={{ px: 1.5, display: "block" }}>นน 0033. + เลขที่หน่วยงาน / เลขทะเบียนส่ง</Typography>
        </Box>
      </Box>
      <Box sx={{ display: "grid", gap: 2 }}>
        <TextField data-memo-field="subject" error={!!fieldErrors.subject} helperText={fieldErrors.subject} size="small" required fullWidth label="เรื่อง" placeholder="ระบุหัวข้อบันทึกข้อความ" InputLabelProps={{ shrink: true }} value={form.subject} onChange={e => change("subject", e.target.value)} inputProps={{ maxLength: 300 }} />
        <TextField data-memo-field="recipient" error={!!fieldErrors.recipient} helperText={fieldErrors.recipient} size="small" required fullWidth label="เรียน" InputLabelProps={{ shrink: true }} value={form.recipient} onChange={e => change("recipient", e.target.value)} inputProps={{ maxLength: 300 }} />
      </Box>
      <Box data-memo-field="proposer" sx={{ p: 2, borderRadius: 2.5, bgcolor: "#f5f7f6", border: "1px solid #e6ebe8" }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}><PersonOutlineIcon fontSize="small" sx={{ color: "primary.main" }} aria-hidden="true" /><Typography variant="subtitle2" fontWeight={800}>ผู้เสนอเอกสาร</Typography><Typography variant="caption" color="text.secondary">จากบัญชี HOP</Typography></Stack>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(2, minmax(0, 1fr))" }, gap: 2 }}>
          <TextField size="small" fullWidth label="ผู้เสนอจาก HOP" value={options.proposer.name} InputProps={{ readOnly: true }} />
          <TextField size="small" fullWidth label="ตำแหน่งผู้เสนอ" value={options.proposer.position ?? ""} InputProps={{ readOnly: true }} />
        </Box>
      </Box>
    </Stack></CardContent></Card>
    <Card><CardContent><Stack spacing={2}><FormSectionTitle step="02" title="เนื้อหา" description="เขียนรายละเอียดและเลือกข้อความลงท้าย" />
      <MemoContentTemplatePicker ref={templateRef} key={`template-${templateKey}`} initialCode={selectedMemoCode} onSelectReport={() => { invalidate(); setReportMode(true); }} options={options} departmentId={form.departmentId} headUserId={form.headUserId} onDraftChange={() => { setDirty(true); setError(""); }} onApply={async content => {
        if ((form.subject.trim() || form.body.some(p => p.runs.some(r => r.text.trim()))) && !(await confirmSystem("เติมเทมเพลตแทนเรื่อง เนื้อหา และข้อความลงท้ายปัจจุบันหรือไม่? ข้อความที่แก้ไขไว้จะถูกแทนที่"))) return;
        invalidate(); setDirty(true); setError(""); setFieldErrors({}); setForm(current => current && { ...current, ...content }); setEditorKey(n => n + 1);
      }} />
      <Box data-memo-field="body"><MemoBodyEditor key={`body-${editorKey}`} value={form.body} onChange={v => change("body", v)} />{fieldErrors.body && <Typography color="error" variant="caption">{fieldErrors.body}</Typography>}</Box>
      {unresolved.length > 0 && <Alert severity="warning" sx={{ overflowWrap: "anywhere" }}>ยังไม่ได้กรอกตัวแปร: {unresolved.join(", ")} กรุณาแก้ในเรื่อง เนื้อหา หรือข้อความลงท้ายก่อนสร้างไฟล์</Alert>}
      {length > 30000 && <Alert severity="error">เนื้อหายาวเกิน 30,000 ตัวอักษร</Alert>}
      <Autocomplete data-memo-field="closing" freeSolo options={options.closings} inputValue={form.closing} onInputChange={(_, value) => change("closing", value)} renderInput={params => <TextField {...params} error={!!fieldErrors.closing} helperText={fieldErrors.closing} required label="ข้อความลงท้าย" inputProps={{ ...params.inputProps, maxLength: 500 }} />} />
    </Stack></CardContent></Card>
    <Card><CardContent><Stack spacing={2}><FormSectionTitle step="03" title="เตรียมส่วนลงนาม" description="เลือกผู้ลงนามและเพิ่มความเห็นตามต้องการ" />
      <Alert severity="warning">ชื่อและความเห็นเป็นการเตรียมช่องลงนามเท่านั้น ไม่ถือว่าบุคคลนั้นได้อนุมัติเอกสาร</Alert>
      <FormControlLabel control={<Checkbox checked={form.includeHead} onChange={e => change("includeHead", e.target.checked)} />} label="แสดงส่วนหัวหน้ากลุ่มงาน" />
      {form.includeHead && <>
        {approvers.length === 0 && <Alert severity="info">ไม่พบผู้อนุมัติที่เปิดใช้งาน กรุณาติดต่อผู้ดูแล หรือปิดส่วนนี้หากเอกสารไม่ต้องใช้</Alert>}
        <TextField data-memo-field="headUserId" error={!!fieldErrors.headUserId} helperText={fieldErrors.headUserId ?? "ค่าเริ่มต้นเป็นหัวหน้าหน่วยงานของคุณ สามารถเลือกผู้อนุมัติจากทุกหน่วยงานได้"} select fullWidth label="หัวหน้ากลุ่มงาน" value={form.headUserId ?? ""} onChange={e => change("headUserId", e.target.value || null)}><MenuItem value="">เลือกผู้ลงนาม</MenuItem>{approvers.map(h => <MenuItem key={h.id} value={h.id} sx={{ whiteSpace: "normal" }}>{h.name} · {h.position ?? "ยังไม่มีตำแหน่ง"}</MenuItem>)}</TextField>
        <Autocomplete freeSolo options={options.headComments} inputValue={form.headComment} onInputChange={(_, v) => change("headComment", v)} renderInput={params => <TextField {...params} label="ความเห็นหัวหน้ากลุ่มงาน (เว้นว่างได้)" inputProps={{ ...params.inputProps, maxLength: 1000 }} />} />
      </>}
      <Divider />
      <FormControlLabel control={<Checkbox checked={form.includeDirector} onChange={e => change("includeDirector", e.target.checked)} />} label="แสดงส่วนผู้อำนวยการ" />
      {form.includeDirector && <>
        {options.directors.length === 0 && <Alert severity="info">ไม่พบผู้ดำรงบทบาท Director ที่เปิดใช้งาน กรุณาติดต่อผู้ดูแล หรือปิดส่วนนี้หากเอกสารไม่ต้องใช้</Alert>}
        {options.directors.length > 1 && <Alert severity="warning">พบผู้ดำรงบทบาท Director มากกว่าหนึ่งบัญชี กรุณาให้ผู้ดูแลตรวจสอบผู้ดำรงตำแหน่งปัจจุบัน</Alert>}
        <TextField data-memo-field="director" error={!!fieldErrors.director} fullWidth label="ผู้อำนวยการ / รักษาการ" value={options.directors.length === 1 ? options.directors[0].name : ""} InputLabelProps={{ shrink: true }} InputProps={{ readOnly: true }} helperText={fieldErrors.director ?? "ดึงผู้ดำรงบทบาท Director อัตโนมัติ และตรวจสอบอีกครั้งเมื่อสร้างไฟล์"} />
        <Typography variant="body2" color="text.secondary">นายแพทย์ปฏิบัติการ รักษาการในตำแหน่ง ผู้อำนวยการโรงพยาบาลนาหมื่น</Typography>
        <Autocomplete freeSolo options={options.directorComments} inputValue={form.directorComment} onInputChange={(_, v) => change("directorComment", v)} renderInput={params => <TextField {...params} label="ความเห็นผู้อำนวยการ (เว้นว่างได้)" inputProps={{ ...params.inputProps, maxLength: 1000 }} />} />
      </>}
    </Stack></CardContent></Card>
    {error && <Alert severity="error">{error}</Alert>}
    <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} sx={{ p: 2, border: "1px solid", borderColor: "divider", borderRadius: 3, bgcolor: "white", justifyContent: "flex-end" }}><Button startIcon={<VisibilityOutlinedIcon />} variant="contained" disabled={rendering} onClick={makePreview}>{rendering ? "กำลังสร้าง DOCX และ PDF..." : "สร้าง Preview"}</Button><Button startIcon={<RestartAltIcon />} variant="outlined" onClick={clear}>ล้างฟอร์ม</Button></Stack>
    {preview && <Card><CardContent><Stack spacing={2}><Typography variant="h6" fontWeight={800}>ตัวอย่างก่อนดาวน์โหลด</Typography>
      <Typography variant="body2" color="text.secondary">Word และ PDF ใช้ข้อมูลชุดเดียวกัน ตำแหน่งตัดหน้าอาจต่างกันเล็กน้อย กรุณาตรวจไฟล์ก่อนนำไปใช้งาน</Typography>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}><Button variant="contained" onClick={() => download("docx")}>ดาวน์โหลด DOCX</Button><Button variant="outlined" onClick={() => download("pdf")}>ดาวน์โหลด PDF</Button><Button component="a" href={preview.pdf} target="_blank" rel="noopener noreferrer">เปิด PDF เต็มจอ</Button></Stack>
      <MemoPdfPreview url={preview.pdf} />
    </Stack></CardContent></Card>}
  </Stack>;
}

