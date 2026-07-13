# HOP Rollback Runbook

เอกสารนี้ใช้สำหรับ rollback Hospital Operations Portal (HOP) Phase 1 ในกรณี deploy แล้วพบปัญหารุนแรง

## ก่อน Rollback

1. ประกาศ maintenance window หากมีผลกับผู้ใช้งาน
2. ระบุ release/tag ที่ต้อง rollback กลับไป
3. ระบุว่า rollback เฉพาะ application หรือรวม database/storage
4. ตรวจว่า backup artifact ผ่าน restore test แล้ว
5. บันทึกผู้อนุมัติ rollback และเหตุผล

## Application-Only Rollback

ใช้เมื่อไม่มี migration หรือ storage change ที่ต้องย้อนข้อมูล

```bash
cd /opt/hop
ROLLBACK_REF=<previous-release-tag> \
ROLLBACK_CONFIRM=I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP \
bash deploy/rollback.sh
```

ตรวจหลัง rollback:

```bash
bash deploy/04-crosscheck.sh
```

## Application + Database + Storage Rollback

ใช้เมื่อ migration/storage change ทำให้ระบบใช้งานไม่ได้ และได้รับอนุมัติให้ restore ข้อมูลแล้ว

```bash
cd /opt/hop
ROLLBACK_REF=<previous-release-tag> \
ROLLBACK_CONFIRM=I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP \
RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE \
RESTORE_DATABASE=true \
RESTORE_STORAGE=true \
DB_DUMP_PATH=/opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup \
STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz \
bash deploy/rollback.sh
```

## Storage-Only Rollback

```bash
RESTORE_DATABASE=false \
RESTORE_STORAGE=true \
RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE \
STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz \
bash scripts/backup/restore-hop.sh
```

## Database-Only Rollback

```bash
RESTORE_DATABASE=true \
RESTORE_STORAGE=false \
RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE \
DB_DUMP_PATH=/opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup \
bash scripts/backup/restore-hop.sh
```

## Evidence To Keep

- Deploy log path: `logs/deploy/deploy_YYYYMMDD_HHMMSS.log`
- Restore log path: `backups/logs/restore_YYYYMMDD_HHMMSS.log`
- Backup artifact names
- Crosscheck result
- Smoke test result
- Incident ticket / approval note

## Go / No-Go After Rollback

- [ ] `/health/live` ผ่าน
- [ ] `/health/ready` ผ่าน
- [ ] Login ผ่าน
- [ ] Dashboard เปิดได้
- [ ] Leave request list เปิดได้
- [ ] Permission denied case ทำงานถูกต้อง
- [ ] PDF download ผ่านถ้ามี `SMOKE_LEAVE_REQUEST_ID`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
