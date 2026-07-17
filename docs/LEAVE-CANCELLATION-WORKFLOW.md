# Leave Cancellation Request Workflow

เอกสารนี้อธิบายกระบวนการ “คำขอยกเลิกใบลา” สำหรับใบลาที่ได้รับอนุมัติแล้ว และต้องการยกเลิกใบลาพร้อมคืนยอดวันลา

## แนวคิดหลัก

คำขอยกเลิกใบลาเป็นเอกสารใหม่แยกจากใบลาเดิม

- ใบลาเดิมต้องมีสถานะ `Approved`
- ระบบไม่คืนยอดวันลาทันทีตอนสร้างคำขอ
- ระบบใช้ approval chain เดิมของผู้ขอ
- เมื่ออนุมัติครบทุกขั้น ระบบเปลี่ยนสถานะใบลาเดิมเป็น `CancelledAfterApproval`
- ระบบคืนยอดวันลาผ่าน ledger `leave_balance_transactions`
- เลขที่เอกสารใช้รูปแบบ `LVC-YYYYMM-###`

## เงื่อนไขการสร้างคำขอ

ผู้ใช้สร้างคำขอยกเลิกใบลาได้เมื่อ:

1. ใบลาเดิมเป็นของตนเอง
2. ใบลาเดิมมีสถานะ `Approved`
3. ใบลาเดิมยังไม่ถูกยกเลิกหลังอนุมัติ
4. ไม่มีคำขอยกเลิกใบลาเดิมที่ยังเป็น `Draft`, `Pending` หรือ `ReturnedForRevision`
5. ประเภทลานั้นใช้ leave balance

## Approval Workflow

1. ผู้ขอสร้างคำขอยกเลิกใบลา
2. ผู้ขอกดส่งอนุมัติ
3. ระบบสร้าง approval steps จาก approval chain เดิม
4. ผู้อนุมัติแต่ละขั้นอนุมัติ / ไม่อนุมัติ / ตีกลับรอแก้ไข
5. เมื่อ final approve ระบบยกเลิกใบลาเดิมและคืนยอดวันลา

ผู้อนุมัติทำรายการได้เฉพาะ step ที่ตนเองเป็น current approver และระบบยังบล็อก self approval

## Balance Restoration

เมื่อ final approve:

```text
usedDays = usedDays - originalLeaveDays
```

ระบบบันทึก transaction:

| Field | Value |
|---|---|
| transaction_type | `LeaveCancellationRestore` |
| reference_type | `LeaveCancellationRequest` |
| reference_id | cancellation request id |
| amount_days | จำนวนวันของใบลาเดิม |

มี unique protection จาก `reference_type + reference_id + transaction_type` เพื่อกันคืนยอดซ้ำ

## Status

| Status | ความหมาย |
|---|---|
| Draft | แบบร่าง |
| Pending | รออนุมัติ |
| ReturnedForRevision | ตีกลับรอแก้ไข |
| Approved | อนุมัติยกเลิกใบลาแล้ว |
| Rejected | ไม่อนุมัติ |
| Cancelled | ยกเลิกคำขอ |

## Permission

| Permission | ใช้สำหรับ |
|---|---|
| `LeaveCancellation.ViewOwn` | ดูคำขอยกเลิกใบลาของตนเอง |
| `LeaveCancellation.Create` | สร้างคำขอยกเลิกใบลา |
| `LeaveCancellation.Submit` | ส่งคำขอยกเลิกใบลา |
| `LeaveCancellation.CancelOwn` | ยกเลิกคำขอของตนเอง |
| `LeaveCancellation.ApproveCurrentStep` | อนุมัติ step ปัจจุบัน |
| `LeaveCancellation.ViewDepartment` | ดูคำขอยกเลิกใบลาในหน่วยงาน |
| `LeaveCancellation.ViewAll` | ดูคำขอยกเลิกใบลาทั้งหมด |
| `LeaveCancellation.Manage` | ดูแล/monitor งานยกเลิกใบลา |

Admin และ SuperAdmin ไม่มี `LeaveCancellation.Create` โดย default เพื่อไม่ให้สร้างคำขอยกเลิกแทนผู้ใช้งานแบบไม่ชัดเจน

