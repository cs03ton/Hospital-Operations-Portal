# HOP Fleet Trip Participant Model

## Source of truth

`fleet_request_passengers` คือรายชื่อที่ร้องขอก่อนเดินทางและไม่ถือเป็นหลักฐานว่าบุคคลนั้นเดินทางจริง

`fleet_trip_participants` คือ source of truth ของผู้เดินทางจริง โดยผูกกับ `fleet_trip_records` และยืนยันโดยคนขับที่ได้รับมอบหมายขณะ Trip อยู่สถานะ `IN_PROGRESS`

## Fields

| Field | Meaning |
| --- | --- |
| `trip_id` | Trip จริงที่ผู้ใช้ร่วมเดินทาง |
| `user_id` | User/Employee ภายใน HOP; nullable สำหรับ external ในอนาคต |
| `is_requester` | คำนวณจาก Requester ของคำขอ ห้ามรับค่าจาก client |
| `participant_type` | `EMPLOYEE` หรือ `EXTERNAL` |
| `is_actual_participant` | ระบุว่าเดินทางจริง |
| `created_by_user_id` | คนขับที่ยืนยันรายชื่อ |

Employee คนเดิมมีได้สูงสุดหนึ่งรายการต่อ Trip ด้วย unique index `(trip_id, user_id)` ผู้ขอจะนับเป็น participant เฉพาะเมื่ออยู่ในตารางนี้เท่านั้น และ driver ของ Trip ห้ามถูกเพิ่มเป็น passenger

ไม่มี production backfill จาก requested passenger เพราะระบบไม่สามารถยืนยันย้อนหลังได้ว่าเดินทางจริง
