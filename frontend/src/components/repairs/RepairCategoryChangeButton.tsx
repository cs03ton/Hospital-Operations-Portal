import { useState } from "react";
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from "@mui/material";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { repairChangeCategory, repairOptions, type Repair } from "../../api/repairApi";
import { usePermission } from "../../context/PermissionContext";
import { RepairError } from "./RepairUi";
import { repairTeamLabel } from "../../utils/repairPresentation";

export function RepairCategoryChangeButton({ request, categoryName, allowed, onChanged }: {
  request: Repair; categoryName?: string; allowed: boolean; onChanged: () => Promise<unknown>;
}) {
  const { hasPermission } = usePermission();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [categoryId, setCategoryId] = useState("");
  const [reason, setReason] = useState("");
  const [token, setToken] = useState("");
  const options = useQuery({ queryKey: ["repairs", "category-change-options"], queryFn: repairOptions, enabled: open });
  const categories = (options.data?.categories ?? []).filter(x => x.isActive && x.id !== request.categoryId && (!request.hasStarted || x.teamCode === request.teamCode));
  const selected = categories.find(x => x.id === categoryId);
  const mutation = useMutation({ mutationFn: () => repairChangeCategory(request.id, { categoryId, reason: reason.trim(), concurrencyToken: token }),
    onSuccess: async result => { setOpen(false); if (!result.canView) navigate("/repairs"); await onChanged(); },
    onError: () => { void onChanged(); } });
  if (!allowed || !hasPermission("RepairManagement.Manage")) return null;
  return <>
    <Button variant="outlined" startIcon={<EditOutlinedIcon />} onClick={() => { setCategoryId(""); setReason(""); setToken(request.concurrencyToken); mutation.reset(); setOpen(true); }}>เปลี่ยนประเภทงาน</Button>
    <Dialog open={open} onClose={() => !mutation.isPending && setOpen(false)} fullWidth maxWidth="sm" aria-labelledby="repair-category-title">
      <DialogTitle id="repair-category-title">เปลี่ยนประเภทงาน</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Typography>ประเภทเดิม: {categoryName || "ไม่ระบุ"} · ทีม {repairTeamLabel(request.teamCode)}</Typography>
        <Alert severity="info">{request.hasStarted ? "ใบงานนี้เคยเริ่มซ่อมแล้ว เปลี่ยนได้เฉพาะประเภทในทีมเดิม" : "สามารถเลือกประเภทข้ามทีมได้ เนื่องจากยังไม่เคยเริ่มซ่อม"} สถานะและประวัติใบงานจะคงเดิม</Alert>
        {request.status === "Closed" && <Alert severity="warning">ใบงานนี้ปิดแล้ว การเปลี่ยนประเภทจะปรับการจัดหมวดในรายงานย้อนหลัง โดยไม่เปิดงานใหม่หรือเปลี่ยนข้อมูลการปิดงาน</Alert>}
        <RepairError error={mutation.error || options.error} />
        {mutation.isError && <Typography variant="caption" color="text.secondary">หากข้อมูลเปลี่ยนแปลง กรุณาปิดแล้วเปิดหน้าต่างใหม่เพื่อตรวจสอบข้อมูลล่าสุดก่อนยืนยันอีกครั้ง</Typography>}
        <TextField select label="ประเภทงานใหม่" value={selected ? categoryId : ""} required fullWidth disabled={options.isLoading || mutation.isPending}
          onChange={event => setCategoryId(event.target.value)} helperText={options.isLoading ? "กำลังโหลดประเภทงาน..." : "เลือกประเภทที่เปิดใช้งาน"}>
          {categories.map(x => <MenuItem key={x.id} value={x.id} sx={{ whiteSpace: "normal" }}>{x.name} · {repairTeamLabel(x.teamCode)}</MenuItem>)}
        </TextField>
        {!options.isLoading && !options.isError && categories.length === 0 && <Alert severity="info">ไม่มีประเภทอื่นที่สามารถเปลี่ยนได้</Alert>}
        {options.isError && <Button onClick={() => void options.refetch()}>โหลดประเภทใหม่</Button>}
        {selected && selected.teamCode !== request.teamCode && <Alert severity="warning">ใบงานจะย้ายจากทีม {repairTeamLabel(request.teamCode)} ไปยังทีม {repairTeamLabel(selected.teamCode)} ทีมใหม่จะเห็นงานตามสิทธิ์ของระบบ</Alert>}
        <TextField label="เหตุผลการเปลี่ยนประเภท" value={reason} onChange={event => setReason(event.target.value)} required multiline minRows={3} fullWidth
          inputProps={{ maxLength: 8000 }} disabled={mutation.isPending} />
      </Stack></DialogContent>
      <DialogActions sx={{ p: 2, flexWrap: "wrap", gap: 1 }}>
        <Button disabled={mutation.isPending} onClick={() => setOpen(false)}>ยกเลิก</Button>
        <Button variant="contained" disabled={!selected || !reason.trim() || options.isError || mutation.isPending} onClick={() => mutation.mutate()}>ยืนยันเปลี่ยนประเภท</Button>
      </DialogActions>
    </Dialog>
  </>;
}
