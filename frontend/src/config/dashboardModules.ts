import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import MeetingRoomOutlinedIcon from "@mui/icons-material/MeetingRoomOutlined";
import QueryStatsOutlinedIcon from "@mui/icons-material/QueryStatsOutlined";
import type { SvgIconComponent } from "@mui/icons-material";
import type { DashboardSummary } from "../api/adminApi";
import type { AuthUser } from "../types/auth";

export type DashboardModuleKey = "leave" | "vehicle" | "repair" | "meeting" | "inventory" | "executive";
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
  visibleToAllAuthenticated?: boolean;
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
    description: "ติดตามการใช้รถ งานจัดรถ การอนุมัติ และสถานะการเดินทาง",
    route: "/fleet/dashboard",
    icon: DirectionsCarOutlinedIcon,
    status: "active",
    visibleToAllAuthenticated: true,
    metricLabel: "รายการที่ใช้งานอยู่",
    metricSelector: (summary) => summary?.activeBorrowRequests ?? 0,
    order: 20,
  },
  {
    key: "repair",
    title: "ระบบแจ้งซ่อม",
    description: "แจ้งซ่อม IT และงานทั่วไป ติดตามคิวทีมและผลการแก้ไข",
    route: "/dashboard/repair",
    icon: BuildOutlinedIcon,
    status: "active",
    requiredPermissions: ["RepairManagement.ViewOwn", "RepairManagement.WorkIT", "RepairManagement.WorkGeneral", "RepairManagement.ViewAll"],
    metricLabel: "งานที่ดำเนินการอยู่",
    metricSelector: (summary) => summary?.openRepairRequests ?? 0,
    order: 30,
  },
  {
    key: "meeting",
    title: "ระบบจองห้องประชุม",
    description: "ดูปฏิทินการใช้ห้องและจองห้องประชุมของโรงพยาบาล",
    route: "/meeting-rooms/calendar",
    icon: MeetingRoomOutlinedIcon,
    status: "active",
    requiredPermissions: ["MeetingRoom.Calendar.View"],
    metricLabel: "การจองวันนี้",
    metricSelector: (summary) => summary?.meetingBookingsToday ?? 0,
    order: 40,
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
    order: 50,
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
    order: 60,
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
  if (module.visibleToAllAuthenticated) return true;
  if (user.role === "SuperAdmin") return true;

  const roleAllowed = module.allowedRoles?.includes(user.role) ?? false;
  const permissionAllowed = module.requiredPermissions?.some((permission) => user.permissions.includes(permission)) ?? false;

  return roleAllowed || permissionAllowed;
}

export function getDashboardModuleMetricLabel(module: DashboardModuleDefinition, user: AuthUser | null | undefined) {
  if (module.key === "leave" && user?.role === "Staff") {
    return "คำขอลาของฉันที่รออนุมัติ";
  }

  if (module.key === "vehicle" && user?.permissions.some(permission =>
    permission === "FleetDriver.ViewOwnJobs" ||
    permission === "FleetDriver.ViewOwn" ||
    permission === "FleetDriver.ViewJobs"
  )) {
    return "งานขับรถของฉันที่ยังดำเนินการอยู่";
  }

  if (module.key === "vehicle") {
    return "คำขอใช้รถของฉันที่ยังดำเนินการอยู่";
  }

  return module.metricLabel;
}

export type DashboardHubMetric = { label: string; value: number | string; to?: string };

export function getDashboardHubMetrics(module: DashboardModuleDefinition, user: AuthUser | null | undefined, summary?: DashboardSummary): DashboardHubMetric[] {
  const permissions = new Set(user?.permissions ?? []);
  const isDirector = user?.role === "Director";
  if (module.key === "leave") {
    const canApprove = permissions.has("LeaveRequest.ViewPendingApproval") ||
      permissions.has("LeaveApproval.ApproveCurrentStep") ||
      permissions.has("LeaveCancellation.ApproveCurrentStep");
    const metrics: DashboardHubMetric[] = canApprove || isDirector ? [
      { label: "คำขอลารออนุมัติ", value: summary?.pendingLeaveApprovals ?? 0, to: canApprove ? "/leave/pending-approvals" : undefined },
      { label: "คำขอยกเลิกใบลารออนุมัติ", value: summary?.pendingLeaveCancellationApprovals ?? 0, to: canApprove ? "/leave/pending-approvals" : undefined },
    ] : [];
    if (!isDirector) {
      metrics.push(
        { label: "คำขอลาของฉันที่รออนุมัติ", value: summary?.myLeaveRequestsPending ?? 0, to: permissions.has("LeaveRequest.ViewOwn") || permissions.has("LeaveRequest.ViewAll") ? "/leave?scope=mine&status=pending" : undefined },
        { label: "คำขอยกเลิกใบลาของฉันที่รออนุมัติ", value: summary?.myLeaveCancellationRequestsPending ?? 0, to: permissions.has("LeaveCancellation.ViewOwn") || permissions.has("LeaveCancellation.ViewAll") ? "/leave/cancellations?scope=mine&status=pending" : undefined },
      );
    }
    return metrics;
  }
  if (module.key === "vehicle") {
    const canReview = ["FleetAdminReview.Approve", "FleetAdminReview.Return", "FleetAdminReview.Reject"].some(code => permissions.has(code));
    const canDirectorApprove = ["FleetDirector.Approve", "FleetDirector.Return", "FleetDirector.Reject"].some(code => permissions.has(code));
    const metrics: DashboardHubMetric[] = canReview || canDirectorApprove || isDirector ? [
      {
        label: "คำขอใช้รถรออนุมัติ",
        value: (summary?.pendingFleetReviewRequests ?? 0) + (summary?.pendingFleetDirectorRequests ?? 0),
        to: canReview || canDirectorApprove ? "/fleet/approvals" : undefined,
      },
    ] : [];
    if (!isDirector) {
      metrics.push({
        label: canReview || canDirectorApprove ? "คำขอใช้รถของฉันที่ยังดำเนินการอยู่" : getDashboardModuleMetricLabel(module, user),
        value: canReview || canDirectorApprove ? summary?.myActiveFleetRequests ?? 0 : module.metricSelector(summary),
        to: permissions.has("FleetRequest.ViewOwn") || permissions.has("FleetRequest.ViewAll") ? "/fleet/requests" : undefined,
      });
    }
    return metrics;
  }
  return [{ label: getDashboardModuleMetricLabel(module, user), value: module.metricSelector(summary) }];
}

export const dashboardHubModule = {
  title: "Dashboard Hub",
  route: "/dashboard",
  icon: DashboardOutlinedIcon,
};