## API

| Method | Endpoint |
|---|---|
| GET | `/api/leave-cancellation-requests` |
| GET | `/api/leave-cancellation-requests/{id}` |
| GET | `/api/leave-cancellation-requests/{id}/approvals` |
| GET | `/api/leave-cancellation-requests/eligibility/{leaveRequestId}` |
| POST | `/api/leave-cancellation-requests` |
| PUT | `/api/leave-cancellation-requests/{id}` |
| POST | `/api/leave-cancellation-requests/{id}/submit` |
| POST | `/api/leave-cancellation-requests/{id}/approve` |
| POST | `/api/leave-cancellation-requests/{id}/reject` |
| POST | `/api/leave-cancellation-requests/{id}/return-for-revision` |
| POST | `/api/leave-cancellation-requests/{id}/cancel` |

## Frontend

Route ที่เพิ่ม:

- `/leave/cancellations`
- `/leave/cancellations/create?leaveRequestId={leaveRequestId}`
- `/leave/cancellations/{id}`

หน้ารายการ `คำขอยกเลิกใบลา` ใช้ UI pattern เดียวกับ `รายการคำขอลา`:

- Card layout พร้อม gold accent
- ตารางพร้อม paging และจำนวนรายการต่อหน้า
- ปุ่ม action ขนาดและตำแหน่งสม่ำเสมอ
- Filter toolbar ภาษาไทย

ตัวกรองที่รองรับ:

| ตัวกรอง | Query |
|---|---|
| ประเภทลา | `leaveTypeId` |
| สถานะคำขอ | `status` |
| ขอบเขตรายการ | `scope` |
| ผู้ขอลา | `requesterId` หรือ legacy `userId` |
| ตั้งแต่วันที่ | `fromDate` |
| ถึงวันที่ | `toDate` |

หน้าสร้างคำขอยกเลิกใบลาให้ผู้ใช้เลือกใบลาเดิมของตนเองจาก dropdown ในหน้าเดียวกัน ระบบจะแสดงเฉพาะใบลาที่อนุมัติแล้วและสามารถขอยกเลิกได้

หน้ารายละเอียดใบลาที่อนุมัติแล้วจะแสดงปุ่ม `ขอยกเลิกใบลา` เฉพาะเจ้าของคำขอและเมื่อยังมีสิทธิ์ยกเลิก

## Audit Events

- `LeaveCancellation.Created`
- `LeaveCancellation.Updated`
- `LeaveCancellation.Submitted`
- `LeaveCancellation.Approved`
- `LeaveCancellation.Rejected`
- `LeaveCancellation.Cancelled`
- `LeaveCancellation.Returned`
- `LeaveCancellation.Completed`
- `LeaveCancellation.BalanceRestored`
- `LeaveCancellation.DuplicateBlocked`

## วิธีทดสอบ

1. Login ด้วย `staff01`
2. เปิดใบลาที่อนุมัติแล้ว
3. กด `ขอยกเลิกใบลา`
4. ระบุเหตุผลและส่งอนุมัติ
5. Login ด้วยผู้อนุมัติปัจจุบัน
6. อนุมัติครบทุกขั้น
7. ตรวจว่าใบลาเดิมเปลี่ยนเป็น `CancelledAfterApproval`
8. ตรวจว่า `usedDays` ลดลงตามจำนวนวันลาเดิม
9. ตรวจว่า ledger มี `LeaveCancellationRestore`
10. ทดลอง approve ซ้ำหรือสร้างคำขอซ้ำ ต้องไม่คืนยอดซ้ำ

## Dashboard และ KPI

ระบบนับคำขอยกเลิกใบลาแยกจากคำขอลาปกติ เพื่อไม่ให้ KPI ปะปนกัน

