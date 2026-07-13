# Role Dashboard

Dashboard layout depends on the active role.

## Documentation Center

Phase 1.5 adds Documentation Center routes:

- `/docs`
- `/docs/:slug`

Visibility is role-aware:

- Staff: user guide and FAQ
- Department Head: user guide, head approval guide, and FAQ
- Director: director/approver guide and FAQ
- Admin/SuperAdmin: all documents, including admin guide and release notes

Permissions:

- `Documentation.View`
- `Documentation.AdminView`
- `Documentation.Manage`

## Staff

Shows:

- Welcome
- My Leave Balance
- My Leave Requests
- My Pending Requests
- Recent Notifications
- My Leave Calendar

## Department Head

Shows:

- Welcome
- My Leave Balance
- My Leave Requests
- My Pending Leave Requests
- Department Leave Requests
- My Leave Calendar
- Team Leave Today
- Pending Approvals
- Team Calendar
- Team Leave Statistics
- Employees Near Leave Limit

Department Head dashboard is ordered from personal work to team work:

1. `คำขอลาของฉันที่รออนุมัติ`
   - Shows only the head's own leave requests with `Pending` status.
   - Excludes `Approved`, `Rejected`, `Cancelled`, and `ReturnedForRevision`.
   - CTA opens `/leave?scope=mine&status=pending`.
2. `คำขอลาของหน่วยงาน`
   - Shows leave requests from users in the same department.
   - Excludes the current head's own requests to avoid duplicate counting.
   - Includes `Pending`, `ReturnedForRevision`, `Approved`, `Rejected`, and `Cancelled`.
   - CTA opens `/leave?scope=department`.

## Director

Shows:

- Welcome
- Hospital Leave Summary
- Department Comparison
- Monthly Leave Statistics
- Approval Queue
- Executive Calendar
- Leave Trend Chart
- Executive Dashboard at `/dashboard/executive` when granted `Dashboard.Executive.View` or `LeaveDashboard.ViewExecutiveSummary`

## Executive Dashboard

Phase 1.5 adds a dedicated executive view for Director, Admin, SuperAdmin, or users with explicit permission.

Shows:

- KPI cards: total active users, present today, on leave today, pending approvals, approved today, rejected today, leave rate, approval SLA
- Executive Summary Today
- Monthly Leave Trend for sick leave, personal leave, and vacation leave
- Leave By Department top 10
- Leave By Type for core leave types
- Fiscal-year Yearly Summary
- System Health summary without secrets

## Admin

Admin uses a dedicated Control Center at `/admin/dashboard` and keeps CRUD in management pages such as `/admin/users`.

Shows:

- Welcome
- User Summary
- Department Summary
- Leave Type Summary
- Approval Rules
- LINE Summary
- Health Summary
- Warning / To-do
- Quick Actions
- Pending Approval overview (not personal approval queue)
- Notification Queue
- Audit Log
- Holiday Management
- System Health
- Background Jobs
- Storage Usage
- Backup Status
- Version Information

## SuperAdmin

Shows Admin widgets plus:

- Security Events
- Failed Login
- Permission Denied
- LINE Delivery Status
- Database Status
- API Health
- Queue Monitoring

## Notes

Admin and SuperAdmin do not use the personal leave balance menu. They can still see admin/support widgets according to permissions.
Admin and SuperAdmin do not show the "งานรออนุมัติของฉัน" widget because they are not normal approval queue users by default.
Staff and Department Head do not see Executive Dashboard unless the permission is explicitly assigned.

## Admin Dashboard vs User Management

| Page | Purpose |
|---|---|
| `/admin/dashboard` | ภาพรวมระบบ, warning, quick actions, health, LINE, audit summary |
| `/admin/users` | ตารางข้อมูลผู้ใช้และ CRUD |
| `/admin/departments` | ตารางหน่วยงานและ CRUD |
| `/admin/roles` | ตารางบทบาทและจัดการ permission |

Admin Dashboard requires `AdminDashboard.View` or Admin/SuperAdmin role fallback.
