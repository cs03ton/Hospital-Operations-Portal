# HOP Vehicle Test Scenarios

## Unit

- state transition ทุกเส้นทางและ invalid transition
- interval overlap boundary; vehicle capacity/type; driver capability/license/leave/unavailability
- passenger count/date/mileage/urgent reason validation
- permission + ownership decisions; notification recipient selection

## Integration

- active/inactive requester; incomplete/double/concurrent submit
- concurrent vehicle/driver assignment และ partial unique constraint
- approved leave excludes driver; maintenance excludes vehicle
- review/director permission, return/reject/cancel/reassign history
- driver-only acknowledge/start/complete; override requires reason
- mileage cannot decrease; completion atomically updates request/assignment/vehicle
- notification failure does not roll back transaction; audit includes denied action
- migration up on empty DB and production-like snapshot; seed idempotency

## Frontend / E2E

- Thai labels, DD/MM/YYYY และ 24-hour time
- mobile driver journey, loading/error/empty states, confirm dialogs, double-click prevention
- permission-hidden menu/button plus backend 403 verification
- copy request requires passenger reconfirmation
- accessibility keyboard/focus/labels and responsive layouts

แต่ละ business rule ต้อง trace ไป test ID รูปแบบ `VH-BR-###-*` เมื่อเริ่ม implementation milestone ที่เกี่ยวข้อง
