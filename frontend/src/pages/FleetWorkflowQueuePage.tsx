import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { FLEET_DASHBOARD_QUERY_KEY, fleetWorkflowAction, getFleetWorkflowQueue, type FleetQueueItem } from "../api/fleetApi";
import { EmptyState } from "../components/common/EmptyState";
import { ListPagination } from "../components/common/ListPagination";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";

type QueueKind = "admin-review" | "director-approval";
type WorkflowAction = "approve" | "return" | "reject";
type ActionDialogState = { row: FleetQueueItem; action: WorkflowAction } | null;

export function FleetWorkflowQueuePage({ kind }: { kind: QueueKind }) {
  const queryClient = useQueryClient();
  const [rows, setRows] = useState<FleetQueueItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [keyword, setKeyword] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [dialog, setDialog] = useState<ActionDialogState>(null);
  const [reason, setReason] = useState("");
  const [returnTarget, setReturnTarget] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await getFleetWorkflowQueue(kind);
      setRows(Array.isArray(result) ? result : []);
    } catch {
      setRows([]);
      setError("ไม่สามารถโหลดคิวได้");
    } finally {
      setLoading(false);
    }
  }, [kind]);

  useEffect(() => {
    setPage(1);
    void load();
  }, [load]);

  const statusOptions = useMemo(() => Array.from(new Set(rows.map((item) => item.status))).sort(), [rows]);
  const filteredRows = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    return rows.filter((item) => {
      const searchable = [item.requestNo, item.requesterName, item.requesterDepartmentName, item.purpose, item.missionType, item.destination, item.vehicle, item.driver, getFleetStatusLabel(item.status)]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return (!normalizedKeyword || searchable.includes(normalizedKeyword)) && (!status || item.status === status);
    });
  }, [keyword, rows, status]);
  const lastPage = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const safePage = Math.min(page, lastPage);
  const visibleRows = filteredRows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const openActionDialog = (row: FleetQueueItem, action: WorkflowAction) => {
    setReason("");
    setReturnTarget(action === "return" ? (kind === "admin-review" ? "REQUESTER" : "ADMIN_REVIEW") : "");
    setDialog({ row, action });
  };

  const submitAction = async () => {
    if (!dialog || busy) return;
    if (dialog.action !== "approve" && !reason.trim()) return;
    if (dialog.action === "return" && !returnTarget) return;
    setBusy(true);
    setError("");
    try {
      await fleetWorkflowAction(
        kind,
        dialog.row.id,
        dialog.action,
        dialog.row.concurrencyToken,
        dialog.action === "approve" ? undefined : reason.trim(),
        dialog.action === "return" ? returnTarget : undefined,
      );
      setDialog(null);
      await queryClient.invalidateQueries({ queryKey: FLEET_DASHBOARD_QUERY_KEY });
      await load();
    } catch {
      setDialog(null);
      setError("ดำเนินการไม่สำเร็จ ข้อมูลอาจมีการเปลี่ยนแปลง กรุณาโหลดใหม่");
    } finally {
      setBusy(false);
    }
  };

  const stageTitle = kind === "admin-review" ? "หัวหน้าฝ่ายบริหารตรวจสอบ" : "ผู้อำนวยการอนุมัติ";
  return (
    <Box>
      <PageHeader
        title="งานรอตรวจสอบและอนุมัติคำขอใช้รถ"
        subtitle={`${stageTitle} ตรวจสอบรายละเอียดและตัดสินใจคำขอที่อยู่ในขั้นตอนของคุณ`}
      />

      {error && <Alert severity="error" sx={{ mb: 2 }} action={<Button disabled={loading} onClick={() => void load()}>ลองใหม่</Button>}>{error}</Alert>}

      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 2 }}>
          <Grid container spacing={1.5} alignItems="center">
            <Grid item xs={12} md={7}>
              <TextField
                fullWidth
                size="small"
                label="ค้นหาคำขอ"
                placeholder="เลขคำขอ ผู้ขอ หรือปลายทาง"
                value={keyword}
                onChange={(event) => { setKeyword(event.target.value); setPage(1); }}
                InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment> }}
              />
            </Grid>
            <Grid item xs={12} sm={7} md={3}>
              <TextField select fullWidth size="small" label="สถานะคำขอ" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}>
                <MenuItem value="">ทุกสถานะ</MenuItem>
                {statusOptions.map((value) => <MenuItem key={value} value={value}>{getFleetStatusLabel(value)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={5} md={2}>
              <Button fullWidth variant="outlined" startIcon={<RefreshOutlinedIcon />} disabled={loading} onClick={() => void load()}>รีเฟรช</Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1} sx={{ mb: 2 }}>
            <Box>
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography variant="h6" color="primary" fontWeight={800}>{stageTitle}</Typography>
                <Chip size="small" color="warning" label={`${filteredRows.length.toLocaleString("th-TH")} รายการ`} />
              </Stack>
              <Typography variant="body2" color="text.secondary">แสดงเฉพาะคำขอที่รอการดำเนินการในขั้นตอนนี้</Typography>
            </Box>
            {(keyword || status) && <Button onClick={() => { setKeyword(""); setStatus(""); setPage(1); }}>ล้างตัวกรอง</Button>}
          </Stack>

          {loading ? (
            <LoadingState message="กำลังโหลดงานรอตรวจสอบและอนุมัติ..." />
          ) : visibleRows.length ? (
            <Stack spacing={1.5}>
              {visibleRows.map((row) => (
                <WorkflowQueueCard key={row.id} row={row} onAction={openActionDialog} />
              ))}
              <ListPagination
                page={safePage}
                pageSize={pageSize}
                totalItems={filteredRows.length}
                pageSizeOptions={[5, 10, 20, 50]}
                disabled={loading || busy}
                onPageChange={setPage}
                onPageSizeChange={(value) => { setPageSize(value); setPage(1); }}
              />
            </Stack>
          ) : (
            <EmptyState
              icon={CheckCircleOutlineIcon}
              iconColor={rows.length ? "disabled" : "success"}
              title={rows.length ? "ไม่พบคำขอตามเงื่อนไข" : "ดำเนินการครบแล้ว"}
              description={rows.length ? "ลองเปลี่ยนคำค้นหาหรือตัวกรองสถานะ" : "ไม่มีคำขอรอตรวจในขณะนี้"}
            />
          )}
        </CardContent>
      </Card>

      <WorkflowActionDialog
        state={dialog}
        kind={kind}
        reason={reason}
        returnTarget={returnTarget}
        busy={busy}
        onReasonChange={setReason}
        onReturnTargetChange={setReturnTarget}
        onClose={() => !busy && setDialog(null)}
        onConfirm={() => void submitAction()}
      />
    </Box>
  );
}

