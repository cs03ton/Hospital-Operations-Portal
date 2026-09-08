# HOP Vehicle Business Rules

ข้อกำหนด BR-VH-001 ถึง BR-VH-022 เป็น authoritative baseline ตามคำขอ Phase 2.0 โดยมีคำจำกัดความเพิ่มดังนี้:

- Active employee ในระบบปัจจุบันคือ `User.IsActive = true` และมีข้อมูลบุคลากรที่จำเป็น
- `PassengerCount > 0` นับผู้ขอเมื่อร่วมเดินทางและไม่รวมคนขับ
- ช่วงเวลาใช้ half-open interval `[DepartureAt, ExpectedReturnAt)`; เวลาสิ้นสุดต้องมากกว่าเวลาเริ่ม
- Assignment ที่ block availability คือ active assignment ของ request ที่ไม่อยู่ใน `DRAFT`, `RETURNED`, `REJECTED`, `CANCELLED`, `COMPLETED`
- approved leave ทับซ้อนวัน/เวลาเดินทางทำให้ driver ไม่พร้อม โดยใช้ทั้งวันตาม timezone โรงพยาบาล
- ใบขับขี่ที่หมดอายุก่อนสิ้นสุดภารกิจถือว่าไม่พร้อม
- การ return ต้องระบุ target stage และ reason เพื่อป้องกัน ambiguity
- cancellation และ override ต้องระบุ reason ตาม policy และ audit
- ผู้ขอยกเลิกได้ตั้งแต่ `DRAFT` จนถึงก่อนผู้อำนวยการอนุมัติ; หลัง `APPROVED` ต้องดำเนินการโดยงานยานพาหนะที่มีสิทธิ์ พร้อมเหตุผลและ audit
- การ return ใช้ `ReturnTarget=REQUESTER` เมื่อข้อมูลคำขอผิด และ `ReturnTarget=DISPATCHER` เมื่อข้อมูลรถ/คนขับผิด
- คัดลอกคำขอสร้าง draft ใหม่ ไม่คัดลอก passenger rows จนผู้ขอยืนยันรายชื่อใหม่
- Phase 2.0 ไม่ใช้ fairness score งานไกล/ข้ามคืนและไม่ใช้ compatibility matrix แบบ hard-coded; ทั้งสองเรื่องเลื่อนไป Phase 2.1
- Emergency Ambulance Workflow เป็น extension ที่ต้องรองรับใน Phase 2.1 และไม่รวมใน workflow ปกติของ Phase 2.0

## Invariants

1. Active assignment ต่อ request ไม่เกินหนึ่งรายการ (partial unique index)
2. รถและคนขับห้ามมี active interval ทับซ้อนกัน (transactional validation + locking/advisory strategy)
3. ผู้ขอไม่ส่ง vehicle/driver IDs ใน create/update request DTO
4. ผู้ปิดงานเป็น active assigned driver หรือผู้มี override permissionพร้อมเหตุผล
5. `EndMileage >= StartMileage` และ vehicle current mileage ห้ามลดลง
