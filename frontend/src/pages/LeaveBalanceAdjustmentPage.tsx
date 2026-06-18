import { Alert, Button, Card, CardContent, Grid, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useForm } from "react-hook-form";
import { getUsers } from "../api/adminApi";
import { createLeaveBalanceAdjustment, getLeaveBalanceAdjustments, getLeaveTypes, type CreateLeaveBalanceAdjustmentRequest } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";

export function LeaveBalanceAdjustmentPage() {
  const queryClient = useQueryClient();
  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: adjustments = [] } = useQuery({ queryKey: ["leave-balance-adjustments"], queryFn: getLeaveBalanceAdjustments });
  const { register, handleSubmit, reset, formState: { errors } } = useForm<CreateLeaveBalanceAdjustmentRequest>({
    defaultValues: { year: new Date().getFullYear(), adjustmentDays: 0, reason: "" },
  });

  const mutation = useMutation({
    mutationFn: (values: CreateLeaveBalanceAdjustmentRequest) => createLeaveBalanceAdjustment({
      ...values,
      year: Number(values.year),
      adjustmentDays: Number(values.adjustmentDays),
    }),
    onSuccess: async () => {
      reset({ year: new Date().getFullYear(), adjustmentDays: 0, reason: "" });
      await queryClient.invalidateQueries({ queryKey: ["leave-balance-adjustments"] });
    },
  });

  return (
    <>
      <PageHeader title="ปรับยอดวันลา" subtitle="เพิ่มหรือลดยอดสิทธิ์วันลาของบุคลากรพร้อมบันทึกเหตุผล" />
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => mutation.mutate(values))}>
              {mutation.isError && <Alert severity="error">ปรับยอดวันลาไม่สำเร็จ กรุณาตรวจสอบข้อมูล</Alert>}
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <TextField fullWidth select label="ผู้ใช้งาน" error={Boolean(errors.userId)} helperText={errors.userId?.message} defaultValue="" {...register("userId", { required: "กรุณาเลือกผู้ใช้งาน" })}>
                    {users.map((item) => <MenuItem key={item.id} value={item.id}>{item.fullname}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField fullWidth select label="ประเภทการลา" error={Boolean(errors.leaveTypeId)} helperText={errors.leaveTypeId?.message} defaultValue="" {...register("leaveTypeId", { required: "กรุณาเลือกประเภทการลา" })}>
                    {leaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} md={3}>
                  <TextField fullWidth type="number" label="ปี ค.ศ." {...register("year", { required: "กรุณากรอกปี" })} />
                </Grid>
                <Grid item xs={12} md={3}>
                  <TextField fullWidth type="number" label="จำนวนวันที่ปรับ" inputProps={{ step: 0.5 }} error={Boolean(errors.adjustmentDays)} helperText={errors.adjustmentDays?.message} {...register("adjustmentDays", { required: "กรุณากรอกจำนวนวัน" })} />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField fullWidth label="เหตุผล" error={Boolean(errors.reason)} helperText={errors.reason?.message} {...register("reason", { required: "กรุณากรอกเหตุผล" })} />
                </Grid>
              </Grid>
              <Button type="submit" variant="contained" disabled={mutation.isPending}>บันทึกการปรับยอด</Button>
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>วันที่</TableCell>
                  <TableCell>ผู้ใช้งาน</TableCell>
                  <TableCell>ประเภทการลา</TableCell>
                  <TableCell>ปี</TableCell>
                  <TableCell>จำนวนวัน</TableCell>
                  <TableCell>เหตุผล</TableCell>
                  <TableCell>ผู้ปรับยอด</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {adjustments.length ? adjustments.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{dayjs(item.createdAt).format("DD/MM/YYYY HH:mm")}</TableCell>
                    <TableCell>{item.fullname ?? "-"}</TableCell>
                    <TableCell>{item.leaveTypeName ?? "-"}</TableCell>
                    <TableCell>{item.year}</TableCell>
                    <TableCell>{item.adjustmentDays}</TableCell>
                    <TableCell>{item.reason}</TableCell>
                    <TableCell>{item.adjustedByName ?? "-"}</TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={7}>ยังไม่มีประวัติการปรับยอด</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}
