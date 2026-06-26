import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import RestartAltOutlinedIcon from "@mui/icons-material/RestartAltOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import { Alert, Button, Card, CardContent, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle, Divider, FormControlLabel, Grid, IconButton, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Tooltip, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { getDepartments, getUsers } from "../api/adminApi";
import { adjustLeaveBalance, confirmLeaveBalanceRollover, createLeaveBalance, deleteLeaveBalance, downloadLeaveBalanceTemplate, getLeaveBalances, getLeaveTypes, previewLeaveBalanceRollover, rolloverLeaveBalances, updateLeaveBalance, type LeaveBalance, type LeaveBalanceRolloverPreview, type SaveLeaveBalanceRequest } from "../api/leaveApi";
import { EmptyState } from "../components/common/EmptyState";
import { FilterToolbar } from "../components/common/FilterToolbar";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

const currentYear = getCurrentFiscalYear();
const emptyForm: SaveLeaveBalanceRequest = {
  userId: "",
  leaveTypeId: "",
  year: currentYear,
  entitledDays: 0,
  carriedOverDays: 0,
  adjustedDays: 0,
  usedDays: 0,
  pendingDays: 0,
  notes: "",
};

export function LeaveBalanceManagementPage() {
  const queryClient = useQueryClient();
  const { showSuccess, showWarning } = useNotification();
  const [year, setYear] = useState(formatFiscalYear(currentYear).toString());
  const [userId, setUserId] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [leaveTypeId, setLeaveTypeId] = useState("");
  const [editing, setEditing] = useState<LeaveBalance | null>(null);
  const [form, setForm] = useState<SaveLeaveBalanceRequest>(emptyForm);
  const [rolloverBalance, setRolloverBalance] = useState<LeaveBalance | null>(null);
  const [rolloverPreview, setRolloverPreview] = useState<LeaveBalanceRolloverPreview | null>(null);
  const [rolloverReason, setRolloverReason] = useState("");
  const [rolloverEntitlement, setRolloverEntitlement] = useState(0);
  const [updateExistingCarriedOverOnly, setUpdateExistingCarriedOverOnly] = useState(false);
  const [adjustingBalance, setAdjustingBalance] = useState<LeaveBalance | null>(null);
  const [adjustmentDays, setAdjustmentDays] = useState(0);
  const [adjustmentReason, setAdjustmentReason] = useState("");

  const filters = useMemo(
    () => ({
      year: year ? parseFiscalYear(year) : undefined,
      userId: userId || undefined,
      departmentId: departmentId || undefined,
      leaveTypeId: leaveTypeId || undefined,
    }),
    [departmentId, leaveTypeId, userId, year],
  );

  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: balances = [], isLoading } = useQuery({
    queryKey: ["leave-balances", filters],
    queryFn: () => getLeaveBalances(filters),
  });

  const saveMutation = useMutation({
    mutationFn: (payload: SaveLeaveBalanceRequest) => (editing?.id ? updateLeaveBalance(editing.id, payload) : createLeaveBalance(payload)),
    onSuccess: () => {
      showSuccess("ปรับปรุงวันลาคงเหลือเรียบร้อยแล้ว");
      queryClient.invalidateQueries({ queryKey: ["leave-balances"] });
      closeDialog();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteLeaveBalance,
    onSuccess: () => {
      showSuccess("ลบข้อมูลวันลาคงเหลือเรียบร้อยแล้ว");
      queryClient.invalidateQueries({ queryKey: ["leave-balances"] });
    },
  });

  const rolloverMutation = useMutation({
    mutationFn: () => rolloverLeaveBalances(year ? parseFiscalYear(year) : currentYear),
    onSuccess: (result) => {
      showSuccess(`ยกยอดวันลาเรียบร้อยแล้ว สร้าง ${result.createdCount} รายการ ข้าม ${result.skippedCount} รายการ`);
      queryClient.invalidateQueries({ queryKey: ["leave-balances"] });
    },
  });

  const rolloverPreviewMutation = useMutation({
    mutationFn: previewLeaveBalanceRollover,
    onSuccess: (preview) => {
      setRolloverPreview(preview);
      setRolloverEntitlement(preview.newEntitlementDays);
      setRolloverReason(`ยกยอดวันลาปีงบประมาณ ${formatFiscalYear(preview.toFiscalYear)}`);
      setUpdateExistingCarriedOverOnly(false);
    },
  });

  const rolloverConfirmMutation = useMutation({
    mutationFn: () => {
      if (!rolloverBalance || !rolloverPreview) {
        throw new Error("Missing rollover preview.");
      }
      return confirmLeaveBalanceRollover(rolloverBalance.id!, {
        toFiscalYear: rolloverPreview.toFiscalYear,
        newEntitlementDays: rolloverEntitlement,
        reason: rolloverReason,
        updateExistingCarriedOverOnly,
      });
    },
    onSuccess: () => {
      showSuccess("ยืนยันการยกยอดวันลาเรียบร้อยแล้ว");
      queryClient.invalidateQueries({ queryKey: ["leave-balances"] });
      closeRolloverDialog();
    },
  });

  const adjustMutation = useMutation({
    mutationFn: () => {
      if (!adjustingBalance?.id) {
        throw new Error("Missing leave balance.");
      }
      return adjustLeaveBalance(adjustingBalance.id, { adjustmentDays, reason: adjustmentReason });
    },
    onSuccess: () => {
      showSuccess("ปรับยอดวันลาเรียบร้อยแล้ว");
      queryClient.invalidateQueries({ queryKey: ["leave-balances"] });
      closeAdjustDialog();
    },
  });

  function openCreate() {
    setEditing(null);
    setForm({ ...emptyForm, year: year ? parseFiscalYear(year) : currentYear });
  }

  function openEdit(row: LeaveBalance) {
    setEditing(row);
    setForm({
      userId: row.userId,
      leaveTypeId: row.leaveTypeId,
      year: row.year,
      entitledDays: row.entitledDays,
      usedDays: row.usedDays,
      pendingDays: row.pendingDays,
      carriedOverDays: row.carriedOverDays,
      adjustedDays: row.adjustedDays,
      notes: row.notes ?? "",
    });
  }

  function closeDialog() {
    setEditing(null);
    setForm(emptyForm);
  }

  function openRollover(row: LeaveBalance) {
    if (!row.id) {
      return;
    }
    setRolloverBalance(row);
    setRolloverPreview(null);
    rolloverPreviewMutation.mutate(row.id, {
      onError: () => showWarning("ไม่สามารถยกยอดประเภทลานี้ได้"),
    });
  }

  function canCarryOver(row: LeaveBalance) {
    return Boolean(leaveTypes.find((leaveType) => leaveType.id === row.leaveTypeId)?.allowCarryOver);
  }

  function closeRolloverDialog() {
    setRolloverBalance(null);
    setRolloverPreview(null);
    setRolloverReason("");
    setRolloverEntitlement(0);
    setUpdateExistingCarriedOverOnly(false);
  }

  function openAdjust(row: LeaveBalance) {
    setAdjustingBalance(row);
    setAdjustmentDays(0);
    setAdjustmentReason("");
  }

  function closeAdjustDialog() {
    setAdjustingBalance(null);
    setAdjustmentDays(0);
    setAdjustmentReason("");
  }

  async function downloadTemplate() {
    const blob = await downloadLeaveBalanceTemplate();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "leave-balance-import-template.xlsx";
    link.click();
    URL.revokeObjectURL(url);
    showSuccess("ดาวน์โหลด Template เรียบร้อยแล้ว");
  }

  return (
    <>
      <PageHeader title="จัดการวันลาคงเหลือ" subtitle="กำหนดสิทธิ์วันลาตามปีงบประมาณ ยอดยกมา ใช้ไป รออนุมัติ และดาวน์โหลด template สำหรับนำเข้า Excel" />
      <Stack spacing={2}>
        <FilterToolbar>
          <Grid item xs={12} md={2}>
            <TextField size="small" label="ปีงบประมาณ (พ.ศ.)" type="number" value={year} onChange={(event) => setYear(event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField select size="small" label="ผู้ใช้งาน" value={userId} onChange={(event) => setUserId(event.target.value)} fullWidth>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {users.map((user) => (
                <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField select size="small" label="หน่วยงาน" value={departmentId} onChange={(event) => setDepartmentId(event.target.value)} fullWidth>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {departments.map((department) => (
                <MenuItem key={department.id} value={department.id}>{department.name}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField select size="small" label="ประเภทลา" value={leaveTypeId} onChange={(event) => setLeaveTypeId(event.target.value)} fullWidth>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {leaveTypes.map((leaveType) => (
                <MenuItem key={leaveType.id} value={leaveType.id}>{getLeaveTypeLabel(leaveType.name)}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} md={12}>
            <Stack direction="row" spacing={1} justifyContent="flex-end" flexWrap="wrap" useFlexGap>
              <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={downloadTemplate}>ดาวน์โหลด Template</Button>
              <Button variant="outlined" onClick={() => rolloverMutation.mutate()} disabled={rolloverMutation.isPending}>
                ยกยอดปีงบประมาณ
              </Button>
              <Button variant="contained" startIcon={<AddOutlinedIcon />} onClick={openCreate}>เพิ่มยอดวันลา</Button>
            </Stack>
          </Grid>
        </FilterToolbar>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ผู้ใช้งาน</TableCell>
                  <TableCell>หน่วยงาน</TableCell>
                  <TableCell>ประเภทลา</TableCell>
                  <TableCell>ปีงบประมาณ</TableCell>
                  <TableCell>สิทธิ์ประจำปี</TableCell>
                  <TableCell>ยกมาจากปีก่อน</TableCell>
                  <TableCell>ปรับปรุง</TableCell>
                  <TableCell>ใช้ไป</TableCell>
                  <TableCell>รออนุมัติ</TableCell>
                  <TableCell>คงเหลือใช้ได้</TableCell>
                  <TableCell align="right">จัดการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={11}>กำลังโหลดข้อมูลวันลา...</TableCell></TableRow>
                ) : balances.length ? balances.map((row) => (
                  <TableRow key={row.id ?? `${row.userId}-${row.leaveTypeId}-${row.year}`} hover>
                    <TableCell>{row.fullname ?? "-"}</TableCell>
                    <TableCell>{row.departmentName ?? "-"}</TableCell>
                    <TableCell>{getLeaveTypeLabel(row.leaveTypeName)}</TableCell>
                    <TableCell>{formatFiscalYear(row.year)}</TableCell>
                    <TableCell>{row.entitledDays}</TableCell>
                    <TableCell>{row.carriedOverDays}</TableCell>
                    <TableCell>{row.adjustedDays}</TableCell>
                    <TableCell>{row.usedDays}</TableCell>
                    <TableCell>{row.pendingDays}</TableCell>
                    <TableCell>{row.availableDays}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="ดูรายละเอียดยอดวันลา">
                        <IconButton size="small" onClick={() => openEdit(row)} disabled={!row.id}><InfoOutlinedIcon fontSize="small" /></IconButton>
                      </Tooltip>
                      <Tooltip title="แก้ไขยอดวันลา">
                        <IconButton size="small" onClick={() => openEdit(row)} disabled={!row.id}><EditOutlinedIcon fontSize="small" /></IconButton>
                      </Tooltip>
                      <Tooltip title="ปรับยอดวันลา">
                        <IconButton size="small" onClick={() => openAdjust(row)} disabled={!row.id}><TuneOutlinedIcon fontSize="small" /></IconButton>
                      </Tooltip>
                      <Tooltip title={canCarryOver(row) ? "ยกยอดรายคน" : "ประเภทลานี้ไม่รองรับการยกยอด"}>
                        <span>
                          <IconButton size="small" onClick={() => openRollover(row)} disabled={!row.id || !canCarryOver(row)}><RestartAltOutlinedIcon fontSize="small" /></IconButton>
                        </span>
                      </Tooltip>
                      <Tooltip title="ลบยอดวันลา">
                        <IconButton size="small" color="error" onClick={() => row.id && deleteMutation.mutate(row.id)} disabled={!row.id}><DeleteOutlineIcon fontSize="small" /></IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                )) : (
                  <TableRow>
                    <TableCell colSpan={11}>
                      <EmptyState message="ไม่พบยอดวันลา ยังไม่มีข้อมูลตามตัวกรองที่เลือก" />
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>

      <Dialog open={Boolean(editing) || form !== emptyForm} onClose={closeDialog} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? "แก้ไขยอดวันลา" : "เพิ่มยอดวันลา"}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField select label="ผู้ใช้งาน" value={form.userId} onChange={(event) => setForm({ ...form, userId: event.target.value })} fullWidth>
              {users.map((user) => <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>)}
            </TextField>
            <TextField select label="ประเภทลา" value={form.leaveTypeId} onChange={(event) => setForm({ ...form, leaveTypeId: event.target.value })} fullWidth>
              {leaveTypes.map((leaveType) => <MenuItem key={leaveType.id} value={leaveType.id}>{getLeaveTypeLabel(leaveType.name)}</MenuItem>)}
            </TextField>
            <TextField label="ปีงบประมาณ (พ.ศ.)" type="number" value={formatFiscalYear(form.year)} onChange={(event) => setForm({ ...form, year: parseFiscalYear(event.target.value) })} fullWidth />
            <Grid container spacing={2}>
              <Grid item xs={12} sm={3}><TextField label="สิทธิ์ประจำปี" type="number" value={form.entitledDays} onChange={(event) => setForm({ ...form, entitledDays: Number(event.target.value) })} fullWidth /></Grid>
              <Grid item xs={12} sm={3}><TextField label="ยกมาจากปีก่อน" type="number" value={form.carriedOverDays} onChange={(event) => setForm({ ...form, carriedOverDays: Number(event.target.value) })} fullWidth /></Grid>
              <Grid item xs={12} sm={3}><TextField label="ปรับปรุง" type="number" value={form.adjustedDays} onChange={(event) => setForm({ ...form, adjustedDays: Number(event.target.value) })} fullWidth /></Grid>
              <Grid item xs={12} sm={3}><TextField label="ใช้ไป" type="number" value={form.usedDays} onChange={(event) => setForm({ ...form, usedDays: Number(event.target.value) })} fullWidth /></Grid>
              <Grid item xs={12} sm={3}><TextField label="รออนุมัติ" type="number" value={form.pendingDays} onChange={(event) => setForm({ ...form, pendingDays: Number(event.target.value) })} fullWidth /></Grid>
            </Grid>
            <TextField label="หมายเหตุ" value={form.notes ?? ""} onChange={(event) => setForm({ ...form, notes: event.target.value })} multiline minRows={2} fullWidth />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog}>ยกเลิก</Button>
          <Button variant="contained" onClick={() => saveMutation.mutate(form)} disabled={!form.userId || !form.leaveTypeId || saveMutation.isPending}>
            บันทึก
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={Boolean(rolloverBalance)} onClose={closeRolloverDialog} fullWidth maxWidth="md">
        <DialogTitle>ตรวจสอบการยกยอดวันลา</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Alert severity="info">กรุณาตรวจสอบข้อมูลก่อนยืนยัน การยกยอดจะสร้างยอดวันลาของปีงบประมาณใหม่</Alert>
            {!rolloverPreview ? (
              <Typography color="text.secondary">กำลังคำนวณตัวอย่างการยกยอด...</Typography>
            ) : (
              <>
                {rolloverPreview.warnings.map((warning) => (
                  <Alert key={warning} severity="warning">{warning}</Alert>
                ))}
                <Grid container spacing={2}>
                  <PreviewField label="ผู้ใช้งาน" value={rolloverPreview.userName ?? "-"} />
                  <PreviewField label="ประเภทลา" value={getLeaveTypeLabel(rolloverPreview.leaveTypeName)} />
                  <PreviewField label="ปีงบประมาณต้นทาง" value={formatFiscalYear(rolloverPreview.fromFiscalYear)} />
                  <PreviewField label="ปีงบประมาณปลายทาง" value={formatFiscalYear(rolloverPreview.toFiscalYear)} />
                  <PreviewField label="สิทธิ์ประจำปีเดิม" value={rolloverPreview.entitlementDays} />
                  <PreviewField label="ยอดยกมาปีเดิม" value={rolloverPreview.carriedOverDays} />
                  <PreviewField label="ปรับปรุง" value={rolloverPreview.adjustedDays} />
                  <PreviewField label="ใช้ไปแล้ว" value={rolloverPreview.usedDays} />
                  <PreviewField label="รออนุมัติ" value={rolloverPreview.pendingDays} />
                  <PreviewField label="คงเหลือปลายปี" value={rolloverPreview.endYearRemaining} />
                  <PreviewField label="ยกยอดได้" value={rolloverPreview.carryOverDays} />
                  <PreviewField label="ยอดถูกตัดออก" value={rolloverPreview.forfeitedDays} />
                </Grid>
                <Divider />
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <TextField label="สิทธิ์ประจำปีใหม่" type="number" value={rolloverEntitlement} onChange={(event) => setRolloverEntitlement(Number(event.target.value))} fullWidth />
                  </Grid>
                  <PreviewField label="ยอดรวมปีใหม่หลังยกยอด" value={rolloverEntitlement + rolloverPreview.carryOverDays} />
                  {rolloverPreview.targetBalanceExists && (
                    <Grid item xs={12}>
                      <FormControlLabel
                        control={<Checkbox checked={updateExistingCarriedOverOnly} onChange={(event) => setUpdateExistingCarriedOverOnly(event.target.checked)} />}
                        label="อัปเดตเฉพาะยอดยกมาในปีงบประมาณปลายทาง"
                      />
                    </Grid>
                  )}
                  <Grid item xs={12}>
                    <TextField label="เหตุผล/หมายเหตุ" value={rolloverReason} onChange={(event) => setRolloverReason(event.target.value)} multiline minRows={2} fullWidth required />
                  </Grid>
                </Grid>
              </>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeRolloverDialog}>ยกเลิก</Button>
          <Button
            variant="contained"
            onClick={() => rolloverConfirmMutation.mutate()}
            disabled={!rolloverPreview || !rolloverReason.trim() || rolloverConfirmMutation.isPending || (rolloverPreview.targetBalanceExists && !updateExistingCarriedOverOnly)}
          >
            ยืนยันการยกยอด
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={Boolean(adjustingBalance)} onClose={closeAdjustDialog} fullWidth maxWidth="sm">
        <DialogTitle>ปรับยอดวันลา</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Typography color="text.secondary">
              {adjustingBalance?.fullname ?? "-"} · {adjustingBalance ? getLeaveTypeLabel(adjustingBalance.leaveTypeName) : "-"} · ปีงบประมาณ {adjustingBalance ? formatFiscalYear(adjustingBalance.year) : "-"}
            </Typography>
            <TextField label="จำนวนวันที่ปรับปรุง" type="number" value={adjustmentDays} onChange={(event) => setAdjustmentDays(Number(event.target.value))} helperText="ใส่ค่าบวกเพื่อเพิ่มวันลา หรือค่าลบเพื่อลดยอด" fullWidth />
            <TextField label="เหตุผลการปรับยอด" value={adjustmentReason} onChange={(event) => setAdjustmentReason(event.target.value)} multiline minRows={3} fullWidth required />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeAdjustDialog}>ยกเลิก</Button>
          <Button variant="contained" onClick={() => adjustMutation.mutate()} disabled={!adjustmentReason.trim() || adjustMutation.isPending}>บันทึกการปรับยอด</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function getCurrentFiscalYear() {
  const now = new Date();
  return now.getMonth() >= 9 ? now.getFullYear() + 1 : now.getFullYear();
}

function formatFiscalYear(year: number) {
  return year > 2400 ? year : year + 543;
}

function parseFiscalYear(value: string) {
  const year = Number(value);
  if (!Number.isFinite(year)) {
    return getCurrentFiscalYear();
  }

  return year > 2400 ? year - 543 : year;
}

function PreviewField({ label, value }: { label: string; value: string | number }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Stack spacing={0.5}>
        <Typography variant="caption" color="text.secondary">{label}</Typography>
        <Typography fontWeight={700}>{value}</Typography>
      </Stack>
    </Grid>
  );
}
