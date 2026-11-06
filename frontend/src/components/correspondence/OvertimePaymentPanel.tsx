import { useEffect, useState } from "react";
import { Alert, Box, Button, Card, CardContent, Chip, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { getOvertimePaymentOptions, type OvertimeClaimDay, type OvertimePaymentInput, type OvertimePaymentOptions, type OvertimeReportEntry } from "../../api/correspondenceApi";
import { dailyOvertimeWork, overtimeDayLimit, paymentAmount } from "../../utils/overtimePayment";
import { formatThaiDate } from "../../utils/dateFormat";
import { moneyValues, thaiMonths } from "../../utils/memoContentTemplates";

export function OvertimePaymentPanel({ month, entries, confirmed, error, onMonthChange, onEdit, onConfirm }: {
  month: string; entries: OvertimeReportEntry[]; confirmed: boolean; error?: string;
  onMonthChange: (month: string) => void; onEdit: () => void; onConfirm: (payment: OvertimePaymentInput) => void;
}) {
  const [loaded, setLoaded] = useState<OvertimePaymentOptions | null>(null), [loadError, setLoadError] = useState(""), [reload, setReload] = useState(0);
  const [choices, setChoices] = useState<{ key: string; days: OvertimeClaimDay[] } | null>(null);
  useEffect(() => {
    const controller = new AbortController(); setLoaded(null); setLoadError("");
    getOvertimePaymentOptions(month, controller.signal).then(data => { if (!data.rules) throw new Error("Missing payment rules"); if (!controller.signal.aborted) setLoaded(data); })
      .catch(() => { if (!controller.signal.aborted) setLoadError("โหลดอัตราและปฏิทินวันหยุดไม่สำเร็จ กรุณาลองใหม่"); });
    return () => controller.abort();
  }, [month, reload]);
  const options = loaded?.month === month ? loaded : null;
  const actual = dailyOvertimeWork(entries, month); const key = JSON.stringify([month, actual.days, options]);
  const rows = choices?.key === key ? choices.days : actual.days.map(day => {
    const dayType = options?.holidayDates.includes(day.date) ? "holiday" as const : "weekday" as const;
    return { date: day.date, dayType, claimHours: options ? overtimeDayLimit(day, dayType, options.rules).maxClaimHours : 0 };
  });
  const invalid = rows.some((row, i) => !options || !Number.isInteger(row.claimHours) || row.claimHours < 0 || row.claimHours > overtimeDayLimit(actual.days[i], row.dayType, options.rules).maxClaimHours);
  const total = options && !invalid ? paymentAmount(rows, options) : null;
  const money = (value: number) => value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  function update(index: number, patch: Partial<OvertimeClaimDay>) {
    if (patch.dayType && options) patch.claimHours = overtimeDayLimit(actual.days[index], patch.dayType, options.rules).maxClaimHours;
    setChoices({ key, days: rows.map((row, i) => i === index ? { ...row, ...patch } : row) }); onEdit();
  }
  return <Card data-report-field="payment" component="section" aria-label="สรุปเบิกจ่ายรายวัน" sx={{ borderTop: "3px solid #c5a65f" }}><CardContent><Stack spacing={2.5}>
    <Box><Typography variant="h6" fontWeight={800}>สรุปเบิกจ่ายรายวัน</Typography><Typography variant="body2" color="text.secondary">ตรวจประเภทวันและยืนยันชั่วโมงที่เบิก ก่อนสร้างเอกสารทั้งสองชุด</Typography></Box>
    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", sm: "repeat(2,minmax(0,1fr))" }, gap: 2 }}>
      <TextField select label="เดือนเอกสาร" value={month.slice(5)} onChange={e => onMonthChange(`${month.slice(0, 4)}-${e.target.value}`)}>{thaiMonths.map((name, i) => <MenuItem key={name} value={String(i + 1).padStart(2, "0")}>{name}</MenuItem>)}</TextField>
      <TextField select label="ปีเอกสาร พ.ศ." value={month.slice(0, 4)} onChange={e => onMonthChange(`${e.target.value}-${month.slice(5)}`)}>{Array.from({ length: 201 }, (_, i) => <MenuItem key={1900 + i} value={String(1900 + i)}>{2443 + i}</MenuItem>)}</TextField>
    </Box>
    {loadError ? <Alert severity="error" action={<Button onClick={() => { onEdit(); setReload(n => n + 1); }}>ลองใหม่</Button>}>{loadError}</Alert> : !options ? <Typography role="status">กำลังโหลดอัตราและปฏิทินวันหยุด...</Typography> : <>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", md: "repeat(3,minmax(0,1fr))" }, gap: 2 }}>
        <TextField label="อัตราวันปกติ (บาท/ชม.)" value={options.weekdayRate} InputProps={{ readOnly: true }} />
        <TextField label="อัตราวันหยุด (บาท/ชม.)" value={options.holidayRate} InputProps={{ readOnly: true }} />
        <TextField label="ผู้จ่ายเงิน" value={options.payerName} InputProps={{ readOnly: true }} />
      </Box>
      <Typography variant="caption" color="text.secondary">วันปกตินับหลัง {options.rules.weekdayStart} น. สูงสุด {options.rules.weekdayMaxHours} ชม./วัน · วันหยุดหักพัก {options.rules.breakStart}–{options.rules.breakEnd} น. สูงสุด {options.rules.holidayMaxHours} ชม./วัน รายงานทั้งสองใช้ชั่วโมงเบิกที่ยืนยันตรงกัน</Typography>
      {actual.error ? <Alert severity="info">{actual.error}</Alert> : <Stack spacing={1.5}>{rows.map((row, i) => {
        const day = actual.days[i], limit = overtimeDayLimit(day, row.dayType, options.rules); const valid = Number.isInteger(row.claimHours) && row.claimHours >= 0 && row.claimHours <= limit.maxClaimHours;
        const rate = row.dayType === "holiday" ? options.holidayRate : options.weekdayRate;
        return <Box key={row.date} sx={{ display: "grid", gridTemplateColumns: { xs: "minmax(0,1fr)", sm: "repeat(2,minmax(0,1fr))", lg: "1.3fr 1.4fr 1fr 1.2fr 1fr" }, gap: 1.5, alignItems: "center", p: 2, border: "1px solid #dce5df", borderRadius: 2, bgcolor: "#f8faf8" }}>
          <Box><Typography fontWeight={800}>{formatThaiDate(row.date)}</Typography><Typography variant="caption" color="text.secondary" display="block">ทำงานจริง {Math.floor(day.minutes / 60)} ชม. {day.minutes % 60} นาที</Typography><Typography variant="caption" color="text.secondary" display="block">{row.dayType === "holiday" ? "หักพัก" : `ก่อน ${options.rules.weekdayStart}`} {Math.floor(limit.excludedMinutes / 60)} ชม. {limit.excludedMinutes % 60} นาที</Typography><Typography variant="body2" color="primary.main" fontWeight={800}>เบิกได้สูงสุด {limit.maxClaimHours} ชม.</Typography></Box>
          <TextField size="small" select label={`ประเภทวัน ${formatThaiDate(row.date)}`} value={row.dayType} onChange={e => update(i, { dayType: e.target.value as OvertimeClaimDay["dayType"] })}><MenuItem value="weekday">วันปกติ</MenuItem><MenuItem value="holiday">วันหยุด</MenuItem></TextField>
          <TextField size="small" label={`ชั่วโมงเบิกที่ยืนยัน ${formatThaiDate(row.date)}`} type="number" value={Number.isFinite(row.claimHours) ? row.claimHours : ""} inputProps={{ min: 0, max: limit.maxClaimHours, step: 1 }} error={!valid} helperText={valid ? `ปรับลดได้ · สูงสุด ${limit.maxClaimHours} ชม.` : `กรอกจำนวนเต็ม 0–${limit.maxClaimHours}`} onChange={e => update(i, { claimHours: e.target.value === "" ? NaN : Number(e.target.value) })} />
          <Typography variant="body2">อัตรา {money(rate)} บาท/ชม.</Typography><Typography fontWeight={800} color="primary.main">{valid ? money(rate * row.claimHours) : "–"} บาท</Typography>
        </Box>;
      })}</Stack>}
      <Box sx={{ p: 2, bgcolor: "#edf5f1", borderRadius: 2 }}><Typography fontWeight={800}>รวมเงิน {total == null ? "–" : money(total)} บาท</Typography>{total != null && <Typography variant="body2">{moneyValues(total.toFixed(2))?.amount_text}</Typography>}</Box>
      <Stack direction={{ xs: "column", sm: "row" }} alignItems={{ sm: "center" }} spacing={1.5}>
        <Button variant="outlined" disabled={!!actual.error || !rows.length || invalid} onClick={() => onConfirm({ month, days: rows.map(row => ({ ...row })) })}>ยืนยันชั่วโมงเบิก</Button>
        {confirmed && <Chip color="success" label="ยืนยันชั่วโมงเบิกแล้ว" />}
      </Stack>
    </>}
    {error && <Alert severity="error">{error}</Alert>}
  </Stack></CardContent></Card>;
}
