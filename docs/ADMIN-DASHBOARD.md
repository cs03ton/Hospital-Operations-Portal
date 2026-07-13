# Admin Dashboard

Admin Dashboard คือศูนย์ควบคุมผู้ดูแลระบบของ HOP ไม่ใช่หน้าจัดการผู้ใช้ซ้ำ

## Concept

| หน้า | วัตถุประสงค์ |
|---|---|
| `/admin/dashboard` | Control Center, overview, warning, quick actions, system status |
| `/admin/users` | CRUD, data grid, เพิ่ม/แก้ไข/ปิดใช้งาน/ลบผู้ใช้ |
| `/admin/departments` | CRUD หน่วยงาน |
| `/admin/roles` | CRUD บทบาทและ permission |

Admin Dashboard ต้องไม่ทำ CRUD รายละเอียดบนหน้าเดียว แต่ให้ลิงก์ไปหน้าจัดการจริงผ่านปุ่ม `ไปจัดการ`

## Route และ API

| ส่วน | ค่า |
|---|---|
| Frontend | `/admin/dashboard` |
| Backend API | `GET /api/admin/dashboard` |
| Permission | `AdminDashboard.View` |
| Role fallback | `Admin`, `SuperAdmin` |

## Widgets

| Widget | รายละเอียด |
|---|---|
| User Summary | ผู้ใช้ทั้งหมด, active, inactive, ไม่มี LINE, ไม่มี employment type, ไม่มีกฎอนุมัติ |
| Department Summary | หน่วยงานทั้งหมด, ไม่มีหัวหน้า, ไม่มีผู้ใช้งาน |
| Role & Permission Summary | role, permission, role ที่ไม่มีผู้ใช้, permission สำคัญที่ยังไม่ assign |
| Leave System Summary | คำขอรออนุมัติ, คำขอวันนี้, balance/rule ที่ขาด |
| Warning / To-do | รายการที่ควรตรวจสอบพร้อมปุ่มไปหน้าจัดการ |
| Health Summary | API, Database, Storage, LINE, Backup, Disk |
| LINE Summary | enabled, bound/unbound users, failed delivery ล่าสุด |
| Audit Summary | failed login, permission denied, admin actions ล่าสุด |
| Quick Actions | ทางลัดไปหน้าจัดการจริง |

## Quick Actions

| Action | Route |
|---|---|
| เพิ่มผู้ใช้ | `/admin/users/create` |
| จัดการผู้ใช้ | `/admin/users` |
| หน่วยงาน | `/admin/departments` |
| บทบาทและสิทธิ์ | `/admin/roles` |
| ตั้งค่า LINE | `/admin/line` |
| Health Center | `/admin/health` |
| Backup Center | `/admin/backup` |
| กฎอนุมัติวันลา | `/admin/approval-chains` |

## Warning / To-do Rules

แสดงเฉพาะรายการที่มีจำนวนมากกว่า 0:

- ผู้ใช้ที่ยังไม่ได้เชื่อม LINE
- ผู้ใช้ที่ยังไม่มีประเภทบุคลากร
- ผู้ใช้ที่ยังไม่มีกฎอนุมัติ
- หน่วยงานที่ยังไม่มีหัวหน้า
- leave balance ที่ยังไม่สร้าง
- permission สำคัญที่ยังไม่ได้ assign
- LINE delivery failed
- backup ล่าสุดควรตรวจสอบ เช่น ไม่พบไฟล์ `hopdb_*.backup` ใน `/opt/hop/backups/postgres`

## Backup Center Summary

Admin Dashboard ควรแสดงสถานะ backup แบบย่อเพื่อเตือนผู้ดูแลระบบก่อนเข้าใช้งานจริง:

| รายการ | ค่ามาตรฐาน |
|---|---|
| Database backup folder | `/opt/hop/backups/postgres` |
| Database backup file | `hopdb_YYYYMMDD_HHMMSS.backup` |
| Storage backup folder | `/opt/hop/backups/storage` |
| Storage backup file | `hop_uploads_YYYYMMDD_HHMMSS.tar.gz` |

หาก backup หายหรือเก่าผิดปกติ ให้ไปที่ `Backup Center` และตรวจ log รอบล่าสุด

## Security

- ห้ามส่ง token/secret/password/connection string ไป frontend
- Staff, Head, Director ทั่วไปเข้าไม่ได้
- Backend enforce จริง ไม่พึ่ง frontend guard อย่างเดียว
- Health summary ใช้ข้อมูล safe DTO จาก Health Center

## Manual Frontend Test Checklist

- [ ] Admin/SuperAdmin เปิด `/admin/dashboard` ได้
- [ ] Staff เปิด `/admin/dashboard` แล้วถูก redirect/deny
- [ ] Summary cards แสดงผู้ใช้ หน่วยงาน role/permission และระบบลา
- [ ] Warning cards แสดงเฉพาะเมื่อมี issue
- [ ] Quick actions พาไป route ถูกต้อง
- [ ] ไม่มี DataGrid CRUD บน Dashboard
- [ ] LINE summary ไม่แสดง token/secret
- [ ] Health summary แสดงสถานะแบบย่อและลิงก์ไป Health Center
- [ ] Recent audit ไม่แสดง stack trace หรือข้อมูลลับ

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
