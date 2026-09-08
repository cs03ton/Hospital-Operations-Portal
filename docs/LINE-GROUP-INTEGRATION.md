# LINE Group Integration

HOP ใช้ LINE Official Account และ Messaging API configuration เดียวกับระบบ LINE เดิม กลุ่มปลายทางไม่อยู่ใน source code หรือ environment variable แต่ถูกค้นพบผ่าน signed webhook และบันทึกในฐานข้อมูลเป็น `Pending` จนผู้มีสิทธิ์ `FleetLineGroup.Manage` ยืนยัน

การทำงานทั้งหมดถูกครอบด้วย `LineGroupNotifications:Enabled` ซึ่ง default เป็น `false`; USER webhook และ USER delivery ไม่ขึ้นกับ flag นี้

## M1 flow

1. LINE ส่ง webhook ไป `/api/line/webhook`
2. HOP ตรวจ `X-Line-Signature` ด้วย channel secret ก่อนอ่าน event
3. event ที่ `source.type=group` ถูกเขียนลง `line_webhook_inbox` โดยใช้ `webhookEventId` ป้องกันซ้ำ
4. background worker ประมวลผล `join`, `leave` และข้อความ `ลงทะเบียนกลุ่ม HOP`
5. กลุ่มใหม่ถูกสร้างเป็น `Pending` พร้อม Fleet subscriptions ค่าเริ่มต้น
6. Fleet Admin ยืนยันผ่าน Admin API แล้วจึงเปลี่ยนเป็น `Active`

M1 ยังไม่ส่ง Fleet notification จริง การ render template และ delivery ผ่าน Fleet outbox จะเริ่มใน M2/M3
