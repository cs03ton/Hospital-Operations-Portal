import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import ArrowForwardOutlinedIcon from "@mui/icons-material/ArrowForwardOutlined";
import BackupOutlinedIcon from "@mui/icons-material/BackupOutlined";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import HealthAndSafetyOutlinedIcon from "@mui/icons-material/HealthAndSafetyOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import LineAxisOutlinedIcon from "@mui/icons-material/LineAxisOutlined";
import ManageSearchOutlinedIcon from "@mui/icons-material/ManageSearchOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import PersonAddAltOutlinedIcon from "@mui/icons-material/PersonAddAltOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import SettingsSuggestOutlinedIcon from "@mui/icons-material/SettingsSuggestOutlined";
import TroubleshootOutlinedIcon from "@mui/icons-material/TroubleshootOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, Skeleton, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import type { SvgIconComponent } from "@mui/icons-material";
import type { ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { getAdminDashboard, type AdminDashboard } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

type SummaryCardProps = {
  title: string;
  value?: number | string;
  subtitle: string;
  icon: SvgIconComponent;
  color?: string;
  isLoading: boolean;
  to?: string;
};

type TodoItem = {
  label: string;
  count: number;
  to: string;
  severity: "warning" | "error" | "info";
};

type DashboardGridColumns = {
  xs: string;
  sm?: string;
  md?: string;
  lg?: string;
  xl?: string;
};

export function AdminDashboardPage() {
  const theme = useTheme();
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-dashboard"],
    queryFn: getAdminDashboard,
    refetchOnWindowFocus: false,
  });
  const todos = buildTodoItems(data);

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
      <Stack spacing={3}>
      <PageHeader
        title="Admin Dashboard"
        subtitle="ภาพรวมระบบและงานที่ผู้ดูแลต้องตรวจสอบ"
      />

      {isError && (
        <Alert severity="error">
          ไม่สามารถโหลดข้อมูล Admin Dashboard ได้ กรุณาลองใหม่อีกครั้ง
        </Alert>
      )}

      <Card
        sx={{
          border: `1px solid ${brandColors.border}`,
          borderTop: `5px solid ${brandColors.accent}`,
          borderRadius: 3,
          boxShadow: `0 18px 42px ${alpha(theme.palette.primary.dark, 0.08)}`,
        }}
      >
        <CardContent sx={{ p: { xs: 2, md: 2.5 } }}>
          <Stack direction={{ xs: "column", lg: "row" }} spacing={2.5} justifyContent="space-between" alignItems={{ xs: "stretch", lg: "center" }}>
            <Box sx={{ maxWidth: 760 }}>
              <Typography variant="h5" fontWeight={900} color="primary.main">
                ศูนย์ควบคุมผู้ดูแลระบบ
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                Dashboard นี้ใช้สำหรับดูภาพรวม คำเตือน สถานะระบบ และทางลัด ไม่ใช่หน้าจัดการข้อมูลรายรายการ
              </Typography>
            </Box>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1} useFlexGap sx={{ flexShrink: 0 }}>
              <Button component={RouterLink} to="/admin/users/create" variant="contained" startIcon={<PersonAddAltOutlinedIcon />} sx={{ minWidth: 132 }}>
                เพิ่มผู้ใช้
              </Button>
              <Button component={RouterLink} to="/admin/health" variant="outlined" startIcon={<HealthAndSafetyOutlinedIcon />} sx={{ minWidth: 132 }}>
                Health Center
              </Button>
              <Button component={RouterLink} to="/admin/line-settings" variant="outlined" startIcon={<NotificationsActiveOutlinedIcon />} sx={{ minWidth: 112 }}>
                LINE
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <DashboardGrid
        columns={{
          xs: "1fr",
          sm: "repeat(2, minmax(0, 1fr))",
          lg: "repeat(4, minmax(0, 1fr))",
        }}
      >
        <SummaryCard title="ผู้ใช้ทั้งหมด" value={data?.users.total} subtitle={`ใช้งาน ${formatNumber(data?.users.active)} · ปิดใช้งาน ${formatNumber(data?.users.inactive)}`} icon={GroupOutlinedIcon} color={brandColors.primary} isLoading={isLoading} to="/admin/users" />
        <SummaryCard title="หน่วยงาน" value={data?.departments.total} subtitle={`ไม่มีหัวหน้า ${formatNumber(data?.departments.withoutHead)} · ไม่มีผู้ใช้ ${formatNumber(data?.departments.withoutUsers)}`} icon={BusinessOutlinedIcon} color={brandColors.secondary} isLoading={isLoading} to="/admin/departments" />
        <SummaryCard title="บทบาทและสิทธิ์" value={data?.roles.total} subtitle={`สิทธิ์ ${formatNumber(data?.roles.permissions)} · Role ไม่มีผู้ใช้ ${formatNumber(data?.roles.unusedRoles)}`} icon={SecurityOutlinedIcon} color={brandColors.info} isLoading={isLoading} to="/admin/roles" />
        <SummaryCard title="ระบบลา" value={data?.leave.todayRequests} subtitle={`รออนุมัติ ${formatNumber(data?.leave.pendingApprovals)} · ยังไม่มี balance ${formatNumber(data?.leave.missingBalances)}`} icon={FactCheckOutlinedIcon} color={brandColors.warning} isLoading={isLoading} to="/admin/leave-support" />
      </DashboardGrid>

      <DashboardGrid columns={{ xs: "1fr", lg: "5fr 7fr" }}>
          <DashboardPanel title="สิ่งที่ควรตรวจสอบ" subtitle="รายการที่ควรจัดการก่อนใช้งานจริง" icon={WarningAmberOutlinedIcon} minHeight={360}>
            {isLoading ? (
              <Stack spacing={1.25}>
                <Skeleton height={44} />
                <Skeleton height={44} />
                <Skeleton height={44} />
              </Stack>
            ) : todos.length === 0 ? (
              <Alert severity="success">ยังไม่มีประเด็นสำคัญที่ต้องตรวจสอบ</Alert>
            ) : (
              <Stack spacing={1.25}>
                {todos.map((item) => (
                  <Alert
                    key={item.label}
                    severity={item.severity}
                    sx={{
                      alignItems: "center",
                      borderRadius: 2,
                      "& .MuiAlert-message": { width: "100%" },
                    }}
                    action={
                      <Button component={RouterLink} to={item.to} color="inherit" size="small" endIcon={<ArrowForwardOutlinedIcon />}>
                        ไปตรวจสอบ
                      </Button>
                    }
                  >
                    {item.label}: {item.count.toLocaleString("th-TH")} รายการ
                  </Alert>
                ))}
              </Stack>
            )}
          </DashboardPanel>

          <DashboardPanel title="สถานะระบบ" subtitle="สถานะสำคัญจาก Health Center" icon={HealthAndSafetyOutlinedIcon} actionTo="/admin/health" minHeight={360}>
            <Grid container spacing={1.25}>
              <HealthMini label="ภาพรวม" status={data?.health.overallStatus} isLoading={isLoading} />
              <HealthMini label="API" status={data?.health.api.status} isLoading={isLoading} />
              <HealthMini label="ฐานข้อมูล" status={data?.health.database.status} isLoading={isLoading} />
              <HealthMini label="Storage" status={data?.health.storage.status} isLoading={isLoading} />
              <HealthMini label="LINE" status={data?.health.line.status} isLoading={isLoading} />
              <HealthMini label="Backup" status={data?.health.backup.status} isLoading={isLoading} />
              <HealthMini label="Disk" status={data?.health.disk.status} isLoading={isLoading} />
            </Grid>
          </DashboardPanel>
      </DashboardGrid>

      <DashboardGrid columns={{ xs: "1fr", md: "repeat(2, minmax(0, 1fr))" }}>
          <DashboardPanel title="สรุป LINE" subtitle="ภาพรวมการเชื่อมต่อ LINE OA และ delivery" icon={LineAxisOutlinedIcon} actionTo="/admin/line-settings" minHeight={300}>
            <Stack spacing={1.25}>
              <Stack direction="row" justifyContent="space-between">
                <Typography color="text.secondary">สถานะ LINE</Typography>
                <Chip size="small" label={data?.line.enabled ? "เปิดใช้งาน" : "ปิดใช้งาน"} color={data?.line.enabled ? "success" : "warning"} />
              </Stack>
              <MetricRow label="เชื่อมต่อแล้ว" value={data?.line.boundUsers} />
              <MetricRow label="ยังไม่เชื่อมต่อ" value={data?.line.unboundUsers} />
              <MetricRow label="ส่งล้มเหลวล่าสุด" value={data?.line.lastFailedDeliveryAt ? formatThaiDateTime(data.line.lastFailedDeliveryAt) : "ไม่มี"} />
            </Stack>
          </DashboardPanel>

          <DashboardPanel title="สรุป Audit Log" subtitle="เหตุการณ์ที่ควรเฝ้าระวัง 7 วันล่าสุด" icon={HistoryOutlinedIcon} actionTo="/admin/audit-logs" minHeight={300}>
            <Stack spacing={1.25}>
              <MetricRow label="Login failed" value={data?.audit.recentFailedLogins} />
              <MetricRow label="Permission denied" value={data?.audit.recentPermissionDenied} />
              <Box>
                <Typography variant="subtitle2" fontWeight={800} sx={{ mb: 1 }}>Admin actions ล่าสุด</Typography>
                {isLoading ? (
                  <Skeleton height={80} />
                ) : data?.audit.recentAdminActions.length ? (
                  <Stack spacing={0.75}>
                    {data.audit.recentAdminActions.map((item) => (
                      <Box key={`${item.createdAt}-${item.action}`} sx={{ p: 1, borderRadius: 2, bgcolor: alpha(theme.palette.primary.main, 0.05) }}>
                        <Typography fontWeight={800}>{item.action}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {item.entityName || "-"} · {item.actorName || "System"} · {formatThaiDateTime(item.createdAt)}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                ) : (
                  <Typography color="text.secondary">ยังไม่มี admin action ล่าสุด</Typography>
                )}
              </Box>
            </Stack>
          </DashboardPanel>
      </DashboardGrid>

      <DashboardPanel title="ทางลัดผู้ดูแลระบบ" subtitle="ลิงก์ไปหน้าจัดการและศูนย์ตรวจสอบจริง ไม่ทำ CRUD บน Dashboard" icon={SettingsSuggestOutlinedIcon}>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, minmax(0, 1fr))",
              lg: "repeat(4, minmax(0, 1fr))",
            },
            gap: 2,
            width: "100%",
            pr: { xs: 0, md: 0.25 },
            "& > *": { minWidth: 0 },
          }}
        >
          {quickActions.map((action) => {
            const Icon = action.icon;
            return (
              <Button
                key={action.to}
                component={RouterLink}
                to={action.to}
                variant="outlined"
                fullWidth
                startIcon={<Icon />}
                sx={{
                  justifyContent: "flex-start",
                  py: 1.35,
                  minHeight: 48,
                  borderRadius: 2,
                  borderColor: alpha(theme.palette.primary.main, 0.22),
                  bgcolor: alpha(theme.palette.background.paper, 0.72),
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {action.label}
              </Button>
            );
          })}
        </Box>
      </DashboardPanel>
      </Stack>
    </Box>
  );
}

