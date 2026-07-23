# LINE Webhook

LINE Webhook ของ HOP ใช้สำหรับรับ event จาก LINE OA และผูกบัญชีผู้ใช้ผ่าน short code

## Endpoint

```http
POST /api/line/webhook
```

ตั้งค่าใน LINE Developers Console:

```text
https://{PUBLIC_APP_URL}/api/line/webhook
```

Production ต้องใช้ HTTPS

## Signature Verification

ระบบตรวจ `X-Line-Signature` ด้วย `Line__ChannelSecret`

- signature ถูกต้อง: ประมวลผล event
- signature ไม่ถูกต้อง: ตอบ `401`
- ไม่มี Channel Secret: ตอบ `401`
- payload verify จาก LINE ที่มี `events: []` และ signature ถูกต้อง: ตอบ `200`

ห้าม log Channel Secret หรือ Access Token

> Note: endpoint นี้เปิดให้ LINE Platform เรียกแบบไม่ต้องใช้ JWT/Cookie ของผู้ใช้ HOP แต่ยังต้องมี `X-Line-Signature` ที่ถูกต้องเสมอ

## ทดสอบ Webhook Verify

การเรียกแบบไม่มี signature ต้องได้ `401 Unauthorized` ซึ่งเป็นพฤติกรรมที่ถูกต้อง:

```bash
curl -i -X POST https://hop.namuenhospital.go.th/api/line/webhook \
  -H "Content-Type: application/json" \
  --data '{"destination":"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx","events":[]}'
```

การทดสอบแบบจำลอง LINE Verify ต้องคำนวณ signature จาก raw body เดียวกันกับที่ส่ง:

```bash
BODY='{"destination":"Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx","events":[]}'
SECRET='<LINE_CHANNEL_SECRET>'
SIG=$(printf '%s' "$BODY" | openssl dgst -sha256 -hmac "$SECRET" -binary | base64)

curl -i -X POST https://hop.namuenhospital.go.th/api/line/webhook \
  -H "Content-Type: application/json" \
  -H "X-Line-Signature: $SIG" \
  --data "$BODY"
```

Expected result:

```text
HTTP/1.1 200 OK
```

ห้ามนำค่า `SECRET` จริงไปใส่ในเอกสารหรือ commit ลง repository

## Supported Events

- `follow`: บันทึก LINE user เป็น `Pending`
- `unfollow`: clear `users.line_user_id` และเปลี่ยน binding เป็น `Unbound`
- `message`: ตรวจ short code แล้ว bind LINE user กับ HOP user

## Message Binding

ระบบตรวจ short code ตามลำดับ:

1. `line_connect_tokens.short_code` สถานะ `Pending`
2. fallback ไป `line_pairing_codes.code` เดิม

ข้อความตอบกลับ:

- สำเร็จ: `เชื่อมต่อ LINE กับ HOP สำเร็จแล้ว`
- หมดอายุ: `รหัสเชื่อมต่อหมดอายุแล้ว กรุณาสร้างรหัสใหม่ใน HOP`
- ไม่พบรหัส: `ไม่พบรหัสเชื่อมต่อ กรุณาตรวจสอบรหัสอีกครั้ง`
- duplicate: `บัญชี LINE หรือบัญชี HOP นี้ถูกเชื่อมต่ออยู่แล้ว`

## Tables

- `line_connect_tokens`
- `line_user_bindings`
- `line_pairing_codes`
