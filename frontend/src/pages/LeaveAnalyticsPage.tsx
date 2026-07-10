import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import InsightsOutlinedIcon from "@mui/icons-material/InsightsOutlined";
import PieChartOutlineOutlinedIcon from "@mui/icons-material/PieChartOutlineOutlined";
import RestartAltOutlinedIcon from "@mui/icons-material/RestartAltOutlined";
import StackedBarChartOutlinedIcon from "@mui/icons-material/StackedBarChartOutlined";
import TableChartOutlinedIcon from "@mui/icons-material/TableChartOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, MenuItem, Skeleton, Stack, Switch, Table, TableBody, TableCell, TableHead, TablePagination, TableRow, TextField, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import type { SvgIconComponent } from "@mui/icons-material";
import { useMemo, useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { downloadLeaveAnalyticsExcel, getLeaveAnalytics, getLeaveAnalyticsOptions, type LeaveAnalytics, type LeaveAnalyticsDepartmentStack, type LeaveAnalyticsHeatmap, type LeaveAnalyticsLeaveTypeBreakdown, type LeaveAnalyticsMonthlyTrend, type LeaveAnalyticsQuery, type LeaveAnalyticsTableItem } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { brandColors } from "../theme/theme";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveStatusLabel, getLeaveTypeLabel, getLeaveTypeWithDurationLabel } from "../utils/leaveLabels";

const coreLeaveCodes = new Set(["SICK_LEAVE", "PERSONAL_LEAVE", "VACATION_LEAVE"]);

