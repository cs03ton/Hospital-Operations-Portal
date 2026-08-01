# LINE LIFF / LINE Mini App

HOP รองรับ LINE LIFF / LINE Mini App แล้ว ดูคู่มือหลักที่ [LINE-LIFF.md](LINE-LIFF.md)

Production ควรใช้ค่า:

```text
LINE_LIFF_ENABLED=true
LINE_LIFF_ID=
Line__LiffEnabled=true
Line__LiffId=
Line__LiffBaseUrl=https://miniapp.line.me
VITE_LIFF_ID=
VITE_LIFF_BASE_URL=https://miniapp.line.me
```

หลักสำคัญที่ยังต้องคงไว้:

- ห้าม approve/reject โดยไม่ login
- ห้าม bypass backend permission
- ห้ามส่ง token/secret ไป frontend
- ใช้ backend endpoint เดิมเป็น source of truth
