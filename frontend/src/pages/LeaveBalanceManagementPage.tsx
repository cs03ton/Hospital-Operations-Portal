import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { Button, Card, CardContent, Dialog, DialogActions, DialogContent, DialogTitle, Grid, IconButton, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Tooltip } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { getUsers } from "../api/adminApi";
import { createLeaveBalance, deleteLeaveBalance, downloadLeaveBalanceTemplate, getLeaveBalances, getLeaveTypes, updateLeaveBalance, type LeaveBalance, type SaveLeaveBalanceRequest } from "../api/leaveApi";
import { EmptyState } from "../components/common/EmptyState";
import { FilterToolbar } from "../components/common/FilterToolbar";
import { PageHeader } from "../components/PageHeader";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

const currentYear = new Date().getFullYear();
const emptyForm: SaveLeaveBalanceRequest = {
  userId: "",
  leaveTypeId: "",
  year: currentYear,
  entitledDays: 0,
  usedDays: 0,
  pendingDays: 0,
};

export function LeaveBalanceManagementPage() {
  const queryClient = useQueryClient();
  const [year, setYear] = useState(currentYear.toString());
  const [userId, setUserId] = useState("");
  const [leaveTypeId, setLeaveTypeId] = useState("");
  const [editing, setEditing] = useState<LeaveBalance | null>(null);
  const [form, setForm] = useState<SaveLeaveBalanceRequest>(emptyForm);

  const filters = useMemo(
    () => ({
      year: year ? Number(year) : undefined,
      userId: userId || undefined,
      leaveTypeId: leaveTypeId || undefined,
    }),
    [leaveTypeId, userId, year],
  );

  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: balances = [], isLoading } = useQuery({
    queryKey: ["leave-balances", filters],
    queryFn: () => getLeaveBalances(filters),
  });

  const saveMutation = useMutation({
    mutationFn: (payload: SaveLeaveBalanceRequest) => (editing?.id ? updateLeaveBalance(editing.id, payload) : createLeaveBalance(payload)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leave-balances"] });
      closeDialog();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteLeaveBalance,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leave-balances"] }),
  });

  function openCreate() {
    setEditing(null);
    setForm({ ...emptyForm, year: year ? Number(year) : currentYear });
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
    });
  }

  function closeDialog() {
    setEditing(null);
    setForm(emptyForm);
  }

  async function downloadTemplate() {
    const blob = await downloadLeaveBalanceTemplate();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "leave-balance-import-template.xlsx";
    link.click();
    URL.revokeObjectURL(url);
  }

  return (
    <>
      <PageHeader title="จัดการวันลาคงเหลือ" subtitle="กำหนดสิทธิ์วันลา ใช้ไป รออนุมัติ และดาวน์โหลด template สำหรับนำเข้า Excel" />
      <Stack spacing={2}>
        <FilterToolbar>
          <Grid item xs={12} md={2}>
            <TextField size="small" label="ปี" type="number" value={year} onChange={(event) => setYear(event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField select size="small" label="ผู้ใช้งาน" value={userId} onChange={(event) => setUserId(event.target.value)} fullWidth>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {users.map((user) => (
                <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>
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
          <Grid item xs={12} md={3}>
            <Stack direction="row" spacing={1} justifyContent="flex-end" flexWrap="wrap" useFlexGap>
              <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={downloadTemplate}>ดาวน์โหลด Template</Button>
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
                  <TableCell>ประเภทลา</TableCell>
                  <TableCell>ปี</TableCell>
                  <TableCell>สิทธิ์ทั้งหมด</TableCell>
                  <TableCell>ใช้ไป</TableCell>
                  <TableCell>รออนุมัติ</TableCell>
                  <TableCell>คงเหลือ</TableCell>
                  <TableCell align="right">จัดการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={8}>กำลังโหลดข้อมูลวันลา...</TableCell></TableRow>
                ) : balances.length ? balances.map((row) => (
                  <TableRow key={row.id ?? `${row.userId}-${row.leaveTypeId}-${row.year}`} hover>
                    <TableCell>{row.fullname ?? "-"}</TableCell>
                    <TableCell>{getLeaveTypeLabel(row.leaveTypeName)}</TableCell>
                    <TableCell>{row.year}</TableCell>
                    <TableCell>{row.entitledDays}</TableCell>
                    <TableCell>{row.usedDays}</TableCell>
                    <TableCell>{row.pendingDays}</TableCell>
                    <TableCell>{row.remainingDays}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="แก้ไขยอดวันลา">
                        <IconButton size="small" onClick={() => openEdit(row)} disabled={!row.id}><EditOutlinedIcon fontSize="small" /></IconButton>
                      </Tooltip>
                      <Tooltip title="ลบยอดวันลา">
                        <IconButton size="small" color="error" onClick={() => row.id && deleteMutation.mutate(row.id)} disabled={!row.id}><DeleteOutlineIcon fontSize="small" /></IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                )) : (
                  <TableRow>
                    <TableCell colSpan={8}>
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
            <TextField label="ปี" type="number" value={form.year} onChange={(event) => setForm({ ...form, year: Number(event.target.value) })} fullWidth />
            <Grid container spacing={2}>
              <Grid item xs={12} sm={4}><TextField label="สิทธิ์ทั้งหมด" type="number" value={form.entitledDays} onChange={(event) => setForm({ ...form, entitledDays: Number(event.target.value) })} fullWidth /></Grid>
              <Grid item xs={12} sm={4}><TextField label="ใช้ไป" type="number" value={form.usedDays} onChange={(event) => setForm({ ...form, usedDays: Number(event.target.value) })} fullWidth /></Grid>
              <Grid item xs={12} sm={4}><TextField label="รออนุมัติ" type="number" value={form.pendingDays} onChange={(event) => setForm({ ...form, pendingDays: Number(event.target.value) })} fullWidth /></Grid>
            </Grid>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog}>ยกเลิก</Button>
          <Button variant="contained" onClick={() => saveMutation.mutate(form)} disabled={!form.userId || !form.leaveTypeId || saveMutation.isPending}>
            บันทึก
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
