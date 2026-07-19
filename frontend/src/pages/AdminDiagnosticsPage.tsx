import { useMemo, useState } from "react";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import FolderZipOutlinedIcon from "@mui/icons-material/FolderZipOutlined";
import PlayArrowOutlinedIcon from "@mui/icons-material/PlayArrowOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  Divider,
  FormControlLabel,
  Grid,
  LinearProgress,
  MenuItem,
  Paper,
  Skeleton,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createSupportBundle,
  downloadSupportBundle,
  getDiagnosticsHistory,
  getDiagnosticsLogs,
  getDiagnosticsRecentErrors,
  getDiagnosticsSummary,
  getSupportBundles,
  runDiagnosticTest,
  type DiagnosticRun,
  type DiagnosticServiceStatus,
  type DiagnosticsLogQuery,
  type SupportBundle,
  type SupportBundleRequest,
} from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

const diagnosticTests = [
  { key: "database", label: "Database" },
  { key: "storage", label: "Storage" },
  { key: "upload", label: "Upload" },
  { key: "pdf", label: "PDF" },
  { key: "line-text", label: "LINE Text" },
  { key: "line-flex", label: "LINE Flex" },
  { key: "backup", label: "Backup" },
  { key: "notification-worker", label: "Notification Worker" },
];

const defaultBundleRequest: SupportBundleRequest = {
  includeAppLogs: true,
  includeNginxLogs: true,
  includePostgresLogs: false,
  includeHealth: true,
  includeDeployInfo: true,
  includeMigrationInfo: true,
  includeLineSummary: true,
  includeBackupSummary: true,
  timeRangeHours: 24,
  reason: "",
};

