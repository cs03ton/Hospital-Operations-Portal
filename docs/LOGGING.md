# Logging and Diagnostics

เอกสารนี้อธิบายแนวทางอ่าน log และใช้ Diagnostics Center ของ HOP อย่างปลอดภัย

## Log Sources

Diagnostics Center รองรับการอ่าน log จาก source ที่ backend อนุญาตเท่านั้น เช่น:

| Source | ตัวอย่างข้อมูล |
|---|---|
| `api` | log ของ `hop-api` |
| `backup` | log งาน backup |
| `nginx` | reverse proxy log ถ้าตั้งค่า path |
| `deploy` | deploy log ถ้าตั้งค่า path |

> **สำคัญ:** Backend จะตรวจ path whitelist ก่อนอ่าน log และไม่อนุญาตให้อ่านไฟล์นอกพื้นที่ที่กำหนด

## Redaction

ก่อนแสดง log บนหน้าเว็บหรือเขียนลง Support Bundle ระบบจะ mask ข้อมูลต่อไปนี้:

- Authorization / Bearer token / JWT
- password, secret, token, key
- connection string password
- LINE User ID เต็ม
- email, phone, citizen ID

## Reference ID

เมื่อเกิด error ผู้ใช้จะเห็น Reference ID เช่น:

```text
Reference ID: 0HNN3SP7005G0:00000003
```

ผู้ดูแลระบบสามารถนำค่าเดียวกันไปค้นหาใน:

1. `Diagnostics Center` > `Logs`
2. `Diagnostics Center` > `Recent Errors`
3. backend service log บน server

## Support Bundle

หากต้องส่ง log ให้ทีม IT:

1. เปิด `/admin/diagnostics`
2. สร้าง Support Bundle พร้อมเหตุผล
3. ตรวจว่าไฟล์ไม่มี secret/token/password
4. ส่งไฟล์ bundle ผ่านช่องทางภายในที่ปลอดภัย

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
