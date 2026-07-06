# HOP Phase 1 Screenshot Catalog

Catalog นี้จัดทำจาก route/menu/permission guard ฝั่ง frontend และ authorization/permission seed ฝั่ง backendแบบ read-only เพื่อให้ screenshot ตรงกับสิทธิ์จริงของระบบ HOP Phase 1

## Role Normalization

| Catalog Role | System Role / Seed Source | Notes |
|---|---|---|
| user | Staff | ผู้ใช้งานทั่วไป |
| head | DepartmentHead | หัวหน้างาน/ผู้อนุมัติระดับหน่วยงาน |
| director | Director | ผู้อนุมัติระดับผู้อำนวยการ |
| hr | LeaveAdmin | เจ้าหน้าที่ HR/ผู้ดูแลระบบลา |
| superadmin | SuperAdmin | ผู้ดูแลระบบสูงสุด |

## Status Values

- Pending: ยังไม่ได้ capture
- Captured: capture แล้วและผ่าน review
- Skipped: ไม่ต้อง capture หรือไม่อยู่ใน Phase 1
- Blocked: ยังเข้าไม่ได้จากสิทธิ์จริงหรือยังไม่มีข้อมูลทดสอบ

## Catalog

| No | Role | Module | Page | Route | Screenshot File | Required | Status | Capture Method | Notes |
|---:|---|---|---|---|---|---|---|---|---|
| 1 | public | Common | Login | `/login` | `login/login-01-page.png` | Yes | Pending | Playwright | Public route; do not capture password after typing |
| 2 | user | Dashboard | Dashboard | `/dashboard` | `dashboard/dashboard-user.png` | Yes | Pending | Playwright | Staff has `Dashboard.View` |
| 3 | user | Common | My Profile | `/profile` | `common/profile-user.png` | Yes | Pending | Playwright | Protected route, no specific permission guard |
| 4 | user | Common | Change Password/Profile Edit | `/profile` | `common/change-password-user.png` | Yes | Pending | Manual | Capture only if UI exposes change password/profile edit state |
| 5 | user | Leave Management | Leave List | `/leave` | `leave/leave-list-user.png` | Yes | Pending | Playwright | Staff has `LeaveRequest.ViewOwn` |
| 6 | user | Leave Management | Create Leave | `/leave/create` | `leave/leave-create.png` | Yes | Pending | Playwright | Staff has `LeaveRequest.Create` |
| 7 | user | Leave Management | Leave Detail Draft | `/leave/:id` | `leave/leave-detail-draft.png` | Yes | Pending | Seed Required | Requires safe test leave id |
| 8 | user | Leave Management | Leave Detail Pending | `/leave/:id` | `leave/leave-detail-pending.png` | Yes | Pending | Seed Required | Requires submitted test leave id |
| 9 | user | Leave Management | Leave Detail Approved | `/leave/:id` | `leave/leave-detail-approved.png` | Yes | Pending | Seed Required | Requires approved test leave id |
| 10 | user | Leave Management | Leave Detail Rejected | `/leave/:id` | `leave/leave-detail-rejected.png` | Yes | Pending | Seed Required | Requires rejected test leave id |
| 11 | user | Leave Management | Leave Detail Cancelled | `/leave/:id` | `leave/leave-detail-cancelled.png` | Yes | Pending | Seed Required | Requires cancelled test leave id |
| 12 | user | Leave Management | Upload Attachment | `/leave/:id` | `leave/leave-upload-attachment.png` | Yes | Pending | Seed Required | Use test file only |
| 13 | user | Leave Management | Leave History | `/leave/:id` | `leave/leave-history-user.png` | Yes | Pending | Seed Required | Timeline/history on leave detail |
| 14 | user | Leave Management | Leave PDF | `/leave/:id` | `leave/leave-pdf-preview.png` | Yes | Pending | Seed Required | API `/api/leave-requests/{id}/pdf` |
| 15 | user | Leave Management | Leave Calendar | `/leave/calendar` | `leave/leave-calendar-user.png` | Yes | Pending | Playwright | Staff has `LeaveRequest.ViewOwn` |
| 16 | user | Leave Management | Leave Balance | `/leave/balances` | `leave/leave-balance-user.png` | Yes | Pending | Playwright | Hidden for Admin/SuperAdmin only |
| 17 | user | Common | Notification | `/notifications` | `common/notification-user.png` | Yes | Pending | Playwright | Protected by `Dashboard.View` |
| 18 | head | Dashboard | Dashboard | `/dashboard` | `dashboard/dashboard-head.png` | Yes | Pending | Playwright | DepartmentHead has `Dashboard.View` |
| 19 | head | Approval | Pending Approval | `/leave/pending-approvals` | `approval/approval-pending-list.png` | Yes | Pending | Playwright | DepartmentHead has `LeaveRequest.ViewPendingApproval` |
| 20 | head | Approval | Approval Detail | `/leave/:id` | `approval/approval-detail-head.png` | Yes | Pending | Seed Required | Requires leave id currently assigned to head |
| 21 | head | Approval | Approve Leave | `/leave/:id` | `approval/approve-leave-head.png` | Yes | Pending | Seed Required | Requires pending approval test leave |
| 22 | head | Approval | Reject Leave | `/leave/:id` | `approval/reject-leave-head.png` | Yes | Pending | Seed Required | Requires pending approval test leave |
| 23 | head | Approval | Approval History | `/leave/:id` | `approval/approval-history.png` | Yes | Pending | Seed Required | Approval timeline/history |
| 24 | head | Leave Management | Team Leave Calendar | `/leave/calendar` | `approval/team-leave-calendar.png` | Yes | Pending | Playwright | DepartmentHead has department view |
| 25 | head | Leave Management | Leave List Department | `/leave` | `leave/leave-list-head.png` | Yes | Pending | Playwright | DepartmentHead has `LeaveRequest.ViewDepartment` |
| 26 | director | Dashboard | Dashboard | `/dashboard` | `dashboard/dashboard-director.png` | Yes | Pending | Playwright | Director has `Dashboard.View` |
| 27 | director | Approval | Pending Approval | `/leave/pending-approvals` | `approval/approval-pending-list-director.png` | Yes | Pending | Playwright | Director has `LeaveRequest.ViewPendingApproval` |
| 28 | director | Approval | Approval Detail | `/leave/:id` | `approval/approval-detail-director.png` | Yes | Pending | Seed Required | Requires leave id assigned to director |
| 29 | director | Approval | Final Approval | `/leave/:id` | `approval/final-approval-director.png` | Yes | Pending | Seed Required | Requires final-step test leave |
| 30 | director | Approval | Reject Leave | `/leave/:id` | `approval/reject-leave-director.png` | Yes | Pending | Seed Required | Requires pending director approval |
| 31 | director | Approval | Approval History | `/leave/:id` | `approval/approval-history-director.png` | Yes | Pending | Seed Required | Approval timeline/history |
| 32 | director | Executive | Executive Dashboard/Leave Report | `/reports/leaves` | `director/executive-dashboard.png` | Yes | Blocked | Seed Required | Seed does not grant `ReportManagement.View` to Director |
| 33 | hr | Dashboard | Dashboard | `/dashboard` | `dashboard/dashboard-hr.png` | Yes | Pending | Playwright | LeaveAdmin has `Dashboard.View` |
| 34 | hr | Leave Management | Leave List Department | `/leave` | `hr/leave-list-hr.png` | Yes | Pending | Playwright | LeaveAdmin has `LeaveRequest.ViewDepartment` |
| 35 | hr | HR | Leave Report | `/reports/leaves` | `hr/leave-report.png` | Yes | Blocked | Seed Required | LeaveAdmin seed lacks `ReportManagement.View` |
| 36 | hr | HR | Leave Balance Management | `/admin/leave-balances` | `hr/leave-balance.png` | Yes | Pending | Playwright | LeaveAdmin has `LeaveAdmin.ManageBalances` |
| 37 | hr | HR | Holiday Management | `/admin/leave-holidays` | `hr/holiday-list.png` | Yes | Pending | Playwright | LeaveAdmin has `LeaveAdmin.ManageHolidays` |
| 38 | hr | HR | Employee Leave History | `/leave` | `hr/employee-leave-history.png` | Yes | Pending | Playwright | Use filters in leave list |
| 39 | hr | HR | Export Report | `/reports/leaves` | `hr/export-report.png` | Yes | Blocked | Seed Required | LeaveAdmin seed lacks `ReportManagement.Export` |
| 40 | hr | HR | Leave Detail | `/leave/:id` | `hr/leave-detail-hr.png` | Yes | Pending | Seed Required | Requires department test leave id |
| 41 | hr | HR | Attachment Viewer | `/leave/:id` | `hr/attachment-viewer.png` | Yes | Pending | Seed Required | Requires test attachment |
| 42 | hr | Leave Management | Leave Type Management | `/leave/types` | `hr/leave-types.png` | Yes | Pending | Playwright | LeaveAdmin has `LeaveAdmin.ManageTypes` |
| 43 | hr | Leave Management | Approval Chain | `/admin/approval-chains` | `hr/approval-chain.png` | Yes | Pending | Playwright | LeaveAdmin has `LeaveAdmin.ManageApprovalChains` |
| 44 | superadmin | Dashboard | Dashboard | `/dashboard` | `dashboard/dashboard-superadmin.png` | Yes | Pending | Playwright | SuperAdmin has all seeded permissions except `LeaveRequest.Create` |
| 45 | superadmin | User Management | User List | `/admin/users` | `user-management/user-list.png` | Yes | Pending | Playwright | Requires `UserManagement.View` |
| 46 | superadmin | User Management | Create User | `/admin/users/create` | `user-management/create-user.png` | Yes | Pending | Playwright | Requires `UserManagement.Create`; use test data only |
| 47 | superadmin | User Management | Edit User | `/admin/users/:id/edit` | `user-management/edit-user.png` | Yes | Pending | Seed Required | Requires safe test user id |
| 48 | superadmin | User Management | Disable User | `/admin/users` | `user-management/disable-user.png` | Yes | Pending | Seed Required | Use test user only |
| 49 | superadmin | User Management | Department List | `/admin/departments` | `user-management/department-list.png` | Yes | Pending | Playwright | Requires `DepartmentManagement.View` |
| 50 | superadmin | User Management | Department Detail | `/admin/departments/:id/edit` | `user-management/department-detail.png` | Yes | Pending | Seed Required | Requires safe test department id |
| 51 | superadmin | User Management | Role List | `/admin/roles` | `user-management/role-list.png` | Yes | Pending | Playwright | Requires `RoleManagement.View` |
| 52 | superadmin | User Management | Role Detail | `/admin/roles/:id/permissions` | `user-management/role-detail.png` | Yes | Pending | Seed Required | Requires role id |
| 53 | superadmin | User Management | Permission List | `/admin/roles/:id/permissions` | `user-management/permission-list.png` | Yes | Pending | Seed Required | Permission matrix on role detail |
| 54 | superadmin | User Management | Permission Detail | `/admin/roles/:id/permissions` | `user-management/permission-detail.png` | No | Pending | Seed Required | Capture if UI exposes detail panel |
| 55 | superadmin | Leave Management | Approval Chain | `/admin/approval-chains` | `superadmin/approval-chain.png` | Yes | Pending | Playwright | Requires `LeaveAdmin.ManageApprovalChains` |
| 56 | superadmin | Leave Management | Holiday List | `/admin/leave-holidays` | `superadmin/holiday-list.png` | Yes | Pending | Playwright | Requires `LeaveAdmin.ManageHolidays` |
| 57 | superadmin | SuperAdmin | Audit Log | `/admin/audit-logs` | `superadmin/audit-log.png` | Yes | Pending | Playwright | Requires `SystemSettings.View` |
| 58 | superadmin | SuperAdmin | Activity Log | `/admin/audit-logs` | `superadmin/activity-log.png` | Yes | Pending | Manual | Same route unless separate activity view exists |
| 59 | superadmin | SuperAdmin | System Setting | `/admin/system-settings` | `superadmin/system-setting.png` | Yes | Pending | Playwright | Requires `SystemSettings.View` |
| 60 | superadmin | SuperAdmin | Notification Setting/LINE Settings | `/admin/line-settings` | `superadmin/notification-setting.png` | Yes | Pending | Playwright | Menu uses LINE settings as notification configuration |
| 61 | superadmin | SuperAdmin | LINE Users | `/admin/line-users` | `superadmin/line-users.png` | No | Pending | Playwright | Requires `System.Line.TestSend` or `SystemSettings.View` |
| 62 | superadmin | Common | Profile | `/profile` | `common/profile-superadmin.png` | Yes | Pending | Playwright | Protected route, no specific permission guard |
| 63 | superadmin | Leave Management | Create Leave Denied | `/leave/create` | `superadmin/leave-create-denied.png` | Yes | Pending | Playwright | Frontend role guard denies Admin/SuperAdmin |

