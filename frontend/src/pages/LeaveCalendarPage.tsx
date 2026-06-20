import { Alert, Box, Button, Card, CardContent, Chip, Dialog, DialogContent, DialogTitle, Grid, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useState } from "react";
import { getDepartments } from "../api/adminApi";
import { getLeaveCalendar, getLeaveHolidays, getLeaveTypes, type LeaveCalendarItem, type LeaveHoliday } from "../api/leaveApi";
import { LeaveCalendarEventChip } from "../components/leave/LeaveCalendarEventChip";
import { LeaveCalendarToolbar } from "../components/leave/LeaveCalendarToolbar";
import { LeaveStatusLegend } from "../components/leave/LeaveStatusLegend";
import { PageHeader } from "../components/PageHeader";

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

const thaiShortDays = ["อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส."];

export function LeaveCalendarPage() {
  const current = dayjs();
  const [year, setYear] = useState(current.year());
  const [month, setMonth] = useState(current.month() + 1);
  const [departmentId, setDepartmentId] = useState("");
  const [leaveTypeId, setLeaveTypeId] = useState("");
  const [status, setStatus] = useState("");
  const [selectedDay, setSelectedDay] = useState<{ date: dayjs.Dayjs; items: LeaveCalendarItem[]; holidays: LeaveHoliday[] } | null>(null);
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: holidays = [] } = useQuery({ queryKey: ["leave-holidays", year], queryFn: () => getLeaveHolidays({ year }) });
  const { data = [], isError, isLoading } = useQuery({
    queryKey: ["leave-calendar", year, month, departmentId, leaveTypeId, status],
    queryFn: () => getLeaveCalendar({
      year,
      month,
      departmentId: departmentId || undefined,
      leaveTypeId: leaveTypeId || undefined,
      status: status || undefined,
    }),
  });

  const selectedMonth = dayjs(`${year}-${String(month).padStart(2, "0")}-01`);
  const daysInMonth = selectedMonth.daysInMonth();
  const years = Array.from({ length: 5 }, (_, index) => current.year() - 2 + index);
  const filteredData = status ? data.filter((item) => item.status === status) : data;
  const activeHolidays = holidays.filter((item) => item.isActive);

  return (
    <>
      <PageHeader title="ปฏิทินการลา" subtitle="ดูรายการลาที่รออนุมัติและอนุมัติแล้วแบบรายเดือน" />
      {isError && <Alert severity="error" sx={{ mb: 2 }}>ไม่สามารถโหลดข้อมูลปฏิทินการลาได้</Alert>}
      <LeaveCalendarToolbar
        month={month}
        year={year}
        departmentId={departmentId}
        leaveTypeId={leaveTypeId}
        status={status}
        years={years}
        months={thaiMonths}
        departments={departments}
        leaveTypes={leaveTypes}
        onMonthChange={setMonth}
        onYearChange={setYear}
        onDepartmentChange={setDepartmentId}
        onLeaveTypeChange={setLeaveTypeId}
        onStatusChange={setStatus}
      />
      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={1.5} sx={{ mb: 2 }}>
            <Box>
              <Typography variant="h6">
                {thaiMonths[month - 1]} {year + 543}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                แสดงข้อมูลการลาตามช่วงวันที่และตัวกรองที่เลือก
              </Typography>
            </Box>
            <LeaveStatusLegend />
          </Stack>
          <Grid container spacing={1.25}>
            {Array.from({ length: daysInMonth }, (_, index) => {
              const date = selectedMonth.date(index + 1);
              const items = filteredData.filter((item) => isLeaveInDate(item, date));
              const dayHolidays = activeHolidays.filter((item) => dayjs(item.holidayDate).isSame(date, "day"));
              const visibleHolidays = dayHolidays.slice(0, 1);
              const visibleItems = items.slice(0, dayHolidays.length ? 1 : 2);
              const hiddenCount = Math.max(items.length - visibleItems.length + dayHolidays.length - visibleHolidays.length, 0);
              return (
                <Grid item xs={12} sm={6} md={3} lg={2} key={date.format("YYYY-MM-DD")}>
                  <Card
                    variant="outlined"
                    sx={(theme) => ({
                      minHeight: 168,
                      boxShadow: "none",
                      bgcolor: date.isSame(current, "day") ? alpha(theme.palette.primary.main, 0.06) : "background.paper",
                      borderColor: date.isSame(current, "day") ? "primary.light" : "divider",
                      transition: theme.transitions.create(["border-color", "box-shadow", "transform"], {
                        duration: theme.transitions.duration.shortest,
                      }),
                      "&:hover": {
                        borderColor: "primary.light",
                        boxShadow: `0 10px 22px ${alpha(theme.palette.primary.main, 0.1)}`,
                        transform: "translateY(-1px)",
                      },
                    })}
                  >
                    <CardContent sx={{ p: 1.25, "&:last-child": { pb: 1.25 } }}>
                      <Stack direction="row" justifyContent="space-between" alignItems="baseline">
                        <Typography fontWeight={800}>{date.format("D")}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {thaiShortDays[date.day()]}
                        </Typography>
                      </Stack>
                      {isLoading ? (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                          กำลังโหลด...
                        </Typography>
                      ) : items.length ? (
                        <Stack spacing={0.75} sx={{ mt: 1 }}>
                          {visibleHolidays.map((holiday) => (
                            <HolidayChip key={holiday.id} holiday={holiday} />
                          ))}
                          {visibleItems.map((item) => (
                            <LeaveCalendarEventChip key={item.id} item={item} />
                          ))}
                          {hiddenCount > 0 && (
                            <Button size="small" variant="text" onClick={() => setSelectedDay({ date, items, holidays: dayHolidays })} sx={{ alignSelf: "flex-start", px: 0.5 }}>
                              ดูเพิ่มเติม {hiddenCount} รายการ
                            </Button>
                          )}
                        </Stack>
                      ) : dayHolidays.length ? (
                        <Stack spacing={0.75} sx={{ mt: 1 }}>
                          {dayHolidays.map((holiday) => (
                            <HolidayChip key={holiday.id} holiday={holiday} />
                          ))}
                        </Stack>
                      ) : (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                          ไม่มีรายการลา
                        </Typography>
                      )}
                    </CardContent>
                  </Card>
                </Grid>
              );
            })}
          </Grid>
        </CardContent>
      </Card>
      <Dialog open={Boolean(selectedDay)} onClose={() => setSelectedDay(null)} fullWidth maxWidth="sm">
        <DialogTitle>
          รายละเอียดการลา {selectedDay?.date.date()} {selectedDay ? thaiMonths[selectedDay.date.month()] : ""}{" "}
          {selectedDay ? selectedDay.date.year() + 543 : ""}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={1}>
            {selectedDay?.holidays.map((holiday) => (
              <HolidayChip key={holiday.id} holiday={holiday} />
            ))}
            {selectedDay?.items.map((item) => (
              <LeaveCalendarEventChip key={item.id} item={item} />
            ))}
          </Stack>
        </DialogContent>
      </Dialog>
    </>
  );
}

function HolidayChip({ holiday }: { holiday: LeaveHoliday }) {
  return (
    <Box
      sx={(theme) => ({
        borderRadius: 1.5,
        px: 1,
        py: 0.75,
        bgcolor: alpha(theme.palette.info.main, 0.08),
        border: "1px solid",
        borderColor: alpha(theme.palette.info.main, 0.28),
        overflow: "hidden",
      })}
    >
      <Stack direction="row" spacing={0.75} alignItems="center" justifyContent="space-between">
        <Typography variant="caption" fontWeight={800} color="info.dark" noWrap>
          {holiday.name}
        </Typography>
        <Chip size="small" color="info" label="วันหยุด" sx={{ height: 20, fontSize: 11, flexShrink: 0 }} />
      </Stack>
    </Box>
  );
}

function isLeaveInDate(item: LeaveCalendarItem, date: dayjs.Dayjs) {
  return (
    dayjs(item.startDate).startOf("day").isBefore(date.endOf("day")) &&
    dayjs(item.endDate).endOf("day").isAfter(date.startOf("day"))
  );
}
