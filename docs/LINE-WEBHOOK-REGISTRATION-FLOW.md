# LINE Webhook Registration Flow

- เพิ่ม HOP LINE OA เข้ากลุ่ม
- ส่งข้อความ `ลงทะเบียนกลุ่ม HOP` หรือให้ระบบรับ `join` event
- กลุ่มปรากฏใน Admin API ด้วยสถานะ Pending และ groupId แบบ mask
- Fleet Admin ตรวจชื่อ/กลุ่มและยืนยัน กลุ่มจึงเป็น Active
- เมื่อ bot ถูกนำออก LINE ส่ง `leave`; HOP เปลี่ยน destination เป็น Disabled และ Attention Required

Webhook ที่ signature ไม่ถูกต้องหรือไม่มี signature ตอบ 401 และไม่เขียน inbox ข้อมูล ส่วน event ซ้ำใช้ `webhookEventId` unique index ป้องกันการประมวลผลซ้ำ