const quickActions = [
  { label: "เพิ่มผู้ใช้", to: "/admin/users/create", icon: PersonAddAltOutlinedIcon },
  { label: "จัดการผู้ใช้", to: "/admin/users", icon: GroupOutlinedIcon },
  { label: "หน่วยงาน", to: "/admin/departments", icon: BusinessOutlinedIcon },
  { label: "บทบาทและสิทธิ์", to: "/admin/roles", icon: SecurityOutlinedIcon },
  { label: "ช่วยเหลือระบบลา", to: "/admin/leave-support", icon: ManageSearchOutlinedIcon },
  { label: "กฎอนุมัติวันลา", to: "/admin/approval-chains", icon: AccountTreeOutlinedIcon },
  { label: "Health Center", to: "/admin/health", icon: HealthAndSafetyOutlinedIcon },
  { label: "Diagnostics Center", to: "/admin/diagnostics", icon: TroubleshootOutlinedIcon },
  { label: "Backup Center", to: "/admin/backup", icon: BackupOutlinedIcon },
  { label: "ตั้งค่า LINE", to: "/admin/line-settings", icon: NotificationsActiveOutlinedIcon },
  { label: "บันทึกการใช้งาน", to: "/admin/audit-logs", icon: HistoryOutlinedIcon },
  { label: "ศูนย์คู่มือ", to: "/docs", icon: MenuBookOutlinedIcon },
];