export function LeaveAnalyticsPage() {
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth() + 1;
  const currentFiscalYear = getFiscalYear(currentYear, currentMonth);
  const [filters, setFilters] = useState<LeaveAnalyticsQuery>({
    fiscalYear: currentFiscalYear,
    status: "Approved",
    coreOnly: true,
  });
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const yearOptions = useMemo(() => buildYearOptions(currentYear), [currentYear]);
  const fiscalYearOptions = useMemo(() => buildFiscalYearOptions(currentFiscalYear), [currentFiscalYear]);
  const queryParams = useMemo(() => sanitizeFilters(filters), [filters]);
  const { data: options } = useQuery({ queryKey: ["leave-analytics", "options"], queryFn: getLeaveAnalyticsOptions });
  const departments = options?.departments ?? [];
  const leaveTypes = options?.leaveTypes ?? [];
  const { data, isError, isLoading } = useQuery({
    queryKey: ["leave-analytics", queryParams],
    queryFn: () => getLeaveAnalytics(queryParams),
  });
  const filteredLeaveTypes = useMemo(
    () => filters.coreOnly ? leaveTypes.filter((item) => coreLeaveCodes.has(item.code)) : leaveTypes,
    [filters.coreOnly, leaveTypes],
  );
  const items = data?.items ?? [];
  const visibleItems = useMemo(
    () => items.slice(page * pageSize, page * pageSize + pageSize),
    [items, page, pageSize],
  );

  function updateFilters(nextFilters: LeaveAnalyticsQuery) {
    setFilters(nextFilters);
    setPage(0);
  }

  function resetFilters() {
    updateFilters({ fiscalYear: currentFiscalYear, status: "Approved", coreOnly: true });
  }

  async function exportExcel() {
    const blob = await downloadLeaveAnalyticsExcel(queryParams);
    const fiscal = toThaiDisplayYear(queryParams.fiscalYear ?? currentFiscalYear);
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `leave-analytics-FY${fiscal}.xlsx`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  return (
    <Box>
      <PageHeader title="วิเคราะห์ข้อมูลการลา" subtitle="วิเคราะห์แนวโน้ม ประเภทการลา หน่วยงาน และความหนาแน่นของการลา" />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={1.5} alignItems="center">
            <Grid item xs={12} md={2}>
              <TextField fullWidth select size="small" label="ปีงบประมาณ" value={filters.fiscalYear ?? currentFiscalYear} onChange={(event) => updateFilters({ ...filters, fiscalYear: Number(event.target.value) })}>
                {fiscalYearOptions.map((year) => <MenuItem key={year} value={year}>{toThaiDisplayYear(year)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} md={2}>
              <TextField fullWidth select size="small" label="ปี" value={filters.year ?? ""} onChange={(event) => updateFilters({ ...filters, year: event.target.value ? Number(event.target.value) : undefined, month: undefined })}>
                <MenuItem value="">ตามปีงบประมาณ</MenuItem>
                {yearOptions.map((year) => <MenuItem key={year} value={year}>{toThaiDisplayYear(year)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} md={2}>
              <TextField fullWidth select size="small" label="เดือน" value={filters.month ?? ""} onChange={(event) => updateFilters({ ...filters, month: event.target.value ? Number(event.target.value) : undefined, year: filters.year ?? currentYear })}>
                <MenuItem value="">ทั้งปี</MenuItem>
                {thaiMonths.map((month, index) => <MenuItem key={month} value={index + 1}>{month}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField fullWidth select size="small" label="หน่วยงาน" value={filters.departmentId ?? ""} onChange={(event) => updateFilters({ ...filters, departmentId: event.target.value || undefined })}>
                <MenuItem value="">ทุกหน่วยงาน</MenuItem>
                {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField fullWidth select size="small" label="ประเภทลา" value={filters.leaveTypeId ?? ""} onChange={(event) => updateFilters({ ...filters, leaveTypeId: event.target.value || undefined })}>
                <MenuItem value="">ทุกประเภทลา</MenuItem>
                {filteredLeaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{getLeaveTypeLabel(item.name || item.code)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} md={2}>
              <TextField fullWidth select size="small" label="สถานะ" value={filters.status ?? "Approved"} onChange={(event) => updateFilters({ ...filters, status: event.target.value })}>
                <MenuItem value="Approved">อนุมัติแล้ว</MenuItem>
                <MenuItem value="Pending">รออนุมัติ</MenuItem>
                <MenuItem value="Rejected">ไม่อนุมัติ</MenuItem>
                <MenuItem value="Cancelled">ยกเลิก</MenuItem>
                <MenuItem value="All">ทุกสถานะ</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} md={4}>
              <Stack direction="row" spacing={1} alignItems="center">
                <Switch checked={filters.coreOnly ?? true} onChange={(event) => updateFilters({ ...filters, coreOnly: event.target.checked, leaveTypeId: undefined })} />
                <Box>
                  <Typography fontWeight={800}>เฉพาะประเภทหลัก</Typography>
                  <Typography variant="caption" color="text.secondary">ลาป่วย ลากิจส่วนตัว ลาพักผ่อน</Typography>
                </Box>
              </Stack>
            </Grid>
            <Grid item xs={12} md={6}>
              <Stack direction="row" spacing={1} justifyContent={{ xs: "flex-start", md: "flex-end" }} flexWrap="wrap" useFlexGap>
                <Button variant="outlined" startIcon={<RestartAltOutlinedIcon />} onClick={resetFilters}>ล้างตัวกรอง</Button>
                <ActionTooltip title="ส่งออก Leave Analytics ตามตัวกรองปัจจุบัน">
                  <Button variant="contained" startIcon={<DownloadOutlinedIcon />} onClick={exportExcel}>Export Excel</Button>
                </ActionTooltip>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {isError && <Alert severity="error" sx={{ mb: 2 }}>ไม่สามารถโหลดข้อมูลวิเคราะห์การลาได้</Alert>}

      <Grid container spacing={2}>
        <KpiCard title="จำนวนรายการลา" value={data?.summary.totalRequests} note="จำนวนคำขอตามเงื่อนไข" icon={TableChartOutlinedIcon} isLoading={isLoading} />
        <KpiCard title="บุคลากรที่ลา" value={data?.summary.uniqueUsers} note="นับผู้ลาแบบ unique" icon={InsightsOutlinedIcon} isLoading={isLoading} color="info.main" />
        <KpiCard title="จำนวนวันลารวม" value={`${formatNumber(data?.summary.totalDays ?? 0)} วัน`} note="รวมจากจำนวนวันที่ระบบคำนวณ" icon={StackedBarChartOutlinedIcon} isLoading={isLoading} color="warning.main" />
        <KpiCard title="ประเภทลาสูงสุด" value={data?.summary.topLeaveType ?? "ยังไม่มีข้อมูล"} note={`หน่วยงานสูงสุด: ${data?.summary.topDepartment ?? "ยังไม่มีข้อมูล"}`} icon={PieChartOutlineOutlinedIcon} isLoading={isLoading} color="secondary.main" />
        <KpiCard title="ลาป่วยรวม" value={`${formatNumber(data?.summary.sickDays ?? 0)} วัน`} note="SICK_LEAVE" icon={InsightsOutlinedIcon} isLoading={isLoading} color="info.main" />
        <KpiCard title="ลากิจรวม" value={`${formatNumber(data?.summary.personalDays ?? 0)} วัน`} note="PERSONAL_LEAVE" icon={InsightsOutlinedIcon} isLoading={isLoading} color="secondary.main" />
        <KpiCard title="ลาพักผ่อนรวม" value={`${formatNumber(data?.summary.vacationDays ?? 0)} วัน`} note="VACATION_LEAVE" icon={InsightsOutlinedIcon} isLoading={isLoading} color="success.main" />
        <KpiCard title="ช่วงข้อมูล" value={data ? `${formatThaiDate(data.filters.startDate)} - ${formatThaiDate(data.filters.endDate)}` : "-"} note={data ? `สถานะ: ${data.filters.status}` : "ตามตัวกรอง"} icon={TableChartOutlinedIcon} isLoading={isLoading} />

        <Grid item xs={12} lg={7}>
          <MonthlyTrendChart rows={data?.monthlyTrend ?? []} isLoading={isLoading} />
        </Grid>
        <Grid item xs={12} lg={5}>
          <LeaveTypeDonut rows={data?.leaveTypeBreakdown ?? []} isLoading={isLoading} />
        </Grid>
        <Grid item xs={12} lg={7}>
          <DepartmentStackedChart rows={data?.departmentStacked ?? []} isLoading={isLoading} />
        </Grid>
        <Grid item xs={12} lg={5}>
          <HeatmapChart rows={data?.heatmap ?? []} isLoading={isLoading} />
        </Grid>
        <Grid item xs={12}>
          <AnalyticsTable rows={visibleItems} total={items.length} page={page} pageSize={pageSize} onPageChange={setPage} onPageSizeChange={setPageSize} />
        </Grid>
      </Grid>
    </Box>
  );
}

function KpiCard({ title, value, note, icon: Icon, color = "primary.main", isLoading }: { title: string; value?: number | string; note: string; icon: SvgIconComponent; color?: string; isLoading: boolean }) {
  return (
    <Grid item xs={12} sm={6} lg={3}>
      <AnalyticsCard>
        <Stack spacing={1.5}>
          <Stack direction="row" spacing={1.25} alignItems="flex-start">
            <IconBubble icon={Icon} />
            <Box sx={{ minWidth: 0 }}>
              <Typography fontWeight={900}>{title}</Typography>
              <Typography variant="body2" color="text.secondary">{note}</Typography>
            </Box>
          </Stack>
          {isLoading ? <Skeleton width={120} height={44} /> : <Typography variant="h4" sx={{ color, fontWeight: 900, wordBreak: "break-word" }}>{value ?? 0}</Typography>}
        </Stack>
      </AnalyticsCard>
    </Grid>
  );
}

function MonthlyTrendChart({ rows, isLoading }: { rows: LeaveAnalyticsMonthlyTrend[]; isLoading: boolean }) {
  const max = Math.max(...rows.map((row) => row.totalDays), 1);
  return (
    <AnalyticsCard title="Monthly Trend" subtitle="แนวโน้มจำนวนวันลาและจำนวนรายการตามเดือน">
      {isLoading ? <SkeletonRows count={8} /> : rows.some((row) => row.totalDays > 0 || row.requestCount > 0) ? (
        <Stack spacing={1.2}>
          {rows.map((row) => (
            <Box key={row.month}>
              <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.5 }}>
                <Typography variant="body2" color="text.secondary">{formatMonth(row.month)}</Typography>
                <Typography variant="body2" fontWeight={900}>{formatNumber(row.totalDays)} วัน · {row.requestCount} รายการ</Typography>
              </Stack>
              <Box sx={(theme) => ({ height: 12, borderRadius: 99, bgcolor: alpha(theme.palette.primary.main, 0.08), overflow: "hidden" })}>
                <Box sx={{ width: `${Math.min(100, (row.totalDays / max) * 100)}%`, height: "100%", bgcolor: brandColors.accent }} />
              </Box>
            </Box>
          ))}
        </Stack>
      ) : <EmptyState />}
    </AnalyticsCard>
  );
}

function DepartmentStackedChart({ rows, isLoading }: { rows: LeaveAnalyticsDepartmentStack[]; isLoading: boolean }) {
  const max = Math.max(...rows.map((row) => row.totalDays), 1);
  return (
    <AnalyticsCard title="Stacked Bar by Department" subtitle="Top 10 หน่วยงาน แยกตามประเภทลาหลัก">
      {isLoading ? <SkeletonRows count={8} /> : rows.length ? (
        <Stack spacing={1.4}>
          {rows.map((row) => (
            <Box key={row.departmentId ?? row.departmentName}>
              <Stack direction="row" justifyContent="space-between" spacing={1} sx={{ mb: 0.5 }}>
                <Typography variant="body2" color="text.secondary" noWrap>{row.departmentName}</Typography>
                <Typography variant="body2" fontWeight={900}>{formatNumber(row.totalDays)} วัน</Typography>
              </Stack>
              <Stack direction="row" sx={(theme) => ({ width: `${Math.max(4, (row.totalDays / max) * 100)}%`, minWidth: row.totalDays > 0 ? 24 : 0, height: 14, borderRadius: 99, bgcolor: alpha(theme.palette.primary.main, 0.08), overflow: "hidden" })}>
                <Segment value={row.sickDays} total={row.totalDays} color="#38BDF8" />
                <Segment value={row.personalDays} total={row.totalDays} color={brandColors.accent} />
                <Segment value={row.vacationDays} total={row.totalDays} color="#22C55E" />
              </Stack>
            </Box>
          ))}
          <LegendRow />
        </Stack>
      ) : <EmptyState />}
    </AnalyticsCard>
  );
}

function LeaveTypeDonut({ rows, isLoading }: { rows: LeaveAnalyticsLeaveTypeBreakdown[]; isLoading: boolean }) {
  const total = rows.reduce((sum, row) => sum + row.totalDays, 0);
  const colors = ["#38BDF8", brandColors.accent, "#22C55E", "#A78BFA", "#F97316", "#94A3B8"];
  const gradient = buildConicGradient(rows.map((row, index) => ({ value: row.totalDays, color: colors[index % colors.length] })));

  return (
    <AnalyticsCard title="Pie / Donut by Leave Type" subtitle="สัดส่วนจำนวนวันลาแยกตามประเภท">
      {isLoading ? <SkeletonRows count={5} /> : total > 0 ? (
        <Stack spacing={2} alignItems="center">
          <Box sx={{ width: 180, height: 180, borderRadius: "50%", background: gradient, position: "relative", boxShadow: `0 16px 32px ${alpha("#0F766E", 0.12)}` }}>
            <Box sx={{ position: "absolute", inset: 34, borderRadius: "50%", bgcolor: "background.paper", display: "grid", placeItems: "center", textAlign: "center", px: 2 }}>
              <Box>
                <Typography variant="h5" fontWeight={900}>{formatNumber(total)}</Typography>
                <Typography variant="caption" color="text.secondary">วัน</Typography>
              </Box>
            </Box>
          </Box>
          <Stack spacing={1} sx={{ width: "100%" }}>
            {rows.map((row, index) => (
              <Stack key={row.leaveTypeId} direction="row" spacing={1} justifyContent="space-between" alignItems="center">
                <Stack direction="row" spacing={1} alignItems="center" sx={{ minWidth: 0 }}>
                  <Box sx={{ width: 10, height: 10, borderRadius: 99, bgcolor: colors[index % colors.length] }} />
                  <Typography variant="body2" noWrap>{getLeaveTypeLabel(row.leaveTypeName)}</Typography>
                </Stack>
                <Typography variant="body2" fontWeight={900}>{formatNumber(row.totalDays)} วัน</Typography>
              </Stack>
            ))}
          </Stack>
        </Stack>
      ) : <EmptyState />}
    </AnalyticsCard>
  );
}

function HeatmapChart({ rows, isLoading }: { rows: LeaveAnalyticsHeatmap[]; isLoading: boolean }) {
  const max = Math.max(...rows.map((row) => row.uniqueUsers), 1);
  return (
    <AnalyticsCard title="Heatmap" subtitle="ความหนาแน่นการลาในแต่ละวัน สีเข้มคือมีผู้ลามาก">
      {isLoading ? <SkeletonRows count={5} /> : rows.length ? (
        <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(18px, 1fr))", gap: 0.75 }}>
          {rows.map((row) => (
            <Box
              key={row.date}
              title={`${formatThaiDate(row.date)}: ${row.uniqueUsers} คน, ${formatNumber(row.totalDays)} วัน`}
              sx={(theme) => ({
                aspectRatio: "1 / 1",
                borderRadius: 0.75,
                bgcolor: row.uniqueUsers === 0 ? alpha(theme.palette.primary.main, 0.06) : alpha(theme.palette.success.main, 0.18 + Math.min(0.72, row.uniqueUsers / max * 0.72)),
                border: `1px solid ${alpha(theme.palette.primary.main, 0.08)}`,
              })}
            />
          ))}
        </Box>
      ) : <EmptyState />}
    </AnalyticsCard>
  );
}

function AnalyticsTable({ rows, total, page, pageSize, onPageChange, onPageSizeChange }: { rows: LeaveAnalyticsTableItem[]; total: number; page: number; pageSize: number; onPageChange: (page: number) => void; onPageSizeChange: (pageSize: number) => void }) {
  return (
    <AnalyticsCard title="รายการข้อมูลการลา" subtitle="ข้อมูลตามตัวกรองปัจจุบัน">
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>วันที่</TableCell>
            <TableCell>ผู้ลา</TableCell>
            <TableCell>หน่วยงาน</TableCell>
            <TableCell>ประเภทลา</TableCell>
            <TableCell align="right">จำนวนวัน</TableCell>
            <TableCell>สถานะ</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.length ? rows.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell>{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</TableCell>
              <TableCell>{item.fullname ?? "-"}</TableCell>
              <TableCell>{item.departmentName ?? "-"}</TableCell>
              <TableCell>{getLeaveTypeWithDurationLabel(item.leaveTypeName, item.durationType)}</TableCell>
              <TableCell align="right">{formatNumber(item.totalDays)}</TableCell>
              <TableCell><Chip size="small" label={getLeaveStatusLabel(item.status)} /></TableCell>
            </TableRow>
          )) : (
            <TableRow><TableCell colSpan={6}>ไม่พบข้อมูลการลาตามเงื่อนไขที่เลือก</TableCell></TableRow>
          )}
        </TableBody>
      </Table>
      <TablePagination
        component="div"
        count={total}
        page={page}
        onPageChange={(_, nextPage) => onPageChange(nextPage)}
        rowsPerPage={pageSize}
        onRowsPerPageChange={(event) => {
          onPageSizeChange(Number(event.target.value));
          onPageChange(0);
        }}
        rowsPerPageOptions={[10, 20, 50]}
        labelRowsPerPage="จำนวนรายการต่อหน้า"
        labelDisplayedRows={({ from, to, count }) => `${from}-${to} จาก ${count}`}
      />
    </AnalyticsCard>
  );
}

function AnalyticsCard({ title, subtitle, children }: { title?: string; subtitle?: string; children: ReactNode }) {
  const theme = useTheme();
  return (
    <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}`, boxShadow: `0 16px 34px ${alpha(theme.palette.primary.dark, 0.08)}` }}>
      <CardContent>
        <Stack spacing={2}>
          {title && (
            <Box>
              <Typography variant="h6" fontWeight={900}>{title}</Typography>
              {subtitle && <Typography variant="body2" color="text.secondary">{subtitle}</Typography>}
            </Box>
          )}
          {children}
        </Stack>
      </CardContent>
    </Card>
  );
}

function IconBubble({ icon: Icon }: { icon: SvgIconComponent }) {
  return (
    <Box sx={(theme) => ({ color: "primary.main", bgcolor: alpha(theme.palette.primary.main, 0.08), borderRadius: 2, p: 1 })}>
      <Icon fontSize="small" />
    </Box>
  );
}

function Segment({ value, total, color }: { value: number; total: number; color: string }) {
  return <Box sx={{ width: `${total > 0 ? Math.max((value / total) * 100, value > 0 ? 3 : 0) : 0}%`, bgcolor: color }} />;
}

function LegendRow() {
  return (
    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
      <Legend color="#38BDF8" label="ลาป่วย" />
      <Legend color={brandColors.accent} label="ลากิจส่วนตัว" />
      <Legend color="#22C55E" label="ลาพักผ่อน" />
    </Stack>
  );
}

function Legend({ color, label }: { color: string; label: string }) {
  return (
    <Stack direction="row" spacing={0.75} alignItems="center">
      <Box sx={{ width: 10, height: 10, borderRadius: 99, bgcolor: color }} />
      <Typography variant="caption" color="text.secondary">{label}</Typography>
    </Stack>
  );
}

function SkeletonRows({ count = 5 }: { count?: number }) {
  return (
    <Stack spacing={1}>
      {Array.from({ length: count }).map((_, index) => <Skeleton key={index} height={28} />)}
    </Stack>
  );
}

function EmptyState() {
  return <Typography color="text.secondary">ไม่พบข้อมูลการลาตามเงื่อนไขที่เลือก</Typography>;
}

function sanitizeFilters(filters: LeaveAnalyticsQuery): LeaveAnalyticsQuery {
  return {
    ...filters,
    year: filters.year || undefined,
    month: filters.month || undefined,
    departmentId: filters.departmentId || undefined,
    leaveTypeId: filters.leaveTypeId || undefined,
    status: filters.status || "Approved",
    coreOnly: filters.coreOnly ?? true,
  };
}

function formatNumber(value: number) {
  return value.toLocaleString("th-TH", { maximumFractionDigits: 1 });
}

function getFiscalYear(year: number, month: number) {
  return month >= 10 ? year + 1 : year;
}

function buildYearOptions(currentYear: number) {
  return Array.from({ length: 7 }, (_, index) => currentYear - 3 + index);
}

function buildFiscalYearOptions(currentFiscalYear: number) {
  return Array.from({ length: 7 }, (_, index) => currentFiscalYear - 3 + index);
}

function toThaiDisplayYear(year: number) {
  return year >= 2400 ? year : year + 543;
}

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

function formatMonth(value: string) {
  const [year, month] = value.split("-").map(Number);
  if (!year || !month) return value;
  return `${thaiMonths[month - 1] ?? ""} ${toThaiDisplayYear(year)}`.trim();
}

function buildConicGradient(parts: Array<{ value: number; color: string }>) {
  const total = parts.reduce((sum, item) => sum + item.value, 0);
  if (total <= 0) return "#E5E7EB";

  let cursor = 0;
  const stops = parts
    .filter((item) => item.value > 0)
    .map((item) => {
      const start = cursor;
      cursor += item.value / total * 100;
      return `${item.color} ${start}% ${cursor}%`;
    });
  return `conic-gradient(${stops.join(", ")})`;
}
