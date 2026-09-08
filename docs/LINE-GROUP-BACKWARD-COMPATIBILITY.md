# LINE Group Backward Compatibility

## Backward Compatibility Plan

LINE Group เป็นเส้นทาง additive ขนานกับ LINE USER เดิมเท่านั้น `LineConfigurationResolver`, `ILineMessagingService`, `LineMessagingService`, `LineRetryWorker`, `line_delivery_logs` และ USER `notification_deliveries` คง public/schema contract เดิม

- USER webhook ใช้ handler/binding เดิม
- GROUP webhook เขียน `line_webhook_inbox` และใช้ worker แยก
- GROUP delivery ใน M3 ต้องใช้ `line_group_delivery_logs` แยก ไม่เพิ่ม nullable destination ลง USER delivery
- migration M1 สร้างเฉพาะสามตารางใหม่ ไม่มี alter/drop/backfill
- Leave events ไม่อยู่ใน canonical Fleet event allowlist และไม่สร้าง group delivery

## Leave Notification Regression Matrix

| Scenario | Expected |
|---|---|
| Leave notification publish | สร้าง USER delivery/log เหมือนเดิม |
| Leave event ขณะมีกลุ่ม Active | ไม่สร้าง GROUP delivery |
| USER webhook follow/message/unfollow | เรียก `ILineUserBindingService` เดิม |
| USER retry | `LineRetryWorker`/`RetryPendingDeliveriesAsync` ทำงานเดิม |
| Group worker failure | Leave USER delivery ยังทำงาน |
| ไม่มี group destination | behavior เท่าก่อน migration |
| feature flag false | ไม่ enqueue/register/deliver GROUP; USER ไม่เปลี่ยน |

## Old vs New Delivery Path

```text
USER (unchanged)
Event -> existing user recipient -> line_delivery_logs / USER notification_deliveries
      -> LineMessagingService -> LineRetryWorker -> LINE userId

GROUP (additive)
Fleet canonical event -> active FLEET destination + exact subscription
      -> line_group_delivery_logs (M3) -> group worker -> shared low-level LINE HTTP -> groupId
```

สอง path มี status, idempotency และ retry แยกกัน GROUP key คือ `EventId + GroupDestinationId + CanonicalEventType`; USER key/unique constraintเดิมห้ามเปลี่ยน

## Feature Flag Rollout Plan

`LineGroupNotifications:Enabled` มีค่า default และ production เริ่มต้นเป็น `false`

1. apply additive migration ขณะ flag false
2. ตรวจ USER/Leave regression และ webhook signature
3. เปิด flag ใน controlled environment เพื่อค้นพบกลุ่ม; กลุ่มยัง Pending
4. Fleet Admin ยืนยัน destination และ subscriptions
5. หลัง M2/M3 ผ่าน UAT จึงเปิด production แบบจำกัดกลุ่ม
6. rollback ทางปฏิบัติให้ปิด flagทันที; database rollback ลบเฉพาะตาราง/indexใหม่

ไม่มี wildcard subscription: ต้อง match `Module=FLEET`, destination `Active` และ canonical event exact matchเท่านั้น
