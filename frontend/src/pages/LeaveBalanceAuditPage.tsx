import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, MenuItem, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { getAuditLeaveBalances, getLeaveBalances, type LeaveBalance } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { bangkokDayjs } from "../utils/dateFormat";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

type Period = "fiscal" | "calendar";

const today = bangkokDayjs();
const currentFiscalYear = today.month() >= 9 ? today.year() + 1 : today.year();

function toGregorianYear(value: string): number | null {
  const input = Number(value);
  if (!Number.isInteger(input)) return null;
  const year = input > 2400 ? input - 543 : input;
  return year >= 2000 && year <= 2200 ? year : null;
}

function displayDays(value: number): string {
  return new Intl.NumberFormat("th-TH", { maximumFractionDigits: 2 }).format(value);
}

function auditStatus(row: LeaveBalance, prior?: LeaveBalance): { label: string; color: "success" | "warning" | "error" | "default" } {
  if (!row.id) return { label: "ยังไม่ตั้งต้นยอด", color: "warning" };
  const calculated = row.entitledDays + row.carriedOverDays + row.adjustedDays - row.usedDays - row.pendingDays;
  if (Math.abs(calculated - row.availableDays) > 0.001) return { label: "ยอดคงเหลือไม่ตรงสูตร", color: "error" };
  if (row.carriedOverDays > 0 && !prior) return { label: "ไม่พบยอดปีก่อน", color: "warning" };
  if (prior && row.carriedOverDays > Math.max(prior.availableDays, 0) + 0.001) return { label: "ยอดยกมาสูงกว่าปีก่อน", color: "warning" };
  return { label: "สูตรคงเหลือตรง", color: "success" };
}

