# HOP Phase 1 Role Permission Matrix

Matrix นี้สรุปสิทธิ์จากระบบจริงตาม frontend route/menu guard และ backend permission seed/controller attributes โดยไม่แก้ไข business logic, database schema, role หรือ permission ใด ๆ

## Evidence Sources

- Frontend routes: `frontend/src/routes/AppRoutes.tsx`
- Frontend menu: `frontend/src/config/menuConfig.ts`
- Frontend guard: `frontend/src/context/PermissionContext.tsx`, `frontend/src/routes/ProtectedRoute.tsx`
- Backend authorization attributes: `backend/Hop.Api/Authorization/RequirePermissionAttribute.cs`, `backend/Hop.Api/Authorization/PermissionPolicyProvider.cs`, `backend/Hop.Api/Authorization/PermissionAuthorizationHandler.cs`
- Backend permission constants: `backend/Hop.Api/Authorization/LeavePermissions.cs`
- Backend role/permission seed: `backend/Hop.Api/Data/DevelopmentDataSeeder.cs`
- Backend controllers: `backend/Hop.Api/Controllers/*.cs`

## Role Mapping

| Requested Role | System Role | Seed Evidence |
|---|---|---|
| user | Staff | `DevelopmentDataSeeder.cs:38-46`, `DevelopmentDataSeeder.cs:356-361` |
| head | DepartmentHead | `DevelopmentDataSeeder.cs:38-46`, `DevelopmentDataSeeder.cs:362-370` |
| director | Director | `DevelopmentDataSeeder.cs:38-46`, `DevelopmentDataSeeder.cs:371-378` |
| hr | LeaveAdmin | `DevelopmentDataSeeder.cs:38-46`, `DevelopmentDataSeeder.cs:379-386` |
| superadmin | SuperAdmin | `DevelopmentDataSeeder.cs:38-46`, `DevelopmentDataSeeder.cs:349-355` |

## Matrix

