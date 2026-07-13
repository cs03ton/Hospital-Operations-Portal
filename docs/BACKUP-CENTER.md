# Backup Center

Backup Center คือหน้าสำหรับให้ Admin และ SuperAdmin ตรวจสอบสถานะการสำรองข้อมูลของ HOP โดยไม่ต้องเปิด shell บน server ทุกครั้ง

## Route และสิทธิ์

| ส่วน | ค่า |
|---|---|
| Frontend | `/admin/backup` |
| เมนู | `จัดการระบบ` > `Backup Center` |
| ผู้ใช้งาน | Admin, SuperAdmin |
| วัตถุประสงค์ | Monitor, ตรวจหลักฐาน backup, ตรวจ restore-test evidence |

> หมายเหตุ: Backup Center ใช้สำหรับตรวจสอบสถานะ ไม่ใช่หน้าสั่ง restore production โดยตรง

## โครงสร้างไฟล์ Backup มาตรฐาน

Production bare-metal ใช้ path หลัก:

```text
/opt/hop/backups/
├── postgres/
│   └── hopdb_YYYYMMDD_HHMMSS.backup
├── storage/
│   └── hop_uploads_YYYYMMDD_HHMMSS.tar.gz
├── logs/
│   └── backup_YYYYMMDD_HHMMSS.log
└── YYYYMMDD_HHMMSS/
    ├── hopdb_YYYYMMDD_HHMMSS.backup
    └── hop_uploads_YYYYMMDD_HHMMSS.tar.gz
```

ตัวอย่างชื่อไฟล์ฐานข้อมูล:

```text
/opt/hop/backups/postgres/hopdb_20260709_142201.backup
```

## สิ่งที่ Backup Center ควรแสดง

| รายการ | ความหมาย | สถานะที่ควรตรวจ |
|---|---|---|
| Last Backup Time | เวลาสำรองข้อมูลล่าสุด | ต้องเป็นรอบล่าสุดตาม schedule |
| Database Backup | ไฟล์ `hopdb_*.backup` ล่าสุด | ต้องมีไฟล์และขนาดไม่เป็น 0 |
| Storage Backup | ไฟล์ `hop_uploads_*.tar.gz` ล่าสุด | ต้องมีไฟล์และขนาดไม่เป็น 0 |
| Backup Log | log รอบล่าสุด | ไม่มี error |
| Restore Evidence | หลักฐานทดสอบ restore | ควรมีอย่างน้อยรายเดือน |
| Retention | จำนวนวันที่เก็บ backup | ตรงกับ `/etc/hop/backup.env` |

## Environment ที่เกี่ยวข้อง

ตัวอย่าง `/etc/hop/backup.env`:

```env
BACKUP_MODE=host
BACKUP_ROOT=/opt/hop/backups
BACKUP_RETENTION_DAYS=7

DB_HOST=localhost
DB_PORT=5432
DB_NAME=hop_db
DB_USER=hop_user

UPLOADS_PATH=/opt/hop/uploads
LOG_FILE=/var/log/hop/backup.log
LOCK_FILE=/var/lock/hop-backup.lock
```

> Warning: ห้ามบันทึก `DB_PASSWORD`, JWT secret หรือ LINE token ลงในเอกสารคู่มือหรือ repository

## คำสั่ง Backup

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh
```

Dry run เพื่อตรวจ path และชื่อไฟล์โดยไม่ dump จริง:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh --dry-run
```

## คำสั่งดูรายการ Backup

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/restore-hop.sh --list
```

ผลลัพธ์ควรแสดง database backup จาก:

```text
/opt/hop/backups/postgres
```

## คำสั่ง Restore สำหรับ Maintenance Window

Restore ต้องทำในช่วง maintenance window และต้องได้รับอนุมัติก่อนเสมอ

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/restore-hop.sh \
  --dump /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup \
  --storage /opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz
```

สำหรับ automation แบบไม่ถามยืนยัน:

```bash
sudo RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE \
  BACKUP_ENV_FILE=/etc/hop/backup.env \
  /opt/hop/scripts/backup/restore-hop.sh --yes
```

## Checklist สำหรับผู้ดูแลระบบ

- [ ] Backup ล่าสุดอยู่ใน `/opt/hop/backups/postgres`
- [ ] ชื่อไฟล์ database เป็น `hopdb_YYYYMMDD_HHMMSS.backup`
- [ ] Storage backup อยู่ใน `/opt/hop/backups/storage`
- [ ] ชื่อไฟล์ storage เป็น `hop_uploads_YYYYMMDD_HHMMSS.tar.gz`
- [ ] Backup log รอบล่าสุดไม่มี error
- [ ] Health Center แสดง Backup เป็น Healthy หรือ Warning ที่อธิบายได้
- [ ] มี restore-test evidence อย่างน้อยเดือนละครั้ง
- [ ] ไม่พบ secret/token/password ใน backup log หรือเอกสาร

## Troubleshooting

| อาการ | สาเหตุที่พบบ่อย | วิธีแก้ |
|---|---|---|
| ไม่พบไฟล์ backup | `BACKUP_ROOT` ไม่ตรง | ตั้ง `BACKUP_ROOT=/opt/hop/backups` |
| ไฟล์ database ชื่อเก่า | ใช้ script รุ่นเก่า | deploy `scripts/backup/backup-hop.sh` รุ่นล่าสุด |
| Storage backup ไม่พบ | `UPLOADS_PATH` ไม่ถูกต้อง | ตรวจว่า `/opt/hop/uploads` มีอยู่และอ่านได้ |
| Backup file 0 byte | `pg_dump` ล้มเหลว | ตรวจ DB env และ log |
| Restore ไม่เลือกไฟล์ล่าสุด | ไฟล์ไม่ได้อยู่ใน `postgres/` | ย้ายหรือระบุ `--dump` แบบเต็ม path |

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
