import { Alert, Button, Card, CardContent, MenuItem, Stack, TextField } from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { Controller, useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { createLeaveRequest, getLeaveTypes, type SaveLeaveRequest } from "../api/leaveApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { isStartDateBeforeOrSameEndDate, isValidApiDate } from "../utils/dateFormat";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveRequestFormPage() {
  const navigate = useNavigate();
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { control, register, handleSubmit, watch, formState: { errors } } = useForm<SaveLeaveRequest>({
    defaultValues: { totalDays: 1, reason: "" },
  });
  const startDate = watch("startDate");
  const mutation = useMutation({
    mutationFn: createLeaveRequest,
    onSuccess: (data) => navigate(`/leave/${data.id}`),
  });

  return (
    <>
      <PageHeader title="สร้างคำขอลา" subtitle="บันทึกคำขอลาเป็นแบบร่างก่อนส่งอนุมัติ" />
      <Card>
        <CardContent>
          <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => mutation.mutate({ ...values, totalDays: Number(values.totalDays) }))}>
            {mutation.isError && <Alert severity="error">{getApiErrorMessage(mutation.error, "สร้างคำขอลาไม่สำเร็จ")}</Alert>}
            <TextField fullWidth select label="ประเภทการลา" InputLabelProps={{ shrink: true }} error={Boolean(errors.leaveTypeId)} helperText={errors.leaveTypeId?.message} {...register("leaveTypeId", { required: "กรุณาเลือกประเภทการลา" })}>
              {leaveTypes.filter((item) => item.isActive).map((item) => (
                <MenuItem key={item.id} value={item.id}>{getLeaveTypeLabel(item.name || item.code)}</MenuItem>
              ))}
            </TextField>
            <Controller
              name="startDate"
              control={control}
              rules={{
                required: "กรุณาเลือกวันที่เริ่มลา",
                validate: (value) => isValidApiDate(value) || "กรุณาเลือกวันที่เริ่มลาให้ถูกต้อง",
              }}
              render={({ field }) => (
                <AppDatePicker
                  label="วันที่เริ่มลา"
                  value={field.value ?? ""}
                  onChange={field.onChange}
                  error={Boolean(errors.startDate)}
                  helperText={errors.startDate?.message ?? "เลือกวันที่จากปฏิทิน"}
                />
              )}
            />
            <Controller
              name="endDate"
              control={control}
              rules={{
                required: "กรุณาเลือกวันที่สิ้นสุด",
                validate: (value) =>
                  !isValidApiDate(value)
                    ? "กรุณาเลือกวันที่สิ้นสุดให้ถูกต้อง"
                    : isStartDateBeforeOrSameEndDate(startDate, value) || "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มลา",
              }}
              render={({ field }) => (
                <AppDatePicker
                  label="วันที่สิ้นสุด"
                  value={field.value ?? ""}
                  onChange={field.onChange}
                  error={Boolean(errors.endDate)}
                  helperText={errors.endDate?.message ?? "เลือกวันที่จากปฏิทิน"}
                />
              )}
            />
            <TextField type="number" label="จำนวนวัน" inputProps={{ min: 0.5, step: 0.5 }} error={Boolean(errors.totalDays)} helperText={errors.totalDays?.message} {...register("totalDays", { required: "กรุณากรอกจำนวนวัน", min: { value: 0.5, message: "จำนวนวันต้องมากกว่า 0" } })} />
            <TextField label="เหตุผล" multiline minRows={4} error={Boolean(errors.reason)} helperText={errors.reason?.message} {...register("reason", { required: "กรุณากรอกเหตุผล" })} />
            <Stack direction="row" spacing={1.5}>
              <Button type="submit" variant="contained" disabled={mutation.isPending}>บันทึกแบบร่าง</Button>
              <Button variant="outlined" onClick={() => navigate("/leave")}>ยกเลิก</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}
