# Admin Health Dashboard / Health Center

เอกสารนี้เป็นเอกสารเดิมของ Admin Health Dashboard และถูกยกระดับเป็น Health Center ใน Phase 1.5

ดูรายละเอียดฉบับล่าสุดที่ [HEALTH-CENTER.md](HEALTH-CENTER.md)

## Route

- Frontend: `/admin/health`
- Backend API: `GET /api/admin/health`

## Permission

ใช้สิทธิ์:

```text
System.Health.View
```

Admin และ SuperAdmin ยังเข้าได้ตามบทบาทเพื่อให้ไม่ lock ผู้ดูแลระบบออกจากหน้า Health Center ส่วนผู้ใช้งานทั่วไปและ Staff ต้องถูกปฏิเสธสิทธิ์เมื่อเข้าผ่าน URL ตรง

## ข้อมูลที่แสดง

| ส่วน | รายละเอียด |
|---|---|
| API Status | สถานะ backend API |
| Database Status | สถานะการเชื่อมต่อฐานข้อมูลและ latency |
| Storage Status | ตรวจว่า storage path เขียนไฟล์ได้หรือไม่ |
| LINE Status | เปิดใช้งานหรือไม่ และเวลาส่งสำเร็จ/ล้มเหลวล่าสุด |
| Disk Usage | เปอร์เซ็นต์การใช้งาน disk ของ storage root |
| CPU Status | จำนวน core และ load average ถ้าระบบปฏิบัติการรองรับ |
| RAM Status | หน่วยความจำรวม ใช้ไป เหลือ และเปอร์เซ็นต์ใช้งาน |
| Queue / Worker Status | จำนวน LINE delivery ที่รอส่ง, failed, retry และสถานะ worker ที่เปิดใช้งาน |
| Backup Status | เวลา backup ล่าสุดจาก `BACKUP_ROOT`, ไฟล์ล่าสุด, ขนาดไฟล์ และ restore-test evidence ถ้ามี |
| App Version | version ของ backend assembly |
| Environment | Development / Production |
| Current Time Server | เวลา UTC จาก server |

## Security

API นี้ห้ามส่งข้อมูลต่อไปนี้:

- Database connection string
- JWT key
- LINE access token
- LINE channel secret
- Password หรือ secret อื่น

Response แสดงเฉพาะ status, message แบบปลอดภัย และ timestamp ที่จำเป็น

## การอ่านสถานะ

| Status | ความหมาย |
|---|---|
| Healthy | พร้อมใช้งาน |
| Warning | ใช้งานได้แต่ควรตรวจสอบ |
| Unhealthy | มีปัญหาที่ควรแก้ก่อนใช้งานจริง |
| Unknown | ตรวจสอบไม่ได้ |

## วิธีทดสอบ

1. Login ด้วยบัญชี Admin หรือ SuperAdmin
2. ไปที่เมนู `Health Center`
3. ตรวจ card แต่ละใบ
4. กดปุ่ม `รีเฟรช`
5. ตรวจ Queue / Worker Status ว่าแสดง `LINE pending`, `failed`, `retry`, `LINE retry` และ `Escalation`
6. ตรวจ CPU/RAM/Disk/Backup
7. ตรวจว่าไม่มี secret/token แสดงบนหน้าเว็บหรือ network response

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
