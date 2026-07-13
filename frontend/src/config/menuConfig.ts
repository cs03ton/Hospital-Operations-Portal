import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import BackupOutlinedIcon from "@mui/icons-material/BackupOutlined";
import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import BarChartOutlinedIcon from "@mui/icons-material/BarChartOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import EventBusyOutlinedIcon from "@mui/icons-material/EventBusyOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import HealthAndSafetyOutlinedIcon from "@mui/icons-material/HealthAndSafetyOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import ManageSearchOutlinedIcon from "@mui/icons-material/ManageSearchOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import MeetingRoomOutlinedIcon from "@mui/icons-material/MeetingRoomOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import RequestQuoteOutlinedIcon from "@mui/icons-material/RequestQuoteOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import SettingsSuggestOutlinedIcon from "@mui/icons-material/SettingsSuggestOutlined";
import WarehouseOutlinedIcon from "@mui/icons-material/WarehouseOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import type { SvgIconComponent } from "@mui/icons-material";
import type { NavigationItem } from "../types/navigation";

const leaveViewPermissions = [
  "LeaveRequest.ViewOwn",
  "LeaveRequest.ViewPendingApproval",
  "LeaveRequest.ViewDepartment",
  "LeaveRequest.ViewAll",
];

const documentationViewerRoles = ["Staff", "DepartmentHead", "Director", "LeaveAdmin", "Admin", "SuperAdmin"];

export type NavigationModule = {
  moduleId: string;
  moduleLabel: string;
  moduleIcon: SvgIconComponent;
  permission?: string;
  enabled: boolean;
  children: NavigationItem[];
};

