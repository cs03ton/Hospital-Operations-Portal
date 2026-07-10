# HOP Health Center

เอกสารนี้อธิบาย Health Center สำหรับ HOP Phase 1.5 เพื่อให้ Admin และ SuperAdmin ตรวจสอบความพร้อมของระบบก่อนและระหว่าง Pilot

## Route และ API

| ส่วน | ค่า |
|---|---|
| Frontend | `/admin/health` |
| Backend API | `GET /api/admin/health` |
| Permission | `System.Health.View` |
| Role fallback | `Admin`, `SuperAdmin` |

## ข้อมูลที่ตรวจสอบ

| Component | รายละเอียด |
|---|---|
| API | สถานะ API และ uptime |
| Database | ตรวจ `SELECT 1`, latency, provider |
| Storage | ตรวจ path และการเขียนไฟล์จริง |
| LINE | enabled, token/secret configured แบบ boolean, last success/failure |
| Queue / Worker | LINE queue pending, failed, retry และ worker configuration |
| Disk | used/free/total และ percent ใช้งาน |
| CPU | processor count และ load average ถ้าระบบรองรับ |
| RAM | total/used/available และ percent ใช้งาน |
| Backup | backup ล่าสุด, restore-test evidence, directory, file size |
| Version | app version, environment, git commit ถ้ามี |

## Status Standard

| Status | ความหมาย | Action |
|---|---|---|
| Healthy | พร้อมใช้งาน | เฝ้าระวังตามปกติ |
| Warning | ใช้งานได้แต่ควรตรวจสอบ | ตรวจ component นั้นก่อน pilot/deploy |
| Unhealthy | มีปัญหาสำคัญ | แก้ก่อนใช้งานจริง |
| Unknown | ตรวจสอบไม่ได้ | ตรวจ config/log เพิ่ม |

## Security

Health Center ห้ามแสดงข้อมูลลับ:

- Database connection string
- JWT key
- LINE access token
- LINE channel secret
- Password
- Stack trace

LINE จะแสดงเฉพาะ `hasAccessToken` และ `hasChannelSecret` เป็น boolean หรือข้อความ “ตั้งค่าแล้ว/ยังไม่ได้ตั้งค่า”

## การใช้งาน

1. Login ด้วย Admin หรือ SuperAdmin
2. เปิดเมนู `Health Center`
3. ตรวจ `Overall Status`
4. ตรวจ card แต่ละ component
5. เปิดแท็บรายละเอียด:
   - ภาพรวม
   - Infrastructure
   - LINE
   - Backup
   - Diagnostics
6. กด `รีเฟรช` หลังแก้ config หรือ restart service

## Environment ที่ควรตั้งค่า

```text
Storage__RootPath=/opt/hop/uploads
BACKUP_ROOT=/opt/hop/backups
Line__Enabled=true
Line__ChannelSecret=...
Line__AccessToken=...
GIT_COMMIT=<release commit>
```

## Acceptance Checklist

- [ ] Admin/SuperAdmin เปิด `/admin/health` ได้
- [ ] Staff เปิดไม่ได้
- [ ] API response ไม่มี secret/token/connection string
- [ ] Database latency แสดงได้
- [ ] Storage writable แสดงถูกต้อง
- [ ] LINE enabled/configured แสดงแบบ masked/safe
- [ ] Disk, CPU, RAM แสดงได้
- [ ] Backup ล่าสุดแสดงได้จาก `BACKUP_ROOT`
- [ ] Restore-test evidence แสดงได้ถ้ามีไฟล์ evidence ใน backup folder

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
