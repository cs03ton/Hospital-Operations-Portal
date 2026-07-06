# LINE User Binding

เอกสารนี้อธิบายการผูกบัญชี LINE OA กับบัญชีผู้ใช้งาน HOP โดยใช้ LINE Webhook, one-time connect token และ pairing code fallback

## เป้าหมาย

- เก็บ `lineUserId` อัตโนมัติเมื่อผู้ใช้เพิ่มเพื่อน LINE OA
- ให้ผู้ใช้ผูก LINE กับบัญชี HOP ของตัวเองอย่างปลอดภัยด้วย QR/short code
- ไม่ให้ frontend เห็น Channel Secret หรือ Channel Access Token
- รองรับการยกเลิกการเชื่อมต่อและการส่งข้อความทดสอบถึงตัวเอง

## Webhook URL

ตั้งค่าใน LINE Developers Console:

```text
https://{PUBLIC_APP_URL}/api/line/webhook
```

Production ต้องใช้ HTTPS และ URL ที่ LINE เข้าถึงได้จากอินเทอร์เน็ต

## Configuration

ตั้งค่าผ่าน environment variables หรือ secret manager:

```text
Line__Enabled=true
Line__ChannelId=2010xxxxxx
Line__ChannelSecret=<secret>
Line__AccessToken=<channel-access-token>
Line__TestUserId=<optional-test-user-id>
Line__PublicAppUrl=https://hop.example.go.th
```

ห้าม commit token หรือ secret ลง Git

## Signature Verification

Endpoint `POST /api/line/webhook` ตรวจ `X-Line-Signature` ด้วย `Line__ChannelSecret`

- Signature ถูกต้อง: ประมวลผล event และตอบ `200`
- Signature ไม่ถูกต้อง: ตอบ `401`
- ไม่มี Channel Secret: ตอบ `401`

ระบบไม่ log secret หรือ access token

## Event ที่รองรับ

### follow

เมื่อผู้ใช้เพิ่มเพื่อน LINE OA:

- อ่าน `source.userId`
- ดึง profile จาก LINE ถ้ามี access token
- บันทึกลง `line_user_bindings`
- ตั้งสถานะ `Pending`
- บันทึก audit event `Line.UserFollowed`

### unfollow

เมื่อผู้ใช้ block/unfollow:

- ตั้งสถานะ `Unbound`
- clear `users.line_user_id`
- บันทึก audit event `Line.UserUnbound`

### message

ใช้สำหรับรับ short code เช่น:

```text
HOP-482913
```

ระบบจะตรวจ `line_connect_tokens` ก่อน แล้ว fallback ไป `line_pairing_codes` เดิม ถ้า code ถูกต้องและยังไม่หมดอายุ ระบบจะผูก LINE user กับบัญชี HOP

## User Flow

1. ผู้ใช้เข้าเมนู `ข้อมูลส่วนตัวของฉัน`
2. กด `เชื่อมต่อ LINE`
3. ระบบสร้าง one-time connect token และ short code อายุ 10 นาที เช่น `HOP-482913`
4. ผู้ใช้สแกน QR หรือเปิด LINE OA แล้วส่ง short code นี้ในแชท
5. Webhook รับ message และผูกบัญชี
6. ระบบส่งข้อความ LINE: `เชื่อมต่อ LINE กับ HOP สำเร็จแล้ว`

## Security Rules

- 1 LINE userId ผูกได้กับ 1 HOP user เท่านั้น
- 1 HOP user มี LINE userId ได้ 1 อัน
- ผู้ใช้สร้าง connect token ได้เฉพาะบัญชีของตัวเอง
- connect token/short code หมดอายุใน 10 นาที
- duplicate binding ถูกปฏิเสธและบันทึก audit event `Line.BindingFailed`
- Channel Secret และ Access Token ไม่ถูกส่งกลับ frontend

## Change LINE Account

ถ้าผู้ใช้เชื่อม LINE แล้ว หน้า `ข้อมูลส่วนตัวของฉัน` จะแสดง:

- Display Name
- LINE picture
- วันที่เชื่อมต่อ
- ปุ่มส่งข้อความทดสอบ
- ปุ่มยกเลิกการเชื่อมต่อ LINE
- ปุ่มเชื่อมต่อ LINE บัญชีอื่น

Flow สำหรับ `เชื่อมต่อ LINE บัญชีอื่น`:

1. แสดง confirm dialog
2. ยกเลิกการเชื่อมต่อบัญชี LINE เดิม
3. clear `users.line_user_id`
4. เปลี่ยน binding เดิมเป็น `Unbound`
5. สร้าง connect token/short code ใหม่
6. ผู้ใช้ส่ง short code ใน LINE OA จากบัญชี LINE ใหม่

ระบบเก็บประวัติ Bound/Unbound ผ่าน `line_user_bindings` และ audit log โดย unique constraint สำหรับ `user_id` จำกัดเฉพาะ binding ที่สถานะ `Bound` เพื่อให้ผู้ใช้เปลี่ยน LINE account ได้โดยไม่เสียประวัติเดิม

## Database

ตาราง:

```text
line_user_bindings
line_connect_tokens
line_pairing_codes
```

`line_user_bindings` มี unique index ที่ `line_user_id` และ `user_id` เมื่อไม่เป็น null

## Admin Monitoring

หน้า `Admin > ผู้ใช้ LINE` ใช้ตรวจสอบ:

- LINE User ID แบบ masked
- Display Name
- HOP User ที่ผูก
- Status
- Last Event
- ส่งข้อความทดสอบ

หน้านี้เป็น monitoring/test tool ไม่ใช่ configuration editor

## API

```http
POST /api/line/webhook

GET  /api/me/line/status
POST /api/me/line/connect-token
POST /api/me/line/disconnect
POST /api/me/profile/line/test-send

GET  /api/admin/line/line-users
POST /api/admin/line/line-users/{id}/test-send
```

## Audit Events

- `Line.UserFollowed`
- `Line.ConnectTokenGenerated`
- `Line.UserBound`
- `Line.UserUnbound`
- `Line.BindingFailed`
- `Line.UserSelfTestSent`
- `Line.UserSelfTestFailed`
- `Line.AdminUserTestSent`
- `Line.AdminUserTestFailed`

## วิธีทดสอบ

1. ตั้งค่า `Line__ChannelSecret` และ `Line__AccessToken`
2. เปิด webhook ใน LINE Developers Console
3. เพิ่มเพื่อน LINE OA
4. ตรวจหน้า `Admin > ผู้ใช้ LINE` ต้องเห็นสถานะ `รอผูกบัญชี`
5. Login HOP แล้วเปิด `ข้อมูลส่วนตัวของฉัน`
6. กด `เชื่อมต่อ LINE`
7. สแกน QR หรือพิมพ์ short code ใน LINE OA
8. refresh หน้า profile ต้องขึ้น `เชื่อมต่อแล้ว`
9. กด `ส่งข้อความทดสอบถึงฉัน`

## Troubleshooting

- ถ้า webhook ได้ `401`: ตรวจ Channel Secret และ `X-Line-Signature`
- ถ้าไม่เห็น follow event: ตรวจ Webhook URL และเปิด Use webhook ใน LINE Developers Console
- ถ้าผูกไม่สำเร็จ: ตรวจว่า code ยังไม่หมดอายุ และบัญชี LINE/HOP ยังไม่ถูกผูกกับบัญชีอื่น
- ถ้าส่งข้อความไม่สำเร็จ: ตรวจ Channel Access Token และ line delivery logs
