# Phase 1 Production Go-Live Preparation

เอกสารนี้สรุปความพร้อมก่อนเปิดใช้งานจริงรอบแรกของ Hospital Operations Portal (HOP) โดยเปิดใช้เฉพาะ User Management และ Leave Management

## System Settings

ค่าที่ต้องกำหนดผ่าน environment/configuration ก่อน deploy:

| Setting | Environment Variable | Purpose |
| --- | --- | --- |
| Hospital Name | `HOSPITAL_NAME` / `VITE_HOSPITAL_NAME` | ชื่อโรงพยาบาลที่แสดงใน backend/frontend |
| Hospital Logo | `HOSPITAL_LOGO_PATH` | path โลโก้ที่ backend ใช้อ้างอิง |
| App Name | `VITE_APP_NAME` | ชื่อระบบใน frontend |
| App Version | `APP_VERSION` / `VITE_APP_VERSION` | version ที่แสดงในระบบ |
| Footer Developer | `FOOTER_DEVELOPER` / `VITE_APP_DEVELOPER` | หน่วยงานผู้พัฒนา |
| Theme Primary | `THEME_PRIMARY_COLOR` | สีหลักของระบบ |
| Theme Secondary | `THEME_SECONDARY_COLOR` | สีรองของระบบ |
| LINE Enabled | `LINE_ENABLED` | เปิด/ปิด LINE Messaging |
| LINE Token | `LINE_CHANNEL_ACCESS_TOKEN` | secret สำหรับส่ง LINE ห้าม commit |

ผู้ดูแลระบบตรวจค่าที่ runtime ใช้จริงได้ที่เมนู **ตั้งค่าระบบ**.

## Leave Balance Management

ก่อน pilot ต้องตรวจว่าเจ้าหน้าที่ทุกคนมี leave balance ของปีปัจจุบันครบ:

1. เข้าเมนู **จัดการวันลาคงเหลือ**
2. กรองปีปัจจุบัน
3. เพิ่มหรือแก้ไขยอดวันลาเป็นรายคน/รายประเภทลา
4. ใช้ **Manual Adjustment** เมื่อต้องมีเหตุผลการปรับยอดและ audit log
5. ดาวน์โหลด template จากปุ่ม **ดาวน์โหลด Template** เพื่อเตรียมนำเข้าข้อมูล Excel ในรอบถัดไป

Audit events ที่เกี่ยวข้อง:

- `LeaveBalance.Create`
- `LeaveBalance.Update`
- `LeaveBalance.Delete`
- `LeaveBalance.Adjust`

## Dashboard

Dashboard Phase 1 แสดง:

- งานรออนุมัติของฉัน
- เจ้าหน้าที่ลาวันนี้
- เจ้าหน้าที่ลาสัปดาห์นี้
- เจ้าหน้าที่ลาเดือนนี้
- วันลาคงเหลือของฉัน
- สรุปสถานะคำขอลาของฉัน
- แนวโน้มเจ้าหน้าที่ลางาน

ข้อมูลทั้งหมดต้องมาจาก leave tables จริง ไม่ใช้ mock data.

## Audit Log Export

ผู้มีสิทธิ์ `SystemSettings.Export` สามารถ export ได้จากหน้า **บันทึกการใช้งาน**:

- Excel: `/api/audit-logs/export-excel`
- PDF: `/api/audit-logs/export-pdf`
- CSV เดิม: `/api/audit-logs/export`

ตัวกรองที่รองรับ:

- คำค้น
- ผู้ใช้งาน
- การกระทำ
- วันที่เริ่มต้น
- วันที่สิ้นสุด

## Deployment Checklist

- Docker Compose config ผ่าน `docker compose config`
- PostgreSQL volume เป็น production volume แยกจาก test
- Migration apply สำเร็จ
- Seed production admin bootstrap เสร็จแล้ว
- `Seed__CreateDefaultAdmin=false` หลัง bootstrap
- Default admin development ไม่ถูกใช้ใน production
- Health check ผ่าน `/health` หรือ `/healthz`
- Login production admin ผ่าน
- Leave workflow ผ่านอย่างน้อย 1 flow ครบ submit/approve/PDF/attachment
- Audit export Excel/PDF ดาวน์โหลดได้
- Backup/restore command ถูกซ้อมอย่างน้อย 1 ครั้ง

## Backup

สำรองฐานข้อมูลด้วย `pg_dump`:

```bash
docker compose exec postgres pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /tmp/hop-backup.dump
docker compose cp postgres:/tmp/hop-backup.dump ./backups/hop-backup.dump
```

Restore ด้วย `pg_restore`:

```bash
docker compose cp ./backups/hop-backup.dump postgres:/tmp/hop-backup.dump
docker compose exec postgres pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists /tmp/hop-backup.dump
```

ห้าม restore ทับ production โดยไม่ผ่าน maintenance window และ backup ล่าสุดก่อนเสมอ.
