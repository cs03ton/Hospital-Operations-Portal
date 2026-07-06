export const routes = {
  login: "/login",
  dashboard: "/dashboard",
  profile: "/profile",
  notifications: "/notifications",
  leaveList: "/leave",
  leaveCreate: "/leave/create",
  leaveCalendar: "/leave/calendar",
  leaveBalances: "/leave/balances",
  pendingApprovals: "/leave/pending-approvals",
  leaveTypes: "/leave/types",
  leaveReports: "/reports/leaves",
  leaveHolidays: "/admin/leave-holidays",
  leaveBalanceManagement: "/admin/leave-balances",
  approvalChains: "/admin/approval-chains",
  userList: "/admin/users",
  userCreate: "/admin/users/create",
  departments: "/admin/departments",
  roles: "/admin/roles",
  auditLogs: "/admin/audit-logs",
  systemSettings: "/admin/system-settings",
} as const;

export function leaveDetail(id: string) {
  return `/leave/${id}`;
}

export function userEdit(id: string) {
  return `/admin/users/${id}/edit`;
}

export function departmentEdit(id: string) {
  return `/admin/departments/${id}/edit`;
}

export function rolePermissions(id: string) {
  return `/admin/roles/${id}/permissions`;
}
