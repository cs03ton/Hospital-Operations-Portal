# LINE Group Troubleshooting

- ไม่พบกลุ่ม: ตรวจ webhook URL, channel secret, bot membership และส่งคำสั่งลงทะเบียนอีกครั้ง
- Pending ค้าง: ตรวจ `line_webhook_inbox` และ worker log; Failed จะหยุดหลัง 5 attempts ใน M1
- ชื่อกลุ่มเป็นค่า fallback: ตรวจ Channel Access Token และสิทธิ์ group summary API
- Attention Required: มักเกิดเมื่อ bot ถูกนำออกจากกลุ่ม ให้เพิ่ม bot ใหม่และลงทะเบียนอีกครั้ง
- กลุ่ม Active แต่ไม่ส่ง: M1 ยังไม่เปิด delivery; หลัง M3 ให้ตรวจ subscription, outbox และ sanitized delivery log
