import { isMemoOutgoingNumber } from "../../utils/memoOutgoingNumber";
import { MemoWorkPeriodsEditor, newWorkPeriodRow } from "./MemoWorkPeriodsEditor";
import { formatMemoWorkPeriods } from "../../utils/memoWorkPeriods";
import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from "react";
import { Alert, Autocomplete, Box, Button, Checkbox, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import { getMemoDepartmentPeople, type MemoContentTemplate, type MemoOptions, type MemoParagraph, type MemoPerson } from "../../api/correspondenceApi";
import { bangkokDayjs } from "../../utils/dateFormat";
import { AppDatePicker } from "../common/AppDatePicker";
import { fillMemoTemplate, formatMemoTemplateDates, memoMonthPeriod, memoTemplateDateKeys, moneyValues, thaiMonths } from "../../utils/memoContentTemplates";
import { focusMemoElement } from "../../utils/memoValidation";
import { MemoTemplateSelect, overtimeReportTemplateCode } from "./MemoTemplateSelect";
import { memoApprovers } from "../../utils/memoPeople";
const labels: Record<string, string> = { month: "เดือน", years: "ปี พ.ศ.", department: "กลุ่มงาน", full_name: "ชื่อผู้เสนอ", position: "ตำแหน่ง", amount: "จำนวนเงิน (บาท)", amount_text: "จำนวนเงินตัวอักษร", work_dates: "วันที่ปฏิบัติงาน", work_times: "ช่วงเวลาปฏิบัติงาน", work_detail: "รายละเอียดงาน", reason: "เหตุผลความจำเป็น", item_count: "จำนวนรายการวัสดุ", reference_document_number: "เลขที่บันทึกข้อความอ้างอิง", reference_document_date: "ลงวันที่บันทึกข้อความอ้างอิง", period_start: "วันเริ่มปฏิบัติงาน", period_end: "วันสิ้นสุดปฏิบัติงาน", staff_list: "รายชื่อผู้ปฏิบัติงานและตำแหน่ง", supervisor_name: "ผู้ควบคุมการปฏิบัติงาน" };
export type MemoTemplateFocusHandle = { focusVariable: (variable: string) => "input" | "apply" | false };
type PickerProps = { options: MemoOptions; departmentId: string; headUserId?: string | null; initialCode?: string; onSelectReport?: () => void; onApply: (content: { subject: string; body: MemoParagraph[]; closing: string; contentTemplateCode: string; workMonth: string | null }) => void; onDraftChange: () => void };
type DepartmentRoster = { departmentId: string; people: MemoPerson[]; selectedIds: string[]; loading: boolean; error: string };
export const MemoContentTemplatePicker = forwardRef<MemoTemplateFocusHandle, PickerProps>(function MemoContentTemplatePicker({ options, departmentId, headUserId, initialCode = "", onSelectReport, onApply, onDraftChange }, ref) {
  const root = useRef<HTMLDivElement>(null);
  const now = bangkokDayjs(); const [code, setCode] = useState(initialCode);
  const [fields, setFields] = useState<Record<string, string>>(() => ({ month: thaiMonths[now.month()], years: String(now.year() + 543), ...memoMonthPeriod(thaiMonths[now.month()], String(now.year() + 543)), staff_list: `${options.proposer.name} ตำแหน่ง ${options.proposer.position ?? "{position}"}` }));
  const template = options.contentTemplates?.find(t => t.code === code && t.isActive);
  const usesDepartmentRoster = template?.code === "MEMO_OT_REQUEST" && template.variables.includes("staff_list");
  const departmentName = options.departments.find(d => d.id === departmentId)?.name ?? "";
  const departmentHeads = options.heads.filter(person => person.departmentId === departmentId);
  const selectedHeadId = headUserId === undefined && departmentHeads.length === 1 ? departmentHeads[0].id : headUserId;
  const supervisorHead = memoApprovers(options).find(person => person.id === selectedHeadId);
  const [supervisorOverride, setSupervisorOverride] = useState<{ departmentId: string; value: string } | null>(null);
  const supervisorName = supervisorOverride?.departmentId === departmentId ? supervisorOverride.value : supervisorHead?.name ?? "";
  const [roster, setRoster] = useState<DepartmentRoster | null>(null), [rosterReload, setRosterReload] = useState(0);
  useEffect(() => {
    if (!usesDepartmentRoster || !departmentId) return;
    const controller = new AbortController();
    setRoster({ departmentId, people: [], selectedIds: [], loading: true, error: "" });
    getMemoDepartmentPeople(departmentId, controller.signal).then(people => {
      if (controller.signal.aborted) return;
      const members = people.filter(person => person.departmentId === departmentId);
      setRoster({ departmentId, people, selectedIds: members.map(person => person.id), loading: false, error: "" });
    }).catch(() => { if (!controller.signal.aborted) setRoster({ departmentId, people: [], selectedIds: [], loading: false, error: "โหลดรายชื่อบุคลากรไม่สำเร็จ กรุณาลองใหม่" }); });
    return () => controller.abort();
  }, [departmentId, usesDepartmentRoster, rosterReload]);
  const currentRoster = roster?.departmentId === departmentId ? roster : null;
  const selectedPeople = currentRoster?.people.filter(person => currentRoster.selectedIds.includes(person.id)) ?? [];
  const rosterText = selectedPeople.map(person => `${person.name} ตำแหน่ง ${person.position?.trim() || "{position}"}`).join("\n");
  const rosterMissingProfile = selectedPeople.some(person => !person.name.trim() || !person.position?.trim());
  const rosterInvalid = !currentRoster || currentRoster.loading || !!currentRoster.error || !selectedPeople.length || selectedPeople.length > 50 || rosterMissingProfile;
  const usesMonthlyRange = ["month", "years", "period_start", "period_end"].every(key => template?.variables.includes(key));
  const usesReference = template?.variables.includes("reference_document_number");
  const referenceNumber = fields.reference_unit_number && isMemoOutgoingNumber(fields.reference_outgoing_number ?? "") ? `นน 0033.${fields.reference_unit_number}/${fields.reference_outgoing_number}` : "";
  const [periods, setPeriods] = useState([newWorkPeriodRow(0)]);
  const usesPeriods = template?.variables.some(key => ["work_dates", "work_times", "work_periods"].includes(key));
  const formattedPeriods = formatMemoWorkPeriods(periods.map(({ startDate, startTime, endDate, endTime }) => ({ startDate, startTime, endDate, endTime })));
  const money = moneyValues(fields.amount ?? "");
  const dates = formatMemoTemplateDates(fields, template?.variables ?? []);
  const values = { ...fields, name_prefix: options.proposer.namePrefix?.trim() ?? "", ...formattedPeriods.values, ...dates.values, supervisor_name: supervisorName, staff_list: usesDepartmentRoster ? rosterText : fields.staff_list, reference_document_number: referenceNumber, full_name: template?.variables.includes("name_prefix") ? (options.proposer.fullname ?? options.proposer.name) : options.proposer.name, position: options.proposer.position ?? "", department: departmentName, amount: money?.amount ?? "", amount_text: money?.amount_text ?? "" };
  const invalidAmount = !!fields.amount && !money;
  const invalidYear = !!fields.years && (!/^[23]\d{3}$/.test(fields.years) || usesMonthlyRange && !memoMonthPeriod(fields.month, fields.years));
  const invalidCount = !!fields.item_count && !/^[1-9]\d{0,5}$/.test(fields.item_count);
  const invalidStaff = usesDepartmentRoster ? rosterInvalid : template?.variables.includes("staff_list") && (fields.staff_list ?? "").split(/\r?\n/).filter(line => line.trim()).length > 50;
  useImperativeHandle(ref, () => ({ focusVariable(variable) {
    if (!template?.variables.includes(variable)) return false;
    const key = variable === "amount_text" ? "amount" : variable;
    const invalid = key === "amount" ? invalidAmount : key === "years" ? invalidYear : key === "item_count" ? invalidCount : key === "staff_list" ? invalidStaff : !!dates.errors[key];
    if (values[variable as keyof typeof values]?.trim() && !invalid) {
      return "apply";
    }
    const section = root.current?.querySelector<HTMLElement>(`[data-template-field="${key}"]`);
    if (!section) return false;
    const inputs = [...section.querySelectorAll<HTMLInputElement>("input:not([type=hidden]):not([readonly]), textarea, [contenteditable=true], [role=combobox]")];
    const target = inputs.find(input => !input.value?.trim()) ?? inputs[0];
    return focusMemoElement(target) ? "input" : false;
  } }));
  if (!options.contentTemplates?.some(t => t.isActive) && !onSelectReport) return null;
  return <Box ref={root} sx={{ p: { xs: 2, sm: 2.5 }, borderRadius: 2.5, bgcolor: "#faf8f2", border: "1px solid #e9e3d5" }}><Stack spacing={2}>
    <Box><Typography fontWeight={800}>เทมเพลตเนื้อหาบันทึกข้อความ</Typography><Typography variant="body2" color="text.secondary">เลือกข้อความตั้งต้น เติมข้อมูล แล้วแก้ไขเนื้อหาได้ตามต้องการ</Typography></Box>
    <MemoTemplateSelect templates={options.contentTemplates} value={code} includeReport={!!onSelectReport} onChange={next => { if (next === overtimeReportTemplateCode && onSelectReport) { onSelectReport(); return; } setCode(next); onDraftChange(); }} />
    {template && <>
      <Alert severity="info">ข้อความร่างตัวอย่าง โปรดตรวจถ้อยคำและเอกสารประกอบกับหน่วยงานที่เกี่ยวข้องก่อนนำไปใช้ ระบบไม่แนบเอกสารให้อัตโนมัติ</Alert>
      <Typography variant="caption" color="text.secondary">ชื่อ ตำแหน่ง และหน่วยงาน ใช้ข้อมูลจากส่วนข้อมูลบันทึกข้อความด้านบน</Typography>
      {usesDepartmentRoster && <Box data-template-field="staff_list"><Stack spacing={1.5}>
        <Typography variant="body2">ชื่อกลุ่มงานในเนื้อหา: <strong>{departmentName || "กรุณาเลือกหน่วยงานด้านบน"}</strong></Typography>
        {currentRoster?.error && <Alert severity="error" action={<Button onClick={() => setRosterReload(n => n + 1)}>ลองใหม่</Button>}>{currentRoster.error}</Alert>}
        {!departmentId && <Alert severity="info">กรุณาเลือกหน่วยงานด้านบนเพื่อดึงรายชื่อผู้ปฏิบัติงาน</Alert>}
        {currentRoster && !currentRoster.loading && !currentRoster.error && !currentRoster.people.length && <Alert severity="info">ไม่พบบุคลากรที่เปิดใช้งาน</Alert>}
        <Autocomplete multiple disableCloseOnSelect fullWidth options={currentRoster?.people ?? []} value={selectedPeople} loading={!!departmentId && (!currentRoster || currentRoster.loading)}
          loadingText="กำลังโหลดรายชื่อ..." noOptionsText="ไม่พบบุคลากร" isOptionEqualToValue={(a, b) => a.id === b.id} getOptionLabel={person => `${person.name} · ${person.position?.trim() || "ยังไม่มีตำแหน่ง"}`}
          onChange={(_, people) => { setRoster(current => current && current.departmentId === departmentId ? { ...current, selectedIds: people.map(person => person.id) } : current); onDraftChange(); }}
          renderOption={(props, person, { selected }) => <li {...props}><Checkbox checked={selected} tabIndex={-1} sx={{ mr: 1 }} /><Box sx={{ minWidth: 0, overflowWrap: "anywhere" }}><Typography variant="body2">{person.name}</Typography><Typography variant="caption" color="text.secondary">{person.position?.trim() || "ยังไม่มีตำแหน่ง"}{options.departments.find(d => d.id === person.departmentId)?.name ? ` · ${options.departments.find(d => d.id === person.departmentId)!.name}` : ""}</Typography></Box></li>}
          sx={{ "& .MuiChip-root": { maxWidth: "100%", height: "auto", my: .25 }, "& .MuiChip-label": { whiteSpace: "normal", overflowWrap: "anywhere", py: .5 } }}
          renderInput={params => <TextField {...params} label="ผู้ปฏิบัติงานในกลุ่มงาน" error={!!currentRoster && !currentRoster.loading && !currentRoster.error && rosterInvalid}
            helperText={rosterMissingProfile ? "ผู้ที่เลือกบางคนยังไม่มีชื่อหรือตำแหน่ง กรุณาแก้ Profile หรือเอารายนั้นออกก่อนเติมข้อความ" : selectedPeople.length > 50 ? "เลือกได้ไม่เกิน 50 คน" : currentRoster && !currentRoster.loading && !currentRoster.error && !selectedPeople.length ? "กรุณาเลือกผู้ปฏิบัติงานอย่างน้อย 1 คน" : "เริ่มต้นด้วยคนในหน่วยงานที่เลือก เพิ่มบุคลากรข้ามหน่วยงานได้ ยกเว้น Admin และ SuperAdmin"} />} />
      </Stack></Box>}
      {usesReference && <Box data-template-field="reference_document_number" component="section" aria-label="หนังสืออ้างอิงในเนื้อหา" sx={{ p: { xs: 2, sm: 2.5 }, border: "1px solid #e9e3d5", borderRadius: 2.5, bgcolor: "#faf8f2", minWidth: 0 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}><DescriptionOutlinedIcon fontSize="small" sx={{ color: "primary.main" }} aria-hidden="true" /><Typography variant="subtitle2" fontWeight={800}>เลขหนังสือและวันที่อ้างอิง</Typography></Stack>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "repeat(3, minmax(0, 1fr))" }, gap: 2 }}>
          <TextField size="small" required fullWidth label="เลขที่หน่วยงานอ้างอิง" placeholder="307" InputLabelProps={{ shrink: true }} value={fields.reference_unit_number ?? ""} onChange={e => { setFields(current => ({ ...current, reference_unit_number: e.target.value.replace(/\D/g, "") })); onDraftChange(); }} inputProps={{ maxLength: 10, inputMode: "numeric" }} />
          <TextField size="small" required fullWidth label="เลขทะเบียนส่งอ้างอิง" placeholder="92" InputLabelProps={{ shrink: true }} value={fields.reference_outgoing_number ?? ""} onChange={e => { setFields(current => ({ ...current, reference_outgoing_number: e.target.value.replace(/[^\d.]/g, "") })); onDraftChange(); }} inputProps={{ maxLength: 20, inputMode: "decimal" }} />
          {template.variables.includes("reference_document_date") && <Box data-template-field="reference_document_date"><AppDatePicker required label={labels.reference_document_date} value={fields.reference_document_date ?? ""} onChange={value => { setFields(current => ({ ...current, reference_document_date: value })); onDraftChange(); }} error={!!dates.errors.reference_document_date} helperText={dates.errors.reference_document_date} /></Box>}
        </Box>
        <Box sx={{ mt: 2, p: 1.5, borderRadius: 2, bgcolor: "#ecf5f1", border: "1px solid #cfe3da" }}>
          <TextField size="small" fullWidth label="เลขที่หนังสืออ้างอิง" value={referenceNumber} placeholder="นน 0033.307/92" InputLabelProps={{ shrink: true }} InputProps={{ readOnly: true }} sx={{ "& .MuiInputBase-root": { bgcolor: "transparent", fontWeight: 800, color: "primary.main" }, "& fieldset": { border: 0 } }} />
          <Typography variant="caption" color="text.secondary" sx={{ px: 1.5, display: "block" }}>นน 0033. + เลขที่หน่วยงาน / เลขทะเบียนส่ง</Typography>
        </Box>
      </Box>}
      {usesPeriods && <Box data-template-field="work_periods"><Box data-template-field="work_dates"><Box data-template-field="work_times"><MemoWorkPeriodsEditor rows={periods} errors={formattedPeriods.errors} onChange={rows => { setPeriods(rows); onDraftChange(); }} /></Box></Box></Box>}
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(2, minmax(0, 1fr))" }, gap: 2 }}>
        {template.variables.filter(key => !["name_prefix", "amount_text", "full_name", "position", "department", "work_dates", "work_times", "work_periods", ...(usesDepartmentRoster ? ["staff_list"] : []), ...(usesReference ? ["reference_document_number", "reference_document_date"] : [])].includes(key)).map(key => {
          if (memoTemplateDateKeys.includes(key)) return <Box key={key} data-template-field={key}><AppDatePicker label={labels[key]} value={fields[key] ?? ""} onChange={value => { setFields(current => ({ ...current, [key]: value })); onDraftChange(); }} error={!!dates.errors[key]} helperText={dates.errors[key] ?? (usesMonthlyRange && key === "period_end" ? "เปลี่ยนเดือนหรือปีจะปรับช่วงวันใหม่เป็นวันที่ 1 ถึงสิ้นเดือน โดยยังแก้วันที่เองได้" : undefined)} /></Box>;
          const readonly = ["full_name", "position", "department"].includes(key);
          return <TextField data-template-field={key} key={key} fullWidth size="small" label={labels[key] ?? key} value={key === "supervisor_name" ? supervisorName : readonly ? values[key as keyof typeof values] ?? "" : fields[key] ?? ""}
            select={key === "month"} multiline={["work_detail", "reason", "work_dates", "work_times", "staff_list"].includes(key)} minRows={key === "staff_list" ? 3 : undefined} sx={key === "staff_list" ? { gridColumn: "1 / -1" } : undefined} InputLabelProps={{ shrink: true }} InputProps={{ readOnly: readonly }}
            error={key === "amount" && invalidAmount || key === "years" && invalidYear || key === "item_count" && invalidCount || key === "staff_list" && invalidStaff}
            helperText={key === "staff_list" ? invalidStaff ? "ระบุได้ไม่เกิน 50 คน" : "ใช้ชื่อและตำแหน่งผู้เสนอเป็นรายการแรก เพิ่มหรือแก้ไขรายชื่อได้คนละบรรทัด ระบบใส่เลขลำดับให้อัตโนมัติ (ไม่เกิน 50 คน)" : key === "supervisor_name" ? supervisorHead ? "เติมชื่อจากหัวหน้ากลุ่มงานที่เลือก สามารถแก้ไขได้" : "เลือกหัวหน้าในส่วนลงนาม หรือกรอกชื่อผู้ควบคุมการปฏิบัติงานเอง" : key === "amount" ? invalidAmount ? "กรอกตัวเลขไม่เกิน 12 หลัก และทศนิยมไม่เกิน 2 ตำแหน่ง" : money?.amount_text ?? "เช่น 5000 หรือ 5000.50" : key === "years" && invalidYear ? usesMonthlyRange ? "กรอกปี พ.ศ. ระหว่าง 2443–2643" : "กรอกปี พ.ศ. ให้ครบ 4 หลัก" : key === "reference_document_number" ? "เช่น นน 0033.307/92" : readonly ? "ข้อมูลจาก HOP" : undefined}
            inputProps={{ maxLength: key === "years" ? 4 : key === "item_count" ? 6 : key === "staff_list" ? 10000 : ["work_detail", "reason"].includes(key) ? 3000 : 300, inputMode: ["amount", "years", "item_count"].includes(key) ? "decimal" : "text" }}
            onChange={e => { const value = ["years", "item_count"].includes(key) ? e.target.value.replace(/\D/g, "") : e.target.value; if (key === "supervisor_name") { setSupervisorOverride({ departmentId, value }); onDraftChange(); return; } setFields(current => {
              const next = { ...current, [key]: value };
              return usesMonthlyRange && ["month", "years"].includes(key) ? { ...next, ...(memoMonthPeriod(next.month, next.years) ?? { period_start: "", period_end: "" }) } : next;
            }); onDraftChange(); }}>
            {key === "month" ? thaiMonths.map(month => <MenuItem key={month} value={month}>{month}</MenuItem>) : null}
          </TextField>;
        })}
      </Box>
      <Typography variant="caption" color="text.secondary">ช่องที่ยังไม่กรอกจะคงตัวแปร เช่น {"{work_dates}"} ไว้ให้แก้ก่อนสร้างไฟล์ การเปลี่ยนค่าในส่วนนี้จะยังไม่เขียนทับเนื้อหาจนกดปุ่มเติมข้อความ</Typography>
      <Button data-memo-apply-template sx={{ alignSelf: "flex-start" }} variant="outlined" disabled={!!invalidStaff || Object.keys(dates.errors).length > 0 || !!usesPeriods && Object.keys(formattedPeriods.errors).length > 0 || template.variables.includes("amount") && invalidAmount || template.variables.includes("years") && invalidYear || template.variables.includes("item_count") && invalidCount} onClick={() => {
        const period = template.variables.includes("month") && template.variables.includes("years") ? memoMonthPeriod(fields.month, fields.years) : null;
        onApply({ ...fillMemoTemplate(template as MemoContentTemplate, values), contentTemplateCode: template.code, workMonth: period?.period_start.slice(0, 7) ?? null });
      }}>เติมข้อความจากเทมเพลต</Button>
    </>}
  </Stack></Box>;
});