function WorkflowQueueCard({ row, onAction }: { row: FleetQueueItem; onAction: (row: FleetQueueItem, action: WorkflowAction) => void }) {
  return (
    <Card variant="outlined" sx={{ boxShadow: "none", borderLeft: "5px solid", borderLeftColor: "warning.main" }}>
      <CardContent sx={{ p: { xs: 1.5, md: 2 }, "&:last-child": { pb: { xs: 1.5, md: 2 } } }}>
        <Stack spacing={1.5}>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="h6" color="primary" fontWeight={900}>{row.requestNo}</Typography>
              <Chip size="small" variant="outlined" color="warning" label={getFleetStatusLabel(row.status)} />
            </Stack>
            <Typography variant="body2" color="text.secondary">ออกเดินทาง {formatThaiDateTime(row.departureAt)}</Typography>
          </Stack>
          <Grid container spacing={1.5}>
            <Grid item xs={12} sm={6} md={3}><Detail label="ผู้ขอ" value={row.requesterName || "-"} /></Grid>
            <Grid item xs={12} sm={6} md={3}><Detail label="หน่วยงาน" value={row.requesterDepartmentName || "-"} /></Grid>
            <Grid item xs={12} sm={6} md={3}><Detail label="ประเภทการเดินทาง" value={row.missionType || "-"} /></Grid>
            <Grid item xs={12} sm={6} md={3}><Detail label="ผู้ร่วมเดินทาง" value={`${(row.passengerCount ?? 0).toLocaleString("th-TH")} คน`} /></Grid>
            <Grid item xs={12} md={6}><Detail label="ภารกิจ/วัตถุประสงค์" value={row.purpose || "-"} /></Grid>
            <Grid item xs={12} md={3}><Detail label="ปลายทาง" value={row.destination || "-"} /></Grid>
            <Grid item xs={12} md={3}><Detail label="คาดว่าจะกลับ" value={formatThaiDateTime(row.expectedReturnAt)} /></Grid>
            <Grid item xs={12} sm={6}><Detail label="รถ" value={row.vehicle || "ยังไม่ได้จัดรถ"} /></Grid>
            <Grid item xs={12} sm={6}><Detail label="คนขับ" value={row.driver || "ยังไม่ได้จัดคนขับ"} /></Grid>
          </Grid>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1} justifyContent="flex-end">
            <Button component={Link} to={`/fleet/requests/${row.id}`} variant="text" startIcon={<VisibilityOutlinedIcon />}>ดูรายละเอียดคำขอ</Button>
            <Button variant="contained" onClick={() => onAction(row, "approve")}>อนุมัติและส่งต่อ</Button>
            <Button variant="outlined" color="warning" onClick={() => onAction(row, "return")}>ส่งกลับแก้ไข</Button>
            <Button variant="outlined" color="error" onClick={() => onAction(row, "reject")}>ไม่อนุมัติ</Button>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return <Box><Typography variant="caption" color="text.secondary" fontWeight={700}>{label}</Typography><Typography variant="body2" fontWeight={700}>{value}</Typography></Box>;
}

