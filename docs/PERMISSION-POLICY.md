# Permission Policy

Phase 1.2 enforces permissions on protected backend APIs and frontend routes.

## Backend Standard

Use permission attributes on controllers or actions:

```csharp
[RequirePermission("UserManagement.View")]
[RequirePermission("RoleManagement.Manage")]
```

Policy names are generated dynamically with the prefix:

```text
Permission:<PermissionCode>
```

## Permission Groups

- Dashboard
- UserManagement
- DepartmentManagement
- RoleManagement
- LeaveManagement
- RepairManagement
- BorrowManagement
- InventoryManagement
- ReportManagement
- SystemSettings

## Actions

- View
- Create
- Edit
- Delete
- Approve
- Export
- Manage

## Enforced APIs

- Dashboard: `Dashboard.View`
- User APIs: `UserManagement.View`, `UserManagement.Create`, `UserManagement.Edit`, `UserManagement.Delete`
- Department APIs: `DepartmentManagement.View`, `DepartmentManagement.Create`, `DepartmentManagement.Edit`, `DepartmentManagement.Delete`
- Role APIs: `RoleManagement.View`, `RoleManagement.Create`, `RoleManagement.Edit`, `RoleManagement.Delete`, `RoleManagement.Manage`
- Permission APIs: `RoleManagement.View`
- Audit Log APIs: `SystemSettings.View`
- Leave Type APIs: `LeaveManagement.View`, `LeaveManagement.Manage`
- Leave Request APIs: `LeaveManagement.View`, `LeaveManagement.Create`, `LeaveManagement.Edit`, `LeaveManagement.Approve`
- Leave Balance user lookup: `LeaveManagement.Manage`
- Approval chain APIs: `ApprovalChain.View`, `ApprovalChain.Create`, `ApprovalChain.Edit`, `ApprovalChain.Delete`
- Leave balance adjustment APIs: `LeaveBalance.Adjust`
- Leave holiday APIs: `LeaveHoliday.Manage`
- Leave attachment download: `LeaveAttachment.Download`
- Audit export: `SystemSettings.Export`
- Audit retention and session management: `SystemSettings.Manage`

## Denied Access

Users without the required permission receive HTTP `403`.

Denied access attempts are written to `audit_logs` with:

- `action = Authorization.Denied`
- `entity_name = Authorization`
- `result = Denied`
- request path in `detail`

## Frontend Standard

Frontend uses:

- `PermissionProvider`
- `PermissionGuard`
- `usePermission()`

Routes, menus, and sensitive action buttons must be guarded by permission code.
