# LINE LIFF Future

Phase ปัจจุบันใช้ web URL ปกติจาก LINE Flex Message เพื่อเปิด HOP frontend

รองรับการเตรียม config สำหรับอนาคต:

```text
LINE_LIFF_ENABLED=false
LINE_LIFF_ID=
Line__LiffEnabled=false
Line__LiffId=
```

เมื่อเปิด LIFF ในอนาคต ต้องยังคงหลักเดิม:

- ห้าม approve/reject โดยไม่ login
- ห้าม bypass backend permission
- ห้ามส่ง token/secret ไป frontend
- ใช้ backend endpoint เดิมเป็น source of truth

