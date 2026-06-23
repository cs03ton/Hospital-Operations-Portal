# Permission Matrix

Phase 1 Production Deploy exposes only User Management and Leave Management capabilities.

## Phase 1 Permission Groups

- `Dashboard`
- `UserManagement`
- `DepartmentManagement`
- `RoleManagement`
- `LeaveManagement`
- `LeaveRequest`
- `LeaveApproval`
- `LeaveApprovalDelegation`
- `LeaveApprovalEscalation`
- `LeaveSupport`
- `LeaveAdmin`
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
LeaveApproval.ApproveCurrentStep
LeaveAttachment.Download
ReportManagement.Export
```

Granular leave permission examples:

```text
LeaveRequest.ViewOwn
LeaveRequest.ViewPendingApproval
LeaveRequest.ViewDepartment
LeaveRequest.ViewAll
LeaveApproval.Override
LeaveSupport.ViewAll
LeaveAdmin.ManageApprovalChains
```

`LeaveAdmin.ManageApprovalChains` ยังคงเป็น permission code เดิมเพื่อ backward compatibility แต่ UI จะแสดงเป็น `จัดการกฎการอนุมัติวันลา`

## Self Profile

ข้อมูลส่วนตัวของผู้ใช้งานใช้ authenticated endpoint:

```text
GET /api/me/profile
PUT /api/me/profile
```

ไม่ต้องมี permission code เพิ่ม เพราะ backend ใช้ `currentUserId` จาก access token และอนุญาตให้แก้เฉพาะ profile ของตนเองเท่านั้น

ผู้ใช้แก้ได้เฉพาะ:

- ชื่อ-นามสกุล
- ตำแหน่ง
- เบอร์โทรศัพท์
- ที่อยู่ระหว่างลา / ที่อยู่ติดต่อ
- รูปโปรไฟล์ URL

ผู้ใช้แก้เองไม่ได้:

- Username
- Role
- LINE User ID
- Department
- Approval Rule
- สถานะบัญชี
- Permission

การเปลี่ยนแปลงจะบันทึก audit event `UserProfile.Updated`

## Approval Rule Visibility

Approval Rule ใช้ model เดิม `approval_chains` แต่ผูกกับผู้ใช้งานผ่าน `users.leave_approval_rule_id`

- Admin/SuperAdmin/LeaveAdmin ที่มี `LeaveAdmin.ManageApprovalChains` จัดการ rule และ preview ได้
- ผู้ขอลาไม่สามารถเลือก rule เองตอน submit
- Approval queue ยังคงแสดงเฉพาะรายการที่ `current_approver_id = currentUserId`

## Recommended Phase 1 Roles

| Role | Purpose | Suggested Permissions |
| --- | --- | --- |
| SuperAdmin | Bootstrap and emergency administration | All Phase 1 permissions, including `LeaveSupport.ViewAll` and `LeaveApproval.Override` |
| Admin / HR | Manage users, departments, roles, leave setup, balances, audit review | `Dashboard.View`, `LeaveRequest.ViewDepartment`, `LeaveApproval.Delegate`, `LeaveApprovalDelegation.Manage`, `LeaveApprovalEscalation.Manage`, `LeaveSupport.ViewAll`, `LeaveAdmin.*`, `ReportManagement.*`, `SystemSettings.*` |
| LeaveAdmin | Manage leave setup and department leave data | `LeaveRequest.ViewDepartment`, `LeaveAdmin.ManageTypes`, `LeaveAdmin.ManageBalances`, `LeaveAdmin.ManageHolidays`, `LeaveAdmin.ManageApprovalChains` |
| Director | Executive approval | `Dashboard.View`, `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveApproval.ApproveCurrentStep` |
| DepartmentHead | Approve current assigned leave step | `Dashboard.View`, `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveApproval.ApproveCurrentStep` |
| Staff | Create and track own leave requests | `Dashboard.View`, `LeaveRequest.ViewOwn`, `LeaveRequest.Create`, `LeaveRequest.EditOwn`, `LeaveRequest.CancelOwn` |

## Current Enforcement

Backend uses:

```text
[RequirePermission("<PermissionCode>")]
[RequireAnyPermission("<PermissionCodeA>", "<PermissionCodeB>")]
```

Frontend uses `PermissionProvider`, `PermissionGuard`, and route-level permission checks.

Users without the required backend permission receive HTTP `403`, and denied attempts are recorded in `audit_logs`.

## Leave Request Visibility Rule

| Role | Visibility |
| --- | --- |
| Staff | เห็นเฉพาะคำขอลาของตัวเอง |
| DepartmentHead | เห็นคำขอของตัวเอง และคำขอของ Staff ในหน่วยงานเดียวกัน |
| Director | เห็นคำขอของทุกคน |
| Admin | เห็นคำขอของทุกคน |
| SuperAdmin | เห็นคำขอของทุกคน |

Backend enforces this through `LeaveRequestAccessService` for leave list, detail, PDF, attachment list, and attachment download.

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
