# Leave Entitlement Rules

## หลักการ

สิทธิ์ลาของ HOP คำนวณจาก backend เท่านั้น โดยใช้ข้อมูลหลัก:

1. ประเภทพนักงาน (`users.employment_type`)
2. วันที่เริ่มงาน (`users.employment_start_date`)
3. ปีงบประมาณ (`FiscalYearHelper`: 1 ต.ค. - 30 ก.ย.)
4. ประเภทลา (`leave_types`)
5. กฎสิทธิ์ลา (`leave_policy_rules`)
6. ยอดที่บันทึกจริง (`leave_balances`)

Frontend ห้ามคำนวณสิทธิ์เอง และต้องแสดงข้อมูลจาก API เท่านั้น

## การตั้งต้นสิทธิ์ลา

ระบบจะตั้งต้นสิทธิ์ลาเมื่อ:

1. ผู้ใช้เปิดใช้งานอยู่
2. มีประเภทพนักงาน
3. มีวันที่เริ่มงาน
4. มี policy rule สำหรับประเภทพนักงานและประเภทลานั้น
5. ยังไม่มี `leave_balances` สำหรับ user + leave type + fiscal year

ถ้าข้อมูลไม่ครบ ระบบจะไม่สร้างยอด และบันทึก audit ว่า skip เพราะสาเหตุใด

## Formula

```text
availableDays = entitledDays + carriedOverDays + adjustedDays - usedDays - pendingDays
```

## ข้อควรระวัง

> Warning: ห้ามแก้ `employment_type` หรือ `employment_start_date` แล้วคาดหวังให้ระบบ rewrite ยอดเดิมอัตโนมัติ ระบบจะไม่ทับยอดเดิมเพื่อป้องกันประวัติผิดพลาด

> Note: ถ้า dashboard แสดง note ว่า “ยังไม่ได้ตั้งต้นยอดวันลาจริง” ให้ HR/Admin initialize หรือปรับยอดผ่านหน้าจัดการวันลาคงเหลือตาม policy

