# Leave Balance Validation

เอกสารนี้สรุป validation ยอดวันลาคงเหลือของระบบ Leave Management

## Current Status

สถานะหลังปรับปรุง: **PASS**

ระบบรองรับ:

- คำนวณวันลาเต็มวันจากวันทำการ
- คำนวณครึ่งวันเช้า/บ่ายเป็น `0.5`
- ไม่นับเสาร์/อาทิตย์
- ไม่นับวันหยุดราชการใน `leave_holidays`
- ตรวจยอดวันลาตาม `userId + leaveTypeId + year`
- นับ `pending_days` เป็นยอดที่ถูกกันไว้แล้ว
- block เมื่อยอดไม่พอ
- รองรับ leave type ที่ไม่ใช้ quota ด้วย `requires_balance = false`

## Backend Enforcement

Backend ตรวจใน:

- `POST /api/leave-requests`
- `PUT /api/leave-requests/{id}`
- `POST /api/leave-requests/{id}/submit`

จุดสำคัญที่สุดคือ submit endpoint เพราะเป็นจุดที่ระบบ reserve ยอดใน `pending_days`

## Error Message

ถ้ายอดไม่พอ ระบบตอบข้อความภาษาไทย เช่น:

```text
วันลาคงเหลือไม่เพียงพอ คงเหลือ 5 วัน มีคำขอรออนุมัติ 3 วัน เหลือใช้ได้ 2 วัน แต่ขอลา 3 วัน
```

หรือถ้าไม่มี pending:

```text
วันลาคงเหลือไม่เพียงพอ คงเหลือ 1 วัน แต่ขอลา 2 วัน
```

## Services

- `LeaveCalendarService`: คำนวณวันทำการและวันหยุด
- `LeaveBalanceValidationService`: ตรวจยอดคงเหลือและ pending days
- `LeaveValidationService`: รวม validation ของคำขอลา, overlap, attachment, balance

## Frontend UX

หน้า “สร้างคำขอลา” แสดง:

- วันลาคงเหลือของประเภทที่เลือก
- จำนวนวันที่คำขอนี้ใช้โดยประมาณ
- pending days ถ้ามี
- warning ถ้ายอดไม่พอ

Frontend เป็น UX helper เท่านั้น Backend เป็นตัว enforce จริง

