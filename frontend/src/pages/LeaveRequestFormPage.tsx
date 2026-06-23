import { Alert, Button, Card, CardContent, FormControl, FormControlLabel, FormHelperText, FormLabel, MenuItem, Radio, RadioGroup, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import dayjs from "dayjs";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { createLeaveRequest, getLeaveHolidays, getLeaveTypes, getMyLeaveBalances, type SaveLeaveRequest } from "../api/leaveApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { isStartDateBeforeOrSameEndDate, isValidApiDate } from "../utils/dateFormat";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveRequestFormPage() {
  const navigate = useNavigate();
  const { showSuccess } = useNotification();
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: leaveBalances = [] } = useQuery({ queryKey: ["leave-balances", "me"], queryFn: getMyLeaveBalances });
  const { control, register, handleSubmit, watch, setValue, formState: { errors } } = useForm<SaveLeaveRequest>({
    defaultValues: { durationType: "FULL_DAY", totalDays: 1, reason: "" },
  });
  const startDate = watch("startDate");
  const endDate = watch("endDate");
  const leaveTypeId = watch("leaveTypeId");
  const durationType = watch("durationType") ?? "FULL_DAY";
  const isHalfDay = durationType === "HALF_DAY_AM" || durationType === "HALF_DAY_PM";
  const holidayYear = startDate && dayjs(startDate).isValid() ? dayjs(startDate).year() : dayjs().year();
  const { data: holidays = [] } = useQuery({ queryKey: ["leave-holidays", holidayYear], queryFn: () => getLeaveHolidays({ year: holidayYear }) });
  const selectedLeaveType = leaveTypes.find((item) => item.id === leaveTypeId);
  const selectedBalance = leaveBalances.find((item) => item.leaveTypeId === leaveTypeId);
  const requestedDays = isHalfDay ? 0.5 : estimateRequestedDays(startDate, endDate);
  const availableDays = selectedBalance?.remainingDays ?? selectedLeaveType?.defaultDaysPerYear ?? 0;
  const shouldValidateBalance = selectedLeaveType?.requiresBalance !== false;
  const hasInsufficientBalance = Boolean(leaveTypeId && shouldValidateBalance && requestedDays > 0 && requestedDays > availableDays);
  const holidayNamesInRange = getHolidayNamesInRange(startDate, isHalfDay ? startDate : endDate, holidays);
  const hasHolidayInRange = holidayNamesInRange.length > 0;
  useEffect(() => {
    setValue("totalDays", isHalfDay ? 0.5 : 1);
    if (isHalfDay && startDate) {
      setValue("endDate", startDate, { shouldValidate: true });
    }
  }, [isHalfDay, setValue, startDate]);
  const mutation = useMutation({
    mutationFn: (values: SaveLeaveRequest) => createLeaveRequest({
      ...values,
      endDate: isHalfDay ? values.startDate : values.endDate,
      totalDays: isHalfDay ? 0.5 : Number(values.totalDays || 1),
    }),
    onSuccess: (data) => {
      showSuccess("เพิ่มคำขอลาสำเร็จเรียบร้อยแล้ว โปรดรออนุมัติ");
      navigate(`/leave/${data.id}`);
    },
  });

  return (
    <>
      <PageHeader title="สร้างคำขอลา" subtitle="บันทึกคำขอลาเป็นแบบร่างก่อนส่งอนุมัติ" />
      <Card>
        <CardContent>
          <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => mutation.mutate(values))}>
            {mutation.isError && <Alert severity="error">{getApiErrorMessage(mutation.error, "สร้างคำขอลาไม่สำเร็จ")}</Alert>}
            {leaveTypeId && (
              <Alert severity={hasInsufficientBalance ? "warning" : "info"}>
                {shouldValidateBalance ? (
                  <>
                    คงเหลือ {availableDays.toLocaleString("th-TH")} วัน · คำขอนี้ใช้ประมาณ {requestedDays.toLocaleString("th-TH")} วัน
                    {selectedBalance && <> · รออนุมัติ {selectedBalance.pendingDays.toLocaleString("th-TH")} วัน</>}
                    {hasInsufficientBalance && " · ยอดวันลาไม่เพียงพอ"}
                  </>
                ) : (
                  "ประเภทการลานี้ไม่ใช้โควตาวันลา"
                )}
              </Alert>
            )}
            {hasHolidayInRange && (
              <Alert severity="warning">
                ไม่สามารถขอลาในวันหยุดได้: {holidayNamesInRange.join(", ")}
              </Alert>
            )}
            <TextField fullWidth select label="ประเภทการลา" InputLabelProps={{ shrink: true }} error={Boolean(errors.leaveTypeId)} helperText={errors.leaveTypeId?.message} {...register("leaveTypeId", { required: "กรุณาเลือกประเภทการลา" })}>
              {leaveTypes.filter((item) => item.isActive).map((item) => (
                <MenuItem key={item.id} value={item.id}>{getLeaveTypeLabel(item.name || item.code)}</MenuItem>
              ))}
            </TextField>
            <Controller
              name="durationType"
              control={control}
              rules={{ required: "กรุณาเลือกประเภทช่วงเวลา" }}
              render={({ field }) => (
                <FormControl error={Boolean(errors.durationType)}>
                  <FormLabel>ประเภทช่วงเวลา</FormLabel>
                  <RadioGroup row {...field}>
                    <FormControlLabel value="FULL_DAY" control={<Radio />} label="เต็มวัน" />
                    <FormControlLabel value="HALF_DAY_AM" control={<Radio />} label="ครึ่งวัน (เช้า)" />
                    <FormControlLabel value="HALF_DAY_PM" control={<Radio />} label="ครึ่งวัน (บ่าย)" />
                  </RadioGroup>
                  <FormHelperText>
                    {errors.durationType?.message ?? "หากเลือกครึ่งวัน ระบบจะคิด 0.5 วันและใช้วันเดียวกัน"}
                  </FormHelperText>
                </FormControl>
              )}
            />
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
                    : isHalfDay && value !== startDate
                      ? "การลาครึ่งวันต้องเลือกวันที่เริ่มลาและวันที่สิ้นสุดเป็นวันเดียวกัน"
                    : isStartDateBeforeOrSameEndDate(startDate, value) || "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มลา",
              }}
              render={({ field }) => (
                <AppDatePicker
                  label="วันที่สิ้นสุด"
                  value={field.value ?? ""}
                  onChange={field.onChange}
                  disabled={isHalfDay}
                  error={Boolean(errors.endDate)}
                  helperText={errors.endDate?.message ?? (isHalfDay ? "ลาครึ่งวันใช้วันเดียวกับวันที่เริ่มลา" : "เลือกวันที่จากปฏิทิน")}
                />
              )}
            />
            <Typography variant="body2" color="text.secondary">
              จำนวนวันที่ใช้โดยประมาณ: {isHalfDay ? "0.5" : "คำนวณจากวันทำการที่เลือก"} วัน
            </Typography>
            <TextField label="เหตุผล" multiline minRows={4} error={Boolean(errors.reason)} helperText={errors.reason?.message} {...register("reason", { required: "กรุณากรอกเหตุผล" })} />
            <Stack direction="row" spacing={1.5}>
              <Button type="submit" variant="contained" disabled={mutation.isPending || hasInsufficientBalance || hasHolidayInRange}>บันทึกแบบร่าง</Button>
              <Button variant="outlined" onClick={() => navigate("/leave")}>ยกเลิก</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}

