# Permission Matrix

Phase 1.1 creates the permission matrix foundation.

## Permission Groups

- Dashboard
- UserManagement
- DepartmentManagement
- RoleManagement
- LeaveManagement
- ApprovalChain
- LeaveBalance
- LeaveHoliday
- LeaveAttachment
- RepairManagement
- BorrowManagement
- InventoryManagement
- ReportManagement
- SystemSettings

## Permission Actions

- View
- Create
- Edit
- Delete
- Approve
- Export
- Manage

## Permission Code Format

```text
<Group>.<Action>
```

Examples:

```text
UserManagement.View
UserManagement.Create
UserManagement.Edit
UserManagement.Delete
```

## Role Permission Assignment

Role permissions are stored in:

```text
role_permissions
```

Admin can edit role permissions from:

```text
/admin/roles/{id}/permissions
```

## Current Enforcement

Phase 1.2 enforces permissions on backend APIs and frontend routes.

Backend uses:

```text
[RequirePermission("<Group>.<Action>")]
```

Frontend uses `PermissionProvider`, `PermissionGuard`, and `usePermission()`.

Users without the required permission receive HTTP `403`, and denied attempts are recorded in `audit_logs`.

See `docs/PERMISSION-POLICY.md`.

## Phase 2.1 Permissions

- `ApprovalChain.View`
- `ApprovalChain.Create`
- `ApprovalChain.Edit`
- `ApprovalChain.Delete`
- `LeaveBalance.Adjust`
- `LeaveHoliday.Manage`
- `LeaveAttachment.Download`
