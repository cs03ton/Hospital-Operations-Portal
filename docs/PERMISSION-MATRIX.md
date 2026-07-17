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
- `AdminDashboard`
- `System`
- `SystemSettings`

`ReportManagement` is used only for Leave reports in Phase 1.
`AdminDashboard` is used for the admin control center at `/admin/dashboard`.
`System` is used for operational support permissions such as `System.Health.View` and `System.Line.TestSend`.

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
AdminDashboard.View
System.Health.View
```

`LeaveAdmin.ManageApprovalChains` ยังคงเป็น permission code เดิมเพื่อ backward compatibility แต่ UI จะแสดงเป็น `จัดการกฎการอนุมัติวันลา`

Granular leave cancellation permission examples:

```text
LeaveCancellation.ViewOwn
LeaveCancellation.Create
LeaveCancellation.Submit
LeaveCancellation.CancelOwn
LeaveCancellation.ApproveCurrentStep
LeaveCancellation.ViewDepartment
LeaveCancellation.ViewAll
LeaveCancellation.Manage
```

`LeaveCancellation.*` ใช้กับเอกสารคำขอยกเลิกใบลา ใบลาเดิมจะถูกเปลี่ยนสถานะเป็น `CancelledAfterApproval` หลัง final approval เท่านั้น

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

> **Phase 1 HR Mapping:** ระบบยังไม่ใช้ role ชื่อ `HR` แยกต่างหากใน seed/runtime ปัจจุบัน งาน HR ด้านระบบลาให้ใช้ role `LeaveAdmin` เป็นหลัก และใช้ `Admin` เฉพาะกรณีผู้ดูแลระบบที่ต้องช่วยงาน HR/Support เพิ่มเติม เพื่อให้สิทธิ์ตรงกับ implementation และลดการสร้าง role ซ้ำซ้อนก่อน pilot

| Role | Purpose | Suggested Permissions |
| --- | --- | --- |
| SuperAdmin | Bootstrap and emergency administration | All Phase 1 permissions, including `LeaveSupport.ViewAll` and `LeaveApproval.Override` |
| Admin | System administration and HR support when explicitly assigned | `Dashboard.View`, `AdminDashboard.View`, `LeaveRequest.ViewDepartment`, `LeaveApproval.Delegate`, `LeaveApprovalDelegation.Manage`, `LeaveApprovalEscalation.Manage`, `LeaveSupport.ViewAll`, `LeaveAdmin.*`, `ReportManagement.*`, `System.Health.View`, `System.Line.TestSend`, `SystemSettings.*` |
| LeaveAdmin | HR operator for leave setup and department leave data | `LeaveRequest.ViewDepartment`, `LeaveAdmin.ManageTypes`, `LeaveAdmin.ManageBalances`, `LeaveAdmin.ManageHolidays`, `LeaveAdmin.ManageApprovalChains` |
| Director | Executive approval | `Dashboard.View`, `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveApproval.ApproveCurrentStep` |
| DepartmentHead | Approve current assigned leave step | `Dashboard.View`, `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveApproval.ApproveCurrentStep` |
| Staff | Create and track own leave requests | `Dashboard.View`, `LeaveRequest.ViewOwn`, `LeaveRequest.Create`, `LeaveRequest.EditOwn`, `LeaveRequest.CancelOwn` |

## Leave Cancellation Role Mapping

| Role | Suggested Leave Cancellation Permissions |
| --- | --- |
| Staff | `LeaveCancellation.ViewOwn`, `LeaveCancellation.Create`, `LeaveCancellation.Submit`, `LeaveCancellation.CancelOwn` |
| DepartmentHead | `LeaveCancellation.ViewOwn`, `LeaveCancellation.Create`, `LeaveCancellation.Submit`, `LeaveCancellation.CancelOwn`, `LeaveCancellation.ApproveCurrentStep`, `LeaveCancellation.ViewDepartment` |
| Director | `LeaveCancellation.ViewOwn`, `LeaveCancellation.Create`, `LeaveCancellation.Submit`, `LeaveCancellation.CancelOwn`, `LeaveCancellation.ApproveCurrentStep` |
| LeaveAdmin | `LeaveCancellation.ViewDepartment`, `LeaveCancellation.Manage` |
| Admin | `LeaveCancellation.ViewDepartment`, `LeaveCancellation.ViewAll`, `LeaveCancellation.Manage` |
| SuperAdmin | `LeaveCancellation.ViewAll`, `LeaveCancellation.Manage`, `LeaveCancellation.ApproveCurrentStep` เฉพาะกรณีได้รับมอบหมายเป็น current approver |

Admin/SuperAdmin ไม่ควรมี `LeaveCancellation.Create` โดย default เพื่อป้องกันการสร้างคำขอยกเลิกใบลาแทนผู้ใช้งานแบบไม่ชัดเจน

## Leave Cancellation Role Mapping

| Role | Suggested Leave Cancellation Permissions |
| --- | --- |
| Staff | `LeaveCancellation.ViewOwn`, `LeaveCancellation.Create`, `LeaveCancellation.Submit`, `LeaveCancellation.CancelOwn` |
| DepartmentHead | `LeaveCancellation.ViewOwn`, `LeaveCancellation.Create`, `LeaveCancellation.Submit`, `LeaveCancellation.CancelOwn`, `LeaveCancellation.ApproveCurrentStep`, `LeaveCancellation.ViewDepartment` |
| Director | `LeaveCancellation.ViewOwn`, `LeaveCancellation.Create`, `LeaveCancellation.Submit`, `LeaveCancellation.CancelOwn`, `LeaveCancellation.ApproveCurrentStep` |
| LeaveAdmin | `LeaveCancellation.ViewDepartment`, `LeaveCancellation.Manage` |
| Admin | `LeaveCancellation.ViewDepartment`, `LeaveCancellation.ViewAll`, `LeaveCancellation.Manage` |
| SuperAdmin | `LeaveCancellation.ViewAll`, `LeaveCancellation.Manage`, `LeaveCancellation.ApproveCurrentStep` เฉพาะกรณีได้รับมอบหมายเป็น current approver |

Admin/SuperAdmin ไม่ควรมี `LeaveCancellation.Create` โดย default เพื่อป้องกันการสร้างคำขอยกเลิกใบลาแทนผู้ใช้งานแบบไม่ชัดเจน

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
| Director | เห็นคำขอของตัวเอง และคำขอที่ตนเองเป็นผู้อนุมัติปัจจุบัน |
| Admin | เห็นตาม granular permission ที่ได้รับ เช่น `LeaveRequest.ViewDepartment`, `LeaveRequest.ViewAll`, หรือ `LeaveSupport.ViewAll` |
| SuperAdmin | เห็นทุกคำขอเมื่อได้รับ explicit permission เช่น `LeaveRequest.ViewAll` หรือ `LeaveSupport.ViewAll` |

Note: BUG-001 fixed. Director no longer receives implicit `ViewAll` from role name alone.

Backend enforces this through `LeaveRequestAccessService` for leave list, detail, PDF, attachment list, attachment preview, and attachment download.

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

หมายเหตุ: หน้า `/reports/leaves` เปิดให้ `Director`, `Admin`, `SuperAdmin` เข้าดูได้ตาม executive/reporting policy แม้ไม่มี `ReportManagement.View` โดย backend ยัง enforce สิทธิ์จริงและแยกสิทธิ์ export ออกจากสิทธิ์ดูรายงาน

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
