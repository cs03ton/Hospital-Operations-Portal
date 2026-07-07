# Role & Permission Management

เอกสารนี้อธิบายการจัดการบทบาทและสิทธิ์ของ Hospital Operations Portal (HOP) Phase 1

## Role Grid

หน้า `บทบาทและสิทธิ์` แสดง Role Grid สำหรับดูและจัดการบทบาท

### Columns

| Column | รายละเอียด |
| --- | --- |
| Role | ชื่อบทบาท |
| Description | รายละเอียดบทบาท |
| Users | จำนวนผู้ใช้ที่ผูกบทบาทนี้ |
| Permissions | จำนวนสิทธิ์ที่ผูกกับบทบาท |
| Status | ใช้งาน / ปิดใช้งาน |
| Created | วันที่สร้าง |
| Action | แก้ไข / จัดการสิทธิ์ / ลบหรือปิดใช้งาน |

### Search / Filter / Sort

- Search: role name, description
- Filter: status
- Sort: role, users, permissions, created date
- Pagination: 10, 20, 50, 100 รายการต่อหน้า

## Permission Grid

หน้าเดียวกันมี Permission Grid สำหรับตรวจสอบสิทธิ์ทั้งหมดในระบบ

### Columns

| Column | รายละเอียด |
| --- | --- |
| Permission Code | รหัสสิทธิ์ เช่น `LeaveRequest.ViewOwn` |
| Module | กลุ่มระบบ เช่น `LeaveRequest`, `UserManagement` |
| Description | ชื่อหรือคำอธิบายสิทธิ์ |
| Roles Count | จำนวนบทบาทที่ใช้สิทธิ์นี้ |
| Status | ใช้งาน / ปิดใช้งาน |
| Created | วันที่สร้าง |
| Action | ลบหรือปิดใช้งาน |

### Search / Filter / Sort

- Search: permission code, name, module, action
- Filter: module, status
- Sort: permission code, module, roles count, created date
- Pagination: 10, 20, 50, 100 รายการต่อหน้า

## Delete Policy

### Role

ห้ามลบ:

- `SuperAdmin`
- `Admin`
- System Role
- Role ที่มี User ใช้งาน

ถ้า role กำหนดเองและไม่มี user ใช้งาน ระบบจะใช้ soft delete โดยตั้งค่า `IsActive = false`

Audit event:

- `Role.SoftDelete`

### Permission

ห้ามลบ permission ที่ถูกใช้โดย role assignment

ถ้า permission ไม่มี role ใช้งาน ระบบจะใช้ soft delete โดยตั้งค่า `IsActive = false`

Audit event:

- `Permission.SoftDelete`

> หมายเหตุ: Permission เป็นส่วนหนึ่งของ authorization contract ระหว่าง backend และ frontend การปิดใช้งานต้องทำอย่างระมัดระวังและทดสอบ permission guard ทุกครั้ง

## API

`GET /api/roles`

Query:

- `page`
- `pageSize`
- `search`
- `sort`
- `direction`
- `status`

`DELETE /api/roles/{id}`

`GET /api/permissions`

Query:

- `page`
- `pageSize`
- `search`
- `sort`
- `direction`
- `module`
- `status`

`DELETE /api/permissions/{id}`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
