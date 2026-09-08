# คู่มือผู้ดูแล LINE Group — Fleet

สิทธิ์ที่ใช้คือ `FleetLineGroup.View` และ `FleetLineGroup.Manage`

Admin API:

- `GET /api/fleet/line-groups` แสดงกลุ่ม ชื่อ groupId แบบ mask สถานะ module และ subscriptions
- `POST /api/fleet/line-groups/{id}/confirm` ยืนยันกลุ่ม Pending
- `POST /api/fleet/line-groups/{id}/disable` ปิดกลุ่มพร้อมเหตุผล
- `PUT /api/fleet/line-groups/{id}/subscriptions` ปรับ event subscriptions
- `POST /api/fleet/line-groups/{id}/test` ส่งข้อความทดสอบเมื่อ feature flag เปิด
- `GET /api/fleet/line-groups/{id}/deliveries` ดูประวัติและสถานะ retry แบบแบ่งหน้า

หน้า Admin อยู่ที่ `/fleet/admin/line-groups` และแสดงเฉพาะผู้มี `FleetLineGroup.View` หรือ `FleetLineGroup.Manage` ทุก mutation ใช้ concurrency token และเขียน audit

ขั้นตอนใช้งาน:

1. ตรวจชื่อและ Group ID แบบ mask ของรายการ Pending
2. เปิดรายละเอียดและตรวจ default Event Subscription
3. กดยืนยันกลุ่ม
4. เมื่อ feature flag เปิดแล้วจึงใช้ส่งข้อความทดสอบ
5. ตรวจ Delivery Log และ Correlation ID หากส่งไม่สำเร็จ

ข้อความทดสอบห้ามมีข้อมูลผู้ป่วย สุขภาพ ใบขับขี่ เหตุผลการลา HN หรือเลขส่วนบุคคล ระบบตรวจซ้ำฝั่ง server
