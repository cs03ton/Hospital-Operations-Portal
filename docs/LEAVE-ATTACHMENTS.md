# Leave Attachments

## Permission Matrix

| ผู้ใช้งาน | Draft | Pending | ReturnedForRevision | Approved/Rejected/Cancelled |
|---|---|---|---|---|
| ผู้ขอ | upload/delete/preview/download | preview/download only | upload/delete/preview/download | preview/download only |
| ผู้อนุมัติ | ถ้าเข้าถึงคำขอได้ preview/download only | preview/download only | preview/download only | preview/download only |
| Admin/SuperAdmin | support view/preview/download ตามสิทธิ์ | support view/preview/download ตามสิทธิ์ | support view/preview/download ตามสิทธิ์ | support view/preview/download ตามสิทธิ์ |

## API

- `GET /api/leave-requests/{id}/attachments`
- `POST /api/leave-requests/{id}/attachments`
- `GET /api/leave-requests/{id}/attachments/{attachmentId}/preview`
- `GET /api/leave-attachments/{id}/download`
- `DELETE /api/leave-attachments/{id}`

## Preview Behavior

1. หน้าเว็บใช้ “ดูตัวอย่าง” เป็นค่าเริ่มต้นสำหรับผู้อนุมัติและผู้มีสิทธิ์ดูคำขอ
2. รองรับ `PDF`, `JPG/JPEG`, `PNG`, `WEBP`
3. ไฟล์ที่ไม่รองรับแสดงข้อความ “ไม่รองรับการแสดงตัวอย่างไฟล์ประเภทนี้”
4. API preview ส่งไฟล์แบบ `Content-Disposition: inline`
5. ปุ่มดาวน์โหลดยังคงอยู่เป็นตัวเลือกเสริมตามสิทธิ์เดิม

## Security Rules

1. ผู้ขอเท่านั้นที่ upload/delete ได้
2. upload/delete ได้เฉพาะ `Draft` หรือ `ReturnedForRevision`
3. ผู้อนุมัติดูและดาวน์โหลดได้เท่านั้น
4. ทุก upload ต้องผ่าน file validation และ file scanning ตาม config
5. ไม่เปิดเผย storage path จริงให้ frontend
6. preview/download ต้องผ่าน leave request visibility เดียวกันกับหน้ารายละเอียด

## Audit Events

- `LeaveAttachment.Uploaded`
- `LeaveAttachment.Deleted`
- `LeaveAttachment.Previewed`
- `LeaveAttachment.Downloaded`
- `LeaveAttachment.ScanFailed`
- `LeaveAttachment.UploadFailed`
