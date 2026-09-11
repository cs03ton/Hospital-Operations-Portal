import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import LocalShippingOutlinedIcon from "@mui/icons-material/LocalShippingOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import RateReviewOutlinedIcon from "@mui/icons-material/RateReviewOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Divider, Skeleton, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  FLEET_DASHBOARD_QUERY_KEY,
  getFleetDashboard,
  getFleetFeedbackEligibleTrips,
  type FleetDashboardApproval,
  type FleetDashboardData,
} from "../api/fleetApi";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";
import { dashboardPollingOptions } from "../config/queryPolling";

type Metric = { label: string; value: number; suffix?: string; tone?: "normal" | "warning" | "error" };

export function FleetDashboardPage() {
  const query = useQuery({
    queryKey: FLEET_DASHBOARD_QUERY_KEY,
    queryFn: getFleetDashboard,
    ...dashboardPollingOptions,
    retry: 1,
  });
  const feedbackQuery = useQuery({
    queryKey: ["fleet-feedback-eligible-trips"],
    queryFn: getFleetFeedbackEligibleTrips,
    ...dashboardPollingOptions,
    retry: false,
  });

  if (query.isLoading) return <DashboardLoading />;
  if (query.isError || !query.data) return <DashboardError retry={() => void query.refetch()} fetching={query.isFetching} />;

  const data = query.data;
  const hasRoleSection = Boolean(data.requester || data.driver || data.dispatcher || data.adminReviewer || data.director || data.admin || data.feedback);
  return (
    <Box sx={{ pb: 4 }}>
      <Stack direction={{ xs: "column", sm: "row" }} alignItems={{ sm: "flex-start" }} justifyContent="space-between" spacing={1}>
        <Box sx={{ flex: 1 }}>
          <PageHeader title="Dashboard รถ" subtitle="ภาพรวมงานยานพาหนะตามบทบาทและสิทธิ์ของคุณ" />
        </Box>
        <Stack direction="row" spacing={1} alignItems="center">
          {data.capabilities.delegatedPermissions.length > 0 && <Chip color="info" label={`รับมอบหมาย ${data.capabilities.delegatedPermissions.length} สิทธิ์`} />}
          <Button variant="outlined" startIcon={query.isFetching ? <CircularProgress size={16} /> : <RefreshOutlinedIcon />} disabled={query.isFetching} onClick={() => void query.refetch()}>รีเฟรช</Button>
        </Stack>
      </Stack>

      <MetricGrid metrics={[
        { label: "งานเดินทางวันนี้", value: data.shared.todayJobs },
        { label: "รถพร้อมใช้งาน", value: data.shared.availableVehicles },
        { label: "รถกำลังใช้งาน", value: data.shared.inUseVehicles },
        { label: "รถไม่พร้อมใช้งาน", value: data.shared.unavailableVehicles, tone: data.shared.unavailableVehicles > 0 ? "warning" : "normal" },
      ]} />

      {!hasRoleSection && <Alert severity="info" sx={{ mt: 3 }}>ยังไม่มีข้อมูล Dashboard ที่ตรงกับขอบเขตสิทธิ์ของคุณ</Alert>}
      <Stack spacing={2.5} sx={{ mt: 3 }}>
        {feedbackQuery.data && feedbackQuery.data.length > 0 && <Card variant="outlined" sx={{ borderColor: "warning.light", bgcolor: "warning.50" }}><CardContent><Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}><RateReviewOutlinedIcon color="warning" sx={{ fontSize: 36 }} /><Box sx={{ flex: 1 }}><Typography variant="h6" fontWeight={900} color="primary">การเดินทางล่าสุด</Typography><Typography color="text.secondary">คุณสามารถให้ Feedback การเดินทางได้ตามความสมัครใจ</Typography></Box></Stack><Stack spacing={1.25} sx={{ mt: 2 }}>{feedbackQuery.data.map(trip => <Box key={trip.tripId} sx={{ p: 1.5, borderRadius: 2, bgcolor: "background.paper", border: 1, borderColor: "divider" }}><Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1}><Box><Typography fontWeight={800}>{trip.requestNo} · {trip.destination}</Typography><Typography variant="body2" color="text.secondary">{formatThaiDateTime(trip.tripDate)} · รถ {trip.vehicleDisplay} · คนขับ {trip.driverDisplay}</Typography><Typography variant="caption" color="warning.dark">ให้ Feedback ได้ถึง {formatThaiDateTime(trip.feedbackDeadline)}</Typography></Box><Button component={Link} to={`/fleet/trips/${trip.tripId}/feedback`} variant="contained" color="warning">ให้ Feedback การเดินทาง</Button></Stack></Box>)}</Stack></CardContent></Card>}
        {data.requester && <RequesterSection data={data} />}
        {data.driver && <RoleSection title="งานขับรถของฉัน" icon={<LocalShippingOutlinedIcon />} actionLabel="เปิดงานขับรถ" actionPath="/fleet/driver/jobs" badge={data.badges.myDriverJobs} metrics={[
          { label: "งานวันนี้", value: data.driver.todayJobs }, { label: "งานถัดไป", value: data.driver.upcomingJobs },
          { label: "รอดำเนินการ", value: data.driver.actionRequired, tone: data.driver.actionRequired ? "warning" : "normal" },
          { label: "สำเร็จเดือนนี้", value: data.driver.completedThisMonth }, { label: "ระยะทางเดือนนี้", value: data.driver.distanceThisMonth, suffix: " กม." },
        ]} />}
        {data.dispatcher && <RoleSection title="งานจัดรถ" icon={<DirectionsCarOutlinedIcon />} actionLabel="เปิดคิวจัดรถ" actionPath="/fleet/dispatch" badge={data.badges.dispatchQueue} metrics={[
          { label: "รอจัดรถ", value: data.dispatcher.pendingDispatch, tone: data.dispatcher.pendingDispatch ? "warning" : "normal" },
          { label: "ส่งกลับ Dispatcher", value: data.dispatcher.returnedToDispatcher }, { label: "รถพร้อม", value: data.dispatcher.availableVehicles },
          { label: "รถติดงาน", value: data.dispatcher.busyVehicles }, { label: "คนขับพร้อม", value: data.dispatcher.availableDrivers },
          { label: "คนขับติดงาน", value: data.dispatcher.busyDrivers }, { label: "งานเกินกำหนด", value: data.dispatcher.overdueJobs, tone: data.dispatcher.overdueJobs ? "error" : "normal" },
        ]} />}
        {data.adminReviewer && <ApprovalSection title="ตรวจคำขอ" path="/fleet/review" badge={data.badges.reviewQueue} data={data.adminReviewer} />}
        {data.director && <ApprovalSection title="อนุมัติคำขอ" path="/fleet/approvals" badge={data.badges.approvalQueue} data={data.director} />}
        {data.admin && <RoleSection title="ภาพรวมผู้ดูแล Fleet" icon={<FactCheckOutlinedIcon />} actionLabel="เปิดรายงาน" actionPath="/fleet/reports" metrics={[
          { label: "คำขอเดือนนี้", value: data.admin.requestsThisMonth }, { label: "ทริปเดือนนี้", value: data.admin.tripsThisMonth },
          { label: "ระยะทางเดือนนี้", value: data.admin.distanceThisMonth, suffix: " กม." }, { label: "ยกเลิกเดือนนี้", value: data.admin.cancelledThisMonth },
          { label: "งานเกินกำหนด", value: data.admin.overdueJobs, tone: data.admin.overdueJobs ? "error" : "normal" },
          { label: "Outbox ล้มเหลว", value: data.admin.failedOutbox, tone: data.admin.failedOutbox ? "error" : "normal" },
        ]} />}
        {data.feedback && <RoleSection title="Feedback การเดินทาง" icon={<RateReviewOutlinedIcon />} actionLabel="เปิดรายงาน Feedback" actionPath="/fleet/reports" metrics={[
          { label: "Feedback เดือนนี้", value: data.feedback.feedbackCount },
          { label: "อัตราการตอบกลับ", value: data.feedback.responseRate, suffix: "%" },
          { label: "คะแนนรวมเฉลี่ย", value: data.feedback.overallAverage ?? 0, suffix: "/5" },
          { label: "ความปลอดภัยเฉลี่ย", value: data.feedback.safetyAverage ?? 0, suffix: "/5" },
          { label: "แจ้งเหตุการณ์", value: data.feedback.incidentCount, tone: data.feedback.incidentCount ? "warning" : "normal" },
          { label: "ควรตรวจสอบ", value: data.feedback.attentionCount, tone: data.feedback.attentionCount ? "error" : "normal" },
        ]} />}
      </Stack>
      <Typography variant="caption" color="text.secondary" display="block" textAlign="right" sx={{ mt: 2 }}>อัปเดตล่าสุด {formatThaiDateTime(data.generatedAt)}</Typography>
    </Box>
  );
}

