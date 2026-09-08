# Fleet LINE Group Production Readiness

## M4 UI

Route `/fleet/admin/line-groups` รองรับ list/search/status filter, masked group ID, Pending/Active/Disabled, attention state, confirm/disable/test dialogs, subscription editor และ paged delivery logs รวม loading, empty, filtered-empty, error/retry และ concurrency conflict feedback

เมนูและ route ใช้ `FleetLineGroup.View/Manage`; seeder เพิ่มเฉพาะ permission master และไม่ assign ให้ production roles อัตโนมัติ

## Rollout checklist

- apply migrations M1 และ M3 แบบ additive
- คง `LineGroupNotifications:Enabled=false`
- ตรวจ LINE USER/Leave regression และ webhook signature
- map permissions ให้ pilot Fleet Admin ผ่าน change control
- เปิด flag ใน UAT, ลงทะเบียนและยืนยัน test group
- ทดสอบ test-send, transient retry, bot leave และ privacy
- ตรวจ delivery correlation IDs และ health logs
- เปิด production แบบจำกัดกลุ่ม; rollback ทันทีด้วยการปิด flag

## Safety gates

- test-send ถูก block ฝั่ง serverเมื่อ flag false
- test message จำกัด 1,000 ตัวอักษรและ reject patternข้อมูลอ่อนไหว
- groupId ไม่ปรากฏแบบเต็มใน frontend/API/log
- Leave events ไม่มี canonical mapping และไม่สร้าง group delivery
- LINE USER public contract/schema/worker ไม่เปลี่ยน

Feature flag production ยังเป็น false และงานนี้ไม่ได้ apply migration/deploy production
