import { Alert, Box, Card, CardContent, Chip, Grid, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useState } from "react";
import { getDepartments } from "../api/adminApi";
import { getLeaveCalendar, getLeaveTypes } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";

const statusLabels: Record<string, { label: string; color: "warning" | "success" | "default" }> = {
  Pending: { label: "รออนุมัติ", color: "warning" },
  Approved: { label: "อนุมัติแล้ว", color: "success" },
};

const thaiMonths = [
  "มกราคม",
  "กุมภาพันธ์",
  "มีนาคม",
  "เมษายน",
  "พฤษภาคม",
  "มิถุนายน",
  "กรกฎาคม",
  "สิงหาคม",
  "กันยายน",
  "ตุลาคม",
  "พฤศจิกายน",
  "ธันวาคม",
];

export function LeaveCalendarPage() {
  const current = dayjs();
  const [year, setYear] = useState(current.year());
  const [month, setMonth] = useState(current.month() + 1);
  const [departmentId, setDepartmentId] = useState("");
  const [leaveTypeId, setLeaveTypeId] = useState("");
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data = [], isError, isLoading } = useQuery({
    queryKey: ["leave-calendar", year, month, departmentId, leaveTypeId],
    queryFn: () => getLeaveCalendar({
      year,
      month,
      departmentId: departmentId || undefined,
      leaveTypeId: leaveTypeId || undefined,
    }),
  });

  const selectedMonth = dayjs(`${year}-${String(month).padStart(2, "0")}-01`);
  const daysInMonth = selectedMonth.daysInMonth();
  const years = Array.from({ length: 5 }, (_, index) => current.year() - 2 + index);

  return (
    <>
      <PageHeader title="ปฏิทินการลา" subtitle="ดูรายการลาที่รออนุมัติและอนุมัติแล้วแบบรายเดือน" />
      {isError && <Alert severity="error" sx={{ mb: 2 }}>ไม่สามารถโหลดข้อมูลปฏิทินการลาได้</Alert>}
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField select label="เดือน" value={month} onChange={(event) => setMonth(Number(event.target.value))}>
              {Array.from({ length: 12 }, (_, index) => (
                <MenuItem key={index + 1} value={index + 1}>{thaiMonths[index]}</MenuItem>
              ))}
            </TextField>
            <TextField select label="ปี" value={year} onChange={(event) => setYear(Number(event.target.value))}>
              {years.map((item) => <MenuItem key={item} value={item}>{item + 543}</MenuItem>)}
            </TextField>
            <TextField select label="หน่วยงาน" value={departmentId} onChange={(event) => setDepartmentId(event.target.value)}>
              <MenuItem value="">ทุกหน่วยงาน</MenuItem>
              {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
            <TextField select label="ประเภทลา" value={leaveTypeId} onChange={(event) => setLeaveTypeId(event.target.value)}>
              <MenuItem value="">ทุกประเภทลา</MenuItem>
              {leaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
          </Stack>
        </CardContent>
      </Card>
      <Grid container spacing={1.5}>
        {Array.from({ length: daysInMonth }, (_, index) => {
          const date = selectedMonth.date(index + 1);
          const items = data.filter((item) =>
            dayjs(item.startDate).startOf("day").isBefore(date.endOf("day")) &&
            dayjs(item.endDate).endOf("day").isAfter(date.startOf("day")),
          );
          return (
            <Grid item xs={12} sm={6} md={3} lg={2} key={date.format("YYYY-MM-DD")}>
              <Card sx={{ minHeight: 150 }}>
                <CardContent>
                  <Typography fontWeight={700}>{date.format("D MMM")}</Typography>
                  {isLoading ? <Typography color="text.secondary">กำลังโหลด...</Typography> : items.length ? (
                    <Stack spacing={1} sx={{ mt: 1 }}>
                      {items.map((item) => {
                        const status = statusLabels[item.status] ?? { label: item.status, color: "default" as const };
                        return (
                          <Box key={item.id}>
                            <Typography variant="body2">{item.fullname ?? "-"}</Typography>
                            <Typography variant="caption" color="text.secondary">{item.leaveTypeName ?? "-"}</Typography>
                            <Chip size="small" color={status.color} label={status.label} />
                          </Box>
                        );
                      })}
                    </Stack>
                  ) : <Typography color="text.secondary">ไม่มีรายการลา</Typography>}
                </CardContent>
              </Card>
            </Grid>
          );
        })}
      </Grid>
    </>
  );
}
