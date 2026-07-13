import { Navigate, Route, Routes } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { ApprovalChainFormPage } from "../pages/ApprovalChainFormPage";
import { ApprovalChainManagementPage } from "../pages/ApprovalChainManagementPage";
import { AuditLogPage } from "../pages/AuditLogPage";
import { AuditLogExportPage } from "../pages/AuditLogExportPage";
import { AdminBackupPage } from "../pages/AdminBackupPage";
import { AdminDashboardPage } from "../pages/AdminDashboardPage";
import { AdminHealthPage } from "../pages/AdminHealthPage";
import { DashboardPage } from "../pages/DashboardPage";
import { DashboardComingSoonPage } from "../pages/DashboardComingSoonPage";
import { DashboardHubPage } from "../pages/DashboardHubPage";
import { ChangePasswordPage } from "../pages/ChangePasswordPage";
import { DocumentationCenterPage } from "../pages/DocumentationCenterPage";
import { DocumentationDetailPage } from "../pages/DocumentationDetailPage";
import { ExecutiveDashboardPage } from "../pages/ExecutiveDashboardPage";
import { DepartmentManagementPage } from "../pages/DepartmentManagementPage";
import { DepartmentFormPage } from "../pages/DepartmentFormPage";
import { LeaveManagementPage } from "../pages/LeaveManagementPage";
import { LeaveBalanceAdjustmentPage } from "../pages/LeaveBalanceAdjustmentPage";
import { LeaveBalanceManagementPage } from "../pages/LeaveBalanceManagementPage";
import { LeaveBalancePage } from "../pages/LeaveBalancePage";
import { LeaveHolidayManagementPage } from "../pages/LeaveHolidayManagementPage";
import { LeaveCalendarPage } from "../pages/LeaveCalendarPage";
import { LeaveRequestDetailPage } from "../pages/LeaveRequestDetailPage";
import { LeaveRequestFormPage } from "../pages/LeaveRequestFormPage";
import { LeaveTypeManagementPage } from "../pages/LeaveTypeManagementPage";
import { LoginPage } from "../pages/LoginPage";
import { LeaveReportsPage } from "../pages/LeaveReportsPage";
import { LeaveAnalyticsPage } from "../pages/LeaveAnalyticsPage";
import { LeaveSupportPage } from "../pages/LeaveSupportPage";
import { LineSettingsPage } from "../pages/LineSettingsPage";
import { LineUsersPage } from "../pages/LineUsersPage";
import { LineLeaveApprovalPage } from "../pages/LineLeaveApprovalPage";
import { NotificationCenterPage } from "../pages/NotificationCenterPage";
import { PendingApprovalsPage } from "../pages/PendingApprovalsPage";
import { ProfilePage } from "../pages/ProfilePage";
import { ApprovalDelegationPage } from "../pages/ApprovalDelegationPage";
import { UserManagementPage } from "../pages/UserManagementPage";
import { UserFormPage } from "../pages/UserFormPage";
import { RoleManagementPage } from "../pages/RoleManagementPage";
import { RolePermissionsPage } from "../pages/RolePermissionsPage";
import { SystemSettingsPage } from "../pages/SystemSettingsPage";
import { UnauthorizedPage } from "../pages/UnauthorizedPage";
import { canAccessDashboardModule, getDashboardModule, type DashboardModuleKey } from "../config/dashboardModules";
import { useAuth } from "../context/AuthContext";
import { PermissionGuard } from "../context/PermissionContext";
import { ProtectedRoute } from "./ProtectedRoute";

const leaveViewPermissions = [
  "LeaveRequest.ViewOwn",
  "LeaveRequest.ViewPendingApproval",
  "LeaveRequest.ViewDepartment",
  "LeaveRequest.ViewAll",
];

const documentationViewerRoles = ["Staff", "DepartmentHead", "Director", "LeaveAdmin", "Admin", "SuperAdmin"];

function withPermission(element: JSX.Element, permission: string) {
  return (
    <PermissionGuard permission={permission} redirectTo="/unauthorized">
      {element}
    </PermissionGuard>
  );
}

