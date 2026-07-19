# Support Bundle

Support Bundle คือไฟล์รวบรวมข้อมูลวิเคราะห์ incident สำหรับส่งให้ทีม IT โดยออกแบบให้ปลอดภัยและไม่เปิดเผย secret ของ production

## การใช้งาน

1. Login ด้วย Admin หรือ SuperAdmin
2. เปิด `จัดการระบบ` > `Diagnostics Center`
3. ไปที่ Tab `Support Bundle`
4. ระบุเหตุผล เช่น `ตรวจสอบปัญหา LINE ส่งไม่สำเร็จหลัง deploy`
5. เลือกข้อมูลที่ต้องการรวม เช่น Logs, Health, Environment, Migrations, Recent Errors
6. กด `สร้าง Support Bundle`
7. ดาวน์โหลดไฟล์ `.zip` ที่ระบบสร้างให้

## ข้อมูลที่รวมได้

| ข้อมูล | รายละเอียด |
|---|---|
| Health | สถานะ API, database, storage, LINE, backup |
| Environment | ชื่อ key configuration แบบ sanitized ไม่ใส่ค่าลับ |
| Migrations | รายการ migration ที่ apply แล้ว |
| Recent Errors | audit/error ล่าสุดพร้อม Reference ID |
| Logs | log จาก source ที่อนุญาตและถูก redaction แล้ว |

## Redaction Policy

ก่อนสร้าง bundle ระบบจะ mask:

- Bearer/JWT token
- password, secret, access token
- connection string password
- LINE User ID เต็ม
- email, phone, Thai citizen ID

> **Warning:** ห้ามแนบไฟล์ `.env`, `appsettings.Production.json` ที่มีค่าจริง หรือ token จาก LINE Developers เข้า ticket ด้วยตนเอง

## Retention

- Bundle มีสถานะ `Available`, `Expired`, `Deleted`
- ค่า default หมดอายุหลัง 24 ชั่วโมง
- ผู้ดูแลควรลบไฟล์เก่าตามนโยบาย retention ของ production

## Permission

| Permission | ใช้ทำอะไร |
|---|---|
| `System.Diagnostics.View` | ดู history และรายการ bundle |
| `System.Diagnostics.Export` | สร้างและดาวน์โหลด bundle |

## วิธีทดสอบ

1. เปิด `/admin/diagnostics`
2. สร้าง bundle ด้วยเหตุผลอย่างน้อย 5 ตัวอักษร
3. ดาวน์โหลดไฟล์ `.zip`
4. เปิดตรวจไฟล์ใน zip ว่าไม่มี token/password/connection string
5. ตรวจ audit log ว่ามี `Diagnostics.SupportBundleGenerated`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
