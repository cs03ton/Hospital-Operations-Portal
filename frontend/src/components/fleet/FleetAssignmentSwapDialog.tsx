import { useState } from "react";
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { getFleetSwapCandidates, swapFleetAssignments, type FleetRequest, type FleetSwapCandidate } from "../../api/fleetApi";
import { formatThaiDateTime } from "../../utils/dateFormat";
import { getFleetStatusLabel } from "../../utils/fleetLabels";

export function FleetAssignmentSwapDialog({ request, onClose, onSuccess }: { request: FleetRequest; onClose: () => void; onSuccess: () => Promise<void> }) {
  const client = useQueryClient();
  const [text, setText] = useState(""); const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<FleetSwapCandidate | null>(null);
  const [includeDrivers, setIncludeDrivers] = useState(false); const [reason, setReason] = useState(""); const [confirm, setConfirm] = useState(false);
  const assignment = request.activeAssignment!;
  const candidates = useQuery({ queryKey: ["fleet", "swap-candidates", assignment.id, search], queryFn: () => getFleetSwapCandidates(assignment.id, search) });
  const swap = useMutation({ mutationFn: () => swapFleetAssignments(assignment.id, { otherAssignmentId: selected!.assignmentId, includeDrivers, reason: reason.trim(),
    requestConcurrencyToken: request.concurrencyToken, assignmentConcurrencyToken: assignment.concurrencyToken,
    otherRequestConcurrencyToken: selected!.requestConcurrencyToken, otherAssignmentConcurrencyToken: selected!.assignmentConcurrencyToken }),
    onSuccess: async () => { await client.invalidateQueries({ predicate: q => q.queryKey.some(key => typeof key === "string" && key.startsWith("fleet")) }); await onSuccess(); onClose(); },
    onError: async error => { if (isAxiosError(error) && error.response?.status === 409) { setConfirm(false); setSelected(null); await onSuccess(); await candidates.refetch(); } },
  });
  const errorText = (error: unknown) => isAxiosError(error) ? error.response?.data?.message ?? "ดำเนินการไม่สำเร็จ กรุณาลองใหม่" : "ดำเนินการไม่สำเร็จ กรุณาลองใหม่";
  return <Dialog open onClose={swap.isPending ? undefined : onClose} fullWidth maxWidth="md"><DialogTitle>สลับรถกับคำขออื่น</DialogTitle><DialogContent dividers><Stack spacing={2}>
    <Typography fontWeight={800}>{request.requestNo} · {assignment.vehicleCode} · {assignment.registrationNumber}</Typography>
    <Typography variant="body2">{formatThaiDateTime(request.departureAt)} ถึง {formatThaiDateTime(request.expectedReturnAt)} · {request.destination}</Typography>
    {!confirm && <>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}><TextField fullWidth size="small" label="ค้นหาเลขคำขอที่จะสลับ" value={text} onChange={e => setText(e.target.value)} /><Button onClick={() => { setSearch(text.trim()); setSelected(null); swap.reset(); }}>ค้นหา</Button></Stack>
      {candidates.isLoading && <Typography role="status">กำลังค้นหาคำขอ...</Typography>}
      {candidates.isError && <Alert severity="error">{errorText(candidates.error)}</Alert>}
      {candidates.data?.length === 0 && <Alert severity="info">ไม่พบคำขอที่สลับได้ตามสิทธิ์และสถานะ ลองค้นหาเลขคำขออื่น</Alert>}
      <Typography variant="caption" color="text.secondary">แสดงสูงสุด 50 รายการ เฉพาะงานที่ยังไม่เริ่มทริป ความพร้อมของรถและคนขับจะตรวจอีกครั้งเมื่อยืนยัน</Typography>
      <Stack spacing={1} sx={{ maxHeight: 300, overflowY: "auto" }}>{candidates.data?.map(item => <Box key={item.assignmentId} sx={{ p: 1.5, border: "1px solid", borderColor: selected?.assignmentId === item.assignmentId ? "primary.main" : "divider", borderRadius: 2, overflowWrap: "anywhere" }}>
        <Typography fontWeight={800}>{item.requestNo} · {getFleetStatusLabel(item.status)}</Typography><Typography variant="body2">{formatThaiDateTime(item.departureAt)} ถึง {formatThaiDateTime(item.expectedReturnAt)}</Typography><Typography>{item.destination}</Typography><Typography variant="body2">รถ: {item.vehicle} · คนขับ: {item.driver}</Typography>
        <Button variant={selected?.assignmentId === item.assignmentId ? "contained" : "outlined"} size="small" onClick={() => { setSelected(item); swap.reset(); }}>{selected?.assignmentId === item.assignmentId ? "เลือกแล้ว" : "เลือกคำขอนี้"}</Button>
      </Box>)}</Stack>
      <TextField select helperText={includeDrivers ? "ย้ายรถและคนขับไปด้วยกัน" : "คนขับยังอยู่กับคำขอเดิม"} label="รูปแบบการสลับ" value={includeDrivers ? "both" : "vehicle"} onChange={e => setIncludeDrivers(e.target.value === "both")}><MenuItem value="vehicle">สลับเฉพาะรถ</MenuItem><MenuItem value="both">สลับรถพร้อมคนขับ</MenuItem></TextField>
      <TextField required multiline minRows={2} label="เหตุผลการสลับ" value={reason} onChange={e => setReason(e.target.value)} inputProps={{ maxLength: 900 }} />
    </>}
    {confirm && selected && <><Alert severity="info">ตรวจสอบข้อมูลทั้งสองคำขอก่อนยืนยัน การสลับจะบันทึกพร้อมกันทั้งสองงาน</Alert>
      {[{ no: request.requestNo, before: `${assignment.vehicleCode} · ${assignment.registrationNumber}`, after: selected.vehicle, oldDriver: assignment.driverName, newDriver: includeDrivers ? selected.driver : assignment.driverName },
        { no: selected.requestNo, before: selected.vehicle, after: `${assignment.vehicleCode} · ${assignment.registrationNumber}`, oldDriver: selected.driver, newDriver: includeDrivers ? assignment.driverName : selected.driver }].map(x => <Box key={x.no} sx={{ p: 2, border: "1px solid", borderColor: "divider", borderRadius: 2, overflowWrap: "anywhere" }}><Typography fontWeight={800}>{x.no}</Typography><Typography>เดิม: {x.before} · {x.oldDriver}</Typography><Divider sx={{ my: 1 }} /><Typography color="primary" fontWeight={700}>หลังสลับ: {x.after} · {x.newDriver}</Typography></Box>)}
      <Typography sx={{ whiteSpace: "pre-wrap", overflowWrap: "anywhere" }}>เหตุผล: {reason.trim()}</Typography>
      {includeDrivers && <Typography variant="body2" color="text.secondary">งานพร้อมเดินทางที่เปลี่ยนคนขับจะกลับไปรอคนขับรับทราบ</Typography>}
    </>}
    {swap.isError && <Alert severity="error">{errorText(swap.error)}</Alert>}
  </Stack></DialogContent><DialogActions sx={{ flexWrap: "wrap", gap: 1 }}><Button disabled={swap.isPending} onClick={onClose}>ปิด</Button>{confirm ? <><Button disabled={swap.isPending} onClick={() => setConfirm(false)}>กลับไปแก้ไข</Button><Button variant="contained" disabled={swap.isPending} onClick={() => swap.mutate()}>ยืนยันสลับรถ</Button></> : <Button variant="contained" disabled={!selected || !reason.trim() || candidates.isError} onClick={() => setConfirm(true)}>ตรวจสอบก่อนสลับ</Button>}</DialogActions></Dialog>;
}
