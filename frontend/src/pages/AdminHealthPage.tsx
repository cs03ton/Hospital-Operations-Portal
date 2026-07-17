import { useState, type ReactNode } from "react";
import BackupOutlinedIcon from "@mui/icons-material/BackupOutlined";
import CloudDoneOutlinedIcon from "@mui/icons-material/CloudDoneOutlined";
import DataUsageOutlinedIcon from "@mui/icons-material/DataUsageOutlined";
import DnsOutlinedIcon from "@mui/icons-material/DnsOutlined";
import MemoryOutlinedIcon from "@mui/icons-material/MemoryOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import SettingsSuggestOutlinedIcon from "@mui/icons-material/SettingsSuggestOutlined";
import SpeedOutlinedIcon from "@mui/icons-material/SpeedOutlined";
import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import { Box, Button, Card, CardContent, Chip, Divider, Grid, LinearProgress, Paper, Skeleton, Stack, Tab, Tabs, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import type { SvgIconComponent } from "@mui/icons-material";
import { getAdminHealth, type AdminHealth } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { brandColors } from "../theme/theme";

export function AdminHealthPage() {
  const [tab, setTab] = useState(0);
  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ["admin-health"],
    queryFn: getAdminHealth,
    refetchOnWindowFocus: false,
  });

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
    <Stack spacing={3}>
      <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="Health Center" subtitle="ศูนย์ตรวจสุขภาพ API, Database, Storage, LINE, ทรัพยากรเครื่อง และ Backup" />
        <Box>
          <Button variant="contained" startIcon={<RefreshOutlinedIcon />} onClick={() => refetch()} disabled={isFetching}>
            รีเฟรช
          </Button>
        </Box>
      </Stack>

      <OverallStatusCard data={data} isLoading={isLoading} isFetching={isFetching} />

      <HealthStatusGrid>
        <HealthCard title="API" icon={CloudDoneOutlinedIcon} status={data?.api.status} message={data?.api.uptimeSeconds == null ? data?.api.message : `Uptime ${formatDuration(data.api.uptimeSeconds)}`} isLoading={isLoading} />
        <HealthCard title="Database" icon={StorageOutlinedIcon} status={data?.database.status} message={formatLatency(data?.database.latencyMs, data?.database.message, data?.database.provider)} isLoading={isLoading} />
        <HealthCard title="LINE" icon={NotificationsActiveOutlinedIcon} status={data?.line.status} message={buildLineMessage(data)} isLoading={isLoading} />
        <HealthCard title="Storage" icon={DnsOutlinedIcon} status={data?.storage.status} message={data ? (data.storage.writable ? "เขียนไฟล์ได้" : data.storage.message) : undefined} isLoading={isLoading} />
        <HealthCard title="Disk" icon={DataUsageOutlinedIcon} status={data?.disk.status} message={data?.disk.usedPercent == null ? data?.disk.message : `ใช้งาน ${formatNumber(data.disk.usedPercent)}%`} progress={data?.disk.usedPercent} isLoading={isLoading} />
        <HealthCard title="CPU" icon={SpeedOutlinedIcon} status={data?.cpu.status} message={data ? `${data.cpu.processorCount} cores${data.cpu.loadAverage ? ` · Load ${data.cpu.loadAverage}` : ""}` : undefined} isLoading={isLoading} />
        <HealthCard title="RAM" icon={MemoryOutlinedIcon} status={data?.memory.status} message={data?.memory.usedPercent == null ? data?.memory.message : `ใช้งาน ${formatNumber(data.memory.usedPercent)}%`} progress={data?.memory.usedPercent} isLoading={isLoading} />
        <HealthCard title="Backup" icon={BackupOutlinedIcon} status={data?.backup.status} message={data?.backup.lastBackupAt ? `ล่าสุด ${formatThaiDateTime(data.backup.lastBackupAt)}` : data?.backup.message} isLoading={isLoading} />
        <HealthCard title="Queue / Worker" icon={SettingsSuggestOutlinedIcon} status={data?.queue.status} message={buildQueueMessage(data)} isLoading={isLoading} />
        <HealthCard title="Leave Cancellation" icon={SettingsSuggestOutlinedIcon} status={data?.leaveCancellation.status} message={buildLeaveCancellationHealthMessage(data)} isLoading={isLoading} />
      </HealthStatusGrid>

      <Paper sx={{ borderRadius: 3, border: `1px solid ${brandColors.border}`, overflow: "hidden" }}>
        <Tabs value={tab} onChange={(_, next) => setTab(next)} variant="scrollable" scrollButtons="auto">
          <Tab label="ภาพรวม" />
          <Tab label="Infrastructure" />
          <Tab label="LINE" />
          <Tab label="Backup" />
          <Tab label="Diagnostics" />
        </Tabs>
        <Divider />
        <Box sx={{ p: 3 }}>
          {isLoading ? (
            <Stack spacing={1.5}>
              <Skeleton height={28} />
              <Skeleton height={28} />
              <Skeleton height={28} />
            </Stack>
          ) : (
            <>
              {tab === 0 && <OverviewDetails data={data} />}
              {tab === 1 && <InfrastructureDetails data={data} />}
              {tab === 2 && <LineDetails data={data} />}
              {tab === 3 && <BackupDetails data={data} />}
              {tab === 4 && <DiagnosticsDetails data={data} />}
            </>
          )}
        </Box>
      </Paper>

      <Typography variant="caption" color="text.secondary">
        หน้านี้แสดงเฉพาะสถานะการทำงาน ไม่แสดง token, password, connection string หรือข้อมูลลับของระบบ
      </Typography>
    </Stack>
    </Box>
  );
}

