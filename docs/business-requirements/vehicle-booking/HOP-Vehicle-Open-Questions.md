# HOP Vehicle Decisions and Open Questions

อัปเดตการตัดสินใจ: 1 สิงหาคม 2026

## Decisions ที่ปิดแล้ว

| ID | Decision | Status | Implementation note |
|---|---|---|---|
| VH-OQ-001 | ใช้ `users` เป็น personnel source ของ Fleet โดยไม่สร้าง `Employee` table หรือข้อมูลพนักงานซ้ำ | CLOSED | Fleet FK ใช้ `UserId`; เปลี่ยนชื่อเชิงธุรกิจในอนาคตได้โดยไม่สร้าง duplicate master |
| VH-OQ-002 | เก็บเวลาเป็น UTC และแสดงผล `Asia/Bangkok` | CLOSED | ใช้ PostgreSQL `timestamptz`; overlap คำนวณจาก UTC instant |
| VH-OQ-003 | `PassengerCount` นับเฉพาะผู้โดยสาร ไม่รวมคนขับ และนับผู้ขอเมื่อร่วมเดินทาง | CLOSED | passenger rows ต้องสอดคล้องกับ count ก่อน submit |
| VH-OQ-004 | ผู้ขอยกเลิกได้จนถึงก่อนผู้อำนวยการอนุมัติ; หลังอนุมัติต้องดำเนินการผ่านงานยานพาหนะ | ACCEPTED | requester cancel: `DRAFT` ถึง `PENDING_DIRECTOR_APPROVAL`; หลัง `APPROVED` ใช้ Fleet Dispatcher/Admin permission พร้อมเหตุผลและ audit |
| VH-OQ-005 | ส่งกลับตามต้นเหตุ: ข้อมูลคำขอผิดกลับผู้ขอ; ข้อมูลรถ/คนขับผิดกลับ Dispatcher | CLOSED | เก็บ explicit `ReturnTarget=REQUESTER|DISPATCHER` และ reason |
| VH-OQ-007 | Phase 2.0 ยังไม่ทำ vehicle compatibility matrix เต็มรูปแบบ | DEFERRED 2.1 | MVP ใช้ capacity, requested type, status, overlap และ warning; ไม่มี hard-coded substitute matrix |
| VH-OQ-008 | Phase 2.0 ไม่คำนวณ fairness score สำหรับงานไกล/ข้ามคืน | CLOSED | แสดงจำนวนเที่ยวเดือนนี้และงานล่าสุดเป็นข้อมูลประกอบเท่านั้น |
| VH-OQ-009 | Request number ใช้ `VH-yyyyMM-####` | CLOSED | ต้องสร้างแบบ concurrency-safe ด้วย DB sequence/counter และ unique constraint/retry |
| VH-OQ-010 | Fleet Admin ปิดงานแทนได้เมื่อมี permission, บังคับเหตุผลและ audit | CLOSED | ใช้ `FleetTrip.OverrideComplete`; ห้ามลด current mileage |
| VH-OQ-011 | Emergency Ambulance Workflow ต้องรองรับในระบบ แต่เลื่อนไป Phase 2.1 | DEFERRED 2.1 | Phase 2.0 ห้ามตีความคำขอฉุกเฉินเป็น workflow ปกติโดยอัตโนมัติ; เก็บ extension point/feature boundary |
| VH-OQ-012 | Mask เลขใบขับขี่โดย default และจำกัดสิทธิ์ดูเต็ม | CLOSED | full value เฉพาะ `FleetDriver.Manage`; response สำหรับ dispatcher/recommendation ต้อง masked |

## VH-OQ-006: Delegation Decision

สถานะ: **CLOSED — shared core + module scope**

สิ่งที่ระบบเดิมรองรับแล้ว:

- `ApprovalDelegation` มีผู้อนุมัติหลัก ผู้รับมอบหมาย `StartDate`, `EndDate`, active flag และเหตุผล
- ป้องกันช่วงมอบหมายซ้ำซ้อนของผู้อนุมัติคนเดียวกัน
- ตอนใช้ delegation ระบบตรวจว่า delegate เป็น active user และมี `RequiredPermissionCode`
- มี audit สำหรับ create/update/cancel/apply

Gap ที่ต้องแก้ก่อนใช้กับ Fleet:

- delegation record ไม่มี `Scope` และ `RequiredPermissionCode` จึงยังแยกอำนาจ Leave กับ Fleet ไม่ได้
- API จัดการใช้เฉพาะ `LeaveApproval.Delegate` / `LeaveApprovalDelegation.Manage`
- resolver ปัจจุบันผูกกับ `LeaveRequest.StartDate` และ `ApprovalChainService` ของระบบลา

Decision:

1. ใช้ `ApprovalDelegation` เป็นแกนกลางเดียวสำหรับทุกโมดูล
2. โครงสร้างหลักคือ `DelegatorUserId`, `DelegateUserId`, `Scope`, `RequiredPermissionCode`, `StartAt`, `EndAt`, `IsActive`, `Reason` พร้อม audit metadata เดิม
3. `Scope` เป็น allowlist เช่น `LEAVE`, `FLEET`; ห้ามรับ arbitrary scope จาก client
4. `RequiredPermissionCode` ต้องเป็น active permission และต้องขึ้นต้นตรงกับ scope ที่กำหนด เช่น `FLEET` ใช้เฉพาะ `Fleet*`
5. Resolver ต้อง query ด้วย `DelegatorUserId + Scope + RequiredPermissionCode + active time range` และตรวจว่าผู้รับมอบหมายยัง active/มี permission นั้น ณ เวลาดำเนิน action
6. การมี delegation ไม่ได้ grant permission; เป็นเพียงการระบุว่าใครใช้ permission ที่ตนมีอยู่แทนใครได้
7. ใช้ management permission แยกตาม module: Fleet ใช้ `FleetDelegation.Manage`; Leave ใช้ permission เดิม
8. เพิ่ม tests ยืนยัน scope isolation, permission mismatch, expired/future delegation, inactive delegate และ overlapping delegation

ตัวอย่าง:

```text
Scope = LEAVE
RequiredPermissionCode = LeaveApproval.ApproveCurrentStep

Scope = FLEET
RequiredPermissionCode = FleetDirector.Approve
```

หมายเหตุ: ตัวอย่าง Leave ใช้ permission code ที่มีจริงใน HOP ปัจจุบัน (`LeaveApproval.ApproveCurrentStep`) แทน `LeaveRequest.Approve`

Migration ต้อง backward-compatible: เพิ่มคอลัมน์ใหม่แบบ nullable ก่อน, backfill delegation เดิมเป็น `Scope=LEAVE` และ permission เดิม, แปลงช่วง `DateOnly` เดิมเป็น UTC interval ตาม `Asia/Bangkok`, จากนั้นจึงเพิ่ม not-null/check/index constraints และปรับ resolver/API ก่อนถอดคอลัมน์เดิมใน migration ภายหลัง

Delivery boundary: Milestone 2 ใช้ UI/API ของ Requester/Dispatcher เท่านั้นและไม่ integrate delegation. Milestone 3 จะแยก Leave/Fleet UI และ API routing แต่ใช้ core service/table กลางตาม decision นี้

## Open items ที่ยังต้องออกแบบใน milestone ถัดไป

- policy สำหรับ delegation ที่คร่อมช่วงเวลาเดินทาง: ตรวจ ณ เวลาที่ดำเนิน approval โดยใช้ half-open UTC interval `[StartAt, EndAt)`
- emergency ambulance requirements สำหรับ Phase 2.1
- compatibility matrix และ substitute-vehicle configuration สำหรับ Phase 2.1
