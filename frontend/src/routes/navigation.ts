import AdminPanelSettingsOutlinedIcon from "@mui/icons-material/AdminPanelSettingsOutlined";
import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import AssignmentReturnOutlinedIcon from "@mui/icons-material/AssignmentReturnOutlined";
import BarChartOutlinedIcon from "@mui/icons-material/BarChartOutlined";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import HandymanOutlinedIcon from "@mui/icons-material/HandymanOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import MeetingRoomOutlinedIcon from "@mui/icons-material/MeetingRoomOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import EventBusyOutlinedIcon from "@mui/icons-material/EventBusyOutlined";
import type { NavigationItem } from "../types/navigation";

export const navigationItems: NavigationItem[] = [
  { label: "แดชบอร์ด", path: "/dashboard", icon: DashboardOutlinedIcon, permission: "Dashboard.View" },
  { label: "รายการคำขอลา", path: "/leave", icon: EventAvailableOutlinedIcon, permission: "LeaveManagement.View" },
  { label: "วันลาคงเหลือ", path: "/leave/balances", icon: AccountBalanceWalletOutlinedIcon, permission: "LeaveManagement.View" },
  { label: "ประเภทการลา", path: "/leave/types", icon: TuneOutlinedIcon, permission: "LeaveManagement.Manage" },
  { label: "สายอนุมัติวันลา", path: "/admin/approval-chains", icon: AccountTreeOutlinedIcon, permission: "ApprovalChain.View" },
  { label: "ปรับยอดวันลา", path: "/admin/leave-balances/adjustments", icon: AccountBalanceWalletOutlinedIcon, permission: "LeaveBalance.Adjust" },
  { label: "วันหยุดราชการ", path: "/admin/leave-holidays", icon: EventBusyOutlinedIcon, permission: "LeaveHoliday.Manage" },
  { label: "ระบบยืมอุปกรณ์", path: "/borrowing", icon: AssignmentReturnOutlinedIcon, permission: "BorrowManagement.View" },
  { label: "ระบบแจ้งซ่อม", path: "/repairs", icon: HandymanOutlinedIcon, permission: "RepairManagement.View" },
  { label: "ระบบจองรถ", path: "/vehicles", icon: DirectionsCarOutlinedIcon, permission: "BorrowManagement.View" },
  { label: "ระบบจองห้องประชุม", path: "/meeting-rooms", icon: MeetingRoomOutlinedIcon, permission: "BorrowManagement.View" },
  { label: "ระบบเบิกวัสดุ", path: "/materials", icon: FactCheckOutlinedIcon, permission: "InventoryManagement.View" },
  { label: "คลังพัสดุ", path: "/inventory", icon: Inventory2OutlinedIcon, permission: "InventoryManagement.View" },
  { label: "รายงาน", path: "/reports", icon: BarChartOutlinedIcon, permission: "ReportManagement.View" },
  { label: "จัดการผู้ใช้", path: "/admin/users", icon: GroupOutlinedIcon, permission: "UserManagement.View" },
  { label: "จัดการหน่วยงาน", path: "/admin/departments", icon: BusinessOutlinedIcon, permission: "DepartmentManagement.View" },
  { label: "บทบาทและสิทธิ์", path: "/admin/roles", icon: SecurityOutlinedIcon, permission: "RoleManagement.View" },
  { label: "บันทึกการใช้งาน", path: "/admin/audit-logs", icon: HistoryOutlinedIcon, permission: "SystemSettings.View" },
  { label: "ส่งออก Audit Log", path: "/admin/audit-logs/export", icon: DownloadOutlinedIcon, permission: "SystemSettings.Export" },
  { label: "จัดการเซสชัน", path: "/admin/sessions", icon: AdminPanelSettingsOutlinedIcon, permission: "SystemSettings.Manage" },
  { label: "ตั้งค่าระบบ", path: "/admin/settings", icon: SettingsOutlinedIcon, permission: "SystemSettings.View" },
  { label: "ผู้ดูแลระบบ", path: "/administration", icon: AdminPanelSettingsOutlinedIcon, permission: "SystemSettings.Manage" },
];