| Role | Frontend Page | Route | Menu Visible | Backend Permission | API Endpoint | Access | Evidence |
|---|---|---|---|---|---|---|---|
| public | Login | `/login` | No | Anonymous | `POST /api/auth/login` | Allowed | `AppRoutes.tsx:73`; `AuthController.cs:17-18` |
| user | Dashboard | `/dashboard` | Yes | `Dashboard.View` | `GET /api/dashboard/summary` | Allowed | `AppRoutes.tsx:77`; `menuConfig.ts:48`; `DashboardController.cs:16-17`; `DevelopmentDataSeeder.cs:356-361` |
| user | Notifications | `/notifications` | No direct sidebar item | `Dashboard.View` frontend; backend authenticated only | `GET /api/notifications/me`, `GET /api/notifications`, `GET /api/notifications/badge` | Allowed | `AppRoutes.tsx:78`; `NotificationsController.cs:14-17` |
| user | Profile | `/profile` | Header/user menu | Authenticated only | `GET/PUT /api/me/profile` | Allowed | `AppRoutes.tsx:79`; `MeProfileController.cs:15,39,51` |
| user | Leave List | `/leave` | Yes | `LeaveRequest.ViewOwn` | `GET /api/leave-requests` | Allowed | `AppRoutes.tsx:108`; `menuConfig.ts:50`; `LeaveRequestsController.cs:32-33`; `DevelopmentDataSeeder.cs:356-361` |
| user | Create Leave | `/leave/create` | Via page/action | `LeaveRequest.Create` | `POST /api/leave-requests`, `POST /api/leave-requests/policy-preview` | Allowed | `AppRoutes.tsx:109`; `LeaveRequestsController.cs:160-161,219-220`; `DevelopmentDataSeeder.cs:356-361` |
| user | Leave Calendar | `/leave/calendar` | Yes | `LeaveRequest.ViewOwn` | `GET /api/leave-calendar` | Allowed | `AppRoutes.tsx:111`; `menuConfig.ts:51`; `LeaveCalendarController.cs:16-17` |
| user | Leave Balance | `/leave/balances` | Yes | `LeaveRequest.ViewOwn` | `GET /api/leave-balances/me` | Allowed | `AppRoutes.tsx:114`; `menuConfig.ts:52`; `LeaveBalancesController.cs:66-67` |
| user | Pending Approval | `/leave/pending-approvals` | Hidden | `LeaveRequest.ViewPendingApproval` | `GET /api/approvals/my-pending` | Denied | `AppRoutes.tsx:110`; `menuConfig.ts:49`; `DevelopmentDataSeeder.cs:356-361` |
| user | User List | `/admin/users` | Hidden | `UserManagement.View` | `GET /api/users` | Denied | `AppRoutes.tsx:80`; `menuConfig.ts:68`; `UsersController.cs:48-49` |
| head | Dashboard | `/dashboard` | Yes | `Dashboard.View` | `GET /api/dashboard/summary` | Allowed | `DevelopmentDataSeeder.cs:362-370`; `AppRoutes.tsx:77` |
| head | Pending Approval | `/leave/pending-approvals` | Yes | `LeaveRequest.ViewPendingApproval` | `GET /api/approvals/my-pending` | Allowed | `AppRoutes.tsx:110`; `menuConfig.ts:49`; `ApprovalsController.cs:14-15`; `DevelopmentDataSeeder.cs:362-370` |
| head | Leave List Department | `/leave` | Yes | `LeaveRequest.ViewDepartment` | `GET /api/leave-requests` | Allowed | `AppRoutes.tsx:108`; `menuConfig.ts:50`; `LeaveRequestsController.cs:32-33`; `DevelopmentDataSeeder.cs:362-370` |
| head | Create Leave | `/leave/create` | Via page/action | `LeaveRequest.Create` | `POST /api/leave-requests` | Allowed | `AppRoutes.tsx:109`; `LeaveRequestsController.cs:160-161`; `DevelopmentDataSeeder.cs:362-370` |
| head | Leave Calendar | `/leave/calendar` | Yes | `LeaveRequest.ViewDepartment` | `GET /api/leave-calendar` | Allowed | `AppRoutes.tsx:111`; `menuConfig.ts:51`; `LeaveCalendarController.cs:16-17` |
| head | Leave Balance | `/leave/balances` | Yes | `LeaveRequest.ViewOwn` | `GET /api/leave-balances/me` | Allowed | `AppRoutes.tsx:114`; `menuConfig.ts:52`; `LeaveBalancesController.cs:66-67` |
| head | Approve/Reject Leave | `/leave/:id` | Via pending/detail | `LeaveApproval.ApproveCurrentStep` | `POST /api/leave-requests/{id}/approve`, `POST /api/leave-requests/{id}/reject` | Allowed | `LeaveRequestsController.cs:423-431`; `DevelopmentDataSeeder.cs:362-370` |
| head | Reports | `/reports/leaves` | Hidden | `ReportManagement.View` | `GET /api/reports/leaves` | Denied | `AppRoutes.tsx:116`; `menuConfig.ts:59`; `DevelopmentDataSeeder.cs:362-370` |
| director | Dashboard | `/dashboard` | Yes | `Dashboard.View` | `GET /api/dashboard/summary` | Allowed | `DevelopmentDataSeeder.cs:371-378`; `AppRoutes.tsx:77` |
| director | Pending Approval | `/leave/pending-approvals` | Yes | `LeaveRequest.ViewPendingApproval` | `GET /api/approvals/my-pending` | Allowed | `AppRoutes.tsx:110`; `menuConfig.ts:49`; `ApprovalsController.cs:14-15`; `DevelopmentDataSeeder.cs:371-378` |
| director | Leave List | `/leave` | Yes | `LeaveRequest.ViewOwn` or pending approval view | `GET /api/leave-requests` | Allowed | `AppRoutes.tsx:108`; `LeaveRequestsController.cs:32-33`; `DevelopmentDataSeeder.cs:371-378` |
| director | Create Leave | `/leave/create` | Via page/action | `LeaveRequest.Create` | `POST /api/leave-requests` | Allowed | `AppRoutes.tsx:109`; `LeaveRequestsController.cs:160-161`; `DevelopmentDataSeeder.cs:371-378` |
| director | Approve/Reject Leave | `/leave/:id` | Via pending/detail | `LeaveApproval.ApproveCurrentStep` | `POST /api/leave-requests/{id}/approve`, `POST /api/leave-requests/{id}/reject` | Allowed | `LeaveRequestsController.cs:423-431`; `DevelopmentDataSeeder.cs:371-378` |
| director | Executive Dashboard/Leave Report | `/reports/leaves` | Yes | `ReportManagement.View`, `LeaveAnalytics.View`, or Director/Admin/SuperAdmin role policy | `GET /api/reports/leaves` | Allowed | `AppRoutes.tsx`; `menuConfig.ts`; `LeaveReportsController.cs` |
| hr | Dashboard | `/dashboard` | Yes | `Dashboard.View` | `GET /api/dashboard/summary` | Allowed | `DevelopmentDataSeeder.cs:379-386`; `AppRoutes.tsx:77` |
| hr | Leave List Department | `/leave` | Yes | `LeaveRequest.ViewDepartment` | `GET /api/leave-requests` | Allowed | `AppRoutes.tsx:108`; `menuConfig.ts:50`; `LeaveRequestsController.cs:32-33`; `DevelopmentDataSeeder.cs:379-386` |
| hr | Leave Calendar | `/leave/calendar` | Yes | `LeaveRequest.ViewDepartment` | `GET /api/leave-calendar` | Allowed | `AppRoutes.tsx:111`; `menuConfig.ts:51`; `LeaveCalendarController.cs:16-17` |
| hr | Leave Balance Management | `/admin/leave-balances` | Yes | `LeaveAdmin.ManageBalances` | `GET/POST /api/leave-balances` | Allowed | `AppRoutes.tsx:102`; `menuConfig.ts:53`; `LeaveBalancesController.cs:21-22,109-110`; `DevelopmentDataSeeder.cs:379-386` |
| hr | Leave Type Management | `/leave/types` | Yes | `LeaveAdmin.ManageTypes` | `GET/POST /api/leave-types` | Allowed | `AppRoutes.tsx:113`; `menuConfig.ts:54`; `LeaveTypesController.cs:17-31`; `DevelopmentDataSeeder.cs:379-386` |
| hr | Approval Chain | `/admin/approval-chains` | Yes | `LeaveAdmin.ManageApprovalChains` | `GET/POST /api/approval-chains` | Allowed | `AppRoutes.tsx:99`; `menuConfig.ts:55`; `ApprovalChainsController.cs:17-18`; `DevelopmentDataSeeder.cs:379-386` |
| hr | Holiday Management | `/admin/leave-holidays` | Yes | `LeaveAdmin.ManageHolidays` | `GET/POST /api/leave-holidays` | Allowed | `AppRoutes.tsx:103`; `menuConfig.ts:57`; `LeaveHolidaysController.cs:24-25,72-73`; `DevelopmentDataSeeder.cs:379-386` |
| hr | Leave Support | `/admin/leave-support` | Hidden | `LeaveSupport.ViewAll` | `GET /api/leave-support/requests` | Denied | `AppRoutes.tsx:98`; `menuConfig.ts:58`; LeaveAdmin seed lacks `LeaveSupport.ViewAll` at `DevelopmentDataSeeder.cs:379-386` |
| hr | Leave Report | `/reports/leaves` | Hidden | `ReportManagement.View` | `GET /api/reports/leaves` | Denied | `AppRoutes.tsx:116`; `menuConfig.ts:59`; LeaveAdmin seed lacks report permission at `DevelopmentDataSeeder.cs:379-386` |
| superadmin | Dashboard | `/dashboard` | Yes | `Dashboard.View` | `GET /api/dashboard/summary` | Allowed | `DevelopmentDataSeeder.cs:349-355`; `AppRoutes.tsx:77` |
| superadmin | Leave List | `/leave` | Yes | `LeaveRequest.ViewOwn/ViewDepartment/ViewAll` | `GET /api/leave-requests` | Allowed | `AppRoutes.tsx:108`; `menuConfig.ts:50`; `DevelopmentDataSeeder.cs:349-355` |
| superadmin | Create Leave | `/leave/create` | No | `LeaveRequest.Create` revoked and frontend role guard denies | `POST /api/leave-requests` | Denied | `AppRoutes.tsx:54-62`; `DevelopmentDataSeeder.cs:349-355`; `LeaveRequestsController.cs:160-161` |
| superadmin | Pending Approval | `/leave/pending-approvals` | Hidden | `LeaveRequest.ViewPendingApproval` | `GET /api/approvals/my-pending` | Allowed | Menu hidden by role at `menuConfig.ts:49`; permission granted by all-permissions seed except create at `DevelopmentDataSeeder.cs:349-355` |
| superadmin | User List | `/admin/users` | Yes | `UserManagement.View` | `GET /api/users` | Allowed | `AppRoutes.tsx:80`; `menuConfig.ts:68`; `UsersController.cs:48-49` |
| superadmin | Create User | `/admin/users/create` | Via page/action | `UserManagement.Create` | `POST /api/users` | Allowed | `AppRoutes.tsx:81`; `UsersController.cs:72-73` |
| superadmin | Edit User | `/admin/users/:id/edit` | Via page/action | `UserManagement.Edit` | `PUT /api/users/{id}` | Allowed | `AppRoutes.tsx:82`; `UsersController.cs:164-165` |
| superadmin | Department List | `/admin/departments` | Yes | `DepartmentManagement.View` | `GET /api/departments` | Allowed | `AppRoutes.tsx:83`; `menuConfig.ts:69`; `DepartmentsController.cs:17-18` |
| superadmin | Role List | `/admin/roles` | Yes | `RoleManagement.View` | `GET /api/roles` | Allowed | `AppRoutes.tsx:86`; `menuConfig.ts:70`; `RolesController.cs:17-18` |
| superadmin | Role Permissions | `/admin/roles/:id/permissions` | Via role page | `RoleManagement.Manage` | `GET/PUT /api/roles/{roleId}/permissions` | Allowed | `AppRoutes.tsx:87`; `RolesController.cs:117-139` |
| superadmin | Audit Log | `/admin/audit-logs` | Yes | `SystemSettings.View` | `GET /api/audit-logs` | Allowed | `AppRoutes.tsx:88`; `menuConfig.ts:71`; `AuditLogsController.cs:15-19` |
| superadmin | Audit Export | `/admin/audit-logs/export` | Via audit page/action | `SystemSettings.Export` | `GET /api/audit-logs/export-excel`, `GET /api/audit-logs/export-pdf` | Allowed | `AppRoutes.tsx:89`; `AuditLogsController.cs:107-217` |
| superadmin | System Settings | `/admin/system-settings` | Yes | `SystemSettings.View` | `GET /api/system-settings` | Allowed | `AppRoutes.tsx:90`; `menuConfig.ts:72`; `SystemSettingsController.cs:14-15` |
| superadmin | LINE Settings | `/admin/line-settings` | Yes | `System.Line.TestSend` or `SystemSettings.View` | `GET /api/admin/line/settings` | Allowed | `AppRoutes.tsx:91`; `menuConfig.ts:73`; `AdminLineController.cs:18-29` |
| superadmin | LINE Users | `/admin/line-users` | Yes | `System.Line.TestSend` or `SystemSettings.View` | `GET /api/admin/line/line-users` | Allowed | `AppRoutes.tsx:92`; `menuConfig.ts:74`; `AdminLineController.cs:331` |

## Mismatches / Unknowns

| Area | Status | Evidence | Notes |
|---|---|---|---|
| Director executive report | Allowed | `AppRoutes.tsx`, `menuConfig.ts`, `LeaveReportsController.cs` | Director/Admin/SuperAdmin can access report by role policy; explicit permissions still supported |
| HR leave report/export | Denied/Hidden | `AppRoutes.tsx:116`, `DevelopmentDataSeeder.cs:379-386` | HR manual mentions reports, but `LeaveAdmin` seed lacks `ReportManagement.View/Export` |
| HR leave support | Denied/Hidden | `AppRoutes.tsx:98`, `DevelopmentDataSeeder.cs:379-386` | `LeaveSupport.ViewAll` is required but not granted to `LeaveAdmin` |
| SuperAdmin pending approval menu | Hidden but route allowed | `menuConfig.ts:49`, `DevelopmentDataSeeder.cs:349-355` | Menu intentionally hides for Admin/SuperAdmin while permission exists |
| Profile route | Allowed/Unknown granularity | `AppRoutes.tsx:79`, `MeProfileController.cs:15` | Protected by authentication only; no explicit permission |