function OverallStatusCard({ data, isLoading, isFetching }: { data?: AdminHealth; isLoading: boolean; isFetching: boolean }) {
  const theme = useTheme();
  const color = getStatusColor(data?.overallStatus);
  return (
    <Card sx={{ borderTop: `5px solid ${color}`, background: `linear-gradient(135deg, ${alpha(color, 0.1)}, ${theme.palette.background.paper})` }}>
      <CardContent>
        <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
          <Stack spacing={1}>
            <Typography variant="overline" color="text.secondary">Overall Status</Typography>
            {isLoading ? (
              <Skeleton width={220} height={54} />
            ) : (
              <Stack direction="row" spacing={1.5} alignItems="center">
                <Chip label={statusLabel(data?.overallStatus)} sx={{ bgcolor: alpha(color, 0.15), color, fontWeight: 900, fontSize: 18, px: 1.5, py: 2.2 }} />
                {isFetching && <Typography color="text.secondary">กำลังรีเฟรช...</Typography>}
              </Stack>
            )}
          </Stack>
          <Stack spacing={0.5} textAlign={{ xs: "left", md: "right" }}>
            <Typography color="text.secondary">ตรวจล่าสุด</Typography>
            <Typography fontWeight={800}>{formatThaiDateTime(data?.checkedAt)}</Typography>
            <Typography variant="caption" color="text.secondary">
              Environment: {data?.environment ?? "-"} · Version: {data?.version ?? "-"}
            </Typography>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}

function buildQueueMessage(data?: AdminHealth) {
  if (!data) return undefined;
  const parts = [
    `LINE pending ${data.queue.pendingLineDeliveries.toLocaleString("th-TH")}`,
    `failed ${data.queue.failedLineDeliveries.toLocaleString("th-TH")}`,
    `retry ${data.queue.pendingRetries.toLocaleString("th-TH")}`,
    data.queue.lineRetryEnabled ? "LINE retry เปิดใช้งาน" : "LINE retry ปิดใช้งาน",
    data.queue.approvalEscalationEnabled ? "Escalation เปิดใช้งาน" : "Escalation ปิดใช้งาน",
    data.queue.message,
  ].filter(Boolean);
  return parts.join(" · ");
}

function buildLeaveCancellationHealthMessage(data?: AdminHealth) {
  if (!data?.leaveCancellation) return undefined;
  const health = data.leaveCancellation;
  return [
    `รออนุมัติ ${health.pendingApproval.toLocaleString("th-TH")}`,
    `LINE failed ${health.failedNotification.toLocaleString("th-TH")}`,
    `reference issue ${health.failedReferenceIntegrity.toLocaleString("th-TH")}`,
    `restore issue ${health.failedBalanceRestore.toLocaleString("th-TH")}`,
    health.message,
  ].filter(Boolean).join(" · ");
}

function HealthCard({ title, icon: Icon, status, message, progress, isLoading }: { title: string; icon: SvgIconComponent; status?: string; message?: string | null; progress?: number | null; isLoading: boolean }) {
  const color = getStatusColor(status);
  return (
      <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}` }}>
        <CardContent>
          <Stack spacing={2}>
            <Stack direction="row" justifyContent="space-between" spacing={1} alignItems="flex-start">
              <Stack direction="row" spacing={1.5} alignItems="center">
                <Box sx={{ color, bgcolor: alpha(color, 0.1), borderRadius: 2, p: 1 }}>
                  <Icon fontSize="small" />
                </Box>
                <Typography fontWeight={800}>{title}</Typography>
              </Stack>
              {isLoading ? <Skeleton width={90} height={32} /> : <Chip size="small" label={status ?? "Unknown"} sx={{ bgcolor: alpha(color, 0.12), color, fontWeight: 800 }} />}
            </Stack>
            {isLoading ? <Skeleton height={28} /> : <Typography color="text.secondary">{message || "พร้อมใช้งาน"}</Typography>}
            {progress != null && <LinearProgress variant="determinate" value={Math.max(0, Math.min(100, progress))} sx={{ height: 8, borderRadius: 99, bgcolor: alpha(color, 0.12), "& .MuiLinearProgress-bar": { bgcolor: color } }} />}
          </Stack>
        </CardContent>
      </Card>
  );
}

function HealthStatusGrid({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        display: "grid",
        gridTemplateColumns: {
          xs: "1fr",
          md: "repeat(2, minmax(0, 1fr))",
          lg: "repeat(3, minmax(0, 1fr))",
        },
        gap: { xs: 2, md: 2.5 },
        alignItems: "stretch",
        width: "100%",
        "& > *": { minWidth: 0 },
      }}
    >
      {children}
    </Box>
  );
}

function getStatusColor(status?: string) {
  const normalized = (status ?? "").toLowerCase();
  if (normalized === "healthy" || normalized === "success") return brandColors.success;
  if (normalized === "warning" || normalized === "disabled" || normalized === "unknown") return brandColors.warning;
  if (normalized === "unhealthy" || normalized === "unavailable" || normalized === "failed") return brandColors.error;
  return brandColors.info;
}

function statusLabel(status?: string) {
  const normalized = status ?? "Unknown";
  const labels: Record<string, string> = {
    Healthy: "ปกติ",
    Warning: "ควรตรวจสอบ",
    Unhealthy: "ผิดปกติ",
    Unknown: "ไม่ทราบสถานะ",
  };
  return labels[normalized] ?? normalized;
}

function formatLatency(latencyMs?: number | null, message?: string | null, provider?: string | null) {
  const parts = [
    provider,
    latencyMs == null ? null : `${latencyMs.toLocaleString("th-TH")} ms`,
    message,
  ].filter(Boolean);
  return parts.join(" · ");
}

function buildLineMessage(data?: AdminHealth) {
  if (!data) return undefined;
  const parts = [
    data.line.enabled ? "เปิดใช้งาน" : "ปิดใช้งาน",
    data.line.lastSuccessAt ? `สำเร็จล่าสุด ${formatThaiDateTime(data.line.lastSuccessAt)}` : null,
    data.line.lastFailureAt ? `ล้มเหลวล่าสุด ${formatThaiDateTime(data.line.lastFailureAt)}` : null,
    data.line.message,
  ].filter(Boolean);
  return parts.join(" · ");
}

function OverviewDetails({ data }: { data?: AdminHealth }) {
  return (
    <DetailGrid>
      <DetailRow label="Overall Status" value={statusLabel(data?.overallStatus)} status={data?.overallStatus} />
      <DetailRow label="API Uptime" value={data?.api.uptimeSeconds == null ? "-" : formatDuration(data.api.uptimeSeconds)} />
      <DetailRow label="App Version" value={data?.version ?? "-"} />
      <DetailRow label="Environment" value={data?.environment ?? "-"} />
      <DetailRow label="Server Time" value={formatThaiDateTime(data?.currentTimeServer)} />
      <DetailRow label="Timezone" value={data?.timezone ?? "-"} />
    </DetailGrid>
  );
}

function InfrastructureDetails({ data }: { data?: AdminHealth }) {
  return (
    <DetailGrid>
      <DetailRow label="Database" value={formatLatency(data?.database.latencyMs, data?.database.message, data?.database.provider) || "-"} status={data?.database.status} />
      <DetailRow label="Storage" value={data?.storage.path ?? data?.storage.message ?? "-"} status={data?.storage.status} />
      <DetailRow label="Disk Used" value={data?.disk.usedPercent == null ? data?.disk.message ?? "-" : `${formatNumber(data.disk.usedPercent)}% (${formatNumber(data.disk.usedGb)} / ${formatNumber(data.disk.totalGb)} GB)`} status={data?.disk.status} />
      <DetailRow label="Memory Used" value={data?.memory.usedPercent == null ? data?.memory.message ?? "-" : `${formatNumber(data.memory.usedPercent)}% (${formatNumber(data.memory.usedMb)} / ${formatNumber(data.memory.totalMb)} MB)`} status={data?.memory.status} />
      <DetailRow label="CPU" value={`${data?.cpu.processorCount ?? "-"} cores${data?.cpu.loadAverage ? ` · Load ${data.cpu.loadAverage}` : ""}`} status={data?.cpu.status} />
    </DetailGrid>
  );
}

function LineDetails({ data }: { data?: AdminHealth }) {
  return (
    <DetailGrid>
      <DetailRow label="LINE Status" value={data?.line.enabled ? "เปิดใช้งาน" : "ปิดใช้งาน"} status={data?.line.status} />
      <DetailRow label="Access Token" value={data?.line.hasAccessToken ? "ตั้งค่าแล้ว" : "ยังไม่ได้ตั้งค่า"} status={data?.line.hasAccessToken ? "Healthy" : "Warning"} />
      <DetailRow label="Channel Secret" value={data?.line.hasChannelSecret ? "ตั้งค่าแล้ว" : "ยังไม่ได้ตั้งค่า"} status={data?.line.hasChannelSecret ? "Healthy" : "Warning"} />
      <DetailRow label="Last Success" value={formatThaiDateTime(data?.line.lastSuccessAt)} />
      <DetailRow label="Last Failure" value={formatThaiDateTime(data?.line.lastFailureAt)} />
      <DetailRow label="Last Error" value={data?.line.lastError ?? "-"} />
      <DetailRow label="Queue" value={buildQueueMessage(data) ?? "-"} status={data?.queue.status} />
    </DetailGrid>
  );
}

function BackupDetails({ data }: { data?: AdminHealth }) {
  return (
    <DetailGrid>
      <DetailRow label="Backup Status" value={data?.backup.message ?? "พร้อมใช้งาน"} status={data?.backup.status} />
      <DetailRow label="Backup Directory" value={data?.backup.backupDirectory ?? "-"} />
      <DetailRow label="Latest Backup" value={data?.backup.lastBackupAt ? formatThaiDateTime(data.backup.lastBackupAt) : "-"} />
      <DetailRow label="Latest File" value={data?.backup.latestBackupFile ?? "-"} />
      <DetailRow label="Latest Size" value={data?.backup.latestBackupSizeBytes == null ? "-" : formatBytes(data.backup.latestBackupSizeBytes)} />
      <DetailRow label="Last Restore Test" value={data?.backup.lastRestoreTestAt ? formatThaiDateTime(data.backup.lastRestoreTestAt) : "-"} />
    </DetailGrid>
  );
}

function DiagnosticsDetails({ data }: { data?: AdminHealth }) {
  return (
    <DetailGrid>
      <DetailRow label="Git Commit" value={data?.gitCommit ?? "-"} />
      <DetailRow label="Checked At" value={formatThaiDateTime(data?.checkedAt)} />
      <DetailRow label="API Message" value={data?.api.message ?? "-"} />
      <DetailRow label="Leave Cancellation Queue" value={buildLeaveCancellationHealthMessage(data) ?? "-"} status={data?.leaveCancellation.status} />
      <DetailRow label="Storage Writable" value={data?.storage.writable ? "ใช่" : "ไม่ใช่"} status={data?.storage.writable ? "Healthy" : "Unhealthy"} />
      <DetailRow label="Disk Free" value={data?.disk.freeGb == null ? "-" : `${formatNumber(data.disk.freeGb)} GB`} />
      <DetailRow label="CPU Note" value={data?.cpu.message ?? "-"} />
    </DetailGrid>
  );
}

function DetailGrid({ children }: { children: ReactNode }) {
  return <Grid container spacing={2}>{children}</Grid>;
}

function DetailRow({ label, value, status }: { label: string; value?: string | null; status?: string | null }) {
  const color = getStatusColor(status ?? undefined);
  return (
    <Grid item xs={12} md={6}>
      <Paper variant="outlined" sx={{ p: 2, borderRadius: 2, height: "100%" }}>
        <Stack direction="row" justifyContent="space-between" spacing={2}>
          <Typography color="text.secondary">{label}</Typography>
          {status && <Chip size="small" label={statusLabel(status)} sx={{ bgcolor: alpha(color, 0.12), color, fontWeight: 800 }} />}
        </Stack>
        <Typography fontWeight={800} sx={{ mt: 0.75, wordBreak: "break-word" }}>{value || "-"}</Typography>
      </Paper>
    </Grid>
  );
}

function formatNumber(value?: number | null) {
  return value == null ? "-" : value.toLocaleString("th-TH", { maximumFractionDigits: 2 });
}

function formatDuration(seconds: number) {
  const days = Math.floor(seconds / 86400);
  const hours = Math.floor((seconds % 86400) / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return [
    days > 0 ? `${days} วัน` : null,
    hours > 0 ? `${hours} ชม.` : null,
    minutes > 0 ? `${minutes} นาที` : null,
  ].filter(Boolean).join(" ") || "น้อยกว่า 1 นาที";
}

function formatBytes(bytes: number) {
  if (bytes >= 1024 * 1024 * 1024) return `${formatNumber(bytes / 1024 / 1024 / 1024)} GB`;
  if (bytes >= 1024 * 1024) return `${formatNumber(bytes / 1024 / 1024)} MB`;
  if (bytes >= 1024) return `${formatNumber(bytes / 1024)} KB`;
  return `${bytes.toLocaleString("th-TH")} bytes`;
}
