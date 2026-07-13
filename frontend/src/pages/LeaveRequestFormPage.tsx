import { Alert, Button, Card, CardContent, FormControl, FormControlLabel, FormHelperText, FormLabel, MenuItem, Radio, RadioGroup, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import dayjs from "dayjs";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { getMyProfile } from "../api/profileApi";
import { createLeaveRequest, getLeaveHolidays, getLeaveRequest, getLeaveTypes, previewLeavePolicy, updateLeaveRequest, type SaveLeaveRequest } from "../api/leaveApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { appConfig } from "../config/appConfig";
import { useNotification } from "../hooks/useNotification";
import { isStartDateBeforeOrSameEndDate, isValidApiDate } from "../utils/dateFormat";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveRequestFormPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditMode = Boolean(id);
  const { showSuccess } = useNotification();
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: editingRequest } = useQuery({ queryKey: ["leave-requests", id], queryFn: () => getLeaveRequest(id!), enabled: isEditMode });
  const { data: profile } = useQuery({ queryKey: ["me", "profile"], queryFn: getMyProfile });
  const { control, register, handleSubmit, watch, setValue, reset, formState: { errors } } = useForm<SaveLeaveRequest>({
    defaultValues: { durationType: "FULL_DAY", totalDays: 1, reason: "" },
  });
  const startDate = watch("startDate");
  const endDate = watch("endDate");
  const leaveTypeId = watch("leaveTypeId");
  const durationType = watch("durationType") ?? "FULL_DAY";
  const isHalfDay = durationType === "HALF_DAY_AM" || durationType === "HALF_DAY_PM";
  const holidayYear = startDate && dayjs(startDate).isValid() ? dayjs(startDate).year() : dayjs().year();
  const { data: holidays = [] } = useQuery({ queryKey: ["leave-holidays", holidayYear], queryFn: () => getLeaveHolidays({ year: holidayYear }) });
  const visibleLeaveTypes = leaveTypes
    .filter((item) => item.isActive)
    .filter((item) => !appConfig.hideIneligibleLeaveTypes || isLeaveTypeEligibleByGender(item.code, profile?.gender));
  const selectedLeaveType = leaveTypes.find((item) => item.id === leaveTypeId);
  const requestedDays = isHalfDay ? 0.5 : estimateRequestedDays(startDate, endDate);
  const shouldValidateBalance = selectedLeaveType?.requiresBalance !== false;
  const holidayNamesInRange = getHolidayNamesInRange(startDate, isHalfDay ? startDate : endDate, holidays);
  const hasHolidayInRange = holidayNamesInRange.length > 0;
  const canPreviewPolicy = Boolean(
    leaveTypeId &&
    startDate &&
    (isHalfDay || endDate) &&
    isValidApiDate(startDate) &&
    isValidApiDate(isHalfDay ? startDate : endDate),
  );
  const { data: policyPreview, isFetching: isPolicyPreviewLoading } = useQuery({
    queryKey: ["leave-policy-preview", leaveTypeId, startDate, isHalfDay ? startDate : endDate, durationType],
    queryFn: () => previewLeavePolicy({
      leaveTypeId,
      startDate,
      endDate: isHalfDay ? startDate : endDate,
      durationType,
    }),
    enabled: canPreviewPolicy,
  });
  const policyErrors = policyPreview?.errors ?? [];
  const policyWarnings = policyPreview?.warnings ?? [];
  const policyNotes = policyPreview?.policyNotes ?? [];
  const hasPolicyError = policyErrors.length > 0 || policyPreview?.canSubmit === false;
  useEffect(() => {
    if (!editingRequest) {
      return;
    }

    reset({
      leaveTypeId: editingRequest.leaveTypeId,
      startDate: editingRequest.startDate,
      endDate: editingRequest.endDate,
      durationType: editingRequest.durationType,
      totalDays: editingRequest.totalDays,
      reason: editingRequest.reason,
    });
  }, [editingRequest, reset]);
  useEffect(() => {
    setValue("totalDays", isHalfDay ? 0.5 : 1);
    if (isHalfDay && startDate) {
      setValue("endDate", startDate, { shouldValidate: true });
    }
  }, [isHalfDay, setValue, startDate]);
  const mutation = useMutation({
    mutationFn: (values: SaveLeaveRequest) => {
      const payload = {
        ...values,
        endDate: isHalfDay ? values.startDate : values.endDate,
        totalDays: isHalfDay ? 0.5 : Number(values.totalDays || 1),
      };
      return isEditMode ? updateLeaveRequest(id!, payload) : createLeaveRequest(payload);
    },
    onSuccess: (data) => {
      showSuccess(isEditMode ? "บันทึกการแก้ไขคำขอลาเรียบร้อยแล้ว" : "เพิ่มคำขอลาสำเร็จเรียบร้อยแล้ว โปรดรออนุมัติ");
      navigate(`/leave/${data.id}`);
    },
  });

  return (
    <>
      <PageHeader title={isEditMode ? "แก้ไขคำขอลา" : "สร้างคำขอลา"} subtitle={isEditMode ? "แก้ไขข้อมูลคำขอที่ยังเป็นแบบร่างหรือถูกตีกลับรอแก้ไข" : "บันทึกคำขอลาเป็นแบบร่างก่อนส่งอนุมัติ"} />
      <Card>
        <CardContent>
          <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => mutation.mutate(values))}>
            {mutation.isError && <Alert severity="error">{getApiErrorMessage(mutation.error, isEditMode ? "บันทึกการแก้ไขคำขอลาไม่สำเร็จ" : "สร้างคำขอลาไม่สำเร็จ")}</Alert>}
            {leaveTypeId && (
              <Alert severity={hasPolicyError ? "warning" : "info"}>
                {isPolicyPreviewLoading ? (
                  "กำลังตรวจสอบสิทธิ์วันลา..."
                ) : policyPreview ? (
                  <>
                    ประเภทบุคลากร: {policyPreview.employmentTypeName} · ปีงบประมาณ {policyPreview.fiscalYear + 543} ·
                    สิทธิ์ {policyPreview.entitlementDays.toLocaleString("th-TH")} วัน ·
                    ใช้ไปแล้ว {policyPreview.usedDays.toLocaleString("th-TH")} วัน ·
                    รออนุมัติ {policyPreview.pendingDays.toLocaleString("th-TH")} วัน ·
                    คงเหลือใช้ได้ {policyPreview.availableDays.toLocaleString("th-TH")} วัน ·
                    คำขอนี้ใช้ {policyPreview.requestedDays.toLocaleString("th-TH")} วัน
                  </>
                ) : shouldValidateBalance ? (
                  "เลือกประเภทการลาและวันที่ เพื่อให้ระบบตรวจสิทธิ์วันลาอัตโนมัติ"
                ) : (
                  "ประเภทการลานี้ไม่ใช้โควตาวันลา"
                )}
              </Alert>
            )}
            {policyErrors.map((message) => (
              <Alert key={message} severity="error">{message}</Alert>
            ))}
            {policyWarnings.map((message) => (
              <Alert key={message} severity="warning">{message}</Alert>
            ))}
            {policyNotes.map((message) => (
              <Alert key={message} severity="info">{message}</Alert>
            ))}
            {hasHolidayInRange && (
              <Alert severity="warning">
                ไม่สามารถขอลาในวันหยุดได้: {holidayNamesInRange.join(", ")}
              </Alert>
            )}
            <TextField fullWidth select label="ประเภทการลา" InputLabelProps={{ shrink: true }} error={Boolean(errors.leaveTypeId)} helperText={errors.leaveTypeId?.message} {...register("leaveTypeId", { required: "กรุณาเลือกประเภทการลา" })}>
              {visibleLeaveTypes.map((item) => (
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
              <Button type="submit" variant="contained" disabled={mutation.isPending || hasPolicyError || hasHolidayInRange}>{isEditMode ? "บันทึกการแก้ไข" : "บันทึกแบบร่าง"}</Button>
              <Button variant="outlined" onClick={() => navigate(isEditMode ? `/leave/${id}` : "/leave")}>ยกเลิก</Button>
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

function isLeaveTypeEligibleByGender(code?: string | null, gender?: string | null) {
  if (code === "MATERNITY_LEAVE") {
    return gender === "Female";
  }

  if (code === "ORDINATION_LEAVE") {
    return gender === "Male";
  }

  return true;
}

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}
