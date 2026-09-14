import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import PendingActionsOutlinedIcon from "@mui/icons-material/PendingActionsOutlined";
import QueryStatsOutlinedIcon from "@mui/icons-material/QueryStatsOutlined";
import SickOutlinedIcon from "@mui/icons-material/SickOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import TrendingUpOutlinedIcon from "@mui/icons-material/TrendingUpOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, MenuItem, Skeleton, Stack, TextField, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import type { SvgIconComponent } from "@mui/icons-material";
import { useMemo, useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { getExecutiveDashboard, type ExecutiveDashboard, type ExecutiveDepartmentLeave, type ExecutiveLeaveType, type ExecutiveMonthlyTrend, type ExecutiveRank, type ExecutiveTrendPoint, type ExecutiveYearlySummary } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { brandColors } from "../theme/theme";
import { dashboardPollingOptions } from "../config/queryPolling";
import { executiveViewOptions, parseExecutiveView, type ExecutiveView } from "./executiveDashboardView";

export function ExecutiveDashboardPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedView = parseExecutiveView(searchParams.get("view"));
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth() + 1;
  const currentFiscalYear = getFiscalYear(currentYear, currentMonth);
  const [trendMonth, setTrendMonth] = useState(0);
  const [fiscalYear, setFiscalYear] = useState(currentFiscalYear);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const fiscalYearOptions = useMemo(() => buildFiscalYearOptions(currentFiscalYear), [currentFiscalYear]);
  const queryParams = useMemo(() => ({ trendMonth, fiscalYear, startDate: startDate || undefined, endDate: endDate || undefined }), [trendMonth, fiscalYear, startDate, endDate]);
  const { data, isError, isLoading, isFetching, refetch } = useQuery({
    queryKey: ["dashboard", "executive", queryParams],
    queryFn: () => getExecutiveDashboard(queryParams),
    ...dashboardPollingOptions,
  });
  const selectedTrendLabel = data?.period ? ` ระหว่าง ${formatDate(data.period.startDate)} - ${formatDate(data.period.endDate)}` : "";
  const attentionItems = useMemo(() => {
    const rows = data?.attentionItems ?? [];
    if (selectedView === "overview") return rows;
    const moduleName = selectedView === "fleet" ? "Fleet" : selectedView === "repair" ? "Repair" : "Leave";
    return rows.filter((row) => row.module === moduleName);
  }, [data?.attentionItems, selectedView]);

  const changeView = (view: ExecutiveView) => {
    const next = new URLSearchParams(searchParams);
    next.set("view", view);
    setSearchParams(next);
  };

  return (
    <Box>
      <PageHeader title="Executive Dashboard" subtitle="ภาพรวมข้อมูลจริงทั้งโรงพยาบาลจากระบบลา ระบบขอรถ และระบบแจ้งซ่อม" />
      <Stack direction="row" spacing={1.5} flexWrap="wrap" useFlexGap sx={{ mb: 2 }}>
        <Button component={RouterLink} to="/dashboard" variant="outlined" startIcon={<ArrowBackOutlinedIcon />}>
          กลับไป Dashboard Hub
        </Button>
        <Button variant="contained" startIcon={<RefreshOutlinedIcon />} disabled={isFetching} onClick={() => void refetch()}>
          {isFetching ? "กำลังอัปเดต" : "Refresh"}
        </Button>
        <Typography variant="body2" color="text.secondary" sx={{ alignSelf: "center" }}>
          อัปเดตล่าสุด {data?.generatedAtUtc ? formatDateTime(data.generatedAtUtc) : "-"}
        </Typography>
      </Stack>

      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="flex-end" sx={{ mb: 2 }}>
        <TextField
          select
          size="small"
          label="เลือกข้อมูลระบบ"
          value={selectedView}
          onChange={(event) => changeView(event.target.value as ExecutiveView)}
          inputProps={{ "aria-label": "เลือกข้อมูลสำหรับ Executive Dashboard" }}
          sx={{ width: { xs: "100%", sm: 300 }, bgcolor: "background.paper" }}
        >
          {executiveViewOptions.map((option) => (
            <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>
          ))}
        </TextField>
      </Stack>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ xs: "stretch", md: "center" }}>
            <Box sx={{ flex: 1 }}>
              <Typography fontWeight={900}>ตัวกรองข้อมูลผู้บริหาร</Typography>
              <Typography variant="body2" color="text.secondary">
                ค่าเริ่มต้นเป็นปีงบประมาณ เลือกเดือนหรือกำหนดช่วงวันที่เองได้
              </Typography>
            </Box>
            <TextField select size="small" label="ช่วงเวลา" value={trendMonth} onChange={(event) => setTrendMonth(Number(event.target.value))} sx={{ minWidth: 150 }}>
              <MenuItem value={0}>ทั้งปี</MenuItem>
              {thaiMonths.map((month, index) => <MenuItem key={month} value={index + 1}>{month}</MenuItem>)}
            </TextField>
            <TextField select size="small" label="ปีงบประมาณ" value={fiscalYear} onChange={(event) => setFiscalYear(Number(event.target.value))} sx={{ minWidth: 170 }}>
              {fiscalYearOptions.map((year) => <MenuItem key={year} value={year}>{toThaiDisplayYear(year)}</MenuItem>)}
            </TextField>
            <TextField size="small" type="date" label="ตั้งแต่วันที่" value={startDate} onChange={(event) => setStartDate(event.target.value)} InputLabelProps={{ shrink: true }} />
            <TextField size="small" type="date" label="ถึงวันที่" value={endDate} onChange={(event) => setEndDate(event.target.value)} InputLabelProps={{ shrink: true }} />
            {(startDate || endDate) && <Button onClick={() => { setStartDate(""); setEndDate(""); }}>ล้างช่วงวันที่</Button>}
          </Stack>
        </CardContent>
      </Card>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ไม่สามารถโหลดข้อมูล Executive Dashboard ได้ กรุณาลองใหม่อีกครั้ง
        </Alert>
      )}

      <Grid container spacing={2}>
        {selectedView === "overview" && <>
        <Grid item xs={12}><SectionTitle title="ภาพรวมทั้งโรงพยาบาล" subtitle={selectedTrendLabel.trim() || "ปีงบประมาณปัจจุบัน"} /></Grid>
        <KpiCard title="บุคลากรลาวันนี้" value={data?.kpis.onLeaveToday} note="จำนวนบุคลากรไม่ซ้ำ" icon={SickOutlinedIcon} color="warning.main" isLoading={isLoading} />
        <KpiCard title="งานลารออนุมัติ" value={data?.kpis.pendingApprovals} note="คำขอที่ยังไม่จบกระบวนการ" icon={PendingActionsOutlinedIcon} color="warning.main" isLoading={isLoading} />
        <KpiCard title="งานรถที่ยังเปิด" value={data?.fleet.activeRequests} note="งานรถในช่วงที่เลือก" icon={DirectionsCarOutlinedIcon} color="info.main" isLoading={isLoading} />
        <KpiCard title="งานซ่อมที่ยังเปิด" value={data ? data.repairs.submitted + data.repairs.inProgress + data.repairs.waitingParts + data.repairs.awaitingAcceptance : undefined} note="รวมงานใหม่จนถึงรอตรวจรับ" icon={BuildOutlinedIcon} color="secondary.main" isLoading={isLoading} />
        <Grid item xs={12}><AttentionCard rows={attentionItems} isLoading={isLoading} /></Grid>
        <Grid item xs={12}><SystemHealthCard data={data} isLoading={isLoading} /></Grid>
        </>}

        {selectedView === "leave" && <>
        <Grid item xs={12}><SectionTitle title="ระบบลา" subtitle="กำลังคน การอนุมัติ และรูปแบบการลา" /></Grid>
        <KpiCard title="บุคลากรทั้งหมด" value={data?.kpis.totalActiveUsers} note="บัญชีที่เปิดใช้งาน" icon={GroupsOutlinedIcon} isLoading={isLoading} />
        <KpiCard title="มาปฏิบัติงานวันนี้" value={data?.kpis.presentToday} note="บุคลากรที่ไม่อยู่ในสถานะลา" icon={TaskAltOutlinedIcon} color="success.main" isLoading={isLoading} />
        <KpiCard title="ลาวันนี้" value={data?.kpis.onLeaveToday} note="นับจำนวนคนไม่ซ้ำ" icon={SickOutlinedIcon} color="warning.main" isLoading={isLoading} />
        <KpiCard title="รออนุมัติ" value={data?.kpis.pendingApprovals} note={`คิวผู้อำนวยการ ${formatNumber(data?.kpis.directorPendingApprovals ?? 0)} รายการ`} icon={PendingActionsOutlinedIcon} color="warning.main" isLoading={isLoading} />
        <KpiCard title="อนุมัติวันนี้" value={data?.kpis.approvedToday} note="คำขอที่อนุมัติในวันนี้" icon={TaskAltOutlinedIcon} color="success.main" isLoading={isLoading} />
        <KpiCard title="ไม่อนุมัติวันนี้" value={data?.kpis.rejectedToday} note="คำขอที่ไม่อนุมัติในวันนี้" icon={PendingActionsOutlinedIcon} color="error.main" isLoading={isLoading} />
        <KpiCard title="Leave Rate" value={`${formatNumber(data?.kpis.leaveRate ?? 0)}%`} note="ลาวันนี้เทียบกับบุคลากรทั้งหมด" icon={TrendingUpOutlinedIcon} color="info.main" isLoading={isLoading} />
        <KpiCard title="Approval SLA" value={data?.kpis.approvalSlaHours == null ? "ยังไม่มีข้อมูล" : `${formatNumber(data.kpis.approvalSlaHours)} ชม.`} note="ค่าเฉลี่ย submit ถึงจบกระบวนการ" icon={QueryStatsOutlinedIcon} color="secondary.main" isLoading={isLoading} />

        <Grid item xs={12} lg={4}>
          <ExecutiveSummaryCard data={data} isLoading={isLoading} />
        </Grid>
        <Grid item xs={12} lg={8}>
          <MonthlyTrendCard rows={data?.monthlyTrend ?? []} isLoading={isLoading} selectedTrendLabel={selectedTrendLabel} />
        </Grid>

        <Grid item xs={12} md={6} lg={4}>
          <DepartmentCard rows={data?.leaveByDepartment ?? []} isLoading={isLoading} selectedTrendLabel={selectedTrendLabel} />
        </Grid>
        <Grid item xs={12} md={6} lg={4}>
          <LeaveTypeCard rows={data?.leaveByType ?? []} isLoading={isLoading} selectedTrendLabel={selectedTrendLabel} />
        </Grid>
        <Grid item xs={12} lg={4}>
          <YearlySummaryCard rows={data?.yearlySummary ?? []} isLoading={isLoading} selectedFiscalYear={fiscalYear} />
        </Grid>
        <Grid item xs={12}><AttentionCard rows={attentionItems} isLoading={isLoading} /></Grid>
        </>}

        {selectedView === "fleet" && <>
        <Grid item xs={12}><SectionTitle title="ระบบขอรถ" subtitle="คำขอ การเดินทาง และงานที่ต้องติดตาม" /></Grid>
        <KpiCard title="คำขอรถทั้งหมด" value={data?.fleet.totalRequests} note="ตามช่วงเวลาที่เลือก" icon={DirectionsCarOutlinedIcon} isLoading={isLoading} />
        <KpiCard title="งานรถที่ยังเปิด" value={data?.fleet.activeRequests} note="อยู่ระหว่าง workflow" icon={PendingActionsOutlinedIcon} color="warning.main" isLoading={isLoading} />
        <KpiCard title="ทริปเสร็จสิ้น" value={data?.fleet.completedTrips} note="เดินทางเสร็จแล้ว" icon={TaskAltOutlinedIcon} color="success.main" isLoading={isLoading} />
        <KpiCard title="งานฉุกเฉินค้าง" value={data?.fleet.emergencyOpen} note={`อัตราจบทริป ${formatNumber(data?.fleet.completionRate ?? 0)}%`} icon={WarningAmberOutlinedIcon} color="error.main" isLoading={isLoading} />
        <Grid item xs={12} lg={6}><OperationsTrendCard title="แนวโน้มคำขอรถ" rows={data?.fleet.monthlyTrend ?? []} isLoading={isLoading} /></Grid>
        <Grid item xs={12} md={6} lg={3}><RankCard title="หน่วยงานที่ขอรถสูงสุด" rows={data?.fleet.topDepartments ?? []} isLoading={isLoading} /></Grid>
        <Grid item xs={12} md={6} lg={3}><RankCard title="ปลายทางที่ใช้บ่อย" rows={data?.fleet.topDestinations ?? []} isLoading={isLoading} /></Grid>
        <Grid item xs={12}><AttentionCard rows={attentionItems} isLoading={isLoading} /></Grid>
        </>}

        {selectedView === "repair" && <>
        <Grid item xs={12}><SectionTitle title="ระบบแจ้งซ่อม" subtitle="ภาระงานทีม IT และช่างทั่วไป" /></Grid>
        <KpiCard title="แจ้งใหม่" value={data?.repairs.submitted} note="ยังไม่ได้เริ่มดำเนินการ" icon={BuildOutlinedIcon} color="info.main" isLoading={isLoading} />
        <KpiCard title="กำลังดำเนินการ" value={data?.repairs.inProgress} note="ทีมกำลังแก้ไข" icon={QueryStatsOutlinedIcon} isLoading={isLoading} />
        <KpiCard title="รออะไหล่" value={data?.repairs.waitingParts} note="พักเวลาการดำเนินงาน" icon={PendingActionsOutlinedIcon} color="warning.main" isLoading={isLoading} />
        <KpiCard title="รอตรวจรับ" value={data?.repairs.awaitingAcceptance} note={`เร่งด่วนที่ยังเปิด ${formatNumber(data?.repairs.urgentOpen ?? 0)} งาน`} icon={TaskAltOutlinedIcon} color="secondary.main" isLoading={isLoading} />
        <Grid item xs={12} lg={6}><OperationsTrendCard title="แนวโน้มงานแจ้งซ่อม" rows={data?.repairs.monthlyTrend ?? []} isLoading={isLoading} /></Grid>
        <Grid item xs={12} md={6} lg={3}><RankCard title="ประเภทงานสูงสุด" rows={data?.repairs.topCategories ?? []} isLoading={isLoading} /></Grid>
        <Grid item xs={12} md={6} lg={3}><RankCard title="สัดส่วนทีมรับผิดชอบ" rows={data?.repairs.teamDistribution ?? []} isLoading={isLoading} /></Grid>
        <Grid item xs={12}><AttentionCard rows={attentionItems} isLoading={isLoading} /></Grid>
        </>}
      </Grid>
    </Box>
  );
}

