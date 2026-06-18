import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { Alert, Button, Card, CardContent, Checkbox, FormControlLabel, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { createLeaveHoliday, deactivateLeaveHoliday, getLeaveHolidays, updateLeaveHoliday, type LeaveHoliday, type SaveLeaveHolidayRequest } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";

const emptyHoliday: SaveLeaveHolidayRequest = {
  holidayDate: dayjs().format("YYYY-MM-DD"),
  name: "",
  isActive: true,
};

export function LeaveHolidayManagementPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<LeaveHoliday | null>(null);
  const { data = [], isLoading } = useQuery({ queryKey: ["leave-holidays"], queryFn: getLeaveHolidays });
  const { control, register, handleSubmit, reset, formState: { errors } } = useForm<SaveLeaveHolidayRequest>({ defaultValues: emptyHoliday });

  const saveMutation = useMutation({
    mutationFn: (values: SaveLeaveHolidayRequest) => editing ? updateLeaveHoliday(editing.id, values) : createLeaveHoliday(values),
    onSuccess: async () => {
      setEditing(null);
      reset(emptyHoliday);
      await queryClient.invalidateQueries({ queryKey: ["leave-holidays"] });
    },
  });
  const deleteMutation = useMutation({ mutationFn: deactivateLeaveHoliday, onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leave-holidays"] }) });

  function onEdit(item: LeaveHoliday) {
    setEditing(item);
    reset({ holidayDate: item.holidayDate, name: item.name, isActive: item.isActive });
  }

  return (
    <>
      <PageHeader title="วันหยุดราชการ" subtitle="กำหนดวันหยุดที่ต้องตัดออกจากการคำนวณจำนวนวันลา" />
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>{editing ? "แก้ไขวันหยุด" : "เพิ่มวันหยุด"}</Typography>
            <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => saveMutation.mutate(values))}>
              {saveMutation.isError && <Alert severity="error">บันทึกวันหยุดไม่สำเร็จ</Alert>}
              <TextField type="date" label="วันที่" InputLabelProps={{ shrink: true }} error={Boolean(errors.holidayDate)} helperText={errors.holidayDate?.message} {...register("holidayDate", { required: "กรุณาเลือกวันที่" })} />
              <TextField label="ชื่อวันหยุด" error={Boolean(errors.name)} helperText={errors.name?.message} {...register("name", { required: "กรุณากรอกชื่อวันหยุด" })} />
              <Controller name="isActive" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(event) => field.onChange(event.target.checked)} />} label="เปิดใช้งาน" />} />
              <Stack direction="row" spacing={1.5}>
                <Button type="submit" variant="contained" disabled={saveMutation.isPending}>บันทึกข้อมูล</Button>
                {editing && <Button variant="outlined" onClick={() => { setEditing(null); reset(emptyHoliday); }}>ยกเลิก</Button>}
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>วันที่</TableCell>
                  <TableCell>ชื่อวันหยุด</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell align="right">จัดการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={4}>กำลังโหลดวันหยุด...</TableCell></TableRow>
                ) : data.length ? data.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{dayjs(item.holidayDate).format("DD/MM/YYYY")}</TableCell>
                    <TableCell>{item.name}</TableCell>
                    <TableCell>{item.isActive ? "ใช้งาน" : "ปิดใช้งาน"}</TableCell>
                    <TableCell align="right">
                      <IconButton aria-label="แก้ไขวันหยุด" onClick={() => onEdit(item)}><EditOutlinedIcon /></IconButton>
                      <IconButton aria-label="ปิดใช้งานวันหยุด" disabled={!item.isActive || deleteMutation.isPending} onClick={() => deleteMutation.mutate(item.id)}><BlockOutlinedIcon /></IconButton>
                    </TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={4}>ยังไม่มีวันหยุด</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}
