# HOP Phase 1 Screenshot and Access Findings

วันที่ตรวจสอบ: 2 กรกฎาคม 2569

## Scope

ตรวจสอบแบบ read-only จาก frontend routes/menu/guards, backend controller authorization, permission constants และ development seed โดยไม่แก้ไข business logic, database schema, role หรือ permission จริง

## Summary

| รายการ | จำนวน/สถานะ |
|---|---:|
| Frontend routes found | 37 |
| Seeded permission groups | 12 |
| Generated group/action permissions | 84 |
| Granular leave/system permissions | 19 |
| Total seeded permissions inferred | 103 |
| Screenshot catalog rows | 63 |
| Pending screenshots | 60 |
| Captured screenshots | 0 |
| Blocked screenshots | 3 |
| Seed Required screenshots | 27 |

## Roles Found

| Requested Role | Actual Role Found | Evidence | Notes |
|---|---|---|---|
| user | Staff | `backend/Hop.Api/Data/DevelopmentDataSeeder.cs:38-46` | Normalized to lowercase `user` in catalog |
| head | DepartmentHead | `backend/Hop.Api/Data/DevelopmentDataSeeder.cs:38-46` | Normalized to `head` |
| director | Director | `backend/Hop.Api/Data/DevelopmentDataSeeder.cs:38-46` | Normalized to `director` |
| hr | LeaveAdmin | `backend/Hop.Api/Data/DevelopmentDataSeeder.cs:38-46` | No dedicated `HR` role name found in seed; mapped to `LeaveAdmin` |
| superadmin | SuperAdmin | `backend/Hop.Api/Data/DevelopmentDataSeeder.cs:38-46` | Normalized to `superadmin` |
| admin | Admin | `backend/Hop.Api/Data/DevelopmentDataSeeder.cs:38-46` | Exists in system but not part of requested role list |

## Permissions Found

Permission groups generated from seed:

- Dashboard
- UserManagement
- DepartmentManagement
- RoleManagement
- LeaveManagement
- ApprovalChain
- ApprovalDelegation
- LeaveBalance
- LeaveHoliday
- LeaveAttachment
- ReportManagement
- SystemSettings

Generated actions:

- View
- Create
- Edit
- Delete
- Approve
- Export
- Manage

Granular permissions found in `DevelopmentDataSeeder.cs:82-103` and `LeavePermissions.cs:3-25`:

- `LeaveRequest.ViewOwn`
- `LeaveRequest.ViewPendingApproval`
- `LeaveRequest.ViewDepartment`
- `LeaveRequest.ViewAll`
- `LeaveRequest.Create`
- `LeaveRequest.EditOwn`
- `LeaveRequest.CancelOwn`
- `LeaveApproval.ApproveCurrentStep`
- `LeaveApproval.Delegate`
- `LeaveApproval.Override`
- `LeaveApprovalDelegation.Manage`
- `LeaveApprovalEscalation.Manage`
- `LeaveSupport.ViewAll`
- `LeaveAdmin.ManageTypes`
- `LeaveAdmin.ManageBalances`
- `LeaveBalance.Rollover`
- `LeaveAdmin.ManageHolidays`
- `LeaveAdmin.ManageApprovalChains`
- `System.Line.TestSend`

## Routes Found

Frontend routes are declared in `frontend/src/routes/AppRoutes.tsx`:

- `/login`
- `/`
- `/unauthorized`
- `/dashboard`
- `/notifications`
- `/profile`
- `/admin/users`
- `/admin/users/create`
- `/admin/users/:id/edit`
- `/admin/departments`
- `/admin/departments/create`
- `/admin/departments/:id/edit`
- `/admin/roles`
- `/admin/roles/:id/permissions`
- `/admin/audit-logs`
- `/admin/audit-logs/export`
- `/admin/system-settings`
- `/admin/line-settings`
- `/admin/line-users`
- `/admin/leave-support`
- `/admin/approval-chains`
- `/admin/approval-chains/create`
- `/admin/approval-chains/:id/edit`
- `/admin/approval-delegations`
- `/admin/leave-balances`
- `/admin/leave-balances/adjustments`
- `/admin/leave-holidays`
- `/leave`
- `/leave/create`
- `/leave/pending-approvals`
- `/leave/calendar`
- `/line/leave-approval/:id`
- `/leave/types`
- `/leave/balances`
- `/leave/:id`
- `/reports/leaves`
- `*`

## Pages Without Specific Permission Guard

| Route/Page | Guard Level | Evidence | Review Note |
|---|---|---|---|
| `/login` | Public | `AppRoutes.tsx:73` | Expected login entry point |
| `/unauthorized` | Authenticated only | `AppRoutes.tsx:76` | Expected error page |
| `/profile` | Authenticated only | `AppRoutes.tsx:79`; `MeProfileController.cs:15` | No explicit permission; acceptable if endpoint only returns current user data |
| `/` | Authenticated redirect | `AppRoutes.tsx:75` | Redirects to dashboard |
| `*` | Authenticated redirect | `AppRoutes.tsx:117` | Redirects to dashboard |

