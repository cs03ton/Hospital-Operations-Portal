import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import LinkOutlinedIcon from "@mui/icons-material/LinkOutlined";
import PowerSettingsNewOutlinedIcon from "@mui/icons-material/PowerSettingsNewOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import {
  Alert, Box, Button, Card, CardContent, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Grid, MenuItem, Stack,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDeferredValue, useMemo, useState } from "react";
import {
  confirmFleetLineGroup, createFleetLineGroupEndpoint, disableFleetLineGroup, getFleetLineGroupDeliveries, getFleetLineGroups,
  testFleetLineGroup, updateFleetLineGroupSubscriptions, type FleetLineGroup,
  updateFleetLineGroupEndpoint,
} from "../api/fleetApi";
import { ActionDialog } from "../components/common/ActionDialog";
import { EmptyState } from "../components/common/EmptyState";
import { ListPagination } from "../components/common/ListPagination";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { usePermission } from "../context/PermissionContext";
import { formatThaiDateTime } from "../utils/dateFormat";

const eventLabels: Record<string, string> = {
  "Fleet.RequestSubmitted": "ส่งคำขอใหม่", "Fleet.AssignmentCreated": "จัดรถและคนขับ",
  "Fleet.AdminReviewed": "หัวหน้าฝ่ายตรวจแล้ว", "Fleet.Returned": "ส่งกลับ",
  "Fleet.DirectorApproved": "ผู้อำนวยการอนุมัติ", "Fleet.Rejected": "ไม่อนุมัติ",
  "Fleet.Cancelled": "ยกเลิก", "Fleet.AssignmentChanged": "เปลี่ยนรถ/คนขับ",
  "Fleet.DriverAcknowledged": "คนขับรับทราบ", "Fleet.TripCompleted": "จบภารกิจ",
  "Fleet.TripOverdue": "ภารกิจเกินกำหนด",
};

type DialogState = { type: "confirm" | "disable" | "test"; group: FleetLineGroup } | null;
type ConfigState = { group?: FleetLineGroup; displayName: string; groupId: string; endpointUrl: string; clientId: string; clientSecret: string } | null;