| KPI | ความหมาย |
|---|---|
| จำนวนคำขอยกเลิกทั้งหมด | จำนวน LVC ตามขอบเขตสิทธิ์ของผู้ใช้ |
| รออนุมัติ | คำขอยกเลิกใบลาที่อยู่ในขั้นตอนอนุมัติ |
| อนุมัติแล้ว | คำขอยกเลิกใบลาที่อนุมัติครบทุกขั้นแล้ว |
| ไม่อนุมัติ | คำขอยกเลิกใบลาที่ถูกปฏิเสธ |
| ยกเลิกคำขอ | คำขอยกเลิกใบลาที่ผู้ขอยกเลิกเองก่อนจบ workflow |
| ตีกลับรอแก้ไข | คำขอที่ผู้อนุมัติตีกลับให้แก้ไข |
| จำนวนวันลาที่คืน | วันลาที่คืนยอดหลังคำขอยกเลิกอนุมัติครบทุกขั้น |
| Average Approval Time | เวลาเฉลี่ยตั้งแต่ส่งคำขอจนอนุมัติหรือไม่อนุมัติ |
| Approval / Rejection Rate | อัตราอนุมัติและไม่อนุมัติจากคำขอที่ตัดสินแล้ว |

Dashboard แสดงข้อมูลตามสิทธิ์:

- ผู้ขอเห็นคำขอยกเลิกใบลาของตนเอง
- หัวหน้าหน่วยงานเห็นคำขอของตนเองและของหน่วยงานตามสิทธิ์
- ผู้อำนวยการเห็นงานที่ถึงคิวอนุมัติและข้อมูลตามสิทธิ์ dashboard
- Admin/SuperAdmin เห็นภาพรวมตามสิทธิ์ `LeaveCancellation.ViewAll` หรือ `LeaveCancellation.Manage`

ตำแหน่งบน Dashboard ล่าสุด:

| Role | ตำแหน่ง |
|---|---|
| Staff | แสดง `คำขอยกเลิกใบลา` เป็น KPI/summary แยกจาก `คำขอลาของฉัน` |
| Department Head | แสดงหลังข้อมูลคำขอส่วนตัว ก่อนข้อมูลทีม |
| Director | แสดงหลัง `คำขอลาของฉัน` แล้วจึงตามด้วย widget เชิงบริหาร |
| Admin/SuperAdmin | แสดงหลัง `คำขอลาของฉัน` เพื่อ support/monitor ก่อน widget จัดการระบบ |

## Reports และ Monitoring

หน้ารายงานการลาเพิ่มส่วน “รายงานคำขอยกเลิกใบลา” เพื่อแสดงรายการล่าสุด, ใบลาเดิม, ประเภทลา, จำนวนวันที่คืนยอด, สถานะ และผู้อนุมัติปัจจุบัน พร้อมปุ่ม `ดูทั้งหมด` ไปยังหน้ารายการคำขอยกเลิกใบลา

Health Center ตรวจสถานะที่เกี่ยวข้องกับคำขอยกเลิกใบลา:

- Leave Cancellation Queue: จำนวนคำขอยกเลิกที่รออนุมัติ
- Failed Notification: การส่ง LINE/Event ที่เกี่ยวกับคำขอยกเลิกใบลาล้มเหลว
- Reference Integrity: ตรวจว่าทุก LVC อ้างอิง LV เดิมได้ถูกต้อง
- Failed Balance Restore: ตรวจคำขอยกเลิกที่อนุมัติแล้วแต่ยังไม่มี `balance_restored_at`

## Reference Banner

เมื่อใบลาเดิมถูกยกเลิกหลังอนุมัติ สถานะใบลาเดิมจะเป็น `CancelledAfterApproval`
หน้ารายละเอียดใบลาเดิมต้องแสดง banner:

> ใบลานี้ถูกยกเลิกหลังอนุมัติแล้ว อ้างอิงคำขอยกเลิกใบลา LVC-xxxx

ผู้ใช้สามารถกดเปิดคำขอยกเลิกใบลาเพื่อดู workflow และเหตุผลย้อนหลังได้

## Terminology

ใช้คำว่า “คำขอยกเลิกใบลา” สำหรับเอกสาร LVC ทุกหน้า
ใช้คำว่า “คืนวันลา” เฉพาะผลลัพธ์หลังคำขอยกเลิกได้รับการอนุมัติครบทุกขั้น เช่น “คืนวันลาสำเร็จ”

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
