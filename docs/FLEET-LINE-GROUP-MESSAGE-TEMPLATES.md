# Fleet LINE Group Message Templates

M2 เพิ่ม `IFleetLineGroupEventMapper` และ `IFleetLineGroupMessageTemplateService` เป็น group-only services ไม่ได้แก้ USER notification template หรือ `ILineMessagingService`

## Canonical mapping

Mapper ตรวจ `Scope=FLEET` และ source event แบบ exact matchเท่านั้น ไม่มี wildcard และ Leave/unknown events คืน `null`

- `Fleet.VehicleAssigned`/`Fleet.Assigned` → `Fleet.AssignmentCreated`
- `Fleet.AdminReviewApproved` → `Fleet.AdminReviewed`
- `Fleet.RequestReturned` → `Fleet.Returned`
- `Fleet.RequestRejected` → `Fleet.Rejected`
- `Fleet.RequestCancelled`/`Fleet.CancellationApproved` → `Fleet.Cancelled`
- `Fleet.AssignmentReplaced` → `Fleet.AssignmentChanged`
- `Fleet.DriverAccepted` → `Fleet.DriverAcknowledged`
- event ที่ชื่อ canonical อยู่แล้วคงชื่อเดิม

## Text contract

ทุกข้อความมีเลขคำขอ ผู้ขอ หน่วยงาน เวลา Asia/Bangkok ปลายทาง จำนวนผู้ร่วมเดินทาง สถานะ และ authenticated Fleet detail link รถ/คนขับแสดงเมื่อ eventเกี่ยวข้องและมี fallback “ยังไม่ระบุ” AssignmentChanged แสดงเดิม/ใหม่จาก immutable assignment IDs ใน event payload

interface คืน `Format=text` เพื่อให้เพิ่ม Flex renderer ภายหลังโดยไม่เปลี่ยน routing/delivery contract

## Privacy

Template ไม่ query/แสดงข้อมูลผู้ป่วย สุขภาพ ใบขับขี่ driver note เหตุผลการลา เบอร์ผู้ประสานงาน หรือเหตุผล workflow ตัวเลขลักษณะเลขประจำตัว/เบอร์ยาวในข้อความ business fields ถูก mask และข้อความจำกัดความยาว

M2 ยังไม่สร้างหรือส่ง group delivery การเลือก destination, idempotency, retry และ permanent failure policy เป็น M3
