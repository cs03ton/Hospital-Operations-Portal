import { MenuItem, TextField } from "@mui/material";
import type { MemoContentTemplate } from "../../api/correspondenceApi";

export const overtimeReportTemplateCode = "OT_WORK_REPORT";
export const overtimeReportTemplateName = "รายงานผลการปฏิบัติงานนอกเวลาราชการและวันหยุดราชการ";
export function MemoTemplateSelect({ templates, value, onChange, includeReport = true }: { templates?: MemoContentTemplate[]; value: string; onChange: (code: string) => void; includeReport?: boolean }) {
  return <TextField select size="small" label="เลือกเทมเพลต" value={value} onChange={e => onChange(e.target.value)} fullWidth>
    <MenuItem value="">เขียนข้อความเอง</MenuItem>
    {templates?.filter(t => t.isActive).map(t => <MenuItem value={t.code} key={t.code} sx={{ whiteSpace: "normal" }}>{t.name}</MenuItem>)}
    {includeReport && <MenuItem value={overtimeReportTemplateCode} sx={{ whiteSpace: "normal" }}>{overtimeReportTemplateName}</MenuItem>}
  </TextField>;
}
