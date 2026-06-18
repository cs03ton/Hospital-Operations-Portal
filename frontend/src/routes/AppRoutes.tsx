import { Navigate, Route, Routes } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { AdministrationPage } from "../pages/AdministrationPage";
import { ApprovalChainFormPage } from "../pages/ApprovalChainFormPage";
import { ApprovalChainManagementPage } from "../pages/ApprovalChainManagementPage";
import { AssetBorrowingPage } from "../pages/AssetBorrowingPage";
import { AuditLogPage } from "../pages/AuditLogPage";
import { AuditLogExportPage } from "../pages/AuditLogExportPage";
import { DashboardPage } from "../pages/DashboardPage";
import { DepartmentManagementPage } from "../pages/DepartmentManagementPage";
import { DepartmentFormPage } from "../pages/DepartmentFormPage";
import { InventoryPage } from "../pages/InventoryPage";
import { LeaveManagementPage } from "../pages/LeaveManagementPage";
import { LeaveBalanceAdjustmentPage } from "../pages/LeaveBalanceAdjustmentPage";
import { LeaveBalancePage } from "../pages/LeaveBalancePage";
import { LeaveHolidayManagementPage } from "../pages/LeaveHolidayManagementPage";
import { LeaveCalendarPage } from "../pages/LeaveCalendarPage";
import { LeaveRequestDetailPage } from "../pages/LeaveRequestDetailPage";
import { LeaveRequestFormPage } from "../pages/LeaveRequestFormPage";
import { LeaveTypeManagementPage } from "../pages/LeaveTypeManagementPage";
import { LoginPage } from "../pages/LoginPage";
import { MaterialRequestPage } from "../pages/MaterialRequestPage";
import { MeetingRoomBookingPage } from "../pages/MeetingRoomBookingPage";
import { RepairManagementPage } from "../pages/RepairManagementPage";
import { ReportsPage } from "../pages/ReportsPage";
import { LeaveReportsPage } from "../pages/LeaveReportsPage";
import { ApprovalDelegationPage } from "../pages/ApprovalDelegationPage";
import { UserManagementPage } from "../pages/UserManagementPage";
import { UserFormPage } from "../pages/UserFormPage";
import { RoleManagementPage } from "../pages/RoleManagementPage";
import { RolePermissionsPage } from "../pages/RolePermissionsPage";
import { SessionManagementPage } from "../pages/SessionManagementPage";
import { UnauthorizedPage } from "../pages/UnauthorizedPage";
import { VehicleBookingPage } from "../pages/VehicleBookingPage";
import { PermissionGuard } from "../context/PermissionContext";
import { ProtectedRoute } from "./ProtectedRoute";

function withPermission(element: JSX.Element, permission: string) {
  return (
    <PermissionGuard permission={permission} redirectTo="/unauthorized">
      {element}
    </PermissionGuard>
  );
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
          <Route path="/admin/sessions" element={withPermission(<SessionManagementPage />, "SystemSettings.Manage")} />
          <Route path="/admin/settings" element={withPermission(<AdministrationPage />, "SystemSettings.View")} />
          <Route path="/admin/approval-chains" element={withPermission(<ApprovalChainManagementPage />, "ApprovalChain.View")} />
          <Route path="/admin/approval-chains/create" element={withPermission(<ApprovalChainFormPage />, "ApprovalChain.Create")} />
          <Route path="/admin/approval-chains/:id/edit" element={withPermission(<ApprovalChainFormPage />, "ApprovalChain.Edit")} />
          <Route path="/admin/approval-delegations" element={withPermission(<ApprovalDelegationPage />, "ApprovalDelegation.View")} />
          <Route path="/admin/leave-balances/adjustments" element={withPermission(<LeaveBalanceAdjustmentPage />, "LeaveBalance.Adjust")} />
          <Route path="/admin/leave-holidays" element={withPermission(<LeaveHolidayManagementPage />, "LeaveHoliday.Manage")} />
          <Route path="/leave" element={withPermission(<LeaveManagementPage />, "LeaveManagement.View")} />
          <Route path="/leave/create" element={withPermission(<LeaveRequestFormPage />, "LeaveManagement.Create")} />
          <Route path="/leave/calendar" element={withPermission(<LeaveCalendarPage />, "LeaveManagement.View")} />
          <Route path="/leave/types" element={withPermission(<LeaveTypeManagementPage />, "LeaveManagement.Manage")} />
          <Route path="/leave/balances" element={withPermission(<LeaveBalancePage />, "LeaveManagement.View")} />
          <Route path="/leave/:id" element={withPermission(<LeaveRequestDetailPage />, "LeaveManagement.View")} />
          <Route path="/borrowing" element={withPermission(<AssetBorrowingPage />, "BorrowManagement.View")} />
          <Route path="/repairs" element={withPermission(<RepairManagementPage />, "RepairManagement.View")} />
          <Route path="/vehicles" element={withPermission(<VehicleBookingPage />, "BorrowManagement.View")} />
          <Route path="/meeting-rooms" element={withPermission(<MeetingRoomBookingPage />, "BorrowManagement.View")} />
          <Route path="/materials" element={withPermission(<MaterialRequestPage />, "InventoryManagement.View")} />
          <Route path="/inventory" element={withPermission(<InventoryPage />, "InventoryManagement.View")} />
          <Route path="/reports" element={withPermission(<ReportsPage />, "ReportManagement.View")} />
          <Route path="/reports/leaves" element={withPermission(<LeaveReportsPage />, "ReportManagement.View")} />
          <Route path="/administration" element={withPermission(<AdministrationPage />, "SystemSettings.Manage")} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Route>
    </Routes>
  );
}
