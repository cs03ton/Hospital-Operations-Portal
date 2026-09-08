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
- Leave calendar APIs: `LeaveManagement.View`
- Approval delegation APIs: `ApprovalDelegation.View`, `ApprovalDelegation.Create`, `ApprovalDelegation.Edit`, `ApprovalDelegation.Delete`, `ApprovalDelegation.Manage`
- Leave report APIs: `ReportManagement.View`, `ReportManagement.Export`
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
# Fleet Phase 2.0

Fleet permissions follow the existing PascalCase convention and are documented in `docs/business-requirements/vehicle-booking/HOP-Vehicle-Permission-Matrix.md`. Migrations/seeding must not bind Fleet capabilities to job-title role names; administrators assign them through the existing role-permission management flow.

Fleet rollout was approved after Milestone 4.2 UAT on 2026-08-04. `Staff` and `DepartmentHead` receive requester permissions, `Director` additionally receives Fleet director-approval permissions, and `Admin`/`SuperAdmin` receive Fleet administration permissions. Dispatcher and Driver duties remain explicit assignments through Role Management so operational authority is not inferred from unrelated job roles.
