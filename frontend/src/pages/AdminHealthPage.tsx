import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import CloudDoneOutlinedIcon from "@mui/icons-material/CloudDoneOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import BackupOutlinedIcon from "@mui/icons-material/BackupOutlined";
import DnsOutlinedIcon from "@mui/icons-material/DnsOutlined";
import DataUsageOutlinedIcon from "@mui/icons-material/DataUsageOutlined";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import SettingsSuggestOutlinedIcon from "@mui/icons-material/SettingsSuggestOutlined";
import { Box, Button, Card, CardContent, Chip, Grid, Skeleton, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import type { SvgIconComponent } from "@mui/icons-material";
import { getAdminHealth, type AdminHealth } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { brandColors } from "../theme/theme";

export function AdminHealthPage() {
  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ["admin-health"],
    queryFn: getAdminHealth,
    refetchOnWindowFocus: false,
  });

  return (
    <Stack spacing={3}>
      <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="สถานะระบบ" subtitle="ตรวจสอบ API, Database, Storage, LINE, Queue, Backup และสภาพแวดล้อมของระบบ" />
        <Box>
          <Button variant="contained" startIcon={<RefreshOutlinedIcon />} onClick={() => refetch()} disabled={isFetching}>
            รีเฟรช
          </Button>
        </Box>
      </Stack>

      <Grid container spacing={2}>
        <HealthCard title="API Status" icon={CloudDoneOutlinedIcon} status={data?.api.status} message={data?.api.message} isLoading={isLoading} />
        <HealthCard title="Database Status" icon={StorageOutlinedIcon} status={data?.database.status} message={formatLatency(data?.database.latencyMs, data?.database.message)} isLoading={isLoading} />
        <HealthCard title="Storage Status" icon={DnsOutlinedIcon} status={data?.storage.status} message={data ? (data.storage.writable ? "เขียนไฟล์ได้" : data.storage.message) : undefined} isLoading={isLoading} />
        <HealthCard title="LINE Status" icon={NotificationsActiveOutlinedIcon} status={data?.line.status} message={buildLineMessage(data)} isLoading={isLoading} />
        <HealthCard title="Queue / Worker Status" icon={SettingsSuggestOutlinedIcon} status={data?.queue.status} message={buildQueueMessage(data)} isLoading={isLoading} />
        <HealthCard title="Disk Usage" icon={DataUsageOutlinedIcon} status={data?.disk.status} message={data?.disk.usedPercent == null ? data?.disk.message : `ใช้งาน ${data.disk.usedPercent.toLocaleString("th-TH", { maximumFractionDigits: 2 })}%`} isLoading={isLoading} />
        <HealthCard title="Backup Status" icon={BackupOutlinedIcon} status={data?.backup.status} message={data?.backup.lastBackupAt ? `ล่าสุด ${formatThaiDateTime(data.backup.lastBackupAt)}` : data?.backup.message} isLoading={isLoading} />
        <InfoCard title="App Version" value={data?.version ?? "-"} isLoading={isLoading} />
        <InfoCard title="Environment" value={data?.environment ?? "-"} isLoading={isLoading} />
        <InfoCard title="Current Time Server" value={formatThaiDateTime(data?.currentTimeServer)} isLoading={isLoading} />
      </Grid>

      <Typography variant="caption" color="text.secondary">
        หน้านี้แสดงเฉพาะสถานะการทำงาน ไม่แสดง token, password, connection string หรือข้อมูลลับของระบบ
      </Typography>
    </Stack>
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

function HealthCard({ title, icon: Icon, status, message, isLoading }: { title: string; icon: SvgIconComponent; status?: string; message?: string | null; isLoading: boolean }) {
  const theme = useTheme();
  const color = getStatusColor(status);
  return (
    <Grid item xs={12} md={6} lg={4}>
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
          </Stack>
        </CardContent>
      </Card>
    </Grid>
  );
}

function InfoCard({ title, value, isLoading }: { title: string; value: string; isLoading: boolean }) {
  return (
    <Grid item xs={12} md={6} lg={4}>
      <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}` }}>
        <CardContent>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <InfoOutlinedIcon color="primary" />
            <Box>
              <Typography color="text.secondary">{title}</Typography>
              {isLoading ? <Skeleton width={150} height={34} /> : <Typography variant="h6" fontWeight={800}>{value}</Typography>}
            </Box>
          </Stack>
        </CardContent>
      </Card>
    </Grid>
  );
}

function getStatusColor(status?: string) {
  const normalized = (status ?? "").toLowerCase();
  if (normalized === "healthy" || normalized === "success") return brandColors.success;
  if (normalized === "warning" || normalized === "disabled" || normalized === "unknown") return brandColors.warning;
  if (normalized === "unhealthy" || normalized === "unavailable" || normalized === "failed") return brandColors.error;
  return brandColors.info;
}

function formatLatency(latencyMs?: number | null, message?: string | null) {
  if (latencyMs == null) return message;
  return `${message ? `${message} · ` : ""}${latencyMs.toLocaleString("th-TH")} ms`;
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
