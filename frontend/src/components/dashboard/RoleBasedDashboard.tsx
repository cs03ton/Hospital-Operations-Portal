import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import ApprovalOutlinedIcon from "@mui/icons-material/ApprovalOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import HealthAndSafetyOutlinedIcon from "@mui/icons-material/HealthAndSafetyOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import SettingsSuggestOutlinedIcon from "@mui/icons-material/SettingsSuggestOutlined";
import TrendingUpOutlinedIcon from "@mui/icons-material/TrendingUpOutlined";
import { Box, Button, Card, CardContent, Grid, Skeleton, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import type { SvgIconComponent } from "@mui/icons-material";
import { Link as RouterLink } from "react-router-dom";
import type { ReactNode } from "react";
import type { DashboardSummary } from "../../api/adminApi";
import { MyLeaveSummaryCard } from "../leave/MyLeaveSummaryCard";
import { brandColors } from "../../theme/theme";

export type DashboardRole = "Staff" | "DepartmentHead" | "Director" | "Admin" | "SuperAdmin";

type WidgetSize = { xs: number; sm?: number; md?: number; lg?: number };
type WidgetContext = {
  data?: DashboardSummary;
  isLoading: boolean;
  role: DashboardRole;
  userName: string;
};
type DashboardWidgetDefinition = {
  id: string;
  size: WidgetSize;
  render: (context: WidgetContext) => JSX.Element;
};

const roleLayouts: Record<DashboardRole, string[]> = {
  Staff: ["welcome", "leaveBalance", "myLeaveRequests", "myPendingRequests", "recentNotifications", "myLeaveCalendar"],
  DepartmentHead: ["welcome", "pendingApproval", "teamLeaveToday", "teamCalendar", "teamLeaveStats", "employeesNearLeaveLimit", "myLeaveRequests"],
  Director: ["welcome", "hospitalLeaveSummary", "departmentComparison", "monthlyLeaveStats", "approvalQueue", "executiveCalendar", "leaveTrend"],
  Admin: ["welcome", "userSummary", "departmentSummary", "leaveTypeSummary", "approvalRules", "pendingApprovalOverview", "notificationQueue", "auditLog", "holidayManagement", "systemHealth", "backgroundJobs", "storageUsage", "backupStatus", "versionInfo"],
  SuperAdmin: ["welcome", "userSummary", "departmentSummary", "leaveTypeSummary", "approvalRules", "pendingApprovalOverview", "notificationQueue", "auditLog", "holidayManagement", "systemHealth", "backgroundJobs", "storageUsage", "backupStatus", "versionInfo", "securityEvents", "failedLogin", "permissionDenied", "lineDelivery", "databaseStatus", "apiHealth", "queueMonitoring"],
};

export function RoleBasedDashboard({ data, isLoading, role, userName }: WidgetContext) {
  const widgets = roleLayouts[role].map((widgetId) => widgetRegistry[widgetId]).filter(Boolean);

  return (
    <Grid container spacing={2}>
      {widgets.map((widget) => (
        <Grid item xs={widget.size.xs} sm={widget.size.sm} md={widget.size.md} lg={widget.size.lg} key={widget.id}>
          {widget.render({ data, isLoading, role, userName })}
        </Grid>
      ))}
    </Grid>
  );
}

export function normalizeDashboardRole(role?: string | null): DashboardRole {
  if (role === "SuperAdmin") return "SuperAdmin";
  if (role === "Admin") return "Admin";
  if (role === "Director") return "Director";
  if (role === "DepartmentHead") return "DepartmentHead";
  return "Staff";
}

const widgetRegistry: Record<string, DashboardWidgetDefinition> = {
  welcome: {
    id: "welcome",
    size: { xs: 12 },
    render: ({ userName, role }) => (
      <DashboardPanel title={`สวัสดี ${userName}`} subtitle={`แดชบอร์ดสำหรับ ${getRoleLabel(role)}`} icon={HealthAndSafetyOutlinedIcon}>
        <Typography color="text.secondary">ระบบแสดงข้อมูลตามหน้าที่ของคุณ เพื่อให้ทำงานประจำวันได้เร็วและปลอดภัย</Typography>
      </DashboardPanel>
    ),
  },
  leaveBalance: metricWidget("leaveBalance", "วันลาคงเหลือของฉัน", "ยอดรวมวันลาคงเหลือของปีงบประมาณปัจจุบัน", AccountBalanceWalletOutlinedIcon, (data) => data.myRemainingLeaveDays, "primary.main", "/leave/balances"),
  myPendingRequests: metricWidget("myPendingRequests", "คำขอของฉันที่รออนุมัติ", "คำขอลาที่อยู่ในกระบวนการ", FactCheckOutlinedIcon, (data) => data.myLeaveRequestsPending, "warning.main", "/leave"),
  pendingApproval: metricWidget("pendingApproval", "งานรออนุมัติของฉัน", "แสดงเฉพาะคำขอลาที่ถึงคิวผู้ใช้งานปัจจุบัน", ApprovalOutlinedIcon, (data) => data.pendingApprovals, "warning.main", "/leave/pending-approvals"),
  pendingApprovalOverview: metricWidget("pendingApprovalOverview", "ภาพรวมคำขอรออนุมัติ", "จำนวนคำขอที่ยังอยู่ในกระบวนการอนุมัติทั้งระบบ", ApprovalOutlinedIcon, (data) => data.totalPendingLeaveRequests, "warning.main"),
  teamLeaveToday: metricWidget("teamLeaveToday", "เจ้าหน้าที่ลาวันนี้", "คำขอที่อนุมัติและครอบคลุมวันนี้", CalendarMonthOutlinedIcon, (data) => data.staffOnLeaveToday, "success.main", "/leave/calendar"),
  userSummary: metricWidget("userSummary", "ผู้ใช้งานทั้งหมด", "บัญชีที่เปิดใช้งาน", SecurityOutlinedIcon, (data) => data.totalUsers, "primary.main", "/admin/users"),
  departmentSummary: metricWidget("departmentSummary", "หน่วยงานทั้งหมด", "หน่วยงานที่เปิดใช้งาน", SettingsSuggestOutlinedIcon, (data) => data.totalDepartments, "secondary.main", "/admin/departments"),
  leaveTypeSummary: metricWidget("leaveTypeSummary", "ประเภทการลา", "ประเภทลาที่เปิดใช้งาน", AccountBalanceWalletOutlinedIcon, (data) => data.totalLeaveTypes, "info.main", "/leave/types"),
  approvalRules: metricWidget("approvalRules", "กฎการอนุมัติ", "สายอนุมัติที่เปิดใช้งาน", ApprovalOutlinedIcon, (data) => data.totalApprovalRules, "primary.main", "/admin/approval-chains"),
  notificationQueue: metricWidget("notificationQueue", "แจ้งเตือนที่ยังไม่อ่าน", "รายการแจ้งเตือนของผู้ใช้ปัจจุบัน", NotificationsActiveOutlinedIcon, (data) => data.unreadNotifications, "warning.main"),
  auditLog: metricWidget("auditLog", "Audit Log วันนี้", "เหตุการณ์ที่บันทึกวันนี้", HistoryOutlinedIcon, (data) => data.totalAuditLogsToday, "secondary.main", "/admin/audit-logs"),
  holidayManagement: metricWidget("holidayManagement", "วันหยุดราชการปีนี้", "วันหยุดที่เปิดใช้งานในปีปัจจุบัน", CalendarMonthOutlinedIcon, (data) => data.totalHolidaysThisYear, "info.main", "/admin/leave-holidays"),
  failedLogin: metricWidget("failedLogin", "Login ล้มเหลววันนี้", "เหตุการณ์ login ไม่สำเร็จ", SecurityOutlinedIcon, (data) => data.failedLoginEventsToday, "error.main", "/admin/audit-logs"),
  permissionDenied: metricWidget("permissionDenied", "Permission Denied วันนี้", "เหตุการณ์ถูกปฏิเสธสิทธิ์", SecurityOutlinedIcon, (data) => data.permissionDeniedEventsToday, "error.main", "/admin/audit-logs"),
  lineDelivery: metricWidget("lineDelivery", "LINE Delivery ผิดพลาด", "รายการส่ง LINE ที่ล้มเหลว", NotificationsActiveOutlinedIcon, (data) => data.lineFailed, "error.main"),
  queueMonitoring: metricWidget("queueMonitoring", "Queue รอดำเนินการ", "LINE queue ที่ยังรอส่ง", NotificationsActiveOutlinedIcon, (data) => data.lineQueued, "warning.main"),
  apiHealth: statusWidget("apiHealth", "API Health", "สถานะ Backend API", (data) => data.apiHealth),
  databaseStatus: statusWidget("databaseStatus", "Database Status", "สถานะ PostgreSQL", (data) => data.databaseStatus),
  versionInfo: statusWidget("versionInfo", "Version Information", "เวอร์ชันของระบบ", (data) => data.applicationVersion),
  systemHealth: statusWidget("systemHealth", "System Health", "สถานะระบบโดยรวม", (data) => data.apiHealth === "Healthy" && data.databaseStatus === "Healthy" ? "Healthy" : "Warning"),
  myLeaveRequests: {
    id: "myLeaveRequests",
    size: { xs: 12, lg: 6 },
    render: ({ data, isLoading }) => (
      <MyLeaveSummaryCard
        total={data?.myLeaveRequestsTotal ?? 0}
        pending={data?.myLeaveRequestsPending ?? 0}
        approved={data?.myLeaveRequestsApproved ?? 0}
        rejected={data?.myLeaveRequestsRejected ?? 0}
        cancelled={data?.myLeaveRequestsCancelled ?? 0}
        isLoading={isLoading}
      />
    ),
  },
  myLeaveCalendar: placeholderWidget("myLeaveCalendar", "ปฏิทินลาของฉัน", "ดูปฏิทินการลาที่เกี่ยวข้องกับคุณ", CalendarMonthOutlinedIcon, "/leave/calendar"),
  recentNotifications: placeholderWidget("recentNotifications", "แจ้งเตือนล่าสุด", "ไม่มีรายการแจ้งเตือนใหม่", NotificationsActiveOutlinedIcon),
  teamCalendar: placeholderWidget("teamCalendar", "ปฏิทินทีม", "ดูภาพรวมการลาของทีม", CalendarMonthOutlinedIcon, "/leave/calendar"),
  teamLeaveStats: trendWidget("teamLeaveStats", "สถิติการลาของทีม"),
  employeesNearLeaveLimit: placeholderWidget("employeesNearLeaveLimit", "เจ้าหน้าที่ใกล้ใช้สิทธิ์เต็ม", "ยังไม่มีข้อมูลที่ต้องติดตาม", TrendingUpOutlinedIcon),
  hospitalLeaveSummary: trendWidget("hospitalLeaveSummary", "ภาพรวมการลาทั้งโรงพยาบาล"),
  departmentComparison: placeholderWidget("departmentComparison", "เปรียบเทียบรายหน่วยงาน", "พร้อมต่อยอดเป็นกราฟเปรียบเทียบ", TrendingUpOutlinedIcon),
  monthlyLeaveStats: trendWidget("monthlyLeaveStats", "สถิติรายเดือน"),
  approvalQueue: metricWidget("approvalQueue", "คิวอนุมัติ", "งานอนุมัติที่ถึงคิวของคุณ", ApprovalOutlinedIcon, (data) => data.pendingApprovals, "warning.main", "/leave/pending-approvals"),
  executiveCalendar: placeholderWidget("executiveCalendar", "ปฏิทินผู้บริหาร", "ภาพรวมปฏิทินสำหรับผู้บริหาร", CalendarMonthOutlinedIcon, "/leave/calendar"),
  leaveTrend: trendWidget("leaveTrend", "แนวโน้มการลา"),
  backgroundJobs: placeholderWidget("backgroundJobs", "Background Jobs", "ไม่มี job ผิดปกติ", SettingsSuggestOutlinedIcon),
  storageUsage: placeholderWidget("storageUsage", "Storage Usage", "พร้อมเชื่อมต่อ storage metrics ในอนาคต", SettingsSuggestOutlinedIcon),
  backupStatus: placeholderWidget("backupStatus", "Backup Status", "ตรวจสอบตาม runbook backup/restore", SettingsSuggestOutlinedIcon),
  securityEvents: metricWidget("securityEvents", "Security Events วันนี้", "Failed login และ permission denied", SecurityOutlinedIcon, (data) => data.failedLoginEventsToday + data.permissionDeniedEventsToday, "error.main", "/admin/audit-logs"),
};

function metricWidget(id: string, title: string, note: string, icon: SvgIconComponent, selector: (data: DashboardSummary) => number, color: string, to?: string): DashboardWidgetDefinition {
  return {
    id,
    size: { xs: 12, sm: 6, lg: 4 },
    render: ({ data, isLoading }) => (
      <DashboardPanel title={title} subtitle={note} icon={icon} actionTo={to}>
        <MetricValue value={data ? selector(data) : 0} isLoading={isLoading} color={color} />
      </DashboardPanel>
    ),
  };
}

function statusWidget(id: string, title: string, note: string, selector: (data: DashboardSummary) => string): DashboardWidgetDefinition {
  return {
    id,
    size: { xs: 12, sm: 6, lg: 4 },
    render: ({ data, isLoading }) => (
      <DashboardPanel title={title} subtitle={note} icon={HealthAndSafetyOutlinedIcon}>
        {isLoading ? <Skeleton width={120} height={44} /> : <Typography variant="h5" fontWeight={800} color={selector(data ?? emptyDashboard) === "Healthy" ? "success.main" : "warning.main"}>{selector(data ?? emptyDashboard)}</Typography>}
      </DashboardPanel>
    ),
  };
}

function placeholderWidget(id: string, title: string, message: string, icon: SvgIconComponent, to?: string): DashboardWidgetDefinition {
  return {
    id,
    size: { xs: 12, md: 6, lg: 4 },
    render: () => (
      <DashboardPanel title={title} subtitle={message} icon={icon} actionTo={to}>
        <Typography color="text.secondary">ยังไม่มีข้อมูลที่ต้องแสดง</Typography>
      </DashboardPanel>
    ),
  };
}

function trendWidget(id: string, title: string): DashboardWidgetDefinition {
  return {
    id,
    size: { xs: 12, lg: 6 },
    render: ({ data, isLoading }) => (
      <DashboardPanel title={title} subtitle="แนวโน้มการลาในช่วงเวลาปัจจุบัน" icon={TrendingUpOutlinedIcon}>
        <Stack spacing={1.5}>
          <MetricBar label="วันนี้" value={data?.staffOnLeaveToday ?? 0} total={Math.max(data?.staffOnLeaveThisMonth ?? 0, 1)} color="success.main" isLoading={isLoading} />
          <MetricBar label="สัปดาห์นี้" value={data?.staffOnLeaveThisWeek ?? 0} total={Math.max(data?.staffOnLeaveThisMonth ?? 0, 1)} color="info.main" isLoading={isLoading} />
          <MetricBar label="เดือนนี้" value={data?.staffOnLeaveThisMonth ?? 0} total={Math.max(data?.staffOnLeaveThisMonth ?? 0, 1)} color="secondary.main" isLoading={isLoading} />
        </Stack>
      </DashboardPanel>
    ),
  };
}

function DashboardPanel({ title, subtitle, icon: Icon, actionTo, children }: { title: string; subtitle?: string; icon: SvgIconComponent; actionTo?: string; children: ReactNode }) {
  const theme = useTheme();
  return (
    <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}`, bgcolor: alpha(theme.palette.background.paper, 0.98), boxShadow: `0 16px 34px ${alpha(theme.palette.primary.dark, 0.08)}` }}>
      <CardContent>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <Box sx={{ color: "primary.main", bgcolor: alpha(theme.palette.primary.main, 0.08), borderRadius: 2, p: 1 }}>
              <Icon fontSize="small" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle1" fontWeight={800}>{title}</Typography>
              {subtitle && <Typography variant="body2" color="text.secondary">{subtitle}</Typography>}
            </Box>
          </Stack>
          {children}
          {actionTo && <Box><Button component={RouterLink} to={actionTo} size="small" variant="outlined">ดูทั้งหมด</Button></Box>}
        </Stack>
      </CardContent>
    </Card>
  );
}

function MetricValue({ value, isLoading, color }: { value: number; isLoading: boolean; color: string }) {
  if (isLoading) return <Skeleton width={96} height={58} />;
  return <Typography variant="h3" sx={{ color }}>{value.toLocaleString("th-TH")}</Typography>;
}

function MetricBar({ label, value, total, color, isLoading }: { label: string; value: number; total: number; color: string; isLoading: boolean }) {
  const percent = total > 0 ? Math.min(100, Math.round((value / total) * 100)) : 0;
  if (isLoading) return <Skeleton height={34} />;
  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.75 }}>
        <Typography variant="body2" color="text.secondary">{label}</Typography>
        <Typography variant="body2" fontWeight={700}>{value.toLocaleString("th-TH")}</Typography>
      </Stack>
      <Box sx={(theme) => ({ height: 10, borderRadius: 999, bgcolor: alpha(theme.palette.primary.main, 0.08), overflow: "hidden" })}>
        <Box sx={{ width: `${percent}%`, height: "100%", bgcolor: color, borderRadius: 999 }} />
      </Box>
    </Box>
  );
}

function getRoleLabel(role: DashboardRole) {
  const labels: Record<DashboardRole, string> = {
    Staff: "เจ้าหน้าที่",
    DepartmentHead: "หัวหน้าหน่วยงาน",
    Director: "ผู้อำนวยการ",
    Admin: "ผู้ดูแลระบบ",
    SuperAdmin: "ผู้ดูแลระบบสูงสุด",
  };
  return labels[role];
}

const emptyDashboard: DashboardSummary = {
  totalUsers: 0,
  totalDepartments: 0,
  pendingApprovals: 0,
  totalPendingLeaveRequests: 0,
  openRepairRequests: 0,
  activeBorrowRequests: 0,
  inventoryItems: 0,
  staffOnLeaveToday: 0,
  staffOnLeaveThisWeek: 0,
  staffOnLeaveThisMonth: 0,
  myRemainingLeaveDays: 0,
  myLeaveRequestsTotal: 0,
  myLeaveRequestsPending: 0,
  myLeaveRequestsApproved: 0,
  myLeaveRequestsRejected: 0,
  myLeaveRequestsCancelled: 0,
  totalLeaveTypes: 0,
  totalApprovalRules: 0,
  totalHolidaysThisYear: 0,
  totalAuditLogsToday: 0,
  loginEventsToday: 0,
  failedLoginEventsToday: 0,
  permissionDeniedEventsToday: 0,
  unreadNotifications: 0,
  lineQueued: 0,
  lineFailed: 0,
  apiHealth: "Unknown",
  databaseStatus: "Unknown",
  applicationVersion: "-",
};
