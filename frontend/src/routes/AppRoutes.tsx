import { Navigate, Route, Routes } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { ApprovalChainFormPage } from "../pages/ApprovalChainFormPage";
import { ApprovalChainManagementPage } from "../pages/ApprovalChainManagementPage";
import { AuditLogPage } from "../pages/AuditLogPage";
import { AuditLogExportPage } from "../pages/AuditLogExportPage";
import { DashboardPage } from "../pages/DashboardPage";
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
import { LeaveSupportPage } from "../pages/LeaveSupportPage";
import { PendingApprovalsPage } from "../pages/PendingApprovalsPage";
import { ApprovalDelegationPage } from "../pages/ApprovalDelegationPage";
import { UserManagementPage } from "../pages/UserManagementPage";
import { UserFormPage } from "../pages/UserFormPage";
import { RoleManagementPage } from "../pages/RoleManagementPage";
import { RolePermissionsPage } from "../pages/RolePermissionsPage";
import { SystemSettingsPage } from "../pages/SystemSettingsPage";
import { UnauthorizedPage } from "../pages/UnauthorizedPage";
import { useAuth } from "../context/AuthContext";
import { PermissionGuard } from "../context/PermissionContext";
import { ProtectedRoute } from "./ProtectedRoute";

const leaveViewPermissions = [
  "LeaveRequest.ViewOwn",
  "LeaveRequest.ViewPendingApproval",
  "LeaveRequest.ViewDepartment",
  "LeaveRequest.ViewAll",
];

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

function LeaveCreateGuard() {
  const { user } = useAuth();
  if (user?.role === "Admin" || user?.role === "SuperAdmin") {
    return <Navigate to="/unauthorized" replace />;
  }

  return withPermission(<LeaveRequestFormPage />, "LeaveRequest.Create");
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/unauthorized" element={<UnauthorizedPage />} />
          <Route path="/dashboard" element={withPermission(<DashboardPage />, "Dashboard.View")} />
          <Route path="/admin/users" element={withPermission(<UserManagementPage />, "UserManagement.View")} />
          <Route path="/admin/users/create" element={withPermission(<UserFormPage />, "UserManagement.Create")} />
          <Route path="/admin/users/:id/edit" element={withPermission(<UserFormPage />, "UserManagement.Edit")} />
          <Route path="/admin/departments" element={withPermission(<DepartmentManagementPage />, "DepartmentManagement.View")} />
          <Route path="/admin/departments/create" element={withPermission(<DepartmentFormPage />, "DepartmentManagement.Create")} />
          <Route path="/admin/departments/:id/edit" element={withPermission(<DepartmentFormPage />, "DepartmentManagement.Edit")} />
          <Route path="/admin/roles" element={withPermission(<RoleManagementPage />, "RoleManagement.View")} />
          <Route path="/admin/roles/:id/permissions" element={withPermission(<RolePermissionsPage />, "RoleManagement.Manage")} />
          <Route path="/admin/audit-logs" element={withPermission(<AuditLogPage />, "SystemSettings.View")} />
          <Route path="/admin/audit-logs/export" element={withPermission(<AuditLogExportPage />, "SystemSettings.Export")} />
          <Route path="/admin/system-settings" element={withPermission(<SystemSettingsPage />, "SystemSettings.View")} />
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
          <Route path="/leave/types" element={withAnyPermission(<LeaveTypeManagementPage />, ["LeaveRequest.Create", "LeaveAdmin.ManageTypes"])} />
          <Route path="/leave/balances" element={withPermission(<LeaveBalancePage />, "LeaveRequest.ViewOwn")} />
          <Route path="/leave/:id" element={withAnyPermission(<LeaveRequestDetailPage />, leaveViewPermissions)} />
          <Route path="/reports/leaves" element={withPermission(<LeaveReportsPage />, "ReportManagement.View")} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Route>
    </Routes>
  );
}
