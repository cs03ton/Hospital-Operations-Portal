# HOP Vehicle Data Dictionary

## Identity Decision

ชื่อเชิงธุรกิจ `EmployeeId` map ไปยัง `users.id` ใน Phase 2.0 เพราะ schema ปัจจุบันไม่มี employee table. ชื่อ property ใน implementation ใช้ `UserId` เพื่อไม่สร้าง FK ที่ทำให้เข้าใจผิด; API สามารถใช้ `employeeId` เฉพาะ response ที่มีคำอธิบายชัดเจน

Decision นี้ปิดแล้ว: Phase 2.0 จะไม่สร้าง `Employee` table แยก

## Master Tables

- `fleet_vehicle_types`: code unique, name, description, sort_order, is_active, audit timestamps
- `fleet_vehicles`: vehicle_code/registration_number unique, type FK, capacity, fuel, mileage, owning department FK, responsible user FK, status, active, metadata
- `fleet_driver_profiles`: user_id unique, license data, capabilities, driver_status, active, note, metadata
- `fleet_vehicle_unavailability`: vehicle FK, UTC start/end, type/reason, creator
- `fleet_driver_unavailability`: user FK, UTC start/end, reason, creator

## Transaction Tables (planned milestones)

- `fleet_requests`: requester user/department snapshot, mission fields, interval, count, status, return target, concurrency token
- `fleet_request_passengers`: employee user nullable + external snapshot fields
- `fleet_assignments`: request/vehicle/driver, history link, status, active flag
- `fleet_workflow_actions`: action history and actual actor
- `approval_delegations` (shared core): ขยายด้วย `delegator_user_id`, `delegate_user_id`, `scope`, `required_permission_code`, UTC `start_at/end_at`, reason และ active/cancel metadata; Fleet ใช้เฉพาะ `scope=FLEET`
- `fleet_trip_records`: actual interval, mileage/fuel/incident/override metadata

## Constraints

- UTC `timestamptz`; money `numeric(12,2)`; fuel `numeric(10,2)`; mileage non-negative
- end time > start time; capacities > 0; manufacture year bounded by validation
- partial unique active assignment per request
- indexes on interval lookup dimensions, status, request number และ active flags
- master deactivate ด้วย `is_active`; transaction ไม่มี hard delete
- request number รูปแบบ `VH-yyyyMM-####` และต้องออกเลขแบบ concurrency-safe
- license number response ต้อง masked เว้นแต่ caller มี `FleetDriver.Manage`

Delegation indexes/constraints ที่ต้องมี: index สำหรับ `(scope, required_permission_code, delegator_user_id, start_at, end_at)`, check `end_at > start_at`, scope allowlist และ overlap protection ภายใน delegator/scope/permission เดียวกัน