function buildTodoItems(data?: AdminDashboard): TodoItem[] {
  if (!data) return [];
  const items: TodoItem[] = [
    { label: "ผู้ใช้ที่ยังไม่ได้เชื่อม LINE", count: data.users.missingLineBinding, to: "/admin/users", severity: "warning" },
    { label: "ผู้ใช้ที่ยังไม่มีประเภทบุคลากร", count: data.users.missingEmploymentType, to: "/admin/users", severity: "warning" },
    { label: "ผู้ใช้ที่ยังไม่มีกฎอนุมัติ", count: data.users.missingApprovalRule, to: "/admin/users", severity: "error" },
    { label: "หน่วยงานที่ยังไม่มีหัวหน้า", count: data.departments.withoutHead, to: "/admin/departments", severity: "warning" },
    { label: "leave balance ที่ยังไม่สร้าง", count: data.leave.missingBalances, to: "/admin/leave-balances", severity: "error" },
    { label: "permission สำคัญที่ยังไม่ได้ assign", count: data.roles.importantPermissionsUnassigned, to: "/admin/roles", severity: "warning" },
    { label: "LINE delivery failed", count: data.line.lastFailedDeliveryAt ? 1 : 0, to: "/admin/line-settings", severity: "warning" },
    { label: "backup ล่าสุดควรตรวจสอบ", count: data.health.backup.status === "Healthy" ? 0 : 1, to: "/admin/backup", severity: "warning" },
  ];
  return items.filter((item) => item.count > 0);
}