function RequesterSection({ data }: { data: FleetDashboardData }) {
  const requester = data.requester!;
  const statusMetrics = Object.entries(requester.statusCounts).map(([status, value]) => ({ label: getFleetStatusLabel(status), value }));
  return <RoleSection title="คำขอใช้รถของฉัน" icon={<EventAvailableOutlinedIcon />} actionLabel="ดูคำขอทั้งหมด" actionPath="/fleet/requests" metrics={[{ label: "ต้องดำเนินการ", value: requester.actionRequired, tone: requester.actionRequired ? "warning" : "normal" }, ...statusMetrics]}>
    {requester.nextTrip ? <Box sx={{ mt: 2, p: 2, bgcolor: "action.hover", borderRadius: 2 }}><Typography variant="overline" color="text.secondary">การเดินทางถัดไป</Typography><Typography fontWeight={700}>{requester.nextTrip.requestNo} · {requester.nextTrip.destination}</Typography><Typography variant="body2">{formatThaiDateTime(requester.nextTrip.departureAt)} · {getFleetStatusLabel(requester.nextTrip.status)}</Typography><Typography variant="body2" color="text.secondary">รถ {requester.nextTrip.vehicle ?? "ยังไม่จัดรถ"} · คนขับ {requester.nextTrip.driver ?? "ยังไม่จัดคนขับ"}</Typography><Button component={Link} to={`/fleet/requests/${requester.nextTrip.id}`} size="small" sx={{ mt: 1 }}>ดูรายละเอียด</Button></Box> : <Typography color="text.secondary" sx={{ mt: 2 }}>ยังไม่มีการเดินทางที่กำลังจะมาถึง</Typography>}
  </RoleSection>;
}

