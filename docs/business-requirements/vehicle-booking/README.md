# HOP Vehicle Booking and Fleet Management (Phase 2.0)

ชุดเอกสารนี้เป็น baseline สำหรับระบบขอใช้รถราชการและบริหารงานยานพาหนะ โดยยึด architecture และ convention ของ HOP ปัจจุบัน

## ขอบเขตและหลักการ

- ใช้ `User` เป็นแหล่งข้อมูลบุคลากรปัจจุบัน เนื่องจากระบบยังไม่มี `Employee` entity แยก และห้ามทำสำเนาข้อมูลพนักงาน
- ใช้ permission-based authorization ทั้ง backend และ frontend
- หนึ่งคำขอมีรถและคนขับ active ได้อย่างละหนึ่งรายการ
- ระบบเสนอรถ/คนขับ แต่ไม่ auto-assign
- transaction และประวัติ assignment ห้าม hard delete
- เวลาในฐานข้อมูลเป็น UTC และแสดงผล Asia/Bangkok ใน UI (รอยืนยันใน open questions)
- `PassengerCount` หมายถึงผู้โดยสารทั้งหมดรวมผู้ขอเมื่อร่วมเดินทาง แต่ไม่รวมคนขับ

## เอกสาร

- [As-Is](HOP-Vehicle-As-Is.md)
- [To-Be](HOP-Vehicle-To-Be.md)
- [Business Rules](HOP-Vehicle-Business-Rules.md)
- [Workflow](HOP-Vehicle-Workflow.md)
- [State Diagram](HOP-Vehicle-State-Diagram.md)
- [Permission Matrix](HOP-Vehicle-Permission-Matrix.md)
- [Data Dictionary](HOP-Vehicle-Data-Dictionary.md)
- [API Specification](HOP-Vehicle-API-Spec.md)
- [Test Scenarios](HOP-Vehicle-Test-Scenarios.md)
- [Rollout Plan](HOP-Vehicle-Rollout-Plan.md)
- [Open Questions](HOP-Vehicle-Open-Questions.md)
- [Admin Guide](HOP-Vehicle-Admin-Guide.md)
- [Requester Guide](HOP-Vehicle-User-Guide.md)
- [Driver Guide](HOP-Vehicle-Driver-Guide.md)
- [Dispatcher Guide](HOP-Vehicle-Dispatcher-Guide.md)
- [Director Approval Guide](HOP-Vehicle-Director-Approval-Guide.md)
- [Milestone 2](HOP-Vehicle-Milestone-2.md)

## Milestones

1. Foundation, master data, permission และเอกสาร
2. Requester และ Dispatcher
3. Administration Review และ Director Approval
4. Driver workflow และ trip completion
5. Notification, audit, dashboard และ reports
6. Hardening, migration rehearsal และ rollout
