# Leave Support

หน้า **มุมมองช่วยเหลือระบบลา** ใช้สำหรับ SuperAdmin หรือผู้ได้รับสิทธิ์ support ตรวจสอบคำขอลาทั้งหมดโดยไม่ปะปนกับคิวอนุมัติปกติ

## Permission

- `LeaveSupport.ViewAll`

## API

- `GET /api/leave-support/requests`
- `GET /api/leave-support/requests/{id}`

## ความสามารถ

- ค้นหาด้วยเลขที่คำขอหรือชื่อผู้ขอลา
- filter ตามหน่วยงาน สถานะ ผู้อนุมัติปัจจุบัน และช่วงวันที่
- แสดง approval timeline
- แสดง audit trail
- แสดง override log
- แสดงสถานะค้างอนุมัติและเหตุผลที่ค้าง

Support view เป็น read-only โดย default การอนุมัติแทนต้องใช้ override endpoint เท่านั้น
