import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  Grid,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useMemo, useState } from "react";
import { getDepartments } from "../api/adminApi";
import { getLeaveCalendar, getLeaveHolidays, getLeaveTypes, type LeaveCalendarItem, type LeaveHoliday } from "../api/leaveApi";
import { LeaveCalendarEventChip } from "../components/leave/LeaveCalendarEventChip";
import { LeaveCalendarToolbar } from "../components/leave/LeaveCalendarToolbar";
import { LeaveStatusLegend } from "../components/leave/LeaveStatusLegend";
import { PageHeader } from "../components/PageHeader";
import { usePermission } from "../context/PermissionContext";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveDurationTypeLabel, getLeaveTypeColor, getLeaveTypeLabel, getLeaveTypeWithDurationLabel } from "../utils/leaveLabels";

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
  const { hasAnyPermission } = usePermission();
  const canFilterDepartments = hasAnyPermission(["DepartmentManagement.View", "LeaveRequest.ViewAll"]);
  const current = dayjs();
  const [year, setYear] = useState(current.year());
  const [month, setMonth] = useState(current.month() + 1);
  const [departmentId, setDepartmentId] = useState("");
  const [leaveTypeId, setLeaveTypeId] = useState("");
  const [status, setStatus] = useState("");
  const [selectedDay, setSelectedDay] = useState<{ date: dayjs.Dayjs; items: LeaveCalendarItem[]; holidays: LeaveHoliday[] } | null>(null);
  const [detailLeaveTypeId, setDetailLeaveTypeId] = useState("");
  const [detailDepartmentId, setDetailDepartmentId] = useState("");
  const [detailSearch, setDetailSearch] = useState("");
  const { data: departments = [] } = useQuery({
    queryKey: ["departments"],
    queryFn: getDepartments,
    enabled: canFilterDepartments,
    retry: false,
  });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: holidays = [] } = useQuery({ queryKey: ["leave-holidays", year], queryFn: () => getLeaveHolidays({ year }) });
  const { data = [], isError, isLoading } = useQuery({
    queryKey: ["leave-calendar", year, month, canFilterDepartments ? departmentId : "", leaveTypeId, status],
    queryFn: () => getLeaveCalendar({
      year,
      month,
      departmentId: canFilterDepartments ? departmentId || undefined : undefined,
      leaveTypeId: leaveTypeId || undefined,
      status: status || undefined,
    }),
  });

  const selectedMonth = dayjs(`${year}-${String(month).padStart(2, "0")}-01`);
  const daysInMonth = selectedMonth.daysInMonth();
  const years = Array.from({ length: 5 }, (_, index) => current.year() - 2 + index);
  const filteredData = status ? data.filter((item) => item.status === status) : data;
  const activeHolidays = useMemo(() => holidays.filter((item) => item.isActive), [holidays]);
  const eventsByDate = useMemo(() => {
    const map = new Map<string, LeaveCalendarItem[]>();
    const monthStart = selectedMonth.startOf("month");
    const monthEnd = selectedMonth.endOf("month");

    for (const item of filteredData) {
      let cursor = dayjs(item.startDate).startOf("day");
      const endDate = dayjs(item.endDate).startOf("day");
      if (!cursor.isValid() || !endDate.isValid()) {
        continue;
      }

      if (cursor.isBefore(monthStart, "day")) {
        cursor = monthStart;
      }

      const lastDate = endDate.isAfter(monthEnd, "day") ? monthEnd : endDate;
      while (cursor.isBefore(lastDate, "day") || cursor.isSame(lastDate, "day")) {
        const key = cursor.format("YYYY-MM-DD");
        const items = map.get(key) ?? [];
        items.push(item);
        map.set(key, items);
        cursor = cursor.add(1, "day");
      }
    }

    return map;
  }, [filteredData, month, year]);
  const holidaysByDate = useMemo(() => {
    const map = new Map<string, LeaveHoliday[]>();
    for (const holiday of activeHolidays) {
      const key = dayjs(holiday.holidayDate).format("YYYY-MM-DD");
      const items = map.get(key) ?? [];
      items.push(holiday);
      map.set(key, items);
    }
    return map;
  }, [activeHolidays]);
  const detailLeaveTypeOptions = useMemo(() => {
    const options = new Map<string, string>();
    for (const item of selectedDay?.items ?? []) {
      options.set(item.leaveTypeId, getLeaveTypeLabel(item.leaveTypeName));
    }
    return Array.from(options, ([id, label]) => ({ id, label }));
  }, [selectedDay]);
  const detailDepartmentOptions = useMemo(() => {
    const options = new Map<string, string>();
    for (const item of selectedDay?.items ?? []) {
      if (item.departmentId) {
        options.set(item.departmentId, item.departmentName || "-");
      }
    }
    return Array.from(options, ([id, label]) => ({ id, label }));
  }, [selectedDay]);
  const detailItems = useMemo(() => {
    const keyword = detailSearch.trim().toLowerCase();
    return (selectedDay?.items ?? []).filter((item) => {
      const matchesType = !detailLeaveTypeId || item.leaveTypeId === detailLeaveTypeId;
      const matchesDepartment = !detailDepartmentId || item.departmentId === detailDepartmentId;
      const matchesSearch = !keyword || (item.fullname ?? "").toLowerCase().includes(keyword);
      return matchesType && matchesDepartment && matchesSearch;
    });
  }, [detailDepartmentId, detailLeaveTypeId, detailSearch, selectedDay]);

  function openDayDetail(date: dayjs.Dayjs, items: LeaveCalendarItem[], dayHolidays: LeaveHoliday[]) {
    setDetailLeaveTypeId("");
    setDetailDepartmentId("");
    setDetailSearch("");
    setSelectedDay({ date, items, holidays: dayHolidays });
  }

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
        showDepartmentFilter={canFilterDepartments}
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
            <Stack spacing={1} alignItems={{ xs: "flex-start", md: "flex-end" }}>
              <LeaveTypeColorLegend />
              <LeaveStatusLegend />
            </Stack>
          </Stack>
          <Grid container spacing={1.25}>
            {Array.from({ length: daysInMonth }, (_, index) => {
              const date = selectedMonth.date(index + 1);
              const dateKey = date.format("YYYY-MM-DD");
              const items = eventsByDate.get(dateKey) ?? [];
              const dayHolidays = holidaysByDate.get(dateKey) ?? [];
              const visibleHolidays = dayHolidays.slice(0, 1);
              const visibleItems = items.slice(0, 2);
              const hiddenCount = Math.max(items.length - visibleItems.length, 0);
              const isToday = date.isSame(current, "day");
              const isWeekend = date.day() === 0 || date.day() === 6;
              const hasHoliday = dayHolidays.length > 0;
              return (
                <Grid item xs={12} sm={6} md={3} lg={2} key={dateKey}>
                  <Card
                    variant="outlined"
                    onClick={() => openDayDetail(date, items, dayHolidays)}
                    sx={(theme) => ({
                      minHeight: 168,
                      boxShadow: "none",
                      cursor: "pointer",
                      bgcolor: hasHoliday
                        ? alpha(theme.palette.info.main, 0.1)
                        : isWeekend
                          ? alpha(theme.palette.warning.main, 0.12)
                          : isToday
                            ? alpha(theme.palette.primary.main, 0.05)
                            : "background.paper",
                      borderColor: isToday
                        ? "primary.main"
                        : hasHoliday
                          ? alpha(theme.palette.info.main, 0.45)
                          : isWeekend
                            ? alpha(theme.palette.warning.main, 0.45)
                            : "divider",
                      borderWidth: isToday ? 2 : 1,
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
                      {visibleHolidays.length > 0 && (
                        <Stack spacing={0.75} sx={{ mt: 1 }}>
                          {visibleHolidays.map((holiday) => (
                            <HolidayChip key={holiday.id} holiday={holiday} />
                          ))}
                        </Stack>
                      )}
                      {isLoading ? (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                          กำลังโหลด...
                        </Typography>
                      ) : items.length ? (
                        <Stack spacing={0.75} sx={{ mt: 1 }}>
                          {visibleItems.map((item) => (
                            <LeaveCalendarEventChip key={item.id} item={item} />
                          ))}
                          {hiddenCount > 0 && (
                            <Button
                              size="small"
                              variant="text"
                              onClick={(event) => {
                                event.stopPropagation();
                                openDayDetail(date, items, dayHolidays);
                              }}
                              sx={{ alignSelf: "flex-start", px: 0.5, fontWeight: 800 }}
                            >
                              +{hiddenCount} รายการ
                            </Button>
                          )}
                        </Stack>
                      ) : dayHolidays.length || isWeekend ? (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                          วันหยุด
                        </Typography>
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
      <Dialog open={Boolean(selectedDay)} onClose={() => setSelectedDay(null)} fullWidth maxWidth="md">
        <DialogTitle>
          รายละเอียดประจำวัน {selectedDay?.date.date()} {selectedDay ? thaiMonths[selectedDay.date.month()] : ""}{" "}
          {selectedDay ? selectedDay.date.year() + 543 : ""}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2}>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ xs: "stretch", sm: "center" }}>
              <Chip color="primary" label={`จำนวนผู้ลา ${selectedDay?.items.length ?? 0} คน`} />
              {(selectedDay?.holidays.length ?? 0) > 0 && <Chip color="info" label={`วันหยุด ${selectedDay?.holidays.length ?? 0} รายการ`} />}
            </Stack>
            {(selectedDay?.holidays.length ?? 0) > 0 && (
              <Stack spacing={1}>
                {selectedDay?.holidays.map((holiday) => (
                  <HolidayChip key={holiday.id} holiday={holiday} />
                ))}
              </Stack>
            )}
            <Grid container spacing={1.5}>
              <Grid item xs={12} md={4}>
                <TextField
                  fullWidth
                  size="small"
                  label="ค้นหาชื่อผู้ลา"
                  value={detailSearch}
                  onChange={(event) => setDetailSearch(event.target.value)}
                />
              </Grid>
              <Grid item xs={12} md={4}>
                <TextField
                  select
                  fullWidth
                  size="small"
                  label="ประเภทลา"
                  value={detailLeaveTypeId}
                  onChange={(event) => setDetailLeaveTypeId(event.target.value)}
                >
                  <MenuItem value="">ทั้งหมด</MenuItem>
                  {detailLeaveTypeOptions.map((option) => (
                    <MenuItem key={option.id} value={option.id}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12} md={4}>
                <TextField
                  select
                  fullWidth
                  size="small"
                  label="หน่วยงาน"
                  value={detailDepartmentId}
                  onChange={(event) => setDetailDepartmentId(event.target.value)}
                >
                  <MenuItem value="">ทั้งหมด</MenuItem>
                  {detailDepartmentOptions.map((option) => (
                    <MenuItem key={option.id} value={option.id}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
            </Grid>
            <Divider />
            {detailItems.length ? (
              <Stack spacing={1.25}>
                {detailItems.map((item) => (
                  <DayDetailLeaveCard key={item.id} item={item} />
                ))}
              </Stack>
            ) : (
              <Box
                sx={(theme) => ({
                  border: "1px dashed",
                  borderColor: "divider",
                  borderRadius: 2,
                  bgcolor: alpha(theme.palette.primary.main, 0.03),
                  p: 3,
                  textAlign: "center",
                })}
              >
                <Typography fontWeight={800}>ไม่พบรายการตามเงื่อนไข</Typography>
                <Typography variant="body2" color="text.secondary">
                  ลองปรับตัวกรองหรือคำค้นหาอีกครั้ง
                </Typography>
              </Box>
            )}
          </Stack>
        </DialogContent>
      </Dialog>
    </>
  );
}

function LeaveTypeColorLegend() {
  const items = [
    { key: "annual", label: "ลาพักผ่อน" },
    { key: "sick", label: "ลาป่วย" },
    { key: "personal", label: "ลากิจ" },
    { key: "maternity", label: "ลาคลอด" },
    { key: "other", label: "อื่น ๆ" },
  ];

  return (
    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
      <Typography variant="caption" color="text.secondary" fontWeight={700}>
        ประเภทลา:
      </Typography>
      {items.map((item) => (
        <Chip
          key={item.key}
          size="small"
          label={item.label}
          sx={{
            bgcolor: getLeaveTypeColor(item.key),
            border: "1px solid",
            borderColor: "divider",
            fontWeight: 700,
          }}
        />
      ))}
    </Stack>
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

function DayDetailLeaveCard({ item }: { item: LeaveCalendarItem }) {
  return (
    <Card variant="outlined" sx={{ boxShadow: "none" }}>
      <CardContent sx={{ p: 1.5, "&:last-child": { pb: 1.5 } }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} justifyContent="space-between">
          <Box sx={{ minWidth: 0 }}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Chip
                size="small"
                label={getLeaveTypeWithDurationLabel(item.leaveTypeName, item.durationType)}
                sx={{
                  bgcolor: getLeaveTypeColor(item.leaveTypeName),
                  fontWeight: 800,
                }}
              />
              <Typography fontWeight={900}>{item.fullname ?? "-"}</Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              หน่วยงาน: {item.departmentName || "-"}
            </Typography>
          </Box>
          <Grid container spacing={1} sx={{ maxWidth: { md: 460 } }}>
            <Grid item xs={6}>
              <DetailLabel label="วันที่เริ่ม" value={formatThaiDate(item.startDate)} />
            </Grid>
            <Grid item xs={6}>
              <DetailLabel label="วันที่สิ้นสุด" value={formatThaiDate(item.endDate)} />
            </Grid>
            <Grid item xs={6}>
              <DetailLabel label="ช่วงเวลา" value={getLeaveDurationTypeLabel(item.durationType)} />
            </Grid>
            <Grid item xs={6}>
              <DetailLabel label="จำนวนวัน" value={`${item.totalDays} วัน`} />
            </Grid>
          </Grid>
        </Stack>
      </CardContent>
    </Card>
  );
}

function DetailLabel({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" fontWeight={700}>
        {label}
      </Typography>
      <Typography variant="body2" fontWeight={800}>
        {value}
      </Typography>
    </Box>
  );
}