function SummaryCard({ title, value, subtitle, icon: Icon, color = brandColors.primary, isLoading, to }: SummaryCardProps) {
  return (
      <Card
        sx={(theme) => ({
          width: "100%",
          height: "100%",
          minHeight: 184,
          border: `1px solid ${brandColors.border}`,
          borderTop: `4px solid ${brandColors.accent}`,
          borderRadius: 3,
          boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.06)}`,
        })}
      >
        <CardContent sx={{ height: "100%", p: 2 }}>
          <Stack spacing={1.75} sx={{ height: "100%" }}>
            <Stack direction="row" spacing={1.25} alignItems="flex-start" sx={{ minHeight: 54 }}>
              <Box sx={{ width: 42, height: 42, display: "grid", placeItems: "center", color, bgcolor: alpha(color, 0.1), borderRadius: "50%", flexShrink: 0 }}>
                <Icon fontSize="small" />
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography fontWeight={900}>{title}</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ lineHeight: 1.45 }}>{subtitle}</Typography>
              </Box>
            </Stack>
            <Box sx={{ minHeight: 64, display: "flex", alignItems: "center" }}>
              {isLoading ? <Skeleton width={90} height={56} /> : <Typography variant="h3" sx={{ color, fontWeight: 900, lineHeight: 1 }}>{value ?? 0}</Typography>}
            </Box>
            <Box sx={{ mt: "auto" }}>
              {to && (
                <Button component={RouterLink} to={to} size="small" variant="text" endIcon={<ArrowForwardOutlinedIcon />} sx={{ px: 0, fontWeight: 800 }}>
                  ไปจัดการ
                </Button>
              )}
            </Box>
          </Stack>
        </CardContent>
      </Card>
  );
}

function DashboardGrid({ children, columns }: { children: ReactNode; columns: DashboardGridColumns }) {
  return (
    <Box
      sx={{
        display: "grid",
        gridTemplateColumns: columns,
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

function DashboardPanel({ title, subtitle, icon: Icon, actionTo, minHeight, children }: { title: string; subtitle?: string; icon: SvgIconComponent; actionTo?: string; minHeight?: number; children: ReactNode }) {
  const theme = useTheme();
  return (
    <Card
      sx={{
        height: "100%",
        minHeight,
        border: `1px solid ${theme.palette.divider}`,
        borderTop: `4px solid ${brandColors.accent}`,
        borderRadius: 3,
        boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.06)}`,
      }}
    >
      <CardContent sx={{ p: 2 }}>
        <Stack spacing={2}>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ xs: "stretch", sm: "flex-start" }}>
            <Box sx={{ width: 44, height: 44, display: "grid", placeItems: "center", color: "primary.main", bgcolor: alpha(theme.palette.primary.main, 0.08), borderRadius: "50%", flexShrink: 0 }}>
              <Icon fontSize="small" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle1" fontWeight={900}>{title}</Typography>
              {subtitle && <Typography variant="body2" color="text.secondary">{subtitle}</Typography>}
            </Box>
            {actionTo && (
              <Button component={RouterLink} to={actionTo} size="small" variant="outlined" endIcon={<ArrowForwardOutlinedIcon />} sx={{ alignSelf: { xs: "flex-start", sm: "center" } }}>
                ไปจัดการ
              </Button>
            )}
          </Stack>
          {children}
        </Stack>
      </CardContent>
    </Card>
  );
}

