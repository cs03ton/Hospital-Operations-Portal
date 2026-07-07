# User Management

เอกสารนี้อธิบายการจัดการผู้ใช้งาน Phase 1 สำหรับ Hospital Operations Portal

## ข้อมูลสำคัญของผู้ใช้

- รหัสพนักงาน
- ชื่อ-สกุล
- ชื่อผู้ใช้
- หน่วยงาน
- บทบาท
- LINE User ID
- กฎการอนุมัติวันลา

## กฎการอนุมัติวันลา

ช่อง `กฎการอนุมัติวันลา` ใช้กำหนดเส้นทางอนุมัติเมื่อผู้ใช้งานส่งคำขอลา

ผู้ใช้งานไม่สามารถเลือก rule เองตอนยื่นลาได้ Admin ต้องกำหนด rule ให้ถูกต้องในหน้าเพิ่ม/แก้ไขผู้ใช้งาน

## ตัวอย่างการผูก Rule

| Username | Role | Approval Rule |
| --- | --- | --- |
| `staff01` | Staff | `IT-STAFF` |
| `staff02` | Staff | `IT-STAFF` |
| `head01` | DepartmentHead | `IT-HEAD` |
| `director01` | Director | `DIRECTOR` |
| `admin_support` | Admin | `IT-HEAD` |

## วิธีตรวจสอบ Rule

1. ไปที่ `จัดการระบบผู้ใช้` → `จัดการผู้ใช้`
2. ตรวจคอลัมน์ `กฎการอนุมัติวันลา`
3. กดปุ่ม `ทดสอบกฎการอนุมัติ`
4. ตรวจว่าผู้อนุมัติแต่ละขั้นถูกต้อง และไม่มี warning เรื่อง self approval

## ข้อควรระวัง

- ถ้า user ไม่มี approval rule จะไม่สามารถ submit leave request ได้
- ถ้า rule ถูกปิดใช้งานควรเปลี่ยน rule ให้ user ก่อนใช้งานจริง
- ผู้อนุมัติในแต่ละขั้นต้องมี permission `LeaveApproval.ApproveCurrentStep`

## User Management Grid

หน้า `จัดการผู้ใช้` ใช้ตารางมาตรฐานสำหรับข้อมูลหลัก รองรับการใช้งานกับผู้ใช้จำนวนมาก

### Columns

| Column | รายละเอียด |
| --- | --- |
| Avatar | รูป/อักษรย่อผู้ใช้งาน |
| Username | ชื่อผู้ใช้สำหรับ login |
| Full Name | ชื่อ-นามสกุล |
| Department | หน่วยงาน |
| Employment Type | ประเภทบุคลากร |
| Role | บทบาท |
| Status | ใช้งาน / ปิดใช้งาน |
| LINE | สถานะการเชื่อมต่อ LINE |
| Created Date | วันที่สร้างบัญชี |
| Last Login | วันที่ login ล่าสุดจาก refresh token |
| Action | ทดสอบกฎอนุมัติ / แก้ไข / ลบหรือปิดใช้งาน |

### Search / Filter / Sort

- Search: username, ชื่อ-นามสกุล, หน่วยงาน, role
- Filter: หน่วยงาน, role, employment type, status, has LINE
- Sort: username, fullname, department, created date, last login
- Pagination: 10, 20, 50, 100 รายการต่อหน้า

## Delete / Soft Delete Policy

ระบบไม่ hard delete ผู้ใช้ที่มีข้อมูลอ้างอิงสำคัญ เพื่อป้องกันประวัติคำขอลาและ audit log สูญหาย

### Blocked

ระบบจะไม่อนุญาตให้ลบในกรณีต่อไปนี้:

- ผู้ใช้ที่กำลัง login อยู่
- `SuperAdmin` คนสุดท้ายของระบบ
- ผู้ใช้ที่เป็น current approver ของคำขอลาที่ยังไม่จบกระบวนการ

### Soft Delete

ถ้าผู้ใช้มีข้อมูลอ้างอิง เช่น leave request, approval history, audit log, approval chain, LINE binding ระบบจะตั้งค่า:

- `IsActive = false`
- revoke refresh token ที่ยังใช้งานอยู่
- บันทึก audit event `User.SoftDelete`

ข้อความที่แสดง:

> ไม่สามารถลบผู้ใช้งานได้ เนื่องจากมีข้อมูลอ้างอิงในระบบ ระบบจึงปิดการใช้งานผู้ใช้แทน

### Hard Delete

ใช้เฉพาะผู้ใช้ที่ไม่มีข้อมูลอ้างอิงในระบบเท่านั้น

## API

`GET /api/users`

รองรับ backward-compatible list และรองรับ paging เมื่อส่ง `page`

Query:

- `page`
- `pageSize`
- `search`
- `sort`
- `direction`
- `departmentId`
- `roleId`
- `employmentType`
- `status`
- `hasLine`

`DELETE /api/users/{id}`

คืนค่า `DeleteResultResponse` เพื่อระบุว่าเป็น `Deleted`, `SoftDeleted` หรือ `Blocked`
