# Employment Type Change

## สถานะปัจจุบัน

ระบบรองรับการบันทึก `employment_type` บนผู้ใช้ และใช้ค่านี้ในการคำนวณ policy เมื่อสร้างคำขอลา/ตั้งต้นยอดใหม่

หลังปรับปรุงล่าสุด:

- การเปลี่ยนประเภทพนักงานจะถูก audit ด้วย event `Employee.EmploymentProfileChanged`
- ระบบจะไม่ recalculation ทับยอดเดิมแบบอัตโนมัติ
- ระบบจะ initialize เฉพาะ balance ที่ยังไม่มีในปีงบประมาณปัจจุบัน

## เหตุผลที่ไม่ recalculation ทันที

การเปลี่ยนประเภทพนักงานมีผลกับสิทธิ์ในอดีตและอนาคต เช่น รายวันเป็น พกส. หรือพนักงานราชการเป็นข้าราชการ หากแก้ยอดทันทีโดยไม่มี effective date และ preview อาจเกิดปัญหา:

- หัก/คืนวันลาซ้ำ
- ย้อนแก้ประวัติคำขอเดิม
- ทำให้ยอดติดลบโดยไม่รู้ตัว
- กระทบคำขอรออนุมัติและคำขอล่วงหน้า

## Workflow ที่ควรเพิ่มใน phase ถัดไป

1. Admin/HR เปิด dialog “เปลี่ยนประเภทพนักงาน”
2. เลือกประเภทใหม่
3. ระบุวันที่มีผล
4. ระบุเหตุผล
5. ระบบ preview ก่อน/หลังต่อประเภทลา
6. ระบบแสดง warning หาก projected balance ติดลบ
7. ผู้มีสิทธิ์ยืนยัน apply
8. ระบบสร้าง employment history และ adjustment transaction

## Audit ที่เกี่ยวข้อง

- `Employee.EmploymentProfileChanged`
- `LeaveEntitlement.Initialized`
- Future: `Employee.EmploymentTypeChangePreviewed`
- Future: `Employee.EmploymentTypeChanged`