function getHolidayNamesInRange(startDate?: string, endDate?: string, holidays: { holidayDate: string; name: string; isActive: boolean }[] = []) {
  if (!startDate || !endDate || !dayjs(startDate).isValid() || !dayjs(endDate).isValid()) {
    return [];
  }

  const start = dayjs(startDate).startOf("day");
  const end = dayjs(endDate).startOf("day");
  if (end.isBefore(start)) {
    return [];
  }

  return holidays
    .filter((holiday) => holiday.isActive)
    .filter((holiday) => {
      const date = dayjs(holiday.holidayDate).startOf("day");
      return (date.isAfter(start) || date.isSame(start)) && (date.isBefore(end) || date.isSame(end));
    })
    .map((holiday) => holiday.name);
}

function estimateRequestedDays(startDate?: string, endDate?: string) {
  if (!startDate || !endDate || !dayjs(startDate).isValid() || !dayjs(endDate).isValid()) {
    return 0;
  }

  const start = dayjs(startDate).startOf("day");
  const end = dayjs(endDate).startOf("day");
  if (end.isBefore(start)) {
    return 0;
  }

  let days = 0;
  for (let cursor = start; cursor.isBefore(end) || cursor.isSame(end); cursor = cursor.add(1, "day")) {
    if (cursor.day() !== 0 && cursor.day() !== 6) {
      days += 1;
    }
  }

  return days;
}

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}
