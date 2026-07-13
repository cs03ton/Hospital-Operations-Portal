# Backup Strategy

เอกสารนี้สรุปนโยบายการสำรองข้อมูลของ HOP Phase 1 และอ้างอิง script ปัจจุบันที่ใช้จริงบน production server

## Backup Scope

HOP ต้องสำรองข้อมูล 2 ส่วนพร้อมกัน:

1. PostgreSQL database
2. Runtime storage เช่น ไฟล์แนบคำขอลา รูปโปรไฟล์ และ PDF template/generated files

## Standard Backup Location

Production bare-metal ใช้ path มาตรฐาน:

```text
/opt/hop/backups/
├── postgres/
│   └── hopdb_YYYYMMDD_HHMMSS.backup
├── storage/
│   └── hop_uploads_YYYYMMDD_HHMMSS.tar.gz
└── logs/
    └── backup_YYYYMMDD_HHMMSS.log
```

ตัวอย่าง:

```text
/opt/hop/backups/postgres/hopdb_20260709_142201.backup
```

## Backup Command

ใช้ script หลัก:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh
```

Dry run:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh --dry-run
```

## Restore Command

ตรวจรายการ backup:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/restore-hop.sh --list
```

Restore แบบระบุไฟล์:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/restore-hop.sh \
  --dump /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup \
  --storage /opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz
```

## Retention

ค่าเริ่มต้นใน script:

```env
BACKUP_RETENTION_DAYS=7
```

สำหรับ production สามารถเพิ่มเป็น 14 หรือ 30 วันได้ตามพื้นที่ disk และนโยบายโรงพยาบาล

## Backup Center

ผู้ดูแลระบบตรวจสถานะผ่าน:

```text
จัดการระบบ > Backup Center
```

รายละเอียดหน้าจออยู่ที่ [BACKUP-CENTER.md](BACKUP-CENTER.md)

## Restore Test

ควรทดสอบ restore อย่างน้อยเดือนละครั้ง:

1. เตรียม database ทดสอบ
2. restore ไฟล์ `hopdb_*.backup`
3. restore storage archive
4. ตรวจ login
5. ตรวจคำขอลา
6. ตรวจไฟล์แนบ
7. ตรวจรูปโปรไฟล์
8. ตรวจ PDF
9. บันทึกหลักฐานใน [qa/RESTORE-TEST-EVIDENCE-TEMPLATE.md](qa/RESTORE-TEST-EVIDENCE-TEMPLATE.md)

## Security

- ห้าม commit backup file ลง Git
- ห้ามเขียน password/token/secret ลง log หรือคู่มือ
- `/etc/hop/backup.env` ควรตั้ง permission เป็น `600`
- backup folder ควรเข้าถึงได้เฉพาะ admin/server account

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