function withAnyPermission(element: JSX.Element, permissions: string[]) {
  return (
    <PermissionGuard permissions={permissions} redirectTo="/unauthorized">
      {element}
    </PermissionGuard>
  );
}

function withAnyPermissionOrRole(element: JSX.Element, permissions: string[], roles: string[]) {
  return <RoleOrPermissionGuard permissions={permissions} roles={roles}>{element}</RoleOrPermissionGuard>;
}

function RoleOrPermissionGuard({ children, permissions, roles }: { children: JSX.Element; permissions: string[]; roles: string[] }) {
  const { user } = useAuth();
  const roleAllowed = user?.role ? roles.includes(user.role) : false;
  const permissionAllowed = permissions.some((permission) => user?.permissions?.includes(permission));

  if (roleAllowed || permissionAllowed) {
    return children;
  }

  return <Navigate to="/unauthorized" replace />;
}

function LeaveCreateGuard() {
  const { user } = useAuth();
  if (user?.role === "Admin" || user?.role === "SuperAdmin") {
    return <Navigate to="/unauthorized" replace />;
  }

  return withPermission(<LeaveRequestFormPage />, "LeaveRequest.Create");
}

function LeaveTypeGuard() {
  const { user } = useAuth();
  if (user?.role !== "Admin" && user?.role !== "SuperAdmin") {
    return <Navigate to="/unauthorized" replace />;
  }

  return withPermission(<LeaveTypeManagementPage />, "LeaveAdmin.ManageTypes");
}