function ApprovalSection({ title, path, badge, data }: { title: string; path: string; badge: number; data: FleetDashboardApproval }) {
  return <RoleSection title={title} icon={<FactCheckOutlinedIcon />} actionLabel={`เปิด${title}`} actionPath={path} badge={badge} metrics={[
    { label: "รอดำเนินการ", value: data.pending, tone: data.pending ? "warning" : "normal" }, { label: "เร่งด่วน", value: data.urgent, tone: data.urgent ? "error" : "normal" },
    { label: "ใกล้เวลาเดินทาง", value: data.nearDeparture, tone: data.nearDeparture ? "warning" : "normal" }, { label: "เสร็จวันนี้", value: data.completedToday },
    { label: "ส่งกลับวันนี้", value: data.returnedToday }, { label: "ปฏิเสธวันนี้", value: data.rejectedToday },
  ]} />;
}

function RoleSection({ title, icon, actionLabel, actionPath, badge, metrics, children }: { title:string;icon:React.ReactNode;actionLabel:string;actionPath:string;badge?:number;metrics:Metric[];children?:React.ReactNode }) {
  return <Card variant="outlined" sx={{ boxShadow: "none", overflow: "hidden" }}><CardContent sx={{ p: { xs: 2, md: 2.5 } }}><Stack direction={{ xs: "column", sm: "row" }} alignItems={{ sm: "center" }} justifyContent="space-between" spacing={1.5}><Stack direction="row" alignItems="center" spacing={1.25}><Box sx={(theme) => ({ color: "primary.main", bgcolor: alpha(theme.palette.primary.main, 0.08), borderRadius: 2, width: 40, height: 40, display: "grid", placeItems: "center" })}>{icon}</Box><Box><Typography variant="h6" color="primary" fontWeight={800}>{title}</Typography><Typography variant="caption" color="text.secondary">ข้อมูลล่าสุดตามขอบเขตสิทธิ์</Typography></Box>{typeof badge === "number" && badge > 0 && <Chip size="small" color="warning" label={badge.toLocaleString("th-TH")} />}</Stack><Button component={Link} to={actionPath} variant="outlined">{actionLabel}</Button></Stack><Divider sx={{ my: 2 }} /><MetricGrid metrics={metrics} compact />{children}</CardContent></Card>;
}

function MetricGrid({ metrics, compact = false }: { metrics: Metric[]; compact?: boolean }) {
  return <Box sx={{ display: "grid", gridTemplateColumns: { xs: "repeat(2, minmax(0, 1fr))", sm: `repeat(${Math.min(compact ? 3 : 4, Math.max(1, metrics.length))}, minmax(0, 1fr))` }, gap: 1.5 }}>{metrics.map(metric => <Box key={metric.label} sx={(theme) => ({ p: compact ? 1.5 : 2, border: 1, borderColor: metric.tone === "error" ? "error.light" : metric.tone === "warning" ? "warning.light" : "divider", borderRadius: 2, bgcolor: metric.tone === "error" ? alpha(theme.palette.error.main, 0.045) : metric.tone === "warning" ? alpha(theme.palette.warning.main, 0.08) : alpha(theme.palette.primary.main, 0.025), minHeight: compact ? 82 : 104 })}><Typography variant="body2" color="text.secondary" fontWeight={600}>{metric.label}</Typography><Typography variant={compact ? "h6" : "h4"} fontWeight={900} color={metric.tone === "error" ? "error.main" : metric.tone === "warning" ? "warning.dark" : "primary.main"}>{metric.value.toLocaleString("th-TH")}{metric.suffix}</Typography></Box>)}</Box>;
}

function DashboardLoading() { return <Box><PageHeader title="Dashboard รถ" subtitle="ภาพรวมงานยานพาหนะตามบทบาทและสิทธิ์ของคุณ" /><Stack spacing={2}><Skeleton variant="rounded" height={112} /><Skeleton variant="rounded" height={220} /><Stack direction="row" spacing={1} alignItems="center"><CircularProgress size={18} /><Typography color="text.secondary">กำลังโหลด Dashboard รถ…</Typography></Stack></Stack></Box>; }
function DashboardError({ retry, fetching }: { retry:()=>void;fetching:boolean }) { return <Box><PageHeader title="Dashboard รถ" subtitle="ภาพรวมงานยานพาหนะตามบทบาทและสิทธิ์ของคุณ" /><Alert severity="error" action={<Button disabled={fetching} onClick={retry}>ลองใหม่</Button>}>ไม่สามารถโหลด Dashboard รถได้ กรุณาลองใหม่อีกครั้ง</Alert></Box>; }
