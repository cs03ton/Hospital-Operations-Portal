# Leave Request Visibility

ระบบ HOP enforce สิทธิ์การมองเห็นคำขอลาที่ backend เป็นหลัก Frontend guard ใช้เพื่อ UX เท่านั้น

## Visibility Rules

| Role | เห็นคำขอลาได้ |
| --- | --- |
| Staff | เฉพาะคำขอของตัวเอง |
| DepartmentHead | คำขอของตัวเอง และคำขอของ Staff ในหน่วยงานเดียวกัน |
| Director | คำขอของทุกคน |
| Admin | คำขอของทุกคน |
| SuperAdmin | คำขอของทุกคน |

## API Enforcement

API ต่อไปนี้ใช้ rule เดียวกัน:

- `GET /api/leave-requests`
- `GET /api/leave-requests/{id}`
- `GET /api/leave-requests/{id}/pdf`
- `GET /api/leave-requests/{id}/attachments`
- `GET /api/leave-attachments/{id}/download`

ถ้าผู้ใช้ไม่มีสิทธิ์ ระบบต้องตอบ `403 Forbidden` และบันทึก audit event ผ่าน `Authorization.Denied`

## Permission Mapping

ระบบยังรองรับ granular permissions:

- `LeaveRequest.ViewOwn`
- `LeaveRequest.ViewPendingApproval`
- `LeaveRequest.ViewDepartment`
- `LeaveRequest.ViewAll`

Role สำคัญถูก map เพิ่มใน backend:

- `Director`, `Admin`, `SuperAdmin` เทียบเท่า view all สำหรับคำขอลา
- `DepartmentHead` เห็น Staff ในหน่วยงานเดียวกันและคำขอของตัวเอง

## Deployment Note

ต้องทดสอบด้วย user หลาย role ก่อน deploy โดยเฉพาะกรณีหัวหน้าหน่วยงานต้องไม่เห็นคำขอของหน่วยงานอื่น