## APIs Without Permission Guard or Public APIs

| API Area | Authorization | Evidence | Risk/Note |
|---|---|---|---|
| Auth login/refresh | AllowAnonymous | `AuthController.cs:17-18`, `AuthController.cs:66-67` | Expected; ensure rate limiting/account lockout elsewhere |
| Auth logout/me | Authorize only | `AuthController.cs:136-164` | Expected authenticated self-session operations |
| Health | Public/no attribute in controller scan | `HealthController.cs:10` | Usually acceptable for health check; review exposure in production |
| LINE webhook | AllowAnonymous | `LineWebhookController.cs:14,20` | Expected webhook; signature validation should be verified separately |
| User profile image | AllowAnonymous | `UsersController.cs:17-18` | Public profile image route; verify no private data leakage |
| MeProfile | Authorize only | `MeProfileController.cs:15,39,51,114,179,223,235,254,266` | Current-user scope; no role permission |
| MeLine | Authorize only | `MeLineController.cs:11,14,26,48` | Current-user scope; no role permission |
| Notifications | Authorize only | `NotificationsController.cs:14-17,38,77,90,120` | Current-user notification scope; no role permission |

## Menu/Route Mismatches

| Finding | Evidence | Impact |
|---|---|---|
| Director role description says executive/reporting access, but seed does not grant `ReportManagement.View` | `DevelopmentDataSeeder.cs:43`, `DevelopmentDataSeeder.cs:371-378`, `menuConfig.ts:59`, `AppRoutes.tsx:116` | `/reports/leaves` hidden/denied for Director; Executive Dashboard screenshot blocked until seed/role decision |
| HR manual expects reports, but `LeaveAdmin` seed does not grant `ReportManagement.View` or `ReportManagement.Export` | `DevelopmentDataSeeder.cs:379-386`, `menuConfig.ts:59`, `AppRoutes.tsx:116` | HR Leave Report and Export Report screenshots are blocked/seed required |
| HR manual mentions support/help workflows, but `LeaveAdmin` seed lacks `LeaveSupport.ViewAll` | `DevelopmentDataSeeder.cs:379-386`, `menuConfig.ts:58`, `AppRoutes.tsx:98` | `/admin/leave-support` hidden/denied for LeaveAdmin |
| SuperAdmin route can access pending approvals by permission, but menu hides it for Admin/SuperAdmin | `menuConfig.ts:49`, `DevelopmentDataSeeder.cs:349-355` | Hidden menu but direct route likely allowed; document as intentional or adjust if unwanted |
| SuperAdmin has most permissions but `LeaveRequest.Create` is revoked and frontend role guard denies create route | `AppRoutes.tsx:54-62`, `DevelopmentDataSeeder.cs:349-355` | Correctly prevents admin-style accounts from creating personal leave requests |

## Screenshot Status

- Captured: 0
- Pending: 60
- Blocked: 3
- Seed Required: 27

Blocked catalog items:

- Director Executive Dashboard/Leave Report: seed lacks `ReportManagement.View`
- HR Leave Report: seed lacks `ReportManagement.View`
- HR Export Report: seed lacks `ReportManagement.Export`

Seed Required examples:

- leave detail states: draft, pending, approved, rejected, cancelled
- approval detail/action screenshots
- edit user/department/role permission screenshots
- attachment viewer and leave PDF screenshots

## Action Items

| Priority | Action | Owner |
|---|---|---|
| High | Confirm whether `Director` should receive `ReportManagement.View` for executive dashboard/report screenshots | Product/Authorization owner |
| High | Confirm whether `LeaveAdmin`/HR should receive `ReportManagement.View` and `ReportManagement.Export` | Product/Authorization owner |
| Medium | Confirm whether `LeaveAdmin` should receive `LeaveSupport.ViewAll` | Product/Authorization owner |
| Medium | Decide whether SuperAdmin pending approval direct route should remain allowed while menu is hidden | Security/Product owner |
| Medium | Create safe HR test account with role `LeaveAdmin` for screenshot automation | QA/DevOps |
| Medium | Create test leave records for each status and approval step | QA/DevOps |
| Low | Review public profile image endpoint for data leakage expectations | Security reviewer |
| Low | Review webhook endpoint signature validation separately | Security reviewer |

## Automation Readiness

Playwright screenshot automation is prepared as a template. It will not commit real credentials and expects local `tests/screenshots/config/screenshot-users.json`, which is ignored by git.

Recommended command after config and servers are ready:

```powershell
npm run docs:screenshot
```


