# PRD: แจ้งซ่อม จองห้องประชุม ลา และขอรถ (24 ก.ย. 2569)

## ไฟล์ที่ใช้

- SQL สำหรับ DBeaver: `22-prd-repair-meeting-fleet-migrations.sql`
- ตัวอย่างค่าตั้งค่า: `../../.env.production.example` (คัดลอกเป็น `.env.production` บน server แล้วใส่ secret จริง; อย่า commit ไฟล์จริง)

SQL สร้างจาก EF Core migrations หลัง `20260909090000_CorrectFleetRolePermissions` จนถึง `20260924140000_AddFleetRequestAttachments` แบบ idempotent ตาม `__EFMigrationsHistory` ครอบคลุมตารางและสิทธิ์แจ้งซ่อม ห้องประชุม รูปห้อง รายชื่อผู้เข้าประชุม การแจ้ง LINE Group ส่วนกลาง และไฟล์แนบคำขอรถ

กฎนับวันลา การแสดงปี พ.ศ. การซ่อนรายการยกเลิกในปฏิทิน การเลือกผู้โดยสาร การแจ้งเตือน และการปรับ UI เป็นการเปลี่ยนโค้ด ไม่ต้องเพิ่ม migration จาก SQL ไฟล์นี้ SQL นี้ไม่ซ่อมข้อมูลวันลาหรือยอดคงเหลือเก่า

## ก่อนรันใน DBeaver

1. สำรองฐานข้อมูล PRD และไฟล์ใน `Storage__RootPath` พร้อมตรวจว่ากู้คืนได้
2. ใช้ connection ไปยังฐานข้อมูล HOP ที่ถูกต้อง รันคำสั่งอ่านอย่างเดียวด้านล่าง แล้วตรวจว่า baseline migration มีอยู่
3. หาก connection ก่อนหน้าเคยขึ้น `25P02` ให้รัน `ROLLBACK;` ก่อนเริ่ม

```sql
SELECT current_database(), current_schema();
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" >= '20260909090000'
ORDER BY "MigrationId";
```

รัน `22-prd-repair-meeting-fleet-migrations.sql` **ทั้งไฟล์** ด้วย Execute SQL Script และ Stop on error ไฟล์มี transaction และ `COMMIT` ของตัวเอง หากผิดพลาดให้ `ROLLBACK;` ใน connection เดิมแล้วตรวจ schema/history ก่อนลองใหม่ ห้ามรัน `06-prd-repair-schema.sql`, `09-prd-meeting-room-booking.sql` หรือ seed พนักงานซ้ำหลังไฟล์นี้

## หลังรัน

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" >= '20260909090000'
ORDER BY "MigrationId";

SELECT to_regclass('public.repair_requests') AS repair_requests,
       to_regclass('public.meeting_room_bookings') AS meeting_room_bookings,
       to_regclass('public.meeting_room_booking_attendees') AS meeting_room_booking_attendees,
       to_regclass('public.fleet_request_attachments') AS fleet_request_attachments;
```

ให้ตรวจว่ามี migration ล่าสุด `20260924140000_AddFleetRequestAttachments` ก่อน deploy Backend และ Frontend เวอร์ชันเดียวกัน

## ค่า `.env.production` ที่เกี่ยวข้อง

ใช้ `.env.production.example` เป็นฐาน ค่า `POSTGRES_*`, `Jwt__Key`, `Line__*` ต้องใส่ค่าจริงบน server `Database__SeedOnStartup=false` และ `Seed__CreateDefaultAdmin=false` สำหรับ PRD

- `Storage__RootPath` ต้องเป็น volume ถาวรที่ Backend เขียนได้ รูปห้อง เอกสารจองห้อง และไฟล์แนบรถใช้ path นี้
- `FileScan__Enabled=true`, `FileScan__Provider=ClamAV`, `FileScan__FailClosed=true`, `ClamAv__Host=clamav`, `ClamAv__Port=3310` สำหรับไฟล์อัปโหลด
- `Fleet__RolloutMode=Enabled` สำหรับขอรถ และตรวจค่าที่บันทึกไว้ในระบบด้วย
- `Line__Enabled`, token/secret และ `LineGroupNotifications__Enabled` ใช้กับการแจ้งเตือน LINE
- `Repairs__NotificationsEnabled=false` เปิดแจ้งซ่อมได้โดยยังไม่ส่ง LINE; เมื่อตั้งปลายทาง HTTPS ของทีมช่างและ `Repairs__AllowedNotificationHosts__0` ถูกต้องจึงเปลี่ยนเป็น `true`
- ห้องประชุมและกฎลาไม่มี feature flag เพิ่มในรอบนี้

ใช้ `docker compose --env-file .env.production -f docker-compose.prod.yml config` ตรวจค่าที่ Compose อ่านได้โดย **ไม่เผยแพร่ผลลัพธ์** เพราะอาจมี secret จากนั้น deploy Backend/Frontend และตรวจ `/health/ready` กับสิทธิ์ของผู้ใช้จริง
