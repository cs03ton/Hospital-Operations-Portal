# LINE One-Time Connect Link

HOP รองรับการเชื่อมต่อ LINE OA ด้วย one-time connect token และ short code เพื่อให้ผู้ใช้ไม่ต้องกรอก LINE User ID เอง

## User Flow

1. ผู้ใช้ login เข้า HOP
2. เปิดหน้า `ข้อมูลส่วนตัวของฉัน`
3. กด `เชื่อมต่อ LINE`
4. ระบบสร้าง connect token อายุ 10 นาที
5. หน้าเว็บแสดง QR Code, short code และปุ่มเปิด LINE OA
6. ผู้ใช้เพิ่มเพื่อน LINE OA และส่ง short code เช่น `HOP-482913`
7. LINE webhook รับข้อความและผูก LINE userId กับบัญชี HOP
8. ระบบส่งข้อความกลับ: `เชื่อมต่อ LINE กับ HOP สำเร็จแล้ว`

## API

```http
POST /api/me/line/connect-token
GET  /api/me/line/status
POST /api/me/line/disconnect
```

## Configuration

```text
Line__OaAddFriendUrl=https://line.me/R/ti/p/@your_oa_id
LINE_OA_ADD_FRIEND_URL=https://line.me/R/ti/p/@your_oa_id
```

ถ้ายังไม่ได้ตั้งค่า OA URL ระบบยังแสดง short code ให้ผู้ใช้คัดลอกและส่งใน LINE OA ได้

## Security Rules

- token สุ่มด้วย cryptographic random
- token/short code หมดอายุใน 10 นาที
- สร้าง token ใหม่จะ cancel pending token เดิมของ user
- token ใช้ได้ครั้งเดียว
- 1 LINE userId ผูกได้กับ 1 HOP user
- 1 HOP user มี LINE userId ได้ 1 อันในสถานะ `Bound`
- Channel Secret และ Access Token ไม่ถูกส่งไป frontend

## Change LINE Account

ปุ่ม `เชื่อมต่อ LINE บัญชีอื่น` จะ disconnect LINE เดิม แล้วสร้าง connect token ใหม่ให้ผู้ใช้ส่ง short code จากบัญชี LINE ใหม่

Audit events:

- `Line.UserChangeRequested`
- `Line.UserUnbound`
- `Line.ConnectTokenGenerated`
- `Line.UserBound`
