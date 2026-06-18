import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import BarChartOutlinedIcon from "@mui/icons-material/BarChartOutlined";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import EventBusyOutlinedIcon from "@mui/icons-material/EventBusyOutlined";
import type { NavigationItem } from "../types/navigation";

export const navigationItems: NavigationItem[] = [
  { label: "แดชบอร์ด", path: "/dashboard", icon: DashboardOutlinedIcon, permission: "Dashboard.View" },
  { label: "รายการคำขอลา", path: "/leave", icon: EventAvailableOutlinedIcon, permission: "LeaveManagement.View" },
  { label: "ปฏิทินการลา", path: "/leave/calendar", icon: CalendarMonthOutlinedIcon, permission: "LeaveManagement.View" },
  { label: "วันลาคงเหลือ", path: "/leave/balances", icon: AccountBalanceWalletOutlinedIcon, permission: "LeaveManagement.View" },
  { label: "ประเภทการลา", path: "/leave/types", icon: TuneOutlinedIcon, permission: "LeaveManagement.Manage" },
  { label: "สายอนุมัติวันลา", path: "/admin/approval-chains", icon: AccountTreeOutlinedIcon, permission: "ApprovalChain.View" },
  { label: "มอบหมายอนุมัติ", path: "/admin/approval-delegations", icon: AccountTreeOutlinedIcon, permission: "ApprovalDelegation.View" },
  { label: "ปรับยอดวันลา", path: "/admin/leave-balances/adjustments", icon: AccountBalanceWalletOutlinedIcon, permission: "LeaveBalance.Adjust" },
  { label: "วันหยุดราชการ", path: "/admin/leave-holidays", icon: EventBusyOutlinedIcon, permission: "LeaveHoliday.Manage" },
  { label: "รายงานการลา", path: "/reports/leaves", icon: BarChartOutlinedIcon, permission: "ReportManagement.View" },
  { label: "จัดการผู้ใช้", path: "/admin/users", icon: GroupOutlinedIcon, permission: "UserManagement.View" },
  { label: "จัดการหน่วยงาน", path: "/admin/departments", icon: BusinessOutlinedIcon, permission: "DepartmentManagement.View" },
  { label: "บทบาทและสิทธิ์", path: "/admin/roles", icon: SecurityOutlinedIcon, permission: "RoleManagement.View" },
  { label: "บันทึกการใช้งาน", path: "/admin/audit-logs", icon: HistoryOutlinedIcon, permission: "SystemSettings.View" },
  { label: "ส่งออก Audit Log", path: "/admin/audit-logs/export", icon: DownloadOutlinedIcon, permission: "SystemSettings.Export" },
];
