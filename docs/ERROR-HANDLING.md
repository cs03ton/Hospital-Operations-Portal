# Safe Error Handling

เอกสารนี้อธิบายแนวทางจัดการ error แบบปลอดภัยของ HOP

## เป้าหมาย

1. ผู้ใช้เห็นข้อความที่เข้าใจง่าย
2. ผู้ดูแลระบบมี `Reference ID` สำหรับตรวจ log
3. Production ไม่แสดง stack trace
4. ไม่เปิดเผย secret, token, password หรือ connection string

## Backend

Backend ใช้ `GlobalExceptionMiddleware` เพื่อจับ exception ที่ไม่ได้จัดการใน request pipeline

Backend ใช้ header `X-Correlation-ID` เป็น request/correlation id มาตรฐาน หาก client หรือ Nginx ส่ง header นี้มา ระบบจะใช้ค่านี้เป็น `TraceIdentifier` และส่งกลับใน response header เดียวกัน หากไม่ส่งมา backend จะสร้างค่า request id ตาม runtime

Production response:

```json
{
  "message": "เกิดข้อผิดพลาด กรุณาติดต่อผู้ดูแลระบบ",
  "referenceId": "..."
}
```

Development response อาจมีรายละเอียดเพิ่มเพื่อช่วย debug ตาม environment

## Logging

Backend log error พร้อม `ReferenceId` ซึ่งเป็นค่าเดียวกับ `X-Correlation-ID` เมื่อมีการส่ง header นี้เข้ามา

```text
Unhandled exception. ReferenceId=...
```

ผู้ดูแลระบบสามารถใช้ Reference ID จากผู้ใช้ไปค้นหา log ได้

## Frontend

Frontend ใช้ `ErrorBoundary` ครอบ App

เมื่อเกิด error ใน UI จะแสดง:

- `เกิดข้อผิดพลาด`
- `กรุณาลองใหม่อีกครั้ง หรือแจ้งผู้ดูแลระบบ`
- `Reference ID`
- ปุ่ม `กลับหน้าหลัก`
- ปุ่ม `โหลดใหม่`

Production ไม่แสดง stack trace บนหน้าเว็บ

## HTTP Error

ถ้า backend ส่ง `referenceId` มากับ response 500 frontend จะแสดง reference ใน toast โดยไม่แสดงรายละเอียดภายในระบบ

## Checklist

- [ ] Production ไม่แสดง stack trace
- [ ] Response 500 มี `referenceId`
- [ ] Backend log มี `ReferenceId`
- [ ] Response header มี `X-Correlation-ID`
- [ ] Nginx ส่งต่อ `X-Correlation-ID` ไป backend
- [ ] Frontend ErrorBoundary มีปุ่มกลับหน้าหลักและโหลดใหม่
- [ ] ไม่มี token/password/connection string ใน response

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
