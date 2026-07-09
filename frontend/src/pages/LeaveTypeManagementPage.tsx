import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import {
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Grid,
  IconButton,
  Stack,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { createLeaveType, deactivateLeaveType, getLeaveTypes, updateLeaveType, type LeaveType, type SaveLeaveTypeRequest } from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { DataTableCard } from "../components/common/DataTableCard";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { PermissionGuard } from "../context/PermissionContext";
import { useSaveFeedback } from "../hooks/useSaveFeedback";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveTypeManagementPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { showSaveError, showSuccessAndRedirect } = useSaveFeedback();
  const [editing, setEditing] = useState<LeaveType | null>(null);
  const [deleting, setDeleting] = useState<LeaveType | null>(null);
  const canSeeManageColumn = user?.role === "Admin" || user?.role === "SuperAdmin";
  const { data = [], isLoading } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const activeLeaveTypes = data.filter((item) => item.isActive);
  const { control, register, handleSubmit, reset, formState: { errors } } = useForm<SaveLeaveTypeRequest>({
    defaultValues: getEmptyLeaveTypeForm(),
  });

  const saveMutation = useMutation({
    mutationFn: (payload: SaveLeaveTypeRequest) => editing ? updateLeaveType(editing.id, payload) : createLeaveType(payload),
    onSuccess: async () => {
      showSuccessAndRedirect({ successMessage: editing ? "แก้ไขประเภทการลาสำเร็จ" : "เพิ่มประเภทการลาสำเร็จ" });
      setEditing(null);
      reset(getEmptyLeaveTypeForm());
      await queryClient.invalidateQueries({ queryKey: ["leave-types"] });
    },
    onError: (error: unknown) => showSaveError(error),
  });
  const deleteMutation = useMutation({
    mutationFn: deactivateLeaveType,
    onSuccess: () => {
      showSuccessAndRedirect({ successMessage: "ลบประเภทการลาเรียบร้อยแล้ว" });
      setDeleting(null);
      queryClient.invalidateQueries({ queryKey: ["leave-types"] });
    },
    onError: (error: unknown) => showSaveError(error, "ไม่สามารถลบประเภทการลาได้"),
  });

  function onEdit(item: LeaveType) {
    setEditing(item);
    reset(item);
  }

  return (
    <>
      <PageHeader title="จัดการประเภทการลา" subtitle="กำหนดประเภทการลา สิทธิ์ตั้งต้น และเงื่อนไขไฟล์แนบ" />
      <Stack spacing={2}>
        <PermissionGuard permission="LeaveAdmin.ManageTypes">
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>{editing ? "แก้ไขประเภทการลา" : "เพิ่มประเภทการลา"}</Typography>
              <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => saveMutation.mutate({ ...values, defaultDaysPerYear: Number(values.defaultDaysPerYear), carryOverMaxDays: Number(values.carryOverMaxDays) }))}>
                <Grid container spacing={1.5}>
                  <Grid item xs={12} md={3}>
                    <TextField fullWidth size="small" label="รหัส" InputLabelProps={{ shrink: true }} error={Boolean(errors.code)} helperText={errors.code?.message} {...register("code", { required: "กรุณากรอกรหัส" })} />
                  </Grid>
                  <Grid item xs={12} md={3}>
                    <TextField fullWidth size="small" label="ชื่อประเภทการลา" InputLabelProps={{ shrink: true }} error={Boolean(errors.name)} helperText={errors.name?.message} {...register("name", { required: "กรุณากรอกชื่อประเภทการลา" })} />
                  </Grid>
                  <Grid item xs={12} md={3}>
                    <TextField fullWidth size="small" label="รายละเอียด" InputLabelProps={{ shrink: true }} {...register("description")} />
                  </Grid>
                  <Grid item xs={12} md={3}>
                    <TextField fullWidth size="small" type="number" label="สิทธิ์ตั้งต้นต่อปี" InputLabelProps={{ shrink: true }} {...register("defaultDaysPerYear")} />
                  </Grid>
                </Grid>
                <Controller name="requiresBalance" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="ใช้โควตาวันลา" />} />
                <Controller name="useFiscalYear" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="ใช้ปีงบประมาณ (1 ต.ค. - 30 ก.ย.)" />} />
                <Controller name="allowCarryOver" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="อนุญาตให้ยกยอดจากปีก่อน" />} />
                <Grid container spacing={1.5}>
                  <Grid item xs={12} md={3}>
                    <TextField fullWidth size="small" type="number" label="ยกยอดสูงสุด (วัน)" InputLabelProps={{ shrink: true }} {...register("carryOverMaxDays")} />
                  </Grid>
                </Grid>
                <Controller name="requiresAttachment" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="ต้องแนบไฟล์" />} />
                <Controller name="isPaid" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="ได้รับค่าจ้าง" />} />
                <Controller name="isActive" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="เปิดใช้งาน" />} />
                <Stack direction="row" spacing={1.5}>
                  <Button type="submit" variant="contained" startIcon={<AddOutlinedIcon />} disabled={saveMutation.isPending}>บันทึกข้อมูล</Button>
                  {editing && <Button variant="outlined" onClick={() => { setEditing(null); reset(getEmptyLeaveTypeForm()); }}>ยกเลิก</Button>}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </PermissionGuard>

        <DataTableCard>
              <TableHead>
                <TableRow>
                  <TableCell>รหัส</TableCell>
                  <TableCell>ชื่อ</TableCell>
                  <TableCell>สิทธิ์ต่อปี</TableCell>
                  <TableCell>โควตา</TableCell>
                  <TableCell>ปีงบประมาณ</TableCell>
                  <TableCell>ยกยอด</TableCell>
                  <TableCell>ไฟล์แนบ</TableCell>
                  <TableCell>สถานะ</TableCell>
                  {canSeeManageColumn && <TableCell align="right">จัดการ</TableCell>}
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={canSeeManageColumn ? 9 : 8}>กำลังโหลดประเภทการลา...</TableCell></TableRow>
                ) : activeLeaveTypes.length ? activeLeaveTypes.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.code}</TableCell>
                    <TableCell>{getLeaveTypeLabel(item.name || item.code)}</TableCell>
                    <TableCell>{item.defaultDaysPerYear}</TableCell>
                    <TableCell><Chip size="small" label={item.requiresBalance ? "ใช้โควตา" : "ไม่ใช้โควตา"} color={item.requiresBalance ? "success" : "default"} /></TableCell>
                    <TableCell><Chip size="small" label={item.useFiscalYear ? "ใช้ปีงบ" : "ใช้ปีปฏิทิน"} color={item.useFiscalYear ? "success" : "default"} /></TableCell>
                    <TableCell><Chip size="small" label={item.allowCarryOver ? `ได้ไม่เกิน ${item.carryOverMaxDays} วัน` : "ไม่ยกยอด"} color={item.allowCarryOver ? "warning" : "default"} /></TableCell>
                    <TableCell><Chip size="small" label={item.requiresAttachment ? "ต้องแนบ" : "ไม่บังคับ"} color={item.requiresAttachment ? "warning" : "default"} /></TableCell>
                    <TableCell><Chip size="small" label={item.isActive ? "ใช้งาน" : "ปิดใช้งาน"} color={item.isActive ? "success" : "default"} /></TableCell>
                    {canSeeManageColumn && (
                      <TableCell align="right">
                        <PermissionGuard permission="LeaveAdmin.ManageTypes">
                          <ActionTooltip title="แก้ไขประเภทการลา">
                            <IconButton aria-label="แก้ไขประเภทการลา" onClick={() => onEdit(item)}><EditOutlinedIcon /></IconButton>
                          </ActionTooltip>
                          <ActionTooltip title="ลบประเภทการลา">
                            <IconButton
                              aria-label="ลบประเภทการลา"
                              color="error"
                              disabled={deleteMutation.isPending}
                              onClick={() => setDeleting(item)}
                            >
                              <DeleteOutlineOutlinedIcon />
                            </IconButton>
                          </ActionTooltip>
                        </PermissionGuard>
                      </TableCell>
                    )}
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={canSeeManageColumn ? 9 : 8}>ยังไม่มีประเภทการลา</TableCell></TableRow>
                )}
              </TableBody>
        </DataTableCard>
      </Stack>
      <Dialog open={Boolean(deleting)} onClose={() => setDeleting(null)} fullWidth maxWidth="xs">
        <DialogTitle>ยืนยันการลบประเภทการลา</DialogTitle>
        <DialogContent>
          <Stack spacing={1}>
            <Typography>
              ต้องการลบประเภทการลา “{deleting ? getLeaveTypeLabel(deleting.name || deleting.code) : ""}” ใช่หรือไม่?
            </Typography>
            <Typography variant="body2" color="text.secondary">
              ระบบจะซ่อนประเภทการลานี้จากหน้าจัดการและการเลือกใช้งานใหม่ แต่ยังเก็บประวัติคำขอลาเดิมไว้
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleting(null)}>ยกเลิก</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!deleting || deleteMutation.isPending}
            onClick={() => deleting && deleteMutation.mutate(deleting.id)}
          >
            ลบประเภทการลา
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function getEmptyLeaveTypeForm(): SaveLeaveTypeRequest {
  return {
    code: "",
    name: "",
    description: "",
    defaultDaysPerYear: 0,
    requiresBalance: true,
    allowCarryOver: false,
    carryOverMaxDays: 30,
    useFiscalYear: true,
    requiresAttachment: false,
    isPaid: true,
    isActive: true,
  };
}
