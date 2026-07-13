# Disaster Recovery Runbook

เอกสารนี้เป็นแนวทางปฏิบัติเมื่อระบบ HOP มีเหตุขัดข้องรุนแรง เช่น database เสียหาย, deploy ผิดพลาด, storage สูญหาย หรือจำเป็นต้อง restore ข้อมูลกลับจาก backup

## หลักการสำคัญ

1. หยุดความเสียหายก่อน restore
2. สำรองข้อมูลปัจจุบันก่อนเสมอ แม้ระบบจะเสีย
3. Restore ใน maintenance window
4. บันทึกเหตุผล ผู้อนุมัติ และหลักฐานทุกครั้ง
5. ตรวจระบบหลัง restore ก่อนเปิดให้ผู้ใช้จริง

## ผู้มีสิทธิ์ดำเนินการ

| งาน | ผู้มีสิทธิ์ |
|---|---|
| ดูสถานะ backup | `System.Backup.View` |
| verify backup | `System.Backup.Run` |
| restore preview / บันทึก restore request | `System.Backup.Restore` |
| apply retention | `System.Backup.ManageRetention` |
| restore production ผ่าน shell | ผู้ดูแล server ที่ได้รับอนุมัติ |

## ขั้นตอนเมื่อเกิดเหตุ

### 1. ประเมินเหตุ

1. เปิด Health Center
2. ตรวจ API, Database, Storage, Disk, Backup
3. บันทึกเวลาที่พบปัญหาและ Reference ID หากมี
4. หยุด deploy หรือ job ที่กำลังทำงานผิดพลาด

### 2. สำรองข้อมูลปัจจุบันก่อน restore

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh
```

บันทึกชื่อไฟล์ เช่น:

```text
hopdb_20260709_142201.backup
hop_uploads_20260709_142201.tar.gz
```

### 3. เลือก backup ที่ต้องการ restore

1. เปิด Backup Center
2. ไปที่ Backup History
3. เลือก backup ที่ต้องการ
4. กด verify หากยังไม่ verified
5. กด restore preview
6. ตรวจ warning/error
7. บันทึกเหตุผลใน restore request

### 4. Dry-run restore

ก่อน restore production ให้ dry-run:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/restore-hop.sh \
  --dry-run \
  --dump /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup \
  --storage /opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz
```

ถ้าต้องการทดสอบ database แยก:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/restore-hop.sh \
  --dry-run \
  --db-only \
  --target-db hop_restore_test \
  --dump /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup
```

### 5. Restore production

ทำเฉพาะเมื่อได้รับอนุมัติและอยู่ใน maintenance window:

```bash
sudo RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE \
  BACKUP_ENV_FILE=/etc/hop/backup.env \
  /opt/hop/scripts/backup/restore-hop.sh \
  --yes \
  --dump /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup \
  --storage /opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz
```

> **Warning:** คำสั่งนี้สามารถ overwrite database/storage ปัจจุบันได้ ต้องตรวจ path ให้ถูกต้องก่อนรัน

### 6. ตรวจหลัง restore

1. Restart backend service
2. Reload nginx ถ้าจำเป็น
3. ตรวจ `/health/live`
4. ตรวจ `/health/ready`
5. Login ด้วยบัญชี Admin
6. ตรวจ Dashboard
7. ตรวจ User Management
8. ตรวจคำขอลาและ approval timeline
9. ตรวจไฟล์แนบและ profile images
10. ทดสอบดาวน์โหลด PDF
11. ตรวจ Audit Log

## Checklist หลักฐาน

- [ ] เหตุผลการ restore
- [ ] ผู้อนุมัติ
- [ ] ผู้ดำเนินการ
- [ ] backup file ที่ใช้
- [ ] pre-restore backup file
- [ ] restore log
- [ ] health check หลัง restore
- [ ] login test
- [ ] leave workflow smoke test
- [ ] PDF/attachment verification

## สิ่งที่ห้ามทำ

- ห้าม restore production โดยไม่มี backup ปัจจุบัน
- ห้ามใช้ backup ที่ verify ไม่ผ่าน
- ห้ามแชร์ `.env` หรือ password ในเอกสาร incident
- ห้ามลบ backup ที่ใช้ restore ก่อนปิด incident
- ห้ามเปิดระบบให้ผู้ใช้ก่อน smoke test ผ่าน

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