function DashboardModuleGuard({ moduleKey, children }: { moduleKey: DashboardModuleKey; children?: JSX.Element }) {
  const { user } = useAuth();
  const module = getDashboardModule(moduleKey);

  if (!module || !canAccessDashboardModule(module, user)) {
    return <Navigate to="/unauthorized" replace />;
  }

  if (module.status !== "active") {
    return <DashboardComingSoonPage module={module} />;
  }

  return children ?? <DashboardComingSoonPage module={module} />;
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/unauthorized" element={<UnauthorizedPage />} />
          <Route path="/dashboard" element={withPermission(<DashboardHubPage />, "Dashboard.View")} />
          <Route path="/dashboard/leave" element={<DashboardModuleGuard moduleKey="leave"><DashboardPage /></DashboardModuleGuard>} />
          <Route path="/dashboard/vehicle" element={<DashboardModuleGuard moduleKey="vehicle" />} />
          <Route path="/dashboard/repair" element={<DashboardModuleGuard moduleKey="repair" />} />
          <Route path="/dashboard/inventory" element={<DashboardModuleGuard moduleKey="inventory" />} />
          <Route path="/dashboard/executive" element={<DashboardModuleGuard moduleKey="executive"><ExecutiveDashboardPage /></DashboardModuleGuard>} />
          <Route path="/notifications" element={withPermission(<NotificationCenterPage />, "Dashboard.View")} />
          <Route path="/docs" element={withAnyPermissionOrRole(<DocumentationCenterPage />, ["Documentation.View"], documentationViewerRoles)} />
          <Route path="/docs/:slug" element={withAnyPermissionOrRole(<DocumentationDetailPage />, ["Documentation.View"], documentationViewerRoles)} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/profile/change-password" element={<ChangePasswordPage />} />
          <Route path="/admin/users" element={withPermission(<UserManagementPage />, "UserManagement.View")} />
          <Route path="/admin/users/create" element={withPermission(<UserFormPage />, "UserManagement.Create")} />
          <Route path="/admin/users/:id/edit" element={withPermission(<UserFormPage />, "UserManagement.Edit")} />
          <Route path="/admin/departments" element={withPermission(<DepartmentManagementPage />, "DepartmentManagement.View")} />
          <Route path="/admin/departments/create" element={withPermission(<DepartmentFormPage />, "DepartmentManagement.Create")} />
          <Route path="/admin/departments/:id/edit" element={withPermission(<DepartmentFormPage />, "DepartmentManagement.Edit")} />
          <Route path="/admin/dashboard" element={withAnyPermissionOrRole(<AdminDashboardPage />, ["AdminDashboard.View"], ["Admin", "SuperAdmin"])} />
          <Route path="/admin/roles" element={withPermission(<RoleManagementPage />, "RoleManagement.View")} />
          <Route path="/admin/roles/:id/permissions" element={withPermission(<RolePermissionsPage />, "RoleManagement.Manage")} />
          <Route path="/admin/audit-logs" element={withPermission(<AuditLogPage />, "SystemSettings.View")} />
          <Route path="/admin/audit-logs/export" element={withPermission(<AuditLogExportPage />, "SystemSettings.Export")} />
          <Route path="/admin/health" element={withAnyPermissionOrRole(<AdminHealthPage />, ["System.Health.View"], ["Admin", "SuperAdmin"])} />
          <Route path="/admin/backup" element={withAnyPermissionOrRole(<AdminBackupPage />, ["System.Backup.View"], ["SuperAdmin"])} />
          <Route path="/admin/system-settings" element={withPermission(<SystemSettingsPage />, "SystemSettings.View")} />
          <Route path="/admin/line" element={<Navigate to="/admin/line-settings" replace />} />
          <Route path="/admin/line-settings" element={withAnyPermission(<LineSettingsPage />, ["System.Line.TestSend", "SystemSettings.View"])} />
          <Route path="/admin/line-users" element={withAnyPermission(<LineUsersPage />, ["System.Line.TestSend", "SystemSettings.View"])} />
          <Route path="/admin/leave-support" element={withPermission(<LeaveSupportPage />, "LeaveSupport.ViewAll")} />
          <Route path="/admin/approval-chains" element={withPermission(<ApprovalChainManagementPage />, "LeaveAdmin.ManageApprovalChains")} />
          <Route path="/admin/approval-chains/create" element={withPermission(<ApprovalChainFormPage />, "LeaveAdmin.ManageApprovalChains")} />
          <Route path="/admin/approval-chains/:id/edit" element={withPermission(<ApprovalChainFormPage />, "LeaveAdmin.ManageApprovalChains")} />
          <Route path="/admin/approval-delegations" element={withPermission(<ApprovalDelegationPage />, "LeaveApproval.Delegate")} />
          <Route path="/admin/leave-balances" element={withPermission(<LeaveBalanceManagementPage />, "LeaveAdmin.ManageBalances")} />
          <Route path="/admin/leave-balances/adjustments" element={withPermission(<LeaveBalanceAdjustmentPage />, "LeaveAdmin.ManageBalances")} />
          <Route path="/admin/leave-holidays" element={withPermission(<LeaveHolidayManagementPage />, "LeaveAdmin.ManageHolidays")} />
          <Route path="/leave" element={withAnyPermission(<LeaveManagementPage />, leaveViewPermissions)} />
          <Route path="/leave/create" element={<LeaveCreateGuard />} />
          <Route path="/leave/pending-approvals" element={withPermission(<PendingApprovalsPage />, "LeaveRequest.ViewPendingApproval")} />
          <Route path="/leave/calendar" element={withAnyPermission(<LeaveCalendarPage />, leaveViewPermissions)} />
          <Route path="/line/leave-approval/:id" element={withPermission(<LineLeaveApprovalPage />, "LeaveApproval.ApproveCurrentStep")} />
          <Route path="/leave/types" element={<LeaveTypeGuard />} />
          <Route path="/leave/balances" element={withPermission(<LeaveBalancePage />, "LeaveRequest.ViewOwn")} />
          <Route path="/leave/:id/edit" element={withPermission(<LeaveRequestFormPage />, "LeaveRequest.EditOwn")} />
          <Route path="/leave/:id" element={withAnyPermission(<LeaveRequestDetailPage />, leaveViewPermissions)} />
          <Route path="/reports/leaves" element={withPermission(<LeaveReportsPage />, "ReportManagement.View")} />
          <Route path="/reports/leave-analytics" element={withAnyPermissionOrRole(<LeaveAnalyticsPage />, ["LeaveAnalytics.View", "ReportManagement.View"], ["Director", "Admin", "SuperAdmin"])} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Route>
    </Routes>
  );
}