function HealthMini({ label, status, isLoading }: { label: string; status?: string; isLoading: boolean }) {
  const color = getStatusColor(status);
  return (
    <Grid item xs={6} md={3}>
      <Box sx={{ p: 1.35, minHeight: 76, borderRadius: 2.25, bgcolor: alpha(color, 0.08), border: `1px solid ${alpha(color, 0.24)}` }}>
        <Typography variant="caption" color="text.secondary">{label}</Typography>
        {isLoading ? <Skeleton height={28} /> : <Typography fontWeight={900} sx={{ color }}>{statusLabel(status)}</Typography>}
      </Box>
    </Grid>
  );
}

function MetricRow({ label, value }: { label: string; value?: number | string | null }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={2} sx={{ py: 0.75, borderBottom: `1px solid ${alpha(brandColors.border, 0.72)}`, "&:last-child": { borderBottom: 0 } }}>
      <Typography color="text.secondary">{label}</Typography>
      <Typography fontWeight={900}>{typeof value === "number" ? value.toLocaleString("th-TH") : value ?? "-"}</Typography>
    </Stack>
  );
}

function getStatusColor(status?: string) {
  const normalized = (status ?? "").toLowerCase();
  if (normalized === "healthy" || normalized === "success") return brandColors.success;
  if (normalized === "warning" || normalized === "unknown") return brandColors.warning;
  if (normalized === "unhealthy" || normalized === "failed") return brandColors.error;
  return brandColors.info;
}

function statusLabel(status?: string) {
  const labels: Record<string, string> = {
    Healthy: "ปกติ",
    Warning: "ควรตรวจสอบ",
    Unhealthy: "ผิดปกติ",
    Unknown: "ไม่ทราบ",
  };
  return labels[status ?? "Unknown"] ?? status ?? "Unknown";
}

function formatNumber(value?: number) {
  return (value ?? 0).toLocaleString("th-TH");
}