## Checklist

## user

- [ ] Login
- [ ] Dashboard
- [ ] My Profile
- [ ] Change Password/Profile Edit
- [ ] Leave List
- [ ] Create Leave
- [ ] Leave Detail Draft/Pending/Approved/Rejected/Cancelled
- [ ] Upload Attachment
- [ ] Leave History
- [ ] Leave PDF
- [ ] Leave Calendar
- [ ] Leave Balance
- [ ] Notification

## head

- [ ] Dashboard
- [ ] Pending Approval
- [ ] Approval Detail
- [ ] Approve
- [ ] Reject
- [ ] Approval History
- [ ] Team Leave Calendar
- [ ] Leave List Department

## director

- [ ] Dashboard
- [ ] Pending Approval
- [ ] Approval Detail
- [ ] Final Approval
- [ ] Reject Leave
- [ ] Approval History
- [ ] Executive Dashboard/Report permission review

## hr

- [ ] Dashboard
- [ ] Leave List Department
- [ ] Leave Report permission review
- [ ] Leave Balance Management
- [ ] Holiday Management
- [ ] Employee Leave History
- [ ] Export Report permission review
- [ ] Leave Detail
- [ ] Attachment Viewer
- [ ] Leave Type Management
- [ ] Approval Chain

## superadmin

- [ ] Dashboard
- [ ] User List
- [ ] Create User
- [ ] Edit User
- [ ] Disable User
- [ ] Department List/Detail
- [ ] Role List/Detail
- [ ] Permission List/Detail
- [ ] Approval Chain
- [ ] Holiday List
- [ ] Audit Log
- [ ] Activity Log
- [ ] System Setting
- [ ] Notification Setting
- [ ] Profile
- [ ] Leave Create Denied

