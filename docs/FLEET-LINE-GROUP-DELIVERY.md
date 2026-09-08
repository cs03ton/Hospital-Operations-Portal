# Fleet LINE Group Delivery Queue

M3 เพิ่ม parallel delivery path โดยไม่แก้ USER `LineMessagingService`, `LineRetryWorker`, `line_delivery_logs` หรือ `notification_deliveries`

## Isolation

`FleetLineGroupDeliveryWorker` ทำงานหลัง Fleet transaction commit และอ่านเฉพาะ `domain_events` ที่มี `outbox_messages` แล้ว Group enqueue/send failure จึงไม่ rollback Fleet และไม่เปลี่ยน USER delivery status

status histories จาก requester/dispatcher flow ที่ยังไม่มี domain event (`RequestSubmitted`, `VehicleAssigned`, `RequestCancelled`) ถูก project แบบ asynchronous ด้วย `history.Id` เป็น deterministic EventId และ outbox ที่ไม่มี USER deliveries ทำให้ behavior USER เดิมไม่เปลี่ยน

## Routing and idempotency

สร้าง delivery เฉพาะเมื่อครบทุกเงื่อนไข:

- feature flag enabled
- event scope และ aggregate เป็น `FLEET`/`FleetRequest`
- mapper ให้ canonical event exact match
- destination `Module=FLEET`, `Active`, confirmed ก่อน event
- subscription canonical event เปิดอยู่

deduplication key คือ `EventId:GroupDestinationId:CanonicalEventType` พร้อม unique indexes ทั้ง key และสามคอลัมน์

## Retry policy

- transient: timeout/network, HTTP 408, 429, 5xx → exponential retry ถึง `MaxAttempts`
- permanent: 4xx อื่น → Failed และ Attention Required
- 403/404/410 → Failed, Attention Required และ disable destination
- Sent delivery ไม่ถูกเลือกซ้ำ
- error message ถูก sanitize และไม่เก็บ LINE response body

## Delivery log

`line_group_delivery_logs` เก็บ source/canonical event, masked destinationผ่าน API, request, attempt, sent/failed timestamps, sanitized error, correlation ID และ rendered text snapshot เพื่อ retry ข้อความเดิม

Admin endpoints เพิ่ม test send และ paged delivery logs ที่ `/api/fleet/line-groups/{id}/test` และ `/api/fleet/line-groups/{id}/deliveries`
