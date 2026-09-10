import BarChartOutlinedIcon from "@mui/icons-material/BarChartOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import SettingsSuggestOutlinedIcon from "@mui/icons-material/SettingsSuggestOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import type { NavigationItem } from "../types/navigation";
import type { FleetDashboardBadges, FleetDashboardCapabilities } from "../api/fleetApi";

export const fleetPermissionGroups = {
  requester: ["FleetRequest.ViewOwn", "FleetRequest.Create"],
  driver: ["FleetDriver.ViewOwnJobs", "FleetDriver.ViewOwn"],
  dispatcher: ["FleetDispatch.View", "FleetDispatch.Assign"],
  reviewer: ["FleetAdminReview.Approve", "FleetAdminReview.Return", "FleetAdminReview.Reject"],
  director: ["FleetDirector.Approve", "FleetDirector.Return", "FleetDirector.Reject"],
  calendar: ["FleetCalendar.View"],
  reports: ["FleetReport.View", "FleetReport.Export"],
  settings: ["FleetSettings.Manage"],
} as const;

const dashboardPermissions = [...new Set(Object.values(fleetPermissionGroups).flat())];

export const fleetNavigationItems: NavigationItem[] = [
  { label: "Dashboard รถ", path: "/fleet/dashboard", icon: DashboardOutlinedIcon, permissions: ["FleetDashboard.View", ...dashboardPermissions] },
  {
    label: "คำขอใช้รถ",
    path: "/fleet/requests",
    icon: DirectionsCarOutlinedIcon,
    permissions: [...fleetPermissionGroups.requester],
    hiddenForRoles: ["พนักงานขับรถ", "FleetDriver", "Driver"],
  },
  { label: "งานขับรถของฉัน", path: "/fleet/my-trips", icon: DirectionsCarOutlinedIcon, permissions: [...fleetPermissionGroups.driver] },
  { label: "คิวจัดรถ", path: "/fleet/dispatch", icon: DirectionsCarOutlinedIcon, permissions: [...fleetPermissionGroups.dispatcher] },
  { label: "งานรอตรวจสอบและอนุมัติคำขอใช้รถ", path: "/fleet/approvals", icon: FactCheckOutlinedIcon, permissions: [...fleetPermissionGroups.reviewer, ...fleetPermissionGroups.director] },
  { label: "ปฏิทินรถ", path: "/fleet/calendar", icon: CalendarMonthOutlinedIcon, permissions: [...fleetPermissionGroups.calendar] },
  { label: "รายงาน", path: "/fleet/reports", icon: BarChartOutlinedIcon, permissions: [...fleetPermissionGroups.reports] },
  { label: "ตั้งค่า", path: "/fleet/settings", icon: SettingsSuggestOutlinedIcon, permissions: [...fleetPermissionGroups.settings] },
  { label: "LINE Groups", path: "/fleet/admin/line-groups", icon: NotificationsActiveOutlinedIcon, permissions: ["FleetLineGroup.View", "FleetLineGroup.Manage"] },
];

export function visibleFleetNavigationItems(permissions: Iterable<string>, role?: string) {
  const permissionSet = new Set(permissions);
  return fleetNavigationItems.filter(item =>
    !item.hiddenForRoles?.includes(role ?? "") &&
    (item.permissions?.some(permission => permissionSet.has(permission)) ??
      (item.permission ? permissionSet.has(item.permission) : true)),
  );
}

export function fleetNavigationBadgeCount(path: string, badges?: FleetDashboardBadges) {
  if (!badges) return 0;
  if (path === "/fleet/my-trips") return badges.myDriverJobs;
  if (path === "/fleet/dispatch" || path === "/fleet/requests/review") return badges.dispatchQueue;
  if (path === "/fleet/approvals") return badges.reviewQueue + badges.approvalQueue;
  return 0;
}

export function effectiveFleetPermissions(direct: Iterable<string>, capabilities?: FleetDashboardCapabilities) {
  const result = new Set(direct);
  if (!capabilities) return result;
  capabilities.delegatedPermissions.forEach(permission => result.add(permission));
  if (capabilities.canViewOwnRequests) fleetPermissionGroups.requester.forEach(permission => result.add(permission));
  if (capabilities.canViewOwnTrips) fleetPermissionGroups.driver.forEach(permission => result.add(permission));
  if (capabilities.canDispatch) fleetPermissionGroups.dispatcher.forEach(permission => result.add(permission));
  if (capabilities.canAdminReview) fleetPermissionGroups.reviewer.forEach(permission => result.add(permission));
  if (capabilities.canDirectorApprove) fleetPermissionGroups.director.forEach(permission => result.add(permission));
  if (capabilities.canViewCalendar) fleetPermissionGroups.calendar.forEach(permission => result.add(permission));
  if (capabilities.canViewReports) fleetPermissionGroups.reports.forEach(permission => result.add(permission));
  if (capabilities.canManageFleet) fleetPermissionGroups.settings.forEach(permission => result.add(permission));
  return result;
}

export function fleetCapabilitiesAllowAny(required: readonly string[], capabilities?: FleetDashboardCapabilities) {
  if (!capabilities) return false;
  const effective = effectiveFleetPermissions([], capabilities);
  return required.some(permission => effective.has(permission));
}
