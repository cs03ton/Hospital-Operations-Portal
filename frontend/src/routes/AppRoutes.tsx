import { Navigate, Route, Routes } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { ApprovalChainFormPage } from "../pages/ApprovalChainFormPage";
import { ApprovalChainManagementPage } from "../pages/ApprovalChainManagementPage";
import { AuditLogPage } from "../pages/AuditLogPage";
import { AuditLogExportPage } from "../pages/AuditLogExportPage";
import { AdminAnnouncementFormPage } from "../pages/AdminAnnouncementFormPage";
import { AdminAnnouncementsPage } from "../pages/AdminAnnouncementsPage";
import { AdminBackupPage } from "../pages/AdminBackupPage";
import { AdminDashboardPage } from "../pages/AdminDashboardPage";
import { AdminDiagnosticsPage } from "../pages/AdminDiagnosticsPage";
import { AdminHealthPage } from "../pages/AdminHealthPage";
import { DashboardPage } from "../pages/DashboardPage";
import { DashboardComingSoonPage } from "../pages/DashboardComingSoonPage";
import { DashboardHubPage } from "../pages/DashboardHubPage";
import { AnnouncementCenterPage } from "../pages/AnnouncementCenterPage";
import { AnnouncementDetailPage } from "../pages/AnnouncementDetailPage";
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
import { FleetRequestsPage } from "../pages/FleetRequestsPage";
import { FleetRequestFormPage } from "../pages/FleetRequestFormPage";
import { FleetRequestDetailPage } from "../pages/FleetRequestDetailPage";
import { FleetDispatcherQueuePage } from "../pages/FleetDispatcherQueuePage";
import { FleetWorkflowQueuePage } from "../pages/FleetWorkflowQueuePage";
import { FleetDriverJobsPage } from "../pages/FleetDriverJobsPage";
import { FleetDashboardPage } from "../pages/FleetDashboardPage";
import { FleetDelegationsPage } from "../pages/FleetDelegationsPage";
import { FleetHealthPage } from "../pages/FleetHealthPage";
import { FleetLineGroupsPage } from "../pages/FleetLineGroupsPage";
import { FleetRolloutAdminPage } from "../pages/FleetRolloutAdminPage";
import { FleetMaintenancePage } from "../pages/FleetMaintenancePage";
import { FleetMaintenanceFormPage } from "../pages/FleetMaintenanceFormPage";
import { FleetMaintenanceDetailPage } from "../pages/FleetMaintenanceDetailPage";
import { FleetVehicleDocumentsPage } from "../pages/FleetVehicleDocumentsPage";
import { FleetCalendarPage } from "../pages/FleetCalendarPage";
import { FleetCapabilitiesPage } from "../pages/FleetCapabilitiesPage";
import { FleetCapabilityFormPage } from "../pages/FleetCapabilityFormPage";
import { FleetVehicleCapabilitiesPage } from "../pages/FleetVehicleCapabilitiesPage";
import { FleetEmergencyPage } from "../pages/FleetEmergencyPage";
import { FleetEmergencyRequestPage } from "../pages/FleetEmergencyRequestPage";
import { FleetEmergencyPostReviewPage, FleetEmergencyReviewQueuePage } from "../pages/FleetEmergencyReviewPage";
import { FleetEmergencyPoliciesPage } from "../pages/FleetEmergencyPoliciesPage";
import { FleetDriverTripPage } from "../pages/FleetDriverTripPage";
import { FleetFeedbackPage } from "../pages/FleetFeedbackPage";
import { FleetRequestCapabilitiesPage } from "../pages/FleetRequestCapabilitiesPage";
import { FleetRolloutGuard } from "./FleetRolloutGuard";
import { FleetPermissionGuard } from "./FleetPermissionGuard";
import { FleetApprovalsLandingPage, FleetReportsLandingPage, FleetSettingsLandingPage } from "../pages/FleetModuleLandingPages";
import { fleetPermissionGroups } from "../config/fleetNavigation";
import { LeaveRequestFormPage } from "../pages/LeaveRequestFormPage";
import { LeaveCancellationCreatePage } from "../pages/LeaveCancellationCreatePage";
import { LeaveCancellationDetailPage } from "../pages/LeaveCancellationDetailPage";
import { LeaveCancellationListPage } from "../pages/LeaveCancellationListPage";
import { LeaveTypeManagementPage } from "../pages/LeaveTypeManagementPage";
import { LoginPage } from "../pages/LoginPage";
import { LeaveReportsPage } from "../pages/LeaveReportsPage";
import { LeaveAnalyticsPage } from "../pages/LeaveAnalyticsPage";
import { LeaveSupportPage } from "../pages/LeaveSupportPage";
import { LineSettingsPage } from "../pages/LineSettingsPage";
import { LineUsersPage } from "../pages/LineUsersPage";
import { LineLeaveApprovalPage } from "../pages/LineLeaveApprovalPage";
import { LiffEntryPage } from "../pages/LiffEntryPage";
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
  "LeaveApproval.ApproveCurrentStep",
  "LeaveRequest.ViewDepartment",
  "LeaveRequest.ViewAll",
];

