import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import QueryStatsOutlinedIcon from "@mui/icons-material/QueryStatsOutlined";
import type { SvgIconComponent } from "@mui/icons-material";
import type { DashboardSummary } from "../api/adminApi";
import type { AuthUser } from "../types/auth";

export type DashboardModuleKey = "leave" | "vehicle" | "repair" | "inventory" | "executive";
export type DashboardModuleStatus = "active" | "coming_soon" | "planned";

export type DashboardModuleDefinition = {
  key: DashboardModuleKey;
  title: string;
  description: string;
  route: string;
  icon: SvgIconComponent;
  status: DashboardModuleStatus;
  requiredPermissions?: string[];
  allowedRoles?: string[];
  metricLabel: string;
  metricSelector: (summary?: DashboardSummary) => number | string;
  order: number;
};

export const dashboardModules: DashboardModuleDefinition[] = [
  {
    key: "leave",
    title: "ระบบลา",
    description: "ติดตามคำขอลา งานรออนุมัติ วันลาคงเหลือ และปฏิทินการลา",
    route: "/dashboard/leave",
    icon: EventAvailableOutlinedIcon,
    status: "active",
    requiredPermissions: ["Dashboard.View"],
    metricLabel: "งานรออนุมัติ",
    metricSelector: (summary) => summary?.pendingApprovals ?? 0,
    order: 10,
  },
  {
    key: "vehicle",
    title: "ระบบจองรถ/ยืมรถ",
    description: "เตรียมรองรับการจองรถส่วนกลาง สถานะการใช้งาน และงานอนุมัติ",
    route: "/dashboard/vehicle",
    icon: DirectionsCarOutlinedIcon,
    status: "coming_soon",
    allowedRoles: ["SuperAdmin"],
    metricLabel: "รายการที่ใช้งานอยู่",
    metricSelector: (summary) => summary?.activeBorrowRequests ?? 0,
    order: 20,
  },
  {
    key: "repair",
    title: "ระบบแจ้งซ่อม",
    description: "เตรียมรองรับงานแจ้งซ่อม การมอบหมายผู้รับผิดชอบ และ SLA",
    route: "/dashboard/repair",
    icon: BuildOutlinedIcon,
    status: "coming_soon",
    allowedRoles: ["SuperAdmin"],
    metricLabel: "งานเปิดอยู่",
    metricSelector: (summary) => summary?.openRepairRequests ?? 0,
    order: 30,
  },
  {
    key: "inventory",
    title: "ระบบ Inventory",
    description: "เตรียมรองรับคลังพัสดุ ยอดคงเหลือ การเบิกจ่าย และรายงาน",
    route: "/dashboard/inventory",
    icon: Inventory2OutlinedIcon,
    status: "coming_soon",
    allowedRoles: ["SuperAdmin"],
    metricLabel: "รายการพัสดุ",
    metricSelector: (summary) => summary?.inventoryItems ?? 0,
    order: 40,
  },
  {
    key: "executive",
    title: "Executive Dashboard",
    description: "ภาพรวมเชิงบริหาร แนวโน้ม และ KPI สำหรับผู้บริหาร",
    route: "/dashboard/executive",
    icon: QueryStatsOutlinedIcon,
    status: "active",
    requiredPermissions: ["Dashboard.Executive.View", "LeaveDashboard.ViewExecutiveSummary"],
    allowedRoles: ["Director", "Admin", "SuperAdmin"],
    metricLabel: "สถานะระบบ",
    metricSelector: (summary) => summary?.apiHealth ?? "พร้อมใช้งาน",
    order: 50,
  },
];

export function getDashboardModule(key: DashboardModuleKey) {
  return dashboardModules.find((module) => module.key === key);
}

export function getVisibleDashboardModules(user: AuthUser | null | undefined) {
  return dashboardModules
    .filter((module) => canAccessDashboardModule(module, user))
    .sort((a, b) => a.order - b.order);
}

export function canAccessDashboardModule(module: DashboardModuleDefinition, user: AuthUser | null | undefined) {
  if (!user) return false;
  if (user.role === "SuperAdmin") return true;

  const roleAllowed = module.allowedRoles?.includes(user.role) ?? false;
  const permissionAllowed = module.requiredPermissions?.some((permission) => user.permissions.includes(permission)) ?? false;

  return roleAllowed || permissionAllowed;
}

export function getDashboardModuleMetricLabel(module: DashboardModuleDefinition, user: AuthUser | null | undefined) {
  if (module.key === "leave" && user?.role === "Staff") {
    return "คำขอลาของฉันที่รออนุมัติ";
  }

  return module.metricLabel;
}

export const dashboardHubModule = {
  title: "Dashboard Hub",
  route: "/dashboard",
  icon: DashboardOutlinedIcon,
};