function SectionTitle({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <Box sx={{ borderLeft: `4px solid ${brandColors.accent}`, pl: 1.5, py: 0.5 }}>
      <Typography variant="h5" fontWeight={900} color="primary.main">{title}</Typography>
      <Typography variant="body2" color="text.secondary">{subtitle}</Typography>
    </Box>
  );
}

function OperationsTrendCard({ title, rows, isLoading }: { title: string; rows: ExecutiveTrendPoint[]; isLoading: boolean }) {
  const max = Math.max(...rows.map((row) => row.total), 1);
  return (
    <DashboardCard title={title} subtitle="จำนวนรายการใหม่ งานเสร็จ และรายการที่ควรติดตามรายเดือน">
      {isLoading ? <SkeletonRows /> : rows.length ? (
        <Stack spacing={1.25}>
          {rows.map((row) => (
            <Box key={row.month}>
              <Stack direction="row" justifyContent="space-between" spacing={1} sx={{ mb: 0.5 }}>
                <Typography variant="body2" color="text.secondary">{formatMonth(row.month)}</Typography>
                <Typography variant="body2" fontWeight={800}>{formatNumber(row.total)} รายการ</Typography>
              </Stack>
              <Stack direction="row" sx={{ height: 10, borderRadius: 1, overflow: "hidden", bgcolor: "action.hover" }}>
                <Segment value={row.completed} max={max} color={brandColors.success} />
                <Segment value={row.attention} max={max} color={brandColors.warning} />
              </Stack>
            </Box>
          ))}
          <Stack direction="row" spacing={2}><Legend color={brandColors.success} label="เสร็จสิ้น" /><Legend color={brandColors.warning} label="ควรติดตาม" /></Stack>
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function RankCard({ title, rows, isLoading }: { title: string; rows: ExecutiveRank[]; isLoading: boolean }) {
  const max = Math.max(...rows.map((row) => row.count), 1);
  return (
    <DashboardCard title={title} subtitle="5 อันดับจากข้อมูลจริง">
      {isLoading ? <SkeletonRows /> : rows.length ? (
        <Stack spacing={1.25}>{rows.map((row) => <ProgressRow key={row.name} label={row.name} value={row.count} suffix="รายการ" max={max} />)}</Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function AttentionCard({ rows, isLoading }: { rows: ExecutiveDashboard["attentionItems"]; isLoading: boolean }) {
  return (
    <DashboardCard title="รายการที่ควรติดตาม" subtitle="งานฉุกเฉิน งานเกินกำหนด งานเร่งด่วน และงานซ่อมรออะไหล่ สูงสุด 5 รายการ">
      {isLoading ? <SkeletonRows /> : rows.length ? (
        <Stack spacing={1}>
          {rows.map((row) => (
            <Stack key={`${row.module}-${row.id}`} direction={{ xs: "column", sm: "row" }} alignItems={{ xs: "flex-start", sm: "center" }} spacing={1.5} sx={{ py: 1, borderBottom: "1px solid", borderColor: "divider" }}>
              <Chip size="small" color={row.module === "Fleet" ? "info" : row.module === "Repair" ? "warning" : "success"} label={row.module === "Fleet" ? "ขอรถ" : row.module === "Repair" ? "แจ้งซ่อม" : "ระบบลา"} />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography fontWeight={800}>{row.referenceNo} · {row.title}</Typography>
                <Typography variant="body2" color="text.secondary">{statusLabel(row.status)} · อัปเดต {formatDateTime(row.lastActivityAt)}</Typography>
              </Box>
              <Button component={RouterLink} to={row.url} variant="outlined">ดูรายละเอียด</Button>
            </Stack>
          ))}
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function KpiCard({ title, value, note, icon: Icon, color = "primary.main", isLoading }: { title: string; value?: number | string; note: string; icon: SvgIconComponent; color?: string; isLoading: boolean }) {
  return (
    <Grid item xs={12} sm={6} lg={3}>
      <DashboardCard>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <IconBubble icon={Icon} />
            <Box sx={{ minWidth: 0 }}>
              <Typography fontWeight={800}>{title}</Typography>
              <Typography variant="body2" color="text.secondary">{note}</Typography>
            </Box>
          </Stack>
          {isLoading ? <Skeleton width={120} height={52} /> : <Typography variant="h3" sx={{ color, fontWeight: 900 }}>{typeof value === "number" ? formatNumber(value) : value}</Typography>}
        </Stack>
      </DashboardCard>
    </Grid>
  );
}

function ExecutiveSummaryCard({ data, isLoading }: { data?: ExecutiveDashboard; isLoading: boolean }) {
  const summary = data?.todaySummary;
  return (
    <DashboardCard title="สรุปวันนี้" subtitle="ภาพรวมการลาและสถานะคำขอ">
      {isLoading ? <SkeletonRows /> : summary ? (
        <Stack spacing={1.2}>
          <SummaryRow label="ลาทั้งหมดวันนี้" value={`${formatNumber(summary.totalLeaveToday)} คน`} />
          <SummaryRow label="ลาป่วยวันนี้" value={`${formatNumber(summary.sickLeaveToday)} คน`} />
          <SummaryRow label="ลากิจวันนี้" value={`${formatNumber(summary.personalLeaveToday)} คน`} />
          <SummaryRow label="ลาพักผ่อนวันนี้" value={`${formatNumber(summary.vacationLeaveToday)} คน`} />
          <SummaryRow label="คำขอรออนุมัติ" value={`${formatNumber(summary.pendingApprovals)} รายการ`} />
          <SummaryRow label="อนุมัติวันนี้" value={`${formatNumber(summary.approvedToday)} รายการ`} />
          <SummaryRow label="ไม่อนุมัติวันนี้" value={`${formatNumber(summary.rejectedToday)} รายการ`} />
          <SummaryRow label="หน่วยงานลามากสุด" value={summary.topDepartmentToday ?? "ยังไม่มีข้อมูล"} />
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function MonthlyTrendCard({ rows, isLoading, selectedTrendLabel }: { rows: ExecutiveMonthlyTrend[]; isLoading: boolean; selectedTrendLabel: string }) {
  const max = Math.max(...rows.map((row) => row.totalDays), 1);
  return (
    <DashboardCard title="Monthly Leave Trend" subtitle={`ข้อมูลการลาประเภทหลัก${selectedTrendLabel}`}>
      {isLoading ? <SkeletonRows count={6} /> : rows.length ? (
        <Stack spacing={1.2}>
          {rows.map((row) => (
            <Box key={row.month}>
              <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.5 }}>
                <Typography variant="body2" color="text.secondary">{formatMonth(row.month)}</Typography>
                <Typography variant="body2" fontWeight={800}>{formatNumber(row.totalDays)} วัน</Typography>
              </Stack>
              <Stack direction="row" sx={{ height: 12, borderRadius: 99, overflow: "hidden", bgcolor: "action.hover" }}>
                <Segment value={row.sickLeaveDays} max={max} color="#38BDF8" />
                <Segment value={row.personalLeaveDays} max={max} color={brandColors.accent} />
                <Segment value={row.vacationLeaveDays} max={max} color="#22C55E" />
              </Stack>
            </Box>
          ))}
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Legend color="#38BDF8" label="ลาป่วย" />
            <Legend color={brandColors.accent} label="ลากิจ" />
            <Legend color="#22C55E" label="ลาพักผ่อน" />
          </Stack>
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function DepartmentCard({ rows, isLoading, selectedTrendLabel }: { rows: ExecutiveDepartmentLeave[]; isLoading: boolean; selectedTrendLabel: string }) {
  const max = Math.max(...rows.map((row) => row.userCount), 1);
  return (
    <DashboardCard title="Leave By Department" subtitle={`Top 10 หน่วยงาน${selectedTrendLabel}`}>
      {isLoading ? <SkeletonRows /> : rows.length ? (
        <Stack spacing={1.25}>
          {rows.map((row) => <ProgressRow key={row.departmentName} label={row.departmentName} value={row.userCount} suffix="คน" max={max} />)}
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function LeaveTypeCard({ rows, isLoading, selectedTrendLabel }: { rows: ExecutiveLeaveType[]; isLoading: boolean; selectedTrendLabel: string }) {
  const max = Math.max(...rows.map((row) => row.totalDays), 1);
  return (
    <DashboardCard title="Leave By Type" subtitle={`ประเภทลาหลัก${selectedTrendLabel}`}>
      {isLoading ? <SkeletonRows /> : rows.length ? (
        <Stack spacing={1.25}>
          {rows.map((row) => <ProgressRow key={row.leaveTypeCode} label={row.leaveTypeName} value={row.totalDays} suffix="วัน" max={max} />)}
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function YearlySummaryCard({ rows, isLoading, selectedFiscalYear }: { rows: ExecutiveYearlySummary[]; isLoading: boolean; selectedFiscalYear: number }) {
  const max = Math.max(...rows.map((row) => row.usedDays), 1);
  return (
    <DashboardCard title="Yearly Summary" subtitle={`ปีงบประมาณ ${toThaiDisplayYear(selectedFiscalYear)}`}>
      {isLoading ? <SkeletonRows /> : rows.length ? (
        <Stack spacing={1.25}>
          {rows.slice(0, 8).map((row) => <ProgressRow key={row.leaveTypeCode} label={row.leaveTypeName} value={row.usedDays} suffix="วัน" max={max} />)}
        </Stack>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function SystemHealthCard({ data, isLoading }: { data?: ExecutiveDashboard; isLoading: boolean }) {
  const health = data?.systemHealth;
  const items: Array<[string, string, string | null | undefined]> = health ? [
    ["API", health.api.status, health.version],
    ["Database", health.database.status, health.database.message],
    ["Storage", health.storage.status, health.storage.writable ? "Writable" : health.storage.message],
    ["LINE", health.line.status, health.line.enabled ? "Enabled" : "Disabled"],
    ["Disk", health.disk.status, health.disk.usedPercent == null ? health.disk.message : `${formatNumber(health.disk.usedPercent)}% used`],
    ["Backup", health.backup.status, health.backup.lastBackupAt ? formatDateTime(health.backup.lastBackupAt) : health.backup.message],
    ["Version", "Info", health.version],
    ["Environment", "Info", health.environment],
  ] : [];

  return (
    <DashboardCard title="System Health" subtitle="สถานะระบบสำคัญสำหรับผู้บริหาร โดยไม่แสดงข้อมูลลับ">
      {isLoading ? <SkeletonRows count={3} /> : items.length ? (
        <Grid container spacing={1.5}>
          {items.map(([label, status, message]) => (
            <Grid item xs={12} sm={6} md={3} key={label}>
              <Box sx={(theme) => ({ border: `1px solid ${alpha(theme.palette.primary.main, 0.12)}`, borderRadius: 2, p: 1.5, height: "100%" })}>
                <Stack spacing={1}>
                  <Stack direction="row" justifyContent="space-between" spacing={1}>
                    <Typography fontWeight={800}>{label}</Typography>
                    <Chip size="small" label={status} color={statusColor(status)} />
                  </Stack>
                  <Typography variant="body2" color="text.secondary">{message || "-"}</Typography>
                </Stack>
              </Box>
            </Grid>
          ))}
        </Grid>
      ) : <EmptyState />}
    </DashboardCard>
  );
}

function DashboardCard({ title, subtitle, children }: { title?: string; subtitle?: string; children: ReactNode }) {
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

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={2}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={800} textAlign="right">{value}</Typography>
    </Stack>
  );
}

function ProgressRow({ label, value, suffix, max }: { label: string; value: number; suffix: string; max: number }) {
  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" spacing={1} sx={{ mb: 0.5 }}>
        <Typography variant="body2" color="text.secondary" noWrap>{label}</Typography>
        <Typography variant="body2" fontWeight={800}>{formatNumber(value)} {suffix}</Typography>
      </Stack>
      <Box sx={(theme) => ({ height: 9, borderRadius: 99, bgcolor: alpha(theme.palette.primary.main, 0.08), overflow: "hidden" })}>
        <Box sx={{ width: `${Math.min(100, (value / max) * 100)}%`, height: "100%", bgcolor: brandColors.accent }} />
      </Box>
    </Box>
  );
}

function Segment({ value, max, color }: { value: number; max: number; color: string }) {
  return <Box sx={{ width: `${Math.max((value / max) * 100, value > 0 ? 3 : 0)}%`, bgcolor: color }} />;
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
  return <Typography color="text.secondary">ยังไม่มีข้อมูลสำหรับช่วงเวลานี้</Typography>;
}

function statusColor(status: string): "success" | "warning" | "error" | "default" | "info" {
  if (status === "Healthy") return "success";
  if (status === "Warning" || status === "Disabled") return "warning";
  if (status === "Unhealthy") return "error";
  if (status === "Info") return "info";
  return "default";
}

function formatNumber(value: number) {
  return value.toLocaleString("th-TH", { maximumFractionDigits: 1 });
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

function toThaiDisplayYear(year: number) {
  return year >= 2400 ? year : year + 543;
}

function getFiscalYear(year: number, month: number) {
  return month >= 10 ? year + 1 : year;
}

function buildFiscalYearOptions(currentFiscalYear: number) {
  return Array.from({ length: 7 }, (_, index) => currentFiscalYear - 3 + index);
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("th-TH", { dateStyle: "short", timeStyle: "short" });
}

function formatDate(value: string) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("th-TH", { dateStyle: "medium" });
}

function statusLabel(status: string) {
  const labels: Record<string, string> = {
    PendingDispatch: "รอจัดรถ", PendingAdminReview: "รอตรวจสอบ", PendingDirector: "รอผู้บริหารอนุมัติ",
    Approved: "อนุมัติแล้ว", PendingDriverAck: "รอคนขับรับทราบ", Ready: "พร้อมเดินทาง",
    InProgress: "กำลังดำเนินการ", CancellationPending: "รอพิจารณายกเลิก", Returned: "ส่งกลับแก้ไข",
    Submitted: "แจ้งใหม่", WaitingParts: "รออะไหล่", Resolved: "รอตรวจรับ", Closed: "ปิดงาน",
  };
  return labels[status] ?? status;
}