const leaveCancellationViewPermissions = [
  "LeaveCancellation.ViewOwn",
  "LeaveCancellation.ApproveCurrentStep",
  "LeaveCancellation.ViewDepartment",
  "LeaveCancellation.ViewAll",
  "LeaveCancellation.Manage",
];

const documentationViewerRoles = ["Staff", "DepartmentHead", "Director", "LeaveAdmin", "FleetAdminReviewer", "พนักงานขับรถ", "Admin", "SuperAdmin"];

function withPermission(element: JSX.Element, permission: string) {
  return (
    <PermissionGuard permission={permission} redirectTo="/unauthorized">
      {element}
    </PermissionGuard>
  );
}

function withFleetRollout(element: JSX.Element) { return <FleetRolloutGuard>{element}</FleetRolloutGuard>; }

function withFleetPermission(element: JSX.Element, permissions: readonly string[]) {
  return <FleetPermissionGuard permissions={permissions}>{element}</FleetPermissionGuard>;
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
      <Route path="/liff" element={<LiffEntryPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/unauthorized" element={<UnauthorizedPage />} />
          <Route path="/dashboard" element={withPermission(<DashboardHubPage />, "Dashboard.View")} />
          <Route path="/dashboard/leave" element={<DashboardModuleGuard moduleKey="leave"><DashboardPage /></DashboardModuleGuard>} />
          <Route path="/dashboard/vehicle" element={<Navigate to="/fleet/dashboard" replace />} />
          <Route path="/dashboard/repair" element={<DashboardModuleGuard moduleKey="repair" />} />
          <Route path="/dashboard/inventory" element={<DashboardModuleGuard moduleKey="inventory" />} />
          <Route path="/dashboard/executive" element={<DashboardModuleGuard moduleKey="executive"><ExecutiveDashboardPage /></DashboardModuleGuard>} />
          <Route path="/notifications" element={withPermission(<NotificationCenterPage />, "Dashboard.View")} />
          <Route path="/announcements" element={withPermission(<AnnouncementCenterPage />, "Announcement.View")} />
          <Route path="/announcements/:id" element={withPermission(<AnnouncementDetailPage />, "Announcement.View")} />
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
          <Route path="/admin/diagnostics" element={withAnyPermissionOrRole(<AdminDiagnosticsPage />, ["System.Diagnostics.View"], ["Admin", "SuperAdmin"])} />
          <Route path="/admin/backup" element={withAnyPermissionOrRole(<AdminBackupPage />, ["System.Backup.View"], ["SuperAdmin"])} />
          <Route path="/admin/system-settings" element={withPermission(<SystemSettingsPage />, "SystemSettings.View")} />
          <Route path="/admin/fleet-rollout" element={withPermission(<FleetRolloutAdminPage />, "SystemSettings.View")} />
          <Route path="/admin/announcements" element={withAnyPermission(<AdminAnnouncementsPage />, ["Announcement.Manage", "Announcement.Create", "Announcement.EditOwn", "Announcement.EditAll"])} />
          <Route path="/admin/announcements/create" element={withAnyPermission(<AdminAnnouncementFormPage />, ["Announcement.Manage", "Announcement.Create"])} />
          <Route path="/admin/announcements/:id" element={withAnyPermission(<AnnouncementDetailPage />, ["Announcement.Manage", "Announcement.Create", "Announcement.EditOwn", "Announcement.EditAll"])} />
          <Route path="/admin/announcements/:id/edit" element={withAnyPermission(<AdminAnnouncementFormPage />, ["Announcement.Manage", "Announcement.EditOwn", "Announcement.EditAll"])} />
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
          <Route path="/leave/pending-approvals" element={withAnyPermission(<PendingApprovalsPage />, ["LeaveRequest.ViewPendingApproval", "LeaveApproval.ApproveCurrentStep"])} />
          <Route path="/leave/calendar" element={withAnyPermission(<LeaveCalendarPage />, leaveViewPermissions)} />
          <Route path="/leave/cancellations" element={withAnyPermission(<LeaveCancellationListPage />, leaveCancellationViewPermissions)} />
          <Route path="/leave/cancellations/create" element={withPermission(<LeaveCancellationCreatePage />, "LeaveCancellation.Create")} />
          <Route path="/leave/cancellations/:id" element={withAnyPermission(<LeaveCancellationDetailPage />, leaveCancellationViewPermissions)} />
          <Route path="/line/leave-approval/:id" element={withPermission(<LineLeaveApprovalPage />, "LeaveApproval.ApproveCurrentStep")} />
          <Route path="/leave/types" element={<LeaveTypeGuard />} />
          <Route path="/leave/balances" element={withPermission(<LeaveBalancePage />, "LeaveRequest.ViewOwn")} />
          <Route path="/leave/:id/edit" element={withPermission(<LeaveRequestFormPage />, "LeaveRequest.EditOwn")} />
          <Route path="/leave/:id" element={withAnyPermission(<LeaveRequestDetailPage />, leaveViewPermissions)} />
          <Route path="/fleet/requests" element={withFleetRollout(<FleetRequestsPage />)} />
          <Route path="/fleet/requests/create" element={withFleetRollout(withPermission(<FleetRequestFormPage />, "FleetRequest.Create"))} />
          <Route path="/fleet/requests/:id/edit" element={withFleetRollout(withPermission(<FleetRequestFormPage />, "FleetRequest.EditOwn"))} />
          <Route path="/fleet/requests/:id" element={withFleetRollout(<FleetRequestDetailPage />)} />
          <Route path="/fleet/requests/:id/capabilities" element={withFleetRollout(withAnyPermission(<FleetRequestCapabilitiesPage />, ["FleetRequestCapability.ManageOwn", "FleetCompatibility.View"]))} />
          <Route path="/fleet/dispatch" element={withFleetRollout(withPermission(<FleetDispatcherQueuePage />, "FleetDispatch.View"))} />
          <Route path="/fleet/dispatch/:id" element={withFleetRollout(withPermission(<FleetRequestDetailPage />, "FleetDispatch.View"))} />
          <Route path="/fleet/review" element={withFleetRollout(withFleetPermission(<FleetWorkflowQueuePage kind="admin-review" />, ["FleetAdminReview.Approve", "FleetAdminReview.Return", "FleetAdminReview.Reject"]))} />
          <Route path="/fleet/review/:id" element={withFleetRollout(withFleetPermission(<FleetRequestDetailPage />, ["FleetAdminReview.Approve", "FleetAdminReview.Return", "FleetAdminReview.Reject"]))} />
          <Route path="/fleet/director" element={withFleetRollout(withFleetPermission(<FleetWorkflowQueuePage kind="director-approval" />, ["FleetDirector.Approve", "FleetDirector.Return", "FleetDirector.Reject"]))} />
          <Route path="/fleet/director/:id" element={withFleetRollout(withFleetPermission(<FleetRequestDetailPage />, ["FleetDirector.Approve", "FleetDirector.Return", "FleetDirector.Reject"]))} />
          <Route path="/fleet/delegations" element={withFleetRollout(withAnyPermission(<FleetDelegationsPage />, ["FleetDelegation.View", "FleetDelegation.Manage"]))} />
          <Route path="/fleet/driver" element={withFleetRollout(withPermission(<FleetDriverJobsPage />, "FleetDriver.ViewOwnJobs"))} />
          <Route path="/fleet/driver/:id" element={withFleetRollout(withPermission(<FleetDriverJobsPage />, "FleetDriver.ViewOwnJobs"))} />
          <Route path="/fleet/driver/jobs" element={withFleetRollout(withAnyPermission(<FleetDriverJobsPage />, ["FleetDriver.ViewOwnJobs", "FleetDriver.ViewJobs"]))} />
          <Route path="/fleet/driver/jobs/:id" element={withFleetRollout(withAnyPermission(<FleetDriverJobsPage />, ["FleetDriver.ViewOwnJobs", "FleetDriver.ViewJobs"]))} />
          <Route path="/fleet/driver/trips/:id" element={withFleetRollout(withAnyPermission(<FleetDriverJobsPage />, ["FleetDriver.ViewOwnJobs", "FleetDriver.ViewJobs"]))} />
          <Route path="/fleet/dashboard" element={withFleetRollout(withFleetPermission(<FleetDashboardPage />, ["FleetDashboard.View", ...Object.values(fleetPermissionGroups).flat()]))} />
          <Route path="/fleet/trips/:tripId/feedback" element={withFleetRollout(withAnyPermission(<FleetFeedbackPage />, ["FleetFeedback.Create", "FleetFeedback.ViewOwn"]))} />
          <Route path="/fleet/my-trips" element={withFleetRollout(withAnyPermission(<FleetDriverJobsPage />, [...fleetPermissionGroups.driver]))} />
          <Route path="/fleet/requests/review" element={withFleetRollout(withAnyPermission(<FleetDispatcherQueuePage />, [...fleetPermissionGroups.dispatcher]))} />
          <Route path="/fleet/approvals" element={withFleetRollout(withFleetPermission(<FleetApprovalsLandingPage />, [...fleetPermissionGroups.reviewer, ...fleetPermissionGroups.director]))} />
          <Route path="/fleet/reports" element={withFleetRollout(withAnyPermission(<FleetReportsLandingPage />, [...fleetPermissionGroups.reports]))} />
          <Route path="/fleet/settings" element={withFleetRollout(withAnyPermission(<FleetSettingsLandingPage />, [...fleetPermissionGroups.settings]))} />
          <Route path="/fleet/maintenance" element={withFleetRollout(withPermission(<FleetMaintenancePage />, "FleetMaintenance.View"))} />
          <Route path="/fleet/maintenance/create" element={withFleetRollout(withPermission(<FleetMaintenanceFormPage />, "FleetMaintenance.Manage"))} />
          <Route path="/fleet/maintenance/:id" element={withFleetRollout(withPermission(<FleetMaintenanceDetailPage />, "FleetMaintenance.View"))} />
          <Route path="/fleet/maintenance/:id/edit" element={withFleetRollout(withPermission(<FleetMaintenanceFormPage />, "FleetMaintenance.Manage"))} />
          <Route path="/fleet/vehicles/:vehicleId/maintenance" element={withFleetRollout(withPermission(<FleetMaintenancePage />, "FleetMaintenance.View"))} />
          <Route path="/fleet/vehicles/:vehicleId/documents" element={withFleetRollout(withPermission(<FleetVehicleDocumentsPage />, "FleetMaintenance.View"))} />
          <Route path="/fleet/calendar" element={withFleetRollout(<FleetCalendarPage />)} />
          <Route path="/fleet/admin/capabilities" element={withFleetRollout(withAnyPermission(<FleetCapabilitiesPage />, ["FleetCapability.View", "FleetCapability.Manage"]))} />
          <Route path="/fleet/admin/capabilities/create" element={withFleetRollout(withPermission(<FleetCapabilityFormPage />, "FleetCapability.Manage"))} />
          <Route path="/fleet/admin/capabilities/:id" element={withFleetRollout(withPermission(<FleetCapabilityFormPage readOnly />, "FleetCapability.View"))} />
          <Route path="/fleet/admin/capabilities/:id/edit" element={withFleetRollout(withPermission(<FleetCapabilityFormPage />, "FleetCapability.Manage"))} />
          <Route path="/fleet/admin/vehicles/:vehicleId/capabilities" element={withFleetRollout(withPermission(<FleetVehicleCapabilitiesPage />, "FleetVehicleCapability.Manage"))} />
          <Route path="/fleet/admin/emergency-policies" element={withFleetRollout(withAnyPermission(<FleetEmergencyPoliciesPage />, ["FleetEmergencyPolicy.View", "FleetEmergencyPolicy.Manage"]))} />
          <Route path="/fleet/emergency" element={withFleetRollout(withPermission(<FleetEmergencyPage />, "FleetEmergency.ViewQueue"))} />
          <Route path="/fleet/emergency/create" element={withFleetRollout(withPermission(<FleetEmergencyRequestPage />, "FleetEmergency.Create"))} />
          <Route path="/fleet/emergency/review" element={withFleetRollout(withPermission(<FleetEmergencyReviewQueuePage />, "FleetEmergency.Review"))} />
          <Route path="/fleet/emergency/:id/post-review" element={withFleetRollout(withPermission(<FleetEmergencyPostReviewPage />, "FleetEmergency.Review"))} />
            <Route path="/fleet/driver/trips/:id/action" element={withFleetRollout(withAnyPermission(<FleetDriverTripPage />, ["FleetDriver.ViewOwnJobs", "FleetDriver.ViewJobs", "FleetTrip.Start", "FleetTrip.Complete", "FleetDriver.Start", "FleetDriver.Complete", "FleetDriver.StartTrip", "FleetDriver.CompleteTrip"]))} />
          <Route path="/fleet/health" element={withFleetRollout(withAnyPermission(<FleetHealthPage />, ["FleetHealth.View", "FleetHealth.Manage"]))} />
          <Route path="/fleet/admin/line-groups" element={withFleetRollout(withAnyPermission(<FleetLineGroupsPage />, ["FleetLineGroup.View", "FleetLineGroup.Manage"]))} />
          <Route path="/reports/leaves" element={withAnyPermissionOrRole(<LeaveReportsPage />, ["ReportManagement.View", "LeaveAnalytics.View"], ["Director", "Admin", "SuperAdmin"])} />
          <Route path="/reports/leave-analytics" element={withAnyPermissionOrRole(<LeaveAnalyticsPage />, ["LeaveAnalytics.View", "ReportManagement.View"], ["Director", "Admin", "SuperAdmin"])} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Route>
    </Routes>
  );
}
