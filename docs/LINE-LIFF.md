# LINE LIFF Integration

เอกสารนี้อธิบายการเปิดใช้งาน LINE LIFF สำหรับ Hospital Operations Portal (HOP) เพื่อให้ผู้ใช้งานเข้าสู่ระบบหรือเชื่อมบัญชี LINE กับ HOP ได้อย่างปลอดภัย

## ภาพรวม

ระบบรองรับ 2 รูปแบบโดยไม่กระทบการ Login เดิมด้วย Username/Password

1. **LIFF Login**
   ผู้ใช้เปิด `/liff` จาก LINE แล้วระบบตรวจสอบ LINE ID Token กับ LINE Platform ก่อนออก session ของ HOP

2. **LIFF Account Linking**
   ผู้ใช้ Login HOP แล้วเปิดหน้า `ข้อมูลส่วนตัวของฉัน` จาก LIFF เพื่อกด `เชื่อมด้วย LINE LIFF`

> สำคัญ: Backend ต้อง verify LINE ID Token ทุกครั้ง ห้ามเชื่อข้อมูลจาก Frontend อย่างเดียว

## Environment Variables

Production ต้องตั้งค่าอย่างน้อยดังนี้

```env
Line__LiffId=
Line__LiffBaseUrl=https://miniapp.line.me
Line__LoginChannelId=
Line__LoginChannelSecret=
Line__IdTokenVerifyUrl=https://api.line.me/oauth2/v2.1/verify
VITE_LIFF_ID=
VITE_LIFF_BASE_URL=https://miniapp.line.me
PUBLIC_APP_URL=https://hop.namuenhospital.go.th
Line__PublicAppUrl=https://hop.namuenhospital.go.th
```

ห้ามใส่ `Line__LoginChannelSecret`, Channel Secret, Access Token หรือ JWT secret ลงใน Git

## Backend API

| Endpoint | Auth | รายละเอียด |
|---|---|---|
| `POST /api/auth/line/liff` | Anonymous | รับ LINE ID Token, verify กับ LINE, หา HOP user ที่ผูก LINE แล้ว และออก JWT/session |
| `POST /api/me/line/link` | Authenticated | ผู้ใช้ Login แล้วผูก LINE ด้วย ID Token |
| `DELETE /api/me/line/unlink` | Authenticated | ยกเลิกการผูก LINE ของตนเอง |
| `GET /api/me/line/status` | Authenticated | ดูสถานะการเชื่อม LINE |

## Database

ระบบใช้ตารางเดิม `line_user_bindings` และเพิ่มข้อมูล `last_login_at` เพื่อเก็บเวลาที่ Login ผ่าน LIFF ล่าสุด

SQL สำหรับ DBeaver:

```sql
BEGIN;

ALTER TABLE public.line_user_bindings
ADD COLUMN IF NOT EXISTS last_login_at timestamp with time zone;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_line_user_bindings_user_id_active_bound"
ON public.line_user_bindings (user_id)
WHERE user_id IS NOT NULL AND status = 'Bound';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260724090000_AddLineLiffSupport', '9.0.15'
WHERE NOT EXISTS (
    SELECT 1
    FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260724090000_AddLineLiffSupport'
);

COMMIT;
```

## Frontend

| Route | รายละเอียด |
|---|---|
| `/liff` | หน้า entry สำหรับ LIFF Login |
| `/profile` | มีปุ่ม `เชื่อมด้วย LINE LIFF` เมื่อมี `VITE_LIFF_ID` |

Frontend จะ sanitize `returnUrl` ให้เป็น path ภายในระบบเท่านั้น เพื่อป้องกัน open redirect

สำหรับ LINE Mini App ให้ใช้ URL รูปแบบ:

```text
https://miniapp.line.me/{LIFF_ID}?returnUrl=/dashboard
```

`https://liff.line.me/{LIFF_ID}` ยังใช้เปิดได้ในบางกรณีแบบ legacy แต่ production ของ HOP ควรกำหนด `VITE_LIFF_BASE_URL=https://miniapp.line.me`

## ขั้นตอนทดสอบ

1. ตั้งค่า `Line__LoginChannelId`, `Line__LoginChannelSecret`, `Line__LiffId`, `VITE_LIFF_ID`
2. Build frontend ใหม่ด้วยค่า env production
3. Restart backend
4. เปิด LINE Mini App URL จาก LINE Developers หรือ `https://miniapp.line.me/{LIFF_ID}?returnUrl=/dashboard`
5. ถ้า LINE ยังไม่ผูกกับ HOP ระบบควรแจ้งให้ Login และไปผูกบัญชี
6. หลังผูกสำเร็จ เปิด LIFF อีกครั้งควรเข้าสู่ระบบ HOP ได้
7. ตรวจ `line_user_bindings.last_login_at`

## Security Notes

- Backend verify ID Token กับ LINE endpoint จริงทุกครั้ง
- ห้าม log ID Token, Channel Secret หรือ Access Token
- LINE User ID หนึ่งบัญชีผูกได้กับ HOP User เดียว
- HOP User หนึ่งบัญชีผูกได้กับ LINE User เดียวที่ status `Bound`
- หาก `PUBLIC_APP_URL` หรือ LIFF URL เป็น localhost/IP ภายใน LINE จะเปิดไม่ได้จากผู้ใช้งานภายนอก
