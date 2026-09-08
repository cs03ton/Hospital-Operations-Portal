# HOP Vehicle As-Is and Gap Analysis

## กระบวนการปัจจุบัน

ผู้ขอกรอกเอกสารและส่งงานยานพาหนะ งานยานพาหนะเลือกรถ/คนขับ หัวหน้าฝ่ายบริหาร review ผู้อำนวยการอนุมัติ จากนั้นแจ้งคนขับและคนขับปิดงาน กระบวนการยังเป็น manual และไม่มี conflict control แบบ transaction

## Architecture ที่พบ

- Backend เป็น .NET 9 Web API project เดียว แบ่ง Controllers, DTOs, Models, Services, Interfaces, Authorization, Middleware และ Data
- EF Core + PostgreSQL ใช้ migrations เป็น source of truth และตั้งชื่อตาราง/คอลัมน์แบบ snake_case
- ไม่มี `Employee` หรือ `Position` entity แยก ข้อมูลบุคลากรอยู่ใน `User`; `Position` เป็น string และ `Department` เป็น entity
- Authorization ใช้ permission code แบบ PascalCase เช่น `LeaveRequest.ViewOwn` ผ่าน dynamic policies
- Workflow ลาเก็บสถานะเป็น string และ approval rows; ยังไม่มี state-machine abstraction กลาง
- Audit มี actor/action/entity/detail/IP/result แต่ยังไม่มี structured old/new/reason/correlation/user-agent
- In-App notification ใช้ตารางกลาง แต่ LINE delivery log มี FK เฉพาะ leave request จึงต้อง generalize ก่อน Fleet notification
- Request number ของระบบลาอ่านเลขล่าสุดแล้วบวกหนึ่ง จึงยังเสี่ยง concurrent duplicate
- Frontend ใช้ React Router, MUI, React Query, React Hook Form/Yup, permission guards, common loading/error/empty/dialog/table components และรูปแบบ `DD/MM/YYYY HH:mm`
- Frontend มี lint/build/E2E แต่ไม่มี unit-test script แยก

## Reuse

- `User`, `Department`, Role/Permission และ `ApprovalDelegation` เดิม ซึ่งจะขยายเป็นแกนกลางแบบ module-scoped โดยไม่สร้าง delegation table ซ้ำ
- `RequirePermission`, permission middleware, correlation ID และ global exception handling
- Audit service (ชั่วคราว; ต้องขยาย schema ใน milestone notification/audit)
- Notification UI, LINE queue/retry pattern และ file scanning/storage
- Common UI, responsive MUI layout, timeline, date formatting และ API client
- EF migration/runbook, backend xUnit และ Playwright patterns

## Gap / Refactor

| Area | Gap | แนวทาง |
|---|---|---|
| Personnel | ไม่มี Employee entity | อ้าง User ใน Phase 2.0; วาง migration path หากแยกภายหลัง |
| Concurrency | ไม่มี token มาตรฐาน | เพิ่ม concurrency token ใน Fleet transaction และ conditional update |
| Audit | payload ไม่ครบ requirement | เพิ่ม structured metadata แบบ additive |
| LINE | delivery log ผูก leave | generalize reference entity/id แบบ backward-compatible |
| Numbering | query-max มี race | ใช้ PostgreSQL sequence/unique retry |
| Workflow | transition กระจายใน controller | สร้าง Fleet state machine/service |
| Time | semantics ยังไม่เป็นมาตรฐานชัด | UTC storage + Bangkok display; ทดสอบ boundary |

## ความเสี่ยงต่อระบบเดิม

- ห้าม rename/drop ตารางเดิมหรือแก้ leave relationship
- migration ต้อง additive และทดสอบกับ snapshot/ฐานใหม่และสำเนา production
- seeder ปัจจุบันทำงานหลัง migrations เท่านั้น; Fleet seed ต้อง idempotent และไม่สร้าง user/vehicle จริง
- ไม่เปิด navigation จน permission/API/use case พร้อม เพื่อลด broken route
