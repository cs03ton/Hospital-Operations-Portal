# Leave Request Workflow

## Status

| Status | ความหมาย | ผู้ขอแก้ไขข้อมูล/ไฟล์แนบ | ผู้อนุมัติดำเนินการ |
|---|---|---:|---:|
| Draft | แบบร่าง ยังไม่ส่งอนุมัติ | ได้ | ไม่เกี่ยวข้อง |
| Pending | ส่งเข้าสู่สายอนุมัติแล้ว | ไม่ได้ | เฉพาะ current approver |
| ReturnedForRevision | ตีกลับรอแก้ไข | ได้ | รอผู้ขอส่งใหม่ |
| Approved | อนุมัติครบทุกขั้น | ไม่ได้ | ไม่ได้ |
| Rejected | ไม่อนุมัติ | ไม่ได้ | ไม่ได้ |
| Cancelled | ยกเลิกคำขอ | ไม่ได้ | ไม่ได้ |

## Return for Revision

ผู้อนุมัติที่เป็น current approver สามารถตีกลับคำขอได้ผ่าน `POST /api/leave-requests/{id}/return-for-revision` โดยต้องระบุเหตุผลเสมอ

ผลลัพธ์:

1. `leave_requests.status = ReturnedForRevision`
2. `current_approver_id = null`
3. บันทึก `revision_reason`, `revision_count`, `returned_for_revision_at`
4. ขั้นอนุมัติปัจจุบันเป็น `ReturnedForRevision`
5. คืน pending balance ชั่วคราว
6. แจ้งผู้ขอผ่าน Notification และ LINE ถ้าตั้งค่าไว้

## Resubmit

ผู้ขอส่งคำขอใหม่ผ่าน `POST /api/leave-requests/{id}/resubmit`

ระบบจะกลับไปยัง approval step เดิมที่ตีกลับ ไม่เริ่มสายอนุมัติใหม่ทั้งหมด

ข้อกำหนดสิทธิ์:

1. ส่งคำขอใหม่ได้เฉพาะผู้ขอคำขอเท่านั้น
2. ผู้อนุมัติ, Admin, หรือ SuperAdmin ไม่สามารถส่งคำขอใหม่แทนผู้ขอผ่าน normal workflow
3. ถ้าผู้ใช้อื่นพยายามส่งใหม่ ระบบคืนข้อความ “เฉพาะผู้ขอคำขอเท่านั้นที่สามารถส่งคำขอใหม่ได้”

## Cancel Returned Request

ผู้ขอสามารถยกเลิกคำขอที่ถูกตีกลับได้ ระบบจะเปลี่ยนสถานะเป็น `Cancelled` และปิด revision state
