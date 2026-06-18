# Permission Matrix

Phase 1 Production Deploy exposes only User Management and Leave Management capabilities.

## Phase 1 Permission Groups

- `Dashboard`
- `UserManagement`
- `DepartmentManagement`
- `RoleManagement`
- `LeaveManagement`
- `ApprovalChain`
- `ApprovalDelegation`
- `LeaveBalance`
- `LeaveHoliday`
- `LeaveAttachment`
- `ReportManagement`
- `SystemSettings`

`ReportManagement` is used only for Leave reports in Phase 1.

## Hidden Future Permission Groups

These groups must not be granted or exposed in Phase 1:

- `RepairManagement`
- `BorrowManagement`
- `InventoryManagement`

The Phase 1 seeder removes those permission groups if they were created by an earlier local seed.

## Permission Actions

- `View`
- `Create`
- `Edit`
- `Delete`
- `Approve`
- `Export`
- `Manage`

## Permission Code Format

```text
<Group>.<Action>
```

Examples:

```text
UserManagement.View
UserManagement.Create
LeaveManagement.Approve
LeaveAttachment.Download
ReportManagement.Export
```

## Recommended Phase 1 Roles

| Role | Purpose | Suggested Permissions |
| --- | --- | --- |
| SuperAdmin | Bootstrap and emergency administration | All Phase 1 permissions |
| Admin / HR | Manage users, departments, roles, leave setup, balances, audit review | `Dashboard.*`, `UserManagement.*`, `DepartmentManagement.*`, `RoleManagement.*`, `LeaveManagement.*`, `ApprovalChain.*`, `LeaveBalance.*`, `LeaveHoliday.*`, `LeaveAttachment.*`, `ReportManagement.*`, `SystemSettings.View`, `SystemSettings.Export` |
| DepartmentHead | Approve leave and view department leave data | `Dashboard.View`, `LeaveManagement.View`, `LeaveManagement.Approve`, `LeaveAttachment.Download`, `ReportManagement.View` |
| Staff | Create and track own leave requests | `Dashboard.View`, `LeaveManagement.View`, `LeaveManagement.Create`, `LeaveManagement.Edit`, `LeaveAttachment.Download` |

## Current Enforcement

Backend uses:

```text
[RequirePermission("<Group>.<Action>")]
```

Frontend uses `PermissionProvider`, `PermissionGuard`, and route-level permission checks.

Users without the required backend permission receive HTTP `403`, and denied attempts are recorded in `audit_logs`.

## Phase 1 Route Exposure

Allowed frontend routes:

- `/dashboard`
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
- `/admin/approval-chains`
- `/admin/approval-chains/create`
- `/admin/approval-chains/:id/edit`
- `/admin/approval-delegations`
- `/admin/leave-balances/adjustments`
- `/admin/leave-holidays`
- `/leave`
- `/leave/create`
- `/leave/calendar`
- `/leave/types`
- `/leave/balances`
- `/leave/:id`
- `/reports/leaves`

Hidden frontend routes:

- `/borrowing`
- `/repairs`
- `/vehicles`
- `/meeting-rooms`
- `/materials`
- `/inventory`
- `/reports`
- `/admin/settings`
- `/admin/sessions`
- `/administration`

See `docs/PERMISSION-POLICY.md` for policy implementation details.
