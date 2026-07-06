# HOP Configuration

เอกสารนี้อธิบายมาตรฐานการตั้งค่า Hospital Operations Portal (HOP) หลังย้ายค่าลับออกจาก `appsettings*.json`

## หลักการ

1. `appsettings.json` และ `appsettings.Development.json` เก็บได้เฉพาะค่า default ที่ไม่ใช่ความลับ
2. ค่าลับต้องมาจาก environment variables หรือไฟล์ `.env*` ที่ไม่ถูก commit
3. Production ใช้ `.env.production` หรือ Secret Manager ของเครื่องแม่ข่าย
4. Environment variables ของระบบมีสิทธิ์สูงกว่าไฟล์ `.env`
5. ห้ามส่ง secret/token กลับ frontend และห้าม log ค่าเต็ม

## ลำดับการโหลดค่า

Backend โหลดค่าโดยใช้ `DotNetEnv` ผ่าน `EnvFileLoader`

1. `.env`
2. `.env.{ASPNETCORE_ENVIRONMENT}`
3. Environment variables ที่ตั้งอยู่แล้วในระบบหรือ Docker จะไม่ถูก overwrite

ตัวอย่าง Development:

```text
.env
.env.Development
.env.development
```

ตัวอย่าง Production:

```text
.env
.env.Production
.env.production
```

## Environment Variables สำคัญ

| Area | Key | ตัวอย่าง | หมายเหตุ |
|---|---|---|---|
| Database | `ConnectionStrings__DefaultConnection` | `Host=postgres;Port=5432;...` | ใช้กับ backend และ EF migration |
| JWT | `Jwt__Key` | `change-this...` | ต้องยาวอย่างน้อย 32 ตัวอักษร |
| JWT | `Jwt__Issuer` | `Hop.Api` | ไม่ใช่ secret |
| JWT | `Jwt__Audience` | `Hop.Client` | ไม่ใช่ secret |
| LINE | `Line__Enabled` | `true` | เปิด/ปิด LINE integration |
| LINE | `Line__ChannelId` | `2010...` | mask ใน frontend |
| LINE | `Line__AccessToken` | `...` | secret ห้าม commit |
| LINE | `Line__ChannelSecret` | `...` | secret ห้าม commit |
| LINE | `Line__WebhookUrl` | `https://domain/api/line/webhook` | public HTTPS |
| LINE | `Line__PublicAppUrl` | `https://domain` | ใช้สร้าง action URL |
| LINE | `Line__TestUserId` | `U...` | ใช้ทดสอบส่งข้อความ |
| Storage | `Storage__RootPath` | `/app/storage` | path ภายใน backend |
| Storage | `Storage__PublicBaseUrl` | `https://domain/storage` | public URL สำหรับไฟล์ |

## ไฟล์ตัวอย่าง

- `.env.example` ใช้สำหรับ development
- `.env.production.example` ใช้เป็น template สำหรับ server production
- `.env.development` และ `.env.production` ต้องไม่ถูก commit

## ตรวจสอบก่อนรันระบบ

```bash
./deploy/00-check-env.sh
```

Script จะแจ้งเฉพาะชื่อ key ที่ตั้งค่าแล้วหรือขาดหาย โดยไม่พิมพ์ค่าจริงออกมา

## ข้อควรระวัง

> Warning: ห้ามใส่ LINE Channel Access Token, LINE Channel Secret, JWT Key, database password หรือ token อื่น ๆ ลงใน `appsettings*.json`, source code, docs, screenshot หรือ issue tracker

> Tip: หากต้องการเปลี่ยน token ใน production ให้แก้ `.env.production` หรือ Secret Manager แล้ว restart backend container

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