export const navigationModules: NavigationModule[] = [
  {
    moduleId: "DashboardPortal",
    moduleLabel: "Dashboard",
    moduleIcon: DashboardOutlinedIcon,
    enabled: true,
    children: [
      { label: "Dashboard Hub", path: "/dashboard", icon: DashboardOutlinedIcon, permission: "Dashboard.View", activePatterns: ["/dashboard"] },
      { label: "Admin Dashboard", path: "/admin/dashboard", icon: DashboardOutlinedIcon, permission: "AdminDashboard.View", allowedRoles: ["Admin", "SuperAdmin"], activePatterns: ["/admin/dashboard"] },
    ],
  },
  {
    moduleId: "LeaveManagement",
    moduleLabel: "ระบบลา",
    moduleIcon: EventAvailableOutlinedIcon,
    enabled: true,
    children: [
      { label: "แดชบอร์ดการลา", path: "/dashboard/leave", icon: DashboardOutlinedIcon, permission: "Dashboard.View", activePatterns: ["/dashboard/leave"] },
      { label: "งานรออนุมัติของฉัน", path: "/leave/pending-approvals", icon: FactCheckOutlinedIcon, permission: "LeaveRequest.ViewPendingApproval", activePatterns: ["/leave/pending-approvals"], hiddenForRoles: ["Admin", "SuperAdmin"] },
      { label: "รายการคำขอลา", path: "/leave", icon: EventAvailableOutlinedIcon, permissions: leaveViewPermissions, activePatterns: ["/leave", "/leave/:id"] },
      { label: "ปฏิทินการลา", path: "/leave/calendar", icon: CalendarMonthOutlinedIcon, permissions: leaveViewPermissions, activePatterns: ["/leave/calendar"] },
      { label: "วันลาคงเหลือ", path: "/leave/balances", icon: AccountBalanceWalletOutlinedIcon, permission: "LeaveRequest.ViewOwn", activePatterns: ["/leave/balances"], hiddenForRoles: ["Admin", "SuperAdmin"] },
      { label: "จัดการวันลาคงเหลือ", path: "/admin/leave-balances", icon: AccountBalanceWalletOutlinedIcon, permission: "LeaveAdmin.ManageBalances" },
      { label: "ประเภทการลา", path: "/leave/types", icon: TuneOutlinedIcon, permission: "LeaveAdmin.ManageTypes", activePatterns: ["/leave/types"] },
      { label: "กฎการอนุมัติวันลา", path: "/admin/approval-chains", icon: AccountTreeOutlinedIcon, permission: "LeaveAdmin.ManageApprovalChains" },
      { label: "มอบหมายอนุมัติ", path: "/admin/approval-delegations", icon: AccountTreeOutlinedIcon, permission: "LeaveApproval.Delegate" },
      { label: "วันหยุดราชการ", path: "/admin/leave-holidays", icon: EventBusyOutlinedIcon, permission: "LeaveAdmin.ManageHolidays" },
      { label: "ช่วยเหลือระบบลา", path: "/admin/leave-support", icon: ManageSearchOutlinedIcon, permission: "LeaveSupport.ViewAll" },
      { label: "วิเคราะห์การลา", path: "/reports/leave-analytics", icon: BarChartOutlinedIcon, permissions: ["LeaveAnalytics.View", "ReportManagement.View"], allowedRoles: ["Director", "Admin", "SuperAdmin"] },
      { label: "รายงานการลา", path: "/reports/leaves", icon: BarChartOutlinedIcon, permission: "ReportManagement.View" },
    ],
  },
  {
    moduleId: "UserManagement",
    moduleLabel: "จัดการระบบผู้ใช้",
    moduleIcon: GroupOutlinedIcon,
    enabled: true,
    children: [
      { label: "จัดการผู้ใช้", path: "/admin/users", icon: GroupOutlinedIcon, permission: "UserManagement.View" },
      { label: "จัดการหน่วยงาน", path: "/admin/departments", icon: BusinessOutlinedIcon, permission: "DepartmentManagement.View" },
      { label: "บทบาทและสิทธิ์", path: "/admin/roles", icon: SecurityOutlinedIcon, permission: "RoleManagement.View" },
    ],
  },
  {
    moduleId: "SystemManagement",
    moduleLabel: "จัดการระบบ",
    moduleIcon: SettingsSuggestOutlinedIcon,
    enabled: true,
    children: [
      { label: "Health Center", path: "/admin/health", icon: HealthAndSafetyOutlinedIcon, permission: "System.Health.View", allowedRoles: ["Admin", "SuperAdmin"] },
      { label: "Backup Center", path: "/admin/backup", icon: BackupOutlinedIcon, permission: "System.Backup.View", allowedRoles: ["SuperAdmin"] },
      { label: "บันทึกการใช้งาน", path: "/admin/audit-logs", icon: HistoryOutlinedIcon, permission: "SystemSettings.View" },
      { label: "ตั้งค่าระบบ", path: "/admin/system-settings", icon: TuneOutlinedIcon, permission: "SystemSettings.View" },
      { label: "ตั้งค่า LINE", path: "/admin/line-settings", icon: NotificationsActiveOutlinedIcon, permissions: ["System.Line.TestSend", "SystemSettings.View"] },
      { label: "ผู้ใช้ LINE", path: "/admin/line-users", icon: NotificationsActiveOutlinedIcon, permissions: ["System.Line.TestSend", "SystemSettings.View"] },
    ],
  },
  {
    moduleId: "HelpCenter",
    moduleLabel: "ช่วยเหลือ",
    moduleIcon: MenuBookOutlinedIcon,
    enabled: true,
    children: [
      {
        label: "ศูนย์คู่มือ",
        path: "/docs",
        icon: MenuBookOutlinedIcon,
        permission: "Documentation.View",
        allowedRoles: documentationViewerRoles,
        activePatterns: ["/docs", "/docs/:slug"],
      },
    ],
  },
  {
    moduleId: "VehicleBooking",
    moduleLabel: "จองรถ",
    moduleIcon: DirectionsCarOutlinedIcon,
    enabled: false,
    children: [],
  },
  {
    moduleId: "AssetBorrowing",
    moduleLabel: "ยืมคืนครุภัณฑ์",
    moduleIcon: WarehouseOutlinedIcon,
    enabled: false,
    children: [],
  },
  {
    moduleId: "MeetingRoomBooking",
    moduleLabel: "จองห้องประชุม",
    moduleIcon: MeetingRoomOutlinedIcon,
    enabled: false,
    children: [],
  },
  {
    moduleId: "RepairManagement",
    moduleLabel: "แจ้งซ่อม",
    moduleIcon: BuildOutlinedIcon,
    enabled: false,
    children: [],
  },
  {
    moduleId: "InventoryManagement",
    moduleLabel: "คลังพัสดุ",
    moduleIcon: Inventory2OutlinedIcon,
    enabled: false,
    children: [],
  },
  {
    moduleId: "MaterialRequest",
    moduleLabel: "เบิกวัสดุ",
    moduleIcon: RequestQuoteOutlinedIcon,
    enabled: false,
    children: [],
  },
  {
    moduleId: "Reports",
    moduleLabel: "รายงานรวม",
    moduleIcon: BarChartOutlinedIcon,
    enabled: false,
    children: [],
  },
];

export const phaseOneNavigationModules = navigationModules.filter((module) => module.enabled);
export const phaseOneNavigationItems = phaseOneNavigationModules.flatMap((module) => module.children);