export function LeaveBalanceAuditPage() {
  const [period, setPeriod] = useState<Period>("fiscal");
  const [yearInput, setYearInput] = useState(String(currentFiscalYear + 543));
  const [search, setSearch] = useState("");
  const [userId, setUserId] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [leaveTypeId, setLeaveTypeId] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const year = toGregorianYear(yearInput);
  const fiscal = period === "fiscal";

  const { data: current = [], isLoading, isError } = useQuery({
    queryKey: ["leave-balance-audit", year, fiscal],
    queryFn: () => getAuditLeaveBalances({ year: year!, useFiscalYear: fiscal }),
    enabled: year !== null,
  });
  const { data: previous = [], isLoading: previousLoading, isError: previousError } = useQuery({
    queryKey: ["leave-balance-audit", year === null ? null : year - 1],
    queryFn: () => getLeaveBalances({ year: year! - 1 }),
    enabled: year !== null,
  });

  const periodRows = useMemo(() => current.filter((row) => row.useFiscalYear === fiscal), [current, fiscal]);
  const people = useMemo(() => [...new Map(periodRows.map((row) => [row.userId, { name: row.fullname ?? "-", code: row.employeeCode }])).entries()].sort((a, b) => a[1].name.localeCompare(b[1].name, "th")), [periodRows]);
  const departments = useMemo(() => [...new Map(periodRows.filter((row) => row.departmentId).map((row) => [row.departmentId!, row.departmentName ?? "-"])).entries()].sort((a, b) => a[1].localeCompare(b[1], "th")), [periodRows]);
  const leaveTypes = useMemo(() => [...new Map(periodRows.map((row) => [row.leaveTypeId, row.leaveTypeName])).entries()].sort((a, b) => a[1].localeCompare(b[1], "th")), [periodRows]);
  const priorByKey = useMemo(() => new Map(previous.filter((row) => row.useFiscalYear === fiscal).map((row) => [`${row.userId}:${row.leaveTypeId}`, row])), [previous, fiscal]);
  const filtered = useMemo(() => periodRows.filter((row) => {
    const term = search.trim().toLocaleLowerCase("th");
    return (!term || `${row.fullname ?? ""} ${row.employeeCode ?? ""} ${row.departmentName ?? ""} ${row.leaveTypeName}`.toLocaleLowerCase("th").includes(term))
      && (!userId || row.userId === userId)
      && (!departmentId || row.departmentId === departmentId)
      && (!leaveTypeId || row.leaveTypeId === leaveTypeId);
  }).sort((a, b) => (a.fullname ?? "").localeCompare(b.fullname ?? "", "th") || a.leaveTypeName.localeCompare(b.leaveTypeName, "th")), [periodRows, search, userId, departmentId, leaveTypeId]);
  const visible = filtered.slice(page * pageSize, (page + 1) * pageSize);
  const peopleCount = new Set(filtered.map((row) => row.userId)).size;
  const usedTotal = filtered.reduce((sum, row) => sum + row.usedDays, 0);
  const attentionCount = filtered.filter((row) => auditStatus(row, priorByKey.get(`${row.userId}:${row.leaveTypeId}`)).color !== "success").length;

  function changePeriod(next: Period) {
    setPeriod(next);
    setYearInput(String((next === "fiscal" ? currentFiscalYear : today.year()) + 543));
    setUserId("");
    setDepartmentId("");
    setLeaveTypeId("");
    setPage(0);
  }

  return (
    <>
      <PageHeader title="ตรวจสอบยอดวันลารายบุคคล" subtitle="เทียบยอดยกมา ใช้ไป และคงเหลือของแต่ละคนตามรอบสิทธิ์" />
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          <Button component={RouterLink} to="/admin/leave-balances" variant="outlined" startIcon={<ArrowBackOutlinedIcon />}>จัดการวันลาคงเหลือ</Button>
          <Typography variant="body2" color="text.secondary" sx={{ alignSelf: "center" }}>ข้อมูล ณ {today.format("DD/MM/")}{today.year() + 543} {today.format("HH:mm")} น. (เวลาไทย)</Typography>
        </Stack>

        <Card variant="outlined"><CardContent>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ md: "center" }}>
            <TextField select size="small" label="รอบสิทธิ์" value={period} onChange={(event) => changePeriod(event.target.value as Period)} sx={{ minWidth: 180 }}>
              <MenuItem value="fiscal">ปีงบประมาณ</MenuItem><MenuItem value="calendar">ปีปฏิทิน</MenuItem>
            </TextField>
            <TextField size="small" label={fiscal ? "ปีงบประมาณ (พ.ศ.)" : "ปีปฏิทิน (พ.ศ.)"} type="number" value={yearInput} onChange={(event) => { setYearInput(event.target.value); setUserId(""); setDepartmentId(""); setLeaveTypeId(""); setPage(0); }} error={year === null} helperText={year === null ? "ระบุปี พ.ศ. หรือ ค.ศ. ที่ถูกต้อง" : undefined} sx={{ minWidth: 180 }} />
            <TextField select size="small" label="บุคลากร" value={userId} onChange={(event) => { setUserId(event.target.value); setPage(0); }} sx={{ minWidth: 200 }}>
              <MenuItem value="">ทุกคน</MenuItem>{people.map(([id, person]) => <MenuItem key={id} value={id}>{person.name}{person.code ? ` (${person.code})` : ""}</MenuItem>)}
            </TextField>
            <TextField size="small" label="ค้นหาชื่อ รหัส หน่วยงาน หรือประเภทลา" value={search} onChange={(event) => { setSearch(event.target.value); setPage(0); }} sx={{ flex: 1, minWidth: 200 }} />
            <TextField select size="small" label="หน่วยงาน" value={departmentId} onChange={(event) => { setDepartmentId(event.target.value); setPage(0); }} sx={{ minWidth: 180 }}>
              <MenuItem value="">ทั้งหมด</MenuItem>{departments.map(([id, name]) => <MenuItem key={id} value={id}>{name}</MenuItem>)}
            </TextField>
            <TextField select size="small" label="ประเภทลา" value={leaveTypeId} onChange={(event) => { setLeaveTypeId(event.target.value); setPage(0); }} sx={{ minWidth: 180 }}>
              <MenuItem value="">ทั้งหมด</MenuItem>{leaveTypes.map(([id, name]) => <MenuItem key={id} value={id}>{getLeaveTypeLabel(name)}</MenuItem>)}
            </TextField>
          </Stack>
        </CardContent></Card>

        <Alert severity="info">ใช้ไปนับจากใบลาที่อนุมัติแล้วตามวันเริ่มลา; ใบรออนุมัติแสดงแยก ยอดคงเหลือ = สิทธิ์ปีนี้ + ยกมา + ปรับปรุง − ใช้ไป − รออนุมัติ ยอดยกมาอาจต่ำกว่าคงเหลือปีก่อนตามเพดานหรือกฎสิทธิ์</Alert>
        {(isError || previousError) && <Alert severity="error">โหลดข้อมูลยอดวันลาไม่สำเร็จ กรุณาลองใหม่</Alert>}

        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "repeat(2, 1fr)", md: "repeat(4, 1fr)" }, gap: 1.5 }}>
          {[["บุคลากร", peopleCount], ["รายการยอด", filtered.length], ["ใช้ไป (วัน)", displayDays(usedTotal)], ["ควรตรวจเพิ่มเติม", attentionCount]].map(([label, value]) => (
            <Card key={label} variant="outlined"><CardContent sx={{ py: 1.5 }}><Typography variant="body2" color="text.secondary">{label}</Typography><Typography variant="h5" fontWeight={800}>{value}</Typography></CardContent></Card>
          ))}
        </Box>

        <Card variant="outlined"><CardContent>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}><AccountBalanceWalletOutlinedIcon color="primary" /><Typography variant="h6" fontWeight={800}>ยอดวันลา {fiscal ? "ปีงบประมาณ" : "ปีปฏิทิน"} {year === null ? "-" : year + 543}</Typography></Stack>
          <TableContainer sx={{ overflowX: "auto" }}><Table size="small" sx={{ minWidth: 1150 }}>
            <TableHead><TableRow>
              {['บุคลากร / หน่วยงาน', 'ประเภทลา', 'คงเหลือปีก่อน', 'ยกมา', 'สิทธิ์ปีนี้', 'ปรับปรุง', 'ใช้ไป', 'รออนุมัติ', 'คงเหลือ', 'ตรวจสอบ'].map((heading) => <TableCell key={heading}>{heading}</TableCell>)}
            </TableRow></TableHead>
            <TableBody>
              {(isLoading || previousLoading) ? <TableRow><TableCell colSpan={10}>กำลังโหลดข้อมูล...</TableCell></TableRow> : visible.length === 0 ? <TableRow><TableCell colSpan={10}>ไม่พบยอดวันลาตามรอบและตัวกรองที่เลือก</TableCell></TableRow> : visible.map((row) => {
                const prior = priorByKey.get(`${row.userId}:${row.leaveTypeId}`);
                const status = auditStatus(row, prior);
                return <TableRow key={row.id ?? `${row.userId}:${row.leaveTypeId}:${row.year}`} hover>
                  <TableCell><Typography fontWeight={700}>{row.fullname ?? "-"}</Typography><Typography variant="caption" color="text.secondary">{[row.employeeCode, row.departmentName ?? "ไม่ระบุหน่วยงาน"].filter(Boolean).join(" · ")}</Typography></TableCell>
                  <TableCell>{getLeaveTypeLabel(row.leaveTypeName)}{!row.id && <Typography variant="caption" color="text.secondary" display="block">{row.notes}</Typography>}</TableCell>
                  <TableCell>{prior ? displayDays(prior.availableDays) : "-"}</TableCell>
                  <TableCell>{displayDays(row.carriedOverDays)}</TableCell>
                  <TableCell>{displayDays(row.entitledDays)}</TableCell>
                  <TableCell>{displayDays(row.adjustedDays)}</TableCell>
                  <TableCell>{displayDays(row.usedDays)}</TableCell>
                  <TableCell>{displayDays(row.pendingDays)}</TableCell>
                  <TableCell><Typography fontWeight={800} color={row.availableDays < 0 ? "error.main" : "primary.main"}>{displayDays(row.availableDays)}</Typography></TableCell>
                  <TableCell><Chip size="small" color={status.color} label={status.label} /></TableCell>
                </TableRow>;
              })}
            </TableBody>
          </Table></TableContainer>
          <TablePagination component="div" count={filtered.length} page={page} onPageChange={(_, value) => setPage(value)} rowsPerPage={pageSize} onRowsPerPageChange={(event) => { setPageSize(Number(event.target.value)); setPage(0); }} rowsPerPageOptions={[10, 20, 50, 100]} labelRowsPerPage="รายการต่อหน้า" />
          <Typography variant="caption" color="text.secondary">แสดงบุคลากรที่เปิดใช้งานทุกคนสำหรับประเภทลาที่มีบัญชียอดวันลา แถว “ยังไม่ตั้งต้นยอด” แสดงสิทธิ์คำนวณจากกฎปัจจุบัน ไม่ใช่ยอดที่บันทึกจริง ยอดปีก่อนคำนวณจากสถานะคำขอ ณ ปัจจุบัน; หากยอดยกมาสูงกว่า ให้ตรวจประวัติการยกยอดก่อนแก้ไข</Typography>
        </CardContent></Card>
      </Stack>
    </>
  );
}
