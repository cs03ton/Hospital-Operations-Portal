import { Box, Button, Stack, Typography } from "@mui/material";
import { AppDatePicker } from "../common/AppDatePicker";
import { AppTimePicker } from "../common/AppTimePicker";
import { emptyWorkPeriod, type MemoWorkPeriod } from "../../utils/memoWorkPeriods";
export type WorkPeriodRow = MemoWorkPeriod & { id: number; manualEndDate: boolean };
export const newWorkPeriodRow = (id: number): WorkPeriodRow => ({ ...emptyWorkPeriod(), id, manualEndDate: false });
export function MemoWorkPeriodsEditor({ rows, errors, onChange }: { rows: WorkPeriodRow[]; errors: Record<number, string>; onChange: (rows: WorkPeriodRow[]) => void }) {
  const update = (index: number, key: keyof MemoWorkPeriod, value: string) => onChange(rows.map((row, i) => i !== index ? row : {
    ...row, [key]: value, ...(key === "startDate" && !row.manualEndDate ? { endDate: value } : {}), ...(key === "endDate" ? { manualEndDate: true } : {}),
  }));
  return <Stack spacing={2}>
    <Box><Typography fontWeight={800}>วัน–เวลาปฏิบัติงาน</Typography><Typography variant="body2" color="text.secondary">เพิ่มได้หลายวันหรือวันเดียวกันหลายช่วง หากข้ามคืนให้เลือกวันสิ้นสุดจริง (สูงสุด 50 รายการ)</Typography></Box>
    {rows.map((row, index) => <Stack key={row.id} spacing={2} sx={{ p: 2, borderRadius: 2, border: "1px solid", borderColor: errors[index] ? "error.main" : "divider", bgcolor: "white" }}>
      <Stack direction="row" alignItems="center" justifyContent="space-between"><Typography fontWeight={700}>รายการ {index + 1}</Typography><Button color="error" size="small" aria-label={`ลบวันเวลารายการ ${index + 1}`} onClick={() => onChange(rows.length === 1 ? [newWorkPeriodRow(row.id + 1)] : rows.filter((_, i) => i !== index))}>ลบรายการ</Button></Stack>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(2, minmax(0, 1fr))", lg: "repeat(4, minmax(0, 1fr))" }, gap: 2 }}>
        <AppDatePicker label={`วันเริ่ม รายการ ${index + 1}`} size="medium" value={row.startDate} error={!!errors[index]} onChange={v => update(index, "startDate", v)} />
        <AppTimePicker label={`เวลาเริ่ม รายการ ${index + 1}`} value={row.startTime} error={!!errors[index]} onChange={v => update(index, "startTime", v)} />
        <AppDatePicker label={`วันสิ้นสุด รายการ ${index + 1}`} size="medium" value={row.endDate} error={!!errors[index]} onChange={v => update(index, "endDate", v)} />
        <AppTimePicker label={`เวลาสิ้นสุด รายการ ${index + 1}`} value={row.endTime} error={!!errors[index]} onChange={v => update(index, "endTime", v)} />
      </Box>
      {errors[index] && <Typography role="alert" color="error" variant="body2">รายการ {index + 1}: {errors[index]}</Typography>}
    </Stack>)}
    <Button variant="outlined" sx={{ alignSelf: "flex-start" }} disabled={rows.length >= 50} onClick={() => onChange([...rows, newWorkPeriodRow(Math.max(...rows.map(r => r.id)) + 1)])}>เพิ่มวัน–เวลา</Button>
  </Stack>;
}
