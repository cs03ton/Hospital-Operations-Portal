# Leave Request Visibility

ระบบ HOP enforce สิทธิ์การมองเห็นคำขอลาที่ backend เป็นหลัก Frontend guard ใช้เพื่อ UX เท่านั้น

## Visibility Rules

| Role | เห็นคำขอลาได้ |
| --- | --- |
| Staff | เฉพาะคำขอของตัวเอง |
| DepartmentHead | คำขอของตัวเอง และคำขอของ Staff ในหน่วยงานเดียวกัน |
| Director | คำขอของตัวเอง และคำขอที่ตนเองเป็นผู้อนุมัติปัจจุบัน |
| Admin | ตาม permission ที่ได้รับมอบหมาย เช่น `LeaveSupport.ViewAll` หรือ `LeaveRequest.ViewAll` |
| SuperAdmin | ตาม permission ที่ได้รับมอบหมาย เช่น `LeaveSupport.ViewAll` หรือ `LeaveRequest.ViewAll` |

## API Enforcement

API ต่อไปนี้ใช้ rule เดียวกัน:

- `GET /api/leave-requests`
- `GET /api/leave-requests/{id}`
- `GET /api/leave-requests/{id}/pdf`
- `GET /api/leave-requests/{id}/attachments`
- `GET /api/leave-attachments/{id}/download`

ถ้าผู้ใช้ไม่มีสิทธิ์ ระบบต้องตอบ `403 Forbidden` และบันทึก audit event ผ่าน `Authorization.Denied`

### List Scope Query

`GET /api/leave-requests` รองรับ scope สำหรับ navigation จาก dashboard:

| Query | ความหมาย | Backend Enforcement |
| --- | --- | --- |
| `scope=mine` | แสดงเฉพาะคำขอของผู้ใช้ปัจจุบัน | เพิ่มเงื่อนไข `UserId = currentUserId` |
| `scope=department` | แสดงคำขอของคนในหน่วยงานเดียวกัน | ต้องมี `LeaveRequest.ViewDepartment`, `LeaveRequest.ViewAll`, หรือ visibility equivalent และต้องไม่รวมคำขอของตัวเอง |

รองรับ status alias เพื่อให้ URL อ่านง่าย:

- `status=pending` → `Pending`
- `status=returned` → `ReturnedForRevision`
- `status=approved` → `Approved`
- `status=rejected` → `Rejected`
- `status=cancelled` → `Cancelled`

## Permission Mapping

ระบบยังรองรับ granular permissions:

- `LeaveRequest.ViewOwn`
- `LeaveRequest.ViewPendingApproval`
- `LeaveRequest.ViewDepartment`
- `LeaveRequest.ViewAll`
- `LeaveSupport.ViewAll`

Rule สำคัญ:

- `Director` ไม่ได้ `ViewAll` อัตโนมัติ
- `Admin` และ `SuperAdmin` ไม่ได้ `ViewAll` จากชื่อ role เพียงอย่างเดียว ต้องมี permission explicit
- `LeaveRequest.ViewAll` และ `LeaveSupport.ViewAll` เป็น permission ที่อนุญาตให้เห็นคำขอลาทั้งหมด
- `DepartmentHead` เห็น Staff ในหน่วยงานเดียวกันและคำขอของตัวเอง

## Dashboard Rule for Department Head

Dashboard ของหัวหน้าหน่วยงานแยกข้อมูลเป็น 2 กลุ่มเพื่อป้องกันการนับซ้ำ:

1. `คำขอลาของฉันที่รออนุมัติ`
   - ใช้เฉพาะคำขอของหัวหน้าคนนั้นเอง
   - นับเฉพาะ `Pending`
   - ไม่นับ `ReturnedForRevision`, `Approved`, `Rejected`, `Cancelled`
2. `คำขอลาของหน่วยงาน`
   - ใช้คำขอของผู้ใช้ในหน่วยงานเดียวกัน
   - ไม่รวมคำขอของหัวหน้าเอง
   - ไม่แสดงคำขอจากหน่วยงานอื่น

## BUG-001 Fix

แก้ไขแล้ว: Director no longer has implicit `ViewAll`.

สาเหตุเดิมคือ backend visibility ผูก `Director`, `Admin`, และ `SuperAdmin` กับ `ViewAll` โดยตรง ทำให้ Director เห็นรายการคำขอลาทุกใบโดยไม่ต้องมี granular permission
หลังแก้ไข `ViewAll` จะมาจาก explicit permission เท่านั้น:

- `LeaveRequest.ViewAll`
- `LeaveSupport.ViewAll`

## Deployment Note

ต้องทดสอบด้วย user หลาย role ก่อน deploy โดยเฉพาะกรณีหัวหน้าหน่วยงานต้องไม่เห็นคำขอของหน่วยงานอื่น