function WorkflowActionDialog({ state, kind, reason, returnTarget, busy, onReasonChange, onReturnTargetChange, onClose, onConfirm }: {
  state: ActionDialogState;
  kind: QueueKind;
  reason: string;
  returnTarget: string;
  busy: boolean;
  onReasonChange: (value: string) => void;
  onReturnTargetChange: (value: string) => void;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const action = state?.action;
  const title = action === "approve" ? "ยืนยันการอนุมัติ" : action === "return" ? "ส่งคำขอกลับแก้ไข" : "ยืนยันการไม่อนุมัติ";
  const returnOptions = kind === "admin-review"
    ? [{ value: "REQUESTER", label: "ส่งกลับผู้ขอ" }, { value: "DISPATCHER", label: "ส่งกลับงานยานพาหนะ" }]
    : [{ value: "ADMIN_REVIEW", label: "ส่งกลับหัวหน้าฝ่ายบริหาร" }, { value: "DISPATCHER", label: "ส่งกลับงานยานพาหนะ" }];
  return (
    <Dialog open={Boolean(state)} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Alert severity={action === "approve" ? "success" : action === "return" ? "warning" : "error"}>
            คำขอ {state?.row.requestNo} · {state?.row.requesterName}
          </Alert>
          {action === "return" && (
            <TextField select fullWidth label="ส่งกลับไปยัง" value={returnTarget} onChange={(event) => onReturnTargetChange(event.target.value)}>
              {returnOptions.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
            </TextField>
          )}
          {action !== "approve" && (
            <TextField fullWidth multiline minRows={3} required label="เหตุผล" value={reason} onChange={(event) => onReasonChange(event.target.value)} helperText="กรุณาระบุเหตุผลเพื่อให้ผู้รับดำเนินการต่อได้ถูกต้อง" />
          )}
          {action === "approve" && <Typography color="text.secondary">เมื่อยืนยัน ระบบจะส่งคำขอไปยังขั้นตอนถัดไปโดยอัตโนมัติ</Typography>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button disabled={busy} onClick={onClose}>ยกเลิก</Button>
        <Button
          variant="contained"
          color={action === "reject" ? "error" : action === "return" ? "warning" : "primary"}
          disabled={busy || (action !== "approve" && !reason.trim()) || (action === "return" && !returnTarget)}
          onClick={onConfirm}
        >
          {busy ? "กำลังดำเนินการ..." : "ยืนยัน"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
