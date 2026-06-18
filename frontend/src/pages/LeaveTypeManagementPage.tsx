import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { Alert, Button, Card, CardContent, Checkbox, FormControlLabel, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { createLeaveType, deactivateLeaveType, getLeaveTypes, updateLeaveType, type LeaveType, type SaveLeaveTypeRequest } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

export function LeaveTypeManagementPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<LeaveType | null>(null);
  const { data = [], isLoading } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { control, register, handleSubmit, reset, formState: { errors } } = useForm<SaveLeaveTypeRequest>({
    defaultValues: { code: "", name: "", description: "", defaultDaysPerYear: 0, requiresAttachment: false, isPaid: true, isActive: true },
  });

  const saveMutation = useMutation({
    mutationFn: (payload: SaveLeaveTypeRequest) => editing ? updateLeaveType(editing.id, payload) : createLeaveType(payload),
    onSuccess: async () => {
      setEditing(null);
      reset({ code: "", name: "", description: "", defaultDaysPerYear: 0, requiresAttachment: false, isPaid: true, isActive: true });
      await queryClient.invalidateQueries({ queryKey: ["leave-types"] });
    },
  });
  const deleteMutation = useMutation({ mutationFn: deactivateLeaveType, onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leave-types"] }) });

  function onEdit(item: LeaveType) {
    setEditing(item);
    reset(item);
  }

  return (
    <>
      <PageHeader title="จัดการประเภทการลา" subtitle="กำหนดประเภทการลา สิทธิ์ตั้งต้น และเงื่อนไขไฟล์แนบ" />
      <Stack spacing={2}>
        <PermissionGuard permission="LeaveManagement.Manage">
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>{editing ? "แก้ไขประเภทการลา" : "เพิ่มประเภทการลา"}</Typography>
              <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => saveMutation.mutate({ ...values, defaultDaysPerYear: Number(values.defaultDaysPerYear) }))}>
                {saveMutation.isError && <Alert severity="error">บันทึกประเภทการลาไม่สำเร็จ</Alert>}
                <TextField label="รหัส" error={Boolean(errors.code)} helperText={errors.code?.message} {...register("code", { required: "กรุณากรอกรหัส" })} />
                <TextField label="ชื่อประเภทการลา" error={Boolean(errors.name)} helperText={errors.name?.message} {...register("name", { required: "กรุณากรอกชื่อประเภทการลา" })} />
                <TextField label="รายละเอียด" {...register("description")} />
                <TextField type="number" label="สิทธิ์ตั้งต้นต่อปี" {...register("defaultDaysPerYear")} />
                <Controller name="requiresAttachment" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="ต้องแนบไฟล์" />} />
                <Controller name="isPaid" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="ได้รับค่าจ้าง" />} />
                <Controller name="isActive" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />} label="เปิดใช้งาน" />} />
                <Stack direction="row" spacing={1.5}>
                  <Button type="submit" variant="contained" disabled={saveMutation.isPending}>บันทึกข้อมูล</Button>
                  {editing && <Button variant="outlined" onClick={() => { setEditing(null); reset({ code: "", name: "", description: "", defaultDaysPerYear: 0, requiresAttachment: false, isPaid: true, isActive: true }); }}>ยกเลิก</Button>}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </PermissionGuard>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>รหัส</TableCell>
                  <TableCell>ชื่อ</TableCell>
                  <TableCell>สิทธิ์ต่อปี</TableCell>
                  <TableCell>ไฟล์แนบ</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell align="right">จัดการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={6}>กำลังโหลดประเภทการลา...</TableCell></TableRow>
                ) : data.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.code}</TableCell>
                    <TableCell>{item.name}</TableCell>
                    <TableCell>{item.defaultDaysPerYear}</TableCell>
                    <TableCell>{item.requiresAttachment ? "ต้องแนบ" : "ไม่บังคับ"}</TableCell>
                    <TableCell>{item.isActive ? "ใช้งาน" : "ปิดใช้งาน"}</TableCell>
                    <TableCell align="right">
                      <PermissionGuard permission="LeaveManagement.Manage">
                        <IconButton aria-label="แก้ไขประเภทการลา" onClick={() => onEdit(item)}><EditOutlinedIcon /></IconButton>
                        <IconButton aria-label="ปิดใช้งานประเภทการลา" disabled={!item.isActive || deleteMutation.isPending} onClick={() => deleteMutation.mutate(item.id)}><BlockOutlinedIcon /></IconButton>
                      </PermissionGuard>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}