export function FleetLineGroupsPage() {
  const queryClient = useQueryClient();
  const { hasPermission } = usePermission();
  const canManage = hasPermission("FleetLineGroup.Manage");
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search.trim());
  const [status, setStatus] = useState("");
  const [dialog, setDialog] = useState<DialogState>(null);
  const [reason, setReason] = useState("");
  const [message, setMessage] = useState("");
  const [selected, setSelected] = useState<FleetLineGroup | null>(null);
  const [deliveryPage, setDeliveryPage] = useState(1);
  const [subscriptionDraft, setSubscriptionDraft] = useState<Record<string, boolean>>({});
  const [notice, setNotice] = useState<{ severity: "success" | "error"; text: string } | null>(null);
  const [config, setConfig] = useState<ConfigState>(null);

  const groups = useQuery({
    queryKey: ["fleet", "line-groups", status, deferredSearch],
    queryFn: () => getFleetLineGroups({ status: status || undefined, search: deferredSearch || undefined }),
    retry: false,
  });
  const deliveries = useQuery({
    queryKey: ["fleet", "line-groups", selected?.id, "deliveries", deliveryPage],
    queryFn: () => getFleetLineGroupDeliveries(selected!.id, deliveryPage, 20),
    enabled: Boolean(selected), retry: false,
  });
  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ["fleet", "line-groups"] });
  };
  const action = useMutation({
    mutationFn: async () => {
      if (!dialog) return;
      if (dialog.type === "confirm") return confirmFleetLineGroup(dialog.group.id, dialog.group.concurrencyToken, reason.trim() || undefined);
      if (dialog.type === "disable") return disableFleetLineGroup(dialog.group.id, dialog.group.concurrencyToken, reason.trim());
      return testFleetLineGroup(dialog.group.id, message.trim() || undefined);
    },
    onSuccess: async () => {
      setNotice({ severity: "success", text: dialog?.type === "test" ? "ส่งข้อความทดสอบสำเร็จแล้ว" : "บันทึกสถานะกลุ่มเรียบร้อยแล้ว" });
      setDialog(null); setReason(""); setMessage(""); await refresh();
    },
    onError: (error: { response?: { status?: number; data?: { message?: string } } }) => setNotice({
      severity: "error",
      text: error.response?.data?.message ?? (error.response?.status === 409 ? "ข้อมูลถูกแก้ไขโดยผู้ใช้อื่น กรุณาโหลดใหม่แล้วลองอีกครั้ง" : "ดำเนินการไม่สำเร็จ กรุณาตรวจสอบสิทธิ์และการเชื่อมต่อ LINE"),
    }),
  });
  const saveSubscriptions = useMutation({
    mutationFn: () => updateFleetLineGroupSubscriptions(selected!.id, selected!.concurrencyToken, subscriptionDraft),
    onSuccess: async () => { setNotice({ severity: "success", text: "บันทึก Event Subscription แล้ว" }); await refresh(); },
    onError: (error: { response?: { status?: number } }) => setNotice({ severity: "error", text: error.response?.status === 409 ? "ข้อมูล Subscription เปลี่ยนแปลง กรุณาโหลดใหม่" : "บันทึก Subscription ไม่สำเร็จ" }),
  });
  const saveConfiguration = useMutation({
    mutationFn: async () => {
      if (!config) return;
      const data = { displayName: config.displayName.trim(), groupId: config.groupId.trim(), endpointUrl: config.endpointUrl.trim(), clientId: config.clientId.trim(), clientSecret: config.clientSecret.trim() || undefined, concurrencyToken: config.group?.concurrencyToken };
      return config.group ? updateFleetLineGroupEndpoint(config.group.id, data) : createFleetLineGroupEndpoint(data);
    },
    onSuccess: async () => { setNotice({ severity: "success", text: config?.group ? "บันทึกการเชื่อมต่อ LINE Group แล้ว" : "เพิ่มปลายทาง LINE Group แล้ว กรุณาตรวจสอบและยืนยันก่อนเปิดใช้งาน" }); setConfig(null); await refresh(); },
    onError: (error: { response?: { status?: number; data?: { message?: string } } }) => setNotice({ severity: "error", text: error.response?.data?.message ?? (error.response?.status === 409 ? "Group ID นี้มีอยู่ในระบบแล้ว" : "บันทึกการเชื่อมต่อไม่สำเร็จ กรุณาตรวจสอบข้อมูล") }),
  });

  const rows = useMemo(() => groups.data ?? [], [groups.data]);
  const dialogTitle = dialog?.type === "confirm" ? "ยืนยัน LINE Group" : dialog?.type === "disable" ? "ปิดใช้งาน LINE Group" : "ส่งข้อความทดสอบ";
  const openDetails = (group: FleetLineGroup) => {
    setSelected(group); setDeliveryPage(1);
    setSubscriptionDraft(Object.fromEntries(group.events.map(x => [x.eventType, x.isEnabled])));
  };
  const activeCount = useMemo(() => rows.filter(x => x.status === "Active").length, [rows]);

  return <>
    <PageHeader title="ตั้งค่าการแจ้งเตือน LINE Group" subtitle="เชื่อมต่อกลุ่ม LINE สำหรับรับข่าวสารและสถานะงานยานพาหนะ" />
    {notice && <Alert severity={notice.severity} onClose={() => setNotice(null)} sx={{ mb: 2 }}>{notice.text}</Alert>}
    <Card sx={{ mb: 2, background: "linear-gradient(135deg, #F0F8F5 0%, #FFFFFF 70%)" }}><CardContent><Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" alignItems={{ md: "center" }} spacing={2}>
      <Stack direction="row" spacing={1.5} alignItems="center"><Box sx={{ width: 48, height: 48, borderRadius: 2.5, bgcolor: "primary.main", color: "white", display: "grid", placeItems: "center" }}><LinkOutlinedIcon /></Box><Box><Typography variant="h6" fontWeight={900}>เชื่อมต่อ LINE Endpoint</Typography><Typography variant="body2" color="text.secondary">กรอก Endpoint URL, Group ID, Client ID และ Client Secret จากผู้ให้บริการ แล้วทดสอบส่งก่อนเปิดใช้งานจริง</Typography></Box></Stack>
      {canManage && <Button variant="contained" startIcon={<AddOutlinedIcon />} onClick={() => setConfig({ displayName: "", groupId: "", endpointUrl: "", clientId: "", clientSecret: "" })}>เพิ่มการเชื่อมต่อ</Button>}
    </Stack></CardContent></Card>
    <Grid container spacing={2} sx={{ mb: 2 }}>
      <Grid item xs={12} sm={4}><Summary label="กลุ่มที่ตรวจพบ" value={rows.length} /></Grid>
      <Grid item xs={12} sm={4}><Summary label="เปิดใช้งาน" value={activeCount} /></Grid>
      <Grid item xs={12} sm={4}><Summary label="ต้องตรวจสอบ" value={rows.filter(x => x.attentionRequired).length} /></Grid>
    </Grid>
    <Card sx={{ mb: 2 }}><CardContent><Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
      <TextField fullWidth size="small" label="ค้นหาชื่อกลุ่ม" value={search} onChange={e => setSearch(e.target.value)} />
      <TextField select size="small" label="สถานะ" value={status} onChange={e => setStatus(e.target.value)} sx={{ minWidth: 180 }}>
        <MenuItem value="">ทุกสถานะ</MenuItem><MenuItem value="Pending">รอยืนยัน</MenuItem><MenuItem value="Active">ใช้งาน</MenuItem><MenuItem value="Disabled">ปิดใช้งาน</MenuItem>
      </TextField>
      <Button variant="outlined" onClick={() => { setSearch(""); setStatus(""); }}>ล้างตัวกรอง</Button>
    </Stack></CardContent></Card>
    <Card><CardContent>
      {groups.isLoading ? <LoadingState message="กำลังโหลด LINE Groups..." /> : groups.isError ?
        <Alert severity="error" action={<Button onClick={() => void groups.refetch()}>ลองใหม่</Button>}>โหลดรายการ LINE Group ไม่สำเร็จ</Alert> :
        rows.length === 0 ? <EmptyState title={search || status ? "ไม่พบกลุ่มตามตัวกรอง" : "ยังไม่พบ LINE Group"} description={search || status ? "ลองเปลี่ยนคำค้นหาหรือสถานะ" : "เพิ่ม LINE OA เข้ากลุ่มและส่งข้อความ “ลงทะเบียนกลุ่ม HOP”"} /> :
        <TableContainer><Table size="small" sx={{ minWidth: 960 }}><TableHead><TableRow>
          <TableCell>ชื่อกลุ่ม</TableCell><TableCell>Group ID</TableCell><TableCell>ช่องทางส่ง</TableCell><TableCell>สถานะ</TableCell><TableCell>เหตุการณ์ที่แจ้ง</TableCell><TableCell>ตรวจสอบล่าสุด</TableCell><TableCell align="right">จัดการ</TableCell>
        </TableRow></TableHead><TableBody>{rows.map(group => <TableRow hover key={group.id} selected={selected?.id === group.id}>
          <TableCell><Typography fontWeight={800}>{group.displayName}</Typography>{group.attentionRequired && <Typography variant="caption" color="error">ต้องตรวจสอบ: {group.attentionReason ?? "ไม่ทราบสาเหตุ"}</Typography>}</TableCell>
          <TableCell>{group.groupIdMasked}</TableCell><TableCell><Box><Chip size="small" label={group.deliveryProvider === "CUSTOM_ENDPOINT" ? "Endpoint ภายนอก" : "LINE Messaging API"} color={group.deliveryProvider === "CUSTOM_ENDPOINT" ? "primary" : "default"} />{group.endpointUrl && <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5, maxWidth: 240 }} noWrap>{group.endpointUrl}</Typography>}</Box></TableCell><TableCell><StatusChip status={group.status} /></TableCell>
          <TableCell><span>{group.events.filter(x => x.isEnabled).length}/{group.events.length}</span> รายการ</TableCell><TableCell>{formatThaiDateTime(group.lastDetectedAt)}</TableCell>
          <TableCell align="right"><Stack direction="row" spacing={0.5} justifyContent="flex-end">
            <Button size="small" onClick={() => openDetails(group)}>รายละเอียด</Button>
            {canManage && <Button size="small" startIcon={<SettingsOutlinedIcon />} onClick={() => setConfig({ group, displayName: group.displayName, groupId: "", endpointUrl: group.endpointUrl ?? "", clientId: group.clientId ?? "", clientSecret: "" })}>แก้ไข</Button>}
            {canManage && group.status === "Pending" && <Button size="small" startIcon={<CheckCircleOutlineIcon />} onClick={() => setDialog({ type: "confirm", group })}>ยืนยัน</Button>}
            {canManage && group.status !== "Disabled" && <Button size="small" startIcon={<SendOutlinedIcon />} onClick={() => setDialog({ type: "test", group })}>ทดสอบ</Button>}
            {canManage && group.status !== "Disabled" && <Button size="small" color="error" startIcon={<PowerSettingsNewOutlinedIcon />} onClick={() => setDialog({ type: "disable", group })}>ปิด</Button>}
          </Stack></TableCell>
        </TableRow>)}</TableBody></Table></TableContainer>}
    </CardContent></Card>
    {selected && <Card sx={{ mt: 2 }}><CardContent><Stack spacing={2}>
      <Box><Typography variant="h6" fontWeight={900}>เหตุการณ์ที่ต้องการแจ้งเตือน: {selected.displayName}</Typography><Typography variant="body2" color="text.secondary">เลือกเฉพาะเหตุการณ์งานยานพาหนะที่ต้องการส่งเข้ากลุ่ม</Typography></Box>
      <Grid container>{selected.events.map(event => <Grid item xs={12} sm={6} md={4} key={event.eventType}><FormControlLabel
        control={<Checkbox disabled={!canManage || selected.status === "Disabled"} checked={subscriptionDraft[event.eventType] ?? false} onChange={(_, checked) => setSubscriptionDraft(x => ({ ...x, [event.eventType]: checked }))} />}
        label={eventLabels[event.eventType] ?? event.eventType} /></Grid>)}</Grid>
      {canManage && <Button variant="contained" sx={{ alignSelf: "flex-start" }} disabled={saveSubscriptions.isPending || selected.status === "Disabled"} onClick={() => saveSubscriptions.mutate()}>บันทึกเหตุการณ์แจ้งเตือน</Button>}
      <Typography variant="h6" fontWeight={900}>ประวัติการส่งข้อความ</Typography>
      {deliveries.isLoading ? <LoadingState message="กำลังโหลดประวัติการส่ง..." /> : deliveries.isError ? <Alert severity="error">โหลดประวัติการส่งไม่สำเร็จ</Alert> : (deliveries.data?.items.length ?? 0) === 0 ? <EmptyState message="ยังไม่มีประวัติการส่งสำหรับกลุ่มนี้" /> : <>
        <TableContainer><Table size="small"><TableHead><TableRow><TableCell>วันเวลา</TableCell><TableCell>เหตุการณ์</TableCell><TableCell>ผลการส่ง</TableCell><TableCell>จำนวนครั้ง</TableCell><TableCell>ข้อผิดพลาด</TableCell><TableCell>รหัสติดตาม</TableCell></TableRow></TableHead><TableBody>
          {deliveries.data!.items.map(item => <TableRow key={item.id}><TableCell>{formatThaiDateTime(item.createdAt)}</TableCell><TableCell>{eventLabels[item.eventType] ?? "เหตุการณ์ระบบรถ"}</TableCell><TableCell><Chip size="small" label={deliveryStatusLabel(item.status)} color={item.status === "Sent" ? "success" : item.status === "Failed" ? "error" : "default"} /></TableCell><TableCell>{item.attemptCount}</TableCell><TableCell>{item.errorCode ? "ส่งไม่สำเร็จ" : "-"}</TableCell><TableCell>{item.correlationId}</TableCell></TableRow>)}
        </TableBody></Table></TableContainer>
        <ListPagination page={deliveryPage} pageSize={20} totalItems={deliveries.data!.totalItems} onPageChange={setDeliveryPage} onPageSizeChange={() => undefined} pageSizeOptions={[20]} />
      </>}
    </Stack></CardContent></Card>}
    <ActionDialog open={Boolean(dialog)} title={dialogTitle} confirmLabel={dialog?.type === "test" ? "ส่งทดสอบ" : dialog?.type === "disable" ? "ยืนยันปิดใช้งาน" : "ยืนยันกลุ่ม"}
      confirmColor={dialog?.type === "disable" ? "error" : "primary"} isLoading={action.isPending}
      isConfirmDisabled={dialog?.type === "disable" && !reason.trim()} onClose={() => { setDialog(null); setReason(""); setMessage(""); }} onConfirm={() => action.mutate()}>
      <Stack spacing={2}><Typography>{dialog?.group.displayName}</Typography>
        {dialog?.type === "test" ? <TextField multiline minRows={3} label="ข้อความทดสอบ (ไม่บังคับ)" value={message} onChange={e => setMessage(e.target.value)} helperText="ห้ามใส่ข้อมูลผู้ป่วยหรือข้อมูลอ่อนไหว" /> :
          <TextField multiline minRows={2} required={dialog?.type === "disable"} label="เหตุผล/หมายเหตุ" value={reason} onChange={e => setReason(e.target.value)} />}
      </Stack>
    </ActionDialog>
    <Dialog open={Boolean(config)} onClose={() => !saveConfiguration.isPending && setConfig(null)} fullWidth maxWidth="sm">
      <DialogTitle>{config?.group ? "แก้ไขการเชื่อมต่อ LINE Group" : "เพิ่มการเชื่อมต่อ LINE Group"}</DialogTitle>
      <DialogContent dividers><Stack spacing={2} sx={{ pt: 0.5 }}>
        <Alert severity="info">Client Secret จะถูกเข้ารหัสก่อนบันทึกและไม่สามารถเรียกดูจากหน้าเว็บได้</Alert>
        <TextField required label="ชื่อกลุ่มที่แสดงในระบบ" placeholder="เช่น กลุ่มงานยานพาหนะ" value={config?.displayName ?? ""} onChange={e => setConfig(x => x ? { ...x, displayName: e.target.value } : x)} />
        <TextField required={!config?.group} label="LINE Group ID" placeholder="เช่น Cxxxxxxxxxxxxxxxx" value={config?.groupId ?? ""} onChange={e => setConfig(x => x ? { ...x, groupId: e.target.value } : x)} helperText={config?.group ? `ค่าปัจจุบัน ${config.group.groupIdMasked} — เว้นว่างหากไม่ต้องการเปลี่ยน` : "รหัสกลุ่มที่ปลายทางใช้ส่งข้อความ"} />
        <TextField required label="Endpoint URL" type="url" placeholder="https://notification.example.go.th/api/send" value={config?.endpointUrl ?? ""} onChange={e => setConfig(x => x ? { ...x, endpointUrl: e.target.value } : x)} helperText="Production ต้องเป็น HTTPS" />
        <TextField required label="Client ID" value={config?.clientId ?? ""} onChange={e => setConfig(x => x ? { ...x, clientId: e.target.value } : x)} autoComplete="off" />
        <TextField required={!config?.group || !config.group.hasClientSecret} label="Client Secret" type="password" value={config?.clientSecret ?? ""} onChange={e => setConfig(x => x ? { ...x, clientSecret: e.target.value } : x)} autoComplete="new-password" helperText={config?.group?.hasClientSecret ? "มี Secret บันทึกไว้แล้ว เว้นว่างเพื่อใช้ค่าเดิม" : "กรุณากรอก Secret จากผู้ให้บริการ"} />
        <Typography variant="caption" color="text.secondary">ระบบส่งข้อมูลแบบ JSON พร้อม header client-key และ secret-key ตามข้อกำหนดของ endpoint</Typography>
      </Stack></DialogContent>
      <DialogActions><Button onClick={() => setConfig(null)} disabled={saveConfiguration.isPending}>ยกเลิก</Button><Button variant="contained" onClick={() => saveConfiguration.mutate()} disabled={saveConfiguration.isPending || !config?.displayName.trim() || !config.endpointUrl.trim() || !config.clientId.trim() || (!config.group && (!config.groupId.trim() || !config.clientSecret.trim()))}>{config?.group ? "บันทึกการแก้ไข" : "เพิ่มการเชื่อมต่อ"}</Button></DialogActions>
    </Dialog>
  </>;
}

function Summary({ label, value }: { label: string; value: number }) { return <Card><CardContent><Typography color="text.secondary" variant="body2">{label}</Typography><Typography variant="h4" fontWeight={900}>{value}</Typography></CardContent></Card>; }
function StatusChip({ status }: { status: FleetLineGroup["status"] }) {
  return <Chip size="small" label={status === "Pending" ? "รอยืนยัน" : status === "Active" ? "ใช้งาน" : "ปิดใช้งาน"} color={status === "Active" ? "success" : status === "Pending" ? "warning" : "default"} />;
}
function deliveryStatusLabel(status: string) { return ({ Sent: "ส่งสำเร็จ", Failed: "ส่งไม่สำเร็จ", Retry: "รอส่งใหม่", Pending: "รอส่ง" } as Record<string, string>)[status] ?? "กำลังดำเนินการ"; }