export function AdminDiagnosticsPage() {
  const [tab, setTab] = useState(0);
  const [logQuery, setLogQuery] = useState<DiagnosticsLogQuery>({ source: "api", severity: "", search: "", page: 1, pageSize: 80 });
  const [bundleRequest, setBundleRequest] = useState<SupportBundleRequest>(defaultBundleRequest);
  const [lastRun, setLastRun] = useState<DiagnosticRun | null>(null);
  const queryClient = useQueryClient();
  const notification = useNotification();

  const summaryQuery = useQuery({
    queryKey: ["admin-diagnostics-summary"],
    queryFn: getDiagnosticsSummary,
    refetchOnWindowFocus: false,
  });

  const logsQuery = useQuery({
    queryKey: ["admin-diagnostics-logs", logQuery],
    queryFn: () => getDiagnosticsLogs(logQuery),
    enabled: tab === 2,
    refetchOnWindowFocus: false,
  });

  const recentErrorsQuery = useQuery({
    queryKey: ["admin-diagnostics-recent-errors"],
    queryFn: getDiagnosticsRecentErrors,
    enabled: tab === 3,
    refetchOnWindowFocus: false,
  });

  const historyQuery = useQuery({
    queryKey: ["admin-diagnostics-history"],
    queryFn: getDiagnosticsHistory,
    enabled: tab === 0 || tab === 4 || tab === 5,
    refetchOnWindowFocus: false,
  });

  const bundlesQuery = useQuery({
    queryKey: ["admin-support-bundles"],
    queryFn: getSupportBundles,
    enabled: tab === 4 || tab === 5,
    refetchOnWindowFocus: false,
  });

  const runMutation = useMutation({
    mutationFn: runDiagnosticTest,
    onSuccess: async (result) => {
      setLastRun({
        id: result.referenceId ?? crypto.randomUUID(),
        diagnosticType: result.diagnosticType,
        status: result.status,
        startedAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
        durationMs: result.durationMs,
        resultSummary: result.message,
        referenceId: result.referenceId,
        createdBy: null,
        errorMessage: null,
      });
      notification.showSuccess("รัน Diagnostics Test เรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["admin-diagnostics-summary"] });
      await queryClient.invalidateQueries({ queryKey: ["admin-diagnostics-history"] });
    },
    onError: () => notification.showError("ไม่สามารถรัน Diagnostics Test ได้"),
  });

  const bundleMutation = useMutation({
    mutationFn: createSupportBundle,
    onSuccess: async (bundle) => {
      notification.showSuccess("สร้าง Support Bundle เรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["admin-support-bundles"] });
      await queryClient.invalidateQueries({ queryKey: ["admin-diagnostics-history"] });
      void handleDownloadBundle(bundle);
    },
    onError: () => notification.showError("ไม่สามารถสร้าง Support Bundle ได้ กรุณาตรวจสอบเหตุผลและสิทธิ์การใช้งาน"),
  });

  const services = Object.values(summaryQuery.data?.services ?? {});
  const overviewStatus = deriveOverallStatus(services);

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
      <Stack spacing={3}>
        <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
          <PageHeader title="Diagnostics Center" subtitle="ศูนย์ตรวจสอบปัญหา สร้าง Support Bundle และดูข้อมูลวิเคราะห์แบบปลอดภัย" />
          <Box>
            <Button variant="contained" startIcon={<RefreshOutlinedIcon />} onClick={() => void summaryQuery.refetch()} disabled={summaryQuery.isFetching}>
              รีเฟรช
            </Button>
          </Box>
        </Stack>

        <Alert severity="info" icon={<SecurityOutlinedIcon />}>
          หน้านี้ไม่แสดง secret, token, password หรือ connection string และข้อมูลใน log จะถูก mask ก่อนแสดงผลหรือรวมเข้า Support Bundle
        </Alert>

        <Card sx={{ borderTop: `5px solid ${statusColor(overviewStatus)}` }}>
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
              <Stack spacing={1}>
                <Typography variant="overline" color="text.secondary">Diagnostics Status</Typography>
                {summaryQuery.isLoading ? <Skeleton width={220} height={48} /> : <Chip label={statusLabel(overviewStatus)} sx={{ width: "fit-content", bgcolor: alpha(statusColor(overviewStatus), 0.14), color: statusColor(overviewStatus), fontWeight: 900, fontSize: 18, px: 1.5, py: 2.2 }} />}
              </Stack>
              <Stack textAlign={{ xs: "left", md: "right" }} spacing={0.5}>
                <Typography color="text.secondary">ตรวจล่าสุด</Typography>
                <Typography fontWeight={800}>{formatThaiDateTime(summaryQuery.data?.checkedAt)}</Typography>
                <Typography variant="caption" color="text.secondary">ข้อมูลถูก redaction ก่อนแสดงผล</Typography>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <ServiceGrid services={services} isLoading={summaryQuery.isLoading} />

        <Paper sx={{ borderRadius: 3, border: `1px solid ${brandColors.border}`, overflow: "hidden" }}>
          <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable" scrollButtons="auto">
            <Tab label="ภาพรวม" />
            <Tab label="Tests" />
            <Tab label="Logs" />
            <Tab label="Recent Errors" />
            <Tab label="Support Bundle" />
            <Tab label="History" />
          </Tabs>
          <Divider />
          <Box sx={{ p: { xs: 2, md: 3 } }}>
            {tab === 0 && <OverviewTab services={services} runs={historyQuery.data ?? []} />}
            {tab === 1 && <TestsTab lastRun={lastRun} isRunning={runMutation.isPending} onRun={(key) => runMutation.mutate(key)} />}
            {tab === 2 && <LogsTab query={logQuery} setQuery={setLogQuery} isLoading={logsQuery.isLoading} data={logsQuery.data} />}
            {tab === 3 && <RecentErrorsTab isLoading={recentErrorsQuery.isLoading} errors={recentErrorsQuery.data ?? []} />}
            {tab === 4 && (
              <SupportBundleTab
                request={bundleRequest}
                setRequest={setBundleRequest}
                isCreating={bundleMutation.isPending}
                onCreate={() => bundleMutation.mutate(bundleRequest)}
                bundles={bundlesQuery.data ?? []}
                onDownload={(bundle) => void handleDownloadBundle(bundle)}
              />
            )}
            {tab === 5 && <HistoryTab runs={historyQuery.data ?? []} bundles={bundlesQuery.data ?? []} isLoading={historyQuery.isLoading || bundlesQuery.isLoading} onDownload={(bundle) => void handleDownloadBundle(bundle)} />}
          </Box>
        </Paper>
      </Stack>
    </Box>
  );

  async function handleDownloadBundle(bundle: SupportBundle) {
    try {
      const blob = await downloadSupportBundle(bundle.id);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = bundle.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch {
      notification.showError("ไม่สามารถดาวน์โหลด Support Bundle ได้");
    }
  }
}

function ServiceGrid({ services, isLoading }: { services: DiagnosticServiceStatus[]; isLoading: boolean }) {
  if (isLoading) {
    return (
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(2, 1fr)", lg: "repeat(4, 1fr)" }, gap: 2 }}>
        {Array.from({ length: 8 }).map((_, index) => <Skeleton key={index} variant="rounded" height={132} />)}
      </Box>
    );
  }

  return (
    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(2, 1fr)", lg: "repeat(4, 1fr)" }, gap: 2, alignItems: "stretch" }}>
      {services.map((service) => (
        <Card key={service.key} sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}` }}>
          <CardContent>
            <Stack spacing={1.5}>
              <Stack direction="row" justifyContent="space-between" spacing={1}>
                <Typography fontWeight={900}>{service.label}</Typography>
                <Chip size="small" label={statusLabel(service.status)} sx={{ bgcolor: alpha(statusColor(service.status), 0.14), color: statusColor(service.status), fontWeight: 800 }} />
              </Stack>
              <Typography color="text.secondary" sx={{ minHeight: 48 }}>{service.message || "พร้อมใช้งาน"}</Typography>
              {service.latencyMs != null && <Typography variant="caption" color="text.secondary">Latency {service.latencyMs.toLocaleString("th-TH")} ms</Typography>}
            </Stack>
          </CardContent>
        </Card>
      ))}
    </Box>
  );
}

function OverviewTab({ services, runs }: { services: DiagnosticServiceStatus[]; runs: DiagnosticRun[] }) {
  const totals = useMemo(() => ({
    healthy: services.filter((item) => item.status === "Healthy").length,
    warning: services.filter((item) => item.status === "Warning").length,
    unhealthy: services.filter((item) => item.status === "Unhealthy" || item.status === "Failed").length,
  }), [services]);

  return (
    <Grid container spacing={2.5}>
      <Grid item xs={12} md={4}>
        <MetricCard title="Healthy" value={totals.healthy} color={brandColors.success} />
      </Grid>
      <Grid item xs={12} md={4}>
        <MetricCard title="Warning" value={totals.warning} color={brandColors.warning} />
      </Grid>
      <Grid item xs={12} md={4}>
        <MetricCard title="Unhealthy" value={totals.unhealthy} color={brandColors.error} />
      </Grid>
      <Grid item xs={12}>
        <Typography variant="h6" fontWeight={900} gutterBottom>Diagnostics ล่าสุด</Typography>
        <RunsTable runs={runs} emptyText="ยังไม่มีประวัติการรัน diagnostics" />
      </Grid>
    </Grid>
  );
}

function TestsTab({ lastRun, isRunning, onRun }: { lastRun: DiagnosticRun | null; isRunning: boolean; onRun: (key: string) => void }) {
  return (
    <Stack spacing={2.5}>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)", lg: "repeat(4, 1fr)" }, gap: 2 }}>
        {diagnosticTests.map((test) => (
          <Button key={test.key} variant="outlined" startIcon={<PlayArrowOutlinedIcon />} onClick={() => onRun(test.key)} disabled={isRunning} sx={{ justifyContent: "flex-start", py: 1.5 }}>
            {test.label}
          </Button>
        ))}
      </Box>
      {isRunning && <LinearProgress />}
      {lastRun && (
        <Alert severity={lastRun.status === "Healthy" ? "success" : lastRun.status === "Warning" ? "warning" : "error"}>
          {lastRun.diagnosticType}: {lastRun.resultSummary}
        </Alert>
      )}
    </Stack>
  );
}

function LogsTab({ query, setQuery, isLoading, data }: { query: DiagnosticsLogQuery; setQuery: (query: DiagnosticsLogQuery) => void; isLoading: boolean; data?: Awaited<ReturnType<typeof getDiagnosticsLogs>> }) {
  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
        <TextField select label="แหล่ง log" value={query.source ?? "api"} onChange={(event) => setQuery({ ...query, source: event.target.value, page: 1 })} sx={{ minWidth: 180 }}>
          <MenuItem value="api">API</MenuItem>
          <MenuItem value="backup">Backup</MenuItem>
          <MenuItem value="nginx">Nginx</MenuItem>
          <MenuItem value="deploy">Deploy</MenuItem>
        </TextField>
        <TextField select label="ระดับ" value={query.severity ?? ""} onChange={(event) => setQuery({ ...query, severity: event.target.value, page: 1 })} sx={{ minWidth: 160 }}>
          <MenuItem value="">ทั้งหมด</MenuItem>
          <MenuItem value="error">Error</MenuItem>
          <MenuItem value="warning">Warning</MenuItem>
          <MenuItem value="info">Info</MenuItem>
        </TextField>
        <TextField label="ค้นหา" value={query.search ?? ""} onChange={(event) => setQuery({ ...query, search: event.target.value, page: 1 })} InputProps={{ startAdornment: <SearchOutlinedIcon fontSize="small" sx={{ mr: 1, color: "text.secondary" }} /> }} fullWidth />
      </Stack>
      {isLoading ? <Skeleton variant="rounded" height={320} /> : (
        <Paper variant="outlined" sx={{ maxHeight: 520, overflow: "auto", bgcolor: "#10241f", color: "#f8fafc", p: 2, borderRadius: 2 }}>
          {(data?.items ?? []).length === 0 ? (
            <Typography color="inherit">ไม่พบข้อมูล log ตามเงื่อนไข</Typography>
          ) : (
            <Stack spacing={0.75}>
              {data?.items.map((line, index) => (
                <Typography key={`${line.timestamp ?? "line"}-${index}`} component="pre" sx={{ m: 0, whiteSpace: "pre-wrap", wordBreak: "break-word", fontFamily: "Consolas, monospace", fontSize: 13 }}>
                  {line.timestamp ? `${formatThaiDateTime(line.timestamp)} ` : ""}[{line.severity}] {line.message}
                </Typography>
              ))}
            </Stack>
          )}
        </Paper>
      )}
    </Stack>
  );
}

function RecentErrorsTab({ isLoading, errors }: { isLoading: boolean; errors: Awaited<ReturnType<typeof getDiagnosticsRecentErrors>> }) {
  if (isLoading) return <Skeleton variant="rounded" height={280} />;
  return (
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>เวลา</TableCell>
          <TableCell>ประเภท</TableCell>
          <TableCell>รายละเอียด</TableCell>
          <TableCell>ผู้ใช้</TableCell>
          <TableCell>Reference ID</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {errors.length === 0 ? (
          <TableRow><TableCell colSpan={5}>ไม่พบ error ล่าสุด</TableCell></TableRow>
        ) : errors.map((error, index) => (
          <TableRow key={`${error.timestamp}-${error.module}-${index}`}>
            <TableCell>{formatThaiDateTime(error.timestamp)}</TableCell>
            <TableCell>{error.module}</TableCell>
            <TableCell>{error.message}</TableCell>
            <TableCell>{error.actor ?? "-"}</TableCell>
            <TableCell>{error.referenceId ?? "-"}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

function SupportBundleTab({ request, setRequest, isCreating, onCreate, bundles, onDownload }: { request: SupportBundleRequest; setRequest: (request: SupportBundleRequest) => void; isCreating: boolean; onCreate: () => void; bundles: SupportBundle[]; onDownload: (bundle: SupportBundle) => void }) {
  const canCreate = request.reason.trim().length >= 5 && !isCreating;
  return (
    <Grid container spacing={2.5}>
      <Grid item xs={12} md={5}>
        <Card variant="outlined">
          <CardContent>
            <Stack spacing={2}>
              <Stack direction="row" spacing={1.5} alignItems="center">
                <FolderZipOutlinedIcon color="primary" />
                <Typography variant="h6" fontWeight={900}>สร้าง Support Bundle</Typography>
              </Stack>
              <TextField label="เหตุผลการสร้าง bundle" value={request.reason} onChange={(event) => setRequest({ ...request, reason: event.target.value })} multiline minRows={3} required helperText="ระบุเหตุผลอย่างน้อย 5 ตัวอักษร เช่น ใช้ส่งให้ทีม IT วิเคราะห์ incident" />
              <TextField type="number" label="ช่วงเวลาข้อมูลย้อนหลัง (ชั่วโมง)" value={request.timeRangeHours} onChange={(event) => setRequest({ ...request, timeRangeHours: Number(event.target.value) || 24 })} inputProps={{ min: 1, max: 168 }} />
              <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)" }, gap: 1 }}>
                <BundleCheckbox label="App Logs" checked={request.includeAppLogs} onChange={(checked) => setRequest({ ...request, includeAppLogs: checked })} />
                <BundleCheckbox label="Nginx Logs" checked={request.includeNginxLogs} onChange={(checked) => setRequest({ ...request, includeNginxLogs: checked })} />
                <BundleCheckbox label="PostgreSQL Logs" checked={request.includePostgresLogs} onChange={(checked) => setRequest({ ...request, includePostgresLogs: checked })} />
                <BundleCheckbox label="Health" checked={request.includeHealth} onChange={(checked) => setRequest({ ...request, includeHealth: checked })} />
                <BundleCheckbox label="Deploy Info" checked={request.includeDeployInfo} onChange={(checked) => setRequest({ ...request, includeDeployInfo: checked })} />
                <BundleCheckbox label="Migrations" checked={request.includeMigrationInfo} onChange={(checked) => setRequest({ ...request, includeMigrationInfo: checked })} />
                <BundleCheckbox label="LINE Summary" checked={request.includeLineSummary} onChange={(checked) => setRequest({ ...request, includeLineSummary: checked })} />
                <BundleCheckbox label="Backup Summary" checked={request.includeBackupSummary} onChange={(checked) => setRequest({ ...request, includeBackupSummary: checked })} />
              </Box>
              <Button variant="contained" startIcon={<FolderZipOutlinedIcon />} onClick={onCreate} disabled={!canCreate}>
                สร้าง Support Bundle
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Grid>
      <Grid item xs={12} md={7}>
        <Typography variant="h6" fontWeight={900} gutterBottom>Bundle ล่าสุด</Typography>
        <BundlesTable bundles={bundles} onDownload={onDownload} />
      </Grid>
    </Grid>
  );
}

function HistoryTab({ runs, bundles, isLoading, onDownload }: { runs: DiagnosticRun[]; bundles: SupportBundle[]; isLoading: boolean; onDownload: (bundle: SupportBundle) => void }) {
  if (isLoading) return <Skeleton variant="rounded" height={360} />;
  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h6" fontWeight={900} gutterBottom>Diagnostic Runs</Typography>
        <RunsTable runs={runs} emptyText="ยังไม่มี history" />
      </Box>
      <Box>
        <Typography variant="h6" fontWeight={900} gutterBottom>Support Bundles</Typography>
        <BundlesTable bundles={bundles} onDownload={onDownload} />
      </Box>
    </Stack>
  );
}

function RunsTable({ runs, emptyText }: { runs: DiagnosticRun[]; emptyText: string }) {
  return (
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>เวลา</TableCell>
          <TableCell>ประเภท</TableCell>
          <TableCell>สถานะ</TableCell>
          <TableCell>ผลลัพธ์</TableCell>
          <TableCell>Reference ID</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {runs.length === 0 ? (
          <TableRow><TableCell colSpan={5}>{emptyText}</TableCell></TableRow>
        ) : runs.map((run) => (
          <TableRow key={run.id}>
            <TableCell>{formatThaiDateTime(run.startedAt)}</TableCell>
            <TableCell>{run.diagnosticType}</TableCell>
            <TableCell><StatusChip status={run.status} /></TableCell>
            <TableCell>{run.resultSummary ?? run.errorMessage ?? "-"}</TableCell>
            <TableCell>{run.referenceId ?? "-"}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

function BundlesTable({ bundles, onDownload }: { bundles: SupportBundle[]; onDownload: (bundle: SupportBundle) => void }) {
  return (
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>วันที่สร้าง</TableCell>
          <TableCell>ไฟล์</TableCell>
          <TableCell>สถานะ</TableCell>
          <TableCell>หมดอายุ</TableCell>
          <TableCell align="right">ดาวน์โหลด</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {bundles.length === 0 ? (
          <TableRow><TableCell colSpan={5}>ยังไม่มี Support Bundle</TableCell></TableRow>
        ) : bundles.map((bundle) => (
          <TableRow key={bundle.id}>
            <TableCell>{formatThaiDateTime(bundle.createdAt)}</TableCell>
            <TableCell>
              <Typography fontWeight={800}>{bundle.fileName}</Typography>
              <Typography variant="caption" color="text.secondary">{formatBytes(bundle.fileSizeBytes)}</Typography>
            </TableCell>
            <TableCell><StatusChip status={bundle.status} /></TableCell>
            <TableCell>{formatThaiDateTime(bundle.expiresAt)}</TableCell>
            <TableCell align="right">
              <Button size="small" startIcon={<DownloadOutlinedIcon />} onClick={() => onDownload(bundle)} disabled={bundle.status !== "Available"}>
                ดาวน์โหลด
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

function MetricCard({ title, value, color }: { title: string; value: number; color: string }) {
  return (
    <Card sx={{ borderTop: `4px solid ${color}` }}>
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="center">
          <Box sx={{ p: 1.2, borderRadius: 2, bgcolor: alpha(color, 0.12), color }}>
            <FactCheckOutlinedIcon />
          </Box>
          <Box>
            <Typography color="text.secondary">{title}</Typography>
            <Typography variant="h3" fontWeight={900} color={color}>{value.toLocaleString("th-TH")}</Typography>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

function BundleCheckbox({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) {
  return <FormControlLabel control={<Checkbox checked={checked} onChange={(event) => onChange(event.target.checked)} />} label={label} />;
}

function StatusChip({ status }: { status: string }) {
  return <Chip size="small" label={statusLabel(status)} sx={{ bgcolor: alpha(statusColor(status), 0.14), color: statusColor(status), fontWeight: 800 }} />;
}

function statusColor(status?: string) {
  const normalized = (status ?? "").toLowerCase();
  if (normalized === "healthy" || normalized === "success" || normalized === "available") return brandColors.success;
  if (normalized === "warning" || normalized === "unknown" || normalized === "running") return brandColors.warning;
  if (normalized === "failed" || normalized === "unhealthy" || normalized === "expired" || normalized === "deleted") return brandColors.error;
  return brandColors.info;
}

function statusLabel(status?: string) {
  const labels: Record<string, string> = {
    Healthy: "ปกติ",
    Warning: "ควรตรวจสอบ",
    Unhealthy: "ผิดปกติ",
    Failed: "ล้มเหลว",
    Running: "กำลังทำงาน",
    Available: "พร้อมดาวน์โหลด",
    Expired: "หมดอายุ",
    Deleted: "ถูกลบ",
    Unknown: "ไม่ทราบสถานะ",
  };
  return labels[status ?? ""] ?? status ?? "Unknown";
}

function deriveOverallStatus(services: DiagnosticServiceStatus[]) {
  if (services.some((service) => service.status === "Unhealthy" || service.status === "Failed")) return "Unhealthy";
  if (services.some((service) => service.status === "Warning" || service.status === "Unknown")) return "Warning";
  if (services.length > 0) return "Healthy";
  return "Unknown";
}

function formatBytes(value: number) {
  if (!Number.isFinite(value)) return "-";
  if (value < 1024) return `${value.toLocaleString("th-TH")} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toLocaleString("th-TH", { maximumFractionDigits: 1 })} KB`;
  return `${(value / 1024 / 1024).toLocaleString("th-TH", { maximumFractionDigits: 1 })} MB`;
}
