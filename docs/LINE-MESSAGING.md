# LINE Messaging

## Configuration

```text
Line__Enabled=true
Line__ChannelId=
Line__AccessToken=<LINE channel access token>
Line__ChannelAccessToken=<LINE channel access token>
Line__ChannelSecret=<LINE channel secret>
Line__TestUserId=<LINE user id for test send>
Line__Endpoint=https://api.line.me/v2/bot/message/push
```

Aliases in `.env`:

```text
LINE_ENABLED=true
LINE_CHANNEL_ID=
LINE_ACCESS_TOKEN=<LINE channel access token>
LINE_CHANNEL_ACCESS_TOKEN=<LINE channel access token>
LINE_CHANNEL_SECRET=<LINE channel secret>
LINE_TEST_USER_ID=<LINE user id for test send>
LINE_ENDPOINT=https://api.line.me/v2/bot/message/push
```

Do not commit real LINE tokens or secrets. Store them in `.env`, server environment variables, Docker secrets, or a secret manager.

## Behavior

- Leave submit, approve, reject, and cancel create delivery records.
- If LINE is disabled, delivery status is `Disabled`.
- If token or user `line_user_id` is missing, delivery status is `Failed`.
- If LINE API succeeds, delivery status is `Sent`.
- If LINE API fails, delivery status is `Failed` with `next_retry_at`.

## Delivery Table

```text
line_delivery_logs
```

Important fields:

- `leave_request_id`
- `recipient_user_id`
- `event_name`
- `status`
- `payload`
- `response_detail`
- `attempt_count`
- `next_retry_at`
- `sent_at`

## Test

### LINE Operations Center

หน้า `Admin > ตั้งค่า LINE` เป็น LINE Operations Center สำหรับตรวจสอบระบบ ไม่ใช่หน้าสำหรับแก้ไข secret/token

ใช้สำหรับ:

- Monitor สถานะ LINE Messaging API
- Diagnose การโหลดค่า config และ endpoint
- Test ส่งข้อความ
- Audit delivery log และ test history
- จำลอง notification event โดยไม่ต้องสร้างคำขอลาจริง

ข้อกำหนดด้านความปลอดภัย:

- ห้ามแสดง Channel Secret แบบเต็ม
- ห้ามแสดง Channel Access Token แบบเต็ม
- ห้ามแก้ไข token ผ่านหน้าเว็บ
- ให้ตั้งค่าผ่าน environment variables, Docker secret, หรือ secret manager เท่านั้น

Endpoints ที่ใช้:

```http
GET  /api/admin/line/operations-status
POST /api/admin/line/validate
POST /api/admin/line/test-send
GET  /api/admin/line/test-history
GET  /api/admin/line/delivery-logs
POST /api/admin/line/simulate
POST /api/line/webhook
```

ดูรายละเอียดการผูกบัญชีผู้ใช้กับ LINE OA ได้ที่ [LINE-USER-BINDING.md](LINE-USER-BINDING.md)

### Diagnose HTTP 400: Failed to send messages

ถ้า LINE ตอบกลับ:

```json
{"message":"Failed to send messages"}
```

ให้ตรวจตามลำดับ:

1. `LINE User ID` ต้องขึ้นต้นด้วย `U` และเป็น userId ของ LINE OA นี้
2. ผู้รับต้องเพิ่มเพื่อน LINE OA แล้ว
3. Channel Access Token ต้องเป็นของ OA เดียวกับ user ที่เพิ่มเพื่อน
4. ทดสอบ `Send Plain Text Test` ก่อน
5. ถ้า plain text ผ่าน ให้ทดสอบ `Send Minimal Flex Test`
6. ถ้า minimal flex ผ่าน แต่ full flex fail ให้ดู validation และ payload preview:
   - `altText` ต้องไม่ว่าง
   - `contents.type` ต้องเป็น `bubble`
   - action URI ต้องเป็น HTTPS public URL
   - image URL ต้องเป็น HTTPS public URL หรือไม่ส่ง image component

หน้า LINE Operations Center จะแสดง:

- request type: `text` / `flex`
- recipient แบบ masked
- sanitized payload preview
- LINE HTTP status code
- LINE response body

### Test Send API

Endpoint:

```http
POST /api/admin/line/test-send
```

Request:

```json
{
  "toUserId": "Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "message": "ทดสอบการแจ้งเตือนจาก HOP"
}
```

Permission:

- `System.Line.TestSend`
- or `SystemSettings.View`

### Web UI

Open:

```text
Admin > ตั้งค่า LINE
```

The page is read-only for secrets. It shows whether Channel Secret and Channel Access Token are configured, but never displays the secret values.

### Real Test Steps

1. Set `LINE_ENABLED=true`.
2. Set `LINE_ACCESS_TOKEN`, `Line__AccessToken`, or `Line__ChannelAccessToken`.
3. Set `LINE_CHANNEL_SECRET`.
4. Set `LINE_TEST_USER_ID`.
5. Restart backend.
6. Open `Admin > ตั้งค่า LINE`.
7. Click `ส่งข้อความทดสอบ`.
8. Confirm LINE OA receives the message.
9. Add `line_user_id` to real target users.
10. Submit a leave request.
11. Confirm the current approver receives LINE.
12. Approve or reject.
13. Confirm requester receives LINE.
14. Inspect `line_delivery_logs`.

## Security Checklist

- Never log token values.
- Never return token values to frontend.
- Never commit `.env` or real tokens.
- Rotate any token that was accidentally committed.
