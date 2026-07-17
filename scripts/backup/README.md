# HOP Backup / Restore Scripts

เอกสารนี้อธิบายสคริปต์ Backup/Restore สำหรับ Hospital Operations Portal (HOP) บน Ubuntu Production Server

## 1. ภาพรวม

โฟลเดอร์นี้มีสคริปต์หลัก:

| File | Purpose |
|---|---|
| `backup-hop.sh` | สำรอง PostgreSQL database และ uploaded storage ด้วย timestamp เดียวกัน |
| `restore-hop.sh` | restore database/storage จาก backup ล่าสุดหรือไฟล์ที่ระบุ |
| `backup.env.example` | ตัวอย่าง environment variables สำหรับ production |
| `hop-backup.cron.example` | ตัวอย่าง cron job เวลา 02:00 ทุกวัน |
| `install-cron.sh` | ติดตั้ง cron job โดยไม่เขียน password ลง crontab |

## 2. ขอบเขตข้อมูลที่ Backup

Backup ครอบคลุม:

- PostgreSQL schema และ data ผ่าน `pg_dump --format=custom`
- EF migration history ในฐานข้อมูล
- Uploaded storage ตาม `UPLOADS_PATH` หรือ `STORAGE_PATH`
- ไฟล์แนบระบบลา
- รูปโปรไฟล์
- PDF templates/generated files ถ้าอยู่ใต้ storage path เดียวกัน

> หมายเหตุ: โครงสร้างปลายทางใช้ `postgres/` และ `storage/` เพื่อรักษาความเข้ากันได้กับ deploy/rollback runbook เดิมของโปรเจกต์

## 3. สิ่งที่ยังไม่ถูก Backup

- Secret/token/password แบบ plain text
- `/etc/hop/*.env` และ production secret files
- OS package state
- Nginx/systemd live configuration นอก repository
- PostgreSQL roles/global objects ถ้าอยู่นอก database ที่ dump ด้วย `pg_dump`

หากต้องการ backup roles/global objects ให้เพิ่ม runbook แยกด้วย `pg_dumpall --globals-only` และเก็บในพื้นที่ secure

## 4. Prerequisites

Host mode:

- `bash`
- `flock`
- PostgreSQL client tools: `pg_dump`, `pg_restore`, `pg_isready`, `psql`
- สิทธิ์อ่าน uploads directory
- สิทธิ์เขียน backup/log directory

Docker mode:

- `docker`
- PostgreSQL container ที่กำหนดใน `POSTGRES_CONTAINER`
- Docker volume ที่กำหนดใน `STORAGE_DOCKER_VOLUME`

## 5. Environment Variables

ดูตัวอย่างจาก `backup.env.example`

| Variable | Default | Description |
|---|---|---|
| `BACKUP_ENV_FILE` | `/etc/hop/backup.env` ถ้ามี | env file ที่สคริปต์จะ source |
| `BACKUP_MODE` | `host` | `host` หรือ `docker` |
| `BACKUP_ROOT` | `/opt/hop/backups` | root directory สำหรับ backup |
| `BACKUP_RETENTION_DAYS` | `7` | จำนวนวันที่เก็บ backup |
| `DB_HOST` | `localhost` | PostgreSQL host |
| `DB_PORT` | `5432` | PostgreSQL port |
| `DB_NAME` | required | Database name |
| `DB_USER` | required | Database user |
| `DB_PASSWORD` | empty | Database password; ห้าม commit |
| `POSTGRES_CONTAINER` | `hop-prod-postgres` | ใช้เมื่อ `BACKUP_MODE=docker` |
| `UPLOADS_PATH` | fallback to `STORAGE_PATH` | path ของ uploaded files บน bare metal |
| `STORAGE_DOCKER_VOLUME` | `hop_prod_storage` | Docker storage volume |
| `LOG_FILE` | per-run log under backup root | log file path |
| `LOCK_FILE` | `/tmp/hop-backup.lock` | flock lock file |

แนะนำ production:

```bash
sudo install -d -m 700 /etc/hop /opt/hop/backups /var/log/hop
sudo chown -R hop:hop /opt/hop/backups /var/log/hop
sudo cp scripts/backup/backup.env.example /etc/hop/backup.env
sudo chmod 600 /etc/hop/backup.env
sudo nano /etc/hop/backup.env
```

## 6. วิธีรัน Backup ด้วยตนเอง

```bash
BACKUP_ENV_FILE=/etc/hop/backup.env sudo -E /opt/hop/scripts/backup/backup-hop.sh
```

Dry run:

```bash
BACKUP_ENV_FILE=/etc/hop/backup.env sudo -E /opt/hop/scripts/backup/backup-hop.sh --dry-run
```

## 7. วิธีติดตั้ง Cron

ตัวอย่าง cron:

```cron
0 2 * * * BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh >> /var/log/hop-backup-cron.log 2>&1
```

ติดตั้งด้วยสคริปต์:

```bash
sudo chmod +x /opt/hop/scripts/backup/backup-hop.sh /opt/hop/scripts/backup/restore-hop.sh /opt/hop/scripts/backup/install-cron.sh
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/install-cron.sh
```

ตั้งเวลาอื่น:

```bash
sudo CRON_SCHEDULE="30 1 * * *" BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/install-cron.sh
```

## 8. วิธีตรวจสอบ Cron

```bash
sudo crontab -l
systemctl status cron
tail -f /var/log/hop-backup-cron.log
tail -f /var/log/hop/backup.log
```

ถ้าเห็น error แบบนี้:

```text
tee: /var/log/hop/backup.log: No such file or directory
```

แปลว่า `LOG_FILE=/var/log/hop/backup.log` ถูกตั้งไว้ แต่ยังไม่มี directory หรือ user ที่รัน cron เขียนไม่ได้ ให้แก้ด้วย:

```bash
sudo install -d -m 750 -o hop -g hop /var/log/hop
sudo touch /var/log/hop/backup.log
sudo chown hop:hop /var/log/hop/backup.log
sudo chmod 640 /var/log/hop/backup.log
```

หรือเปลี่ยน `/etc/hop/backup.env` ให้ใช้ log ใต้ backup root:

```env
LOG_FILE=/opt/hop/backups/logs/backup.log
```

สคริปต์รุ่นล่าสุดจะ fallback ไปที่ `BACKUP_ROOT/logs/backup_*.log` ให้อัตโนมัติถ้า `LOG_FILE` เขียนไม่ได้

ถ้าไฟล์ database backup ยังไปลง `/opt/hop/backups/db` แทน `/opt/hop/backups/postgres` แปลว่า server ยังรันสคริปต์รุ่นเก่าหรือ cron ชี้ผิดไฟล์ ให้ตรวจด้วย:

```bash
sudo crontab -l
grep -n 'db_dir=' /opt/hop/scripts/backup/backup-hop.sh
grep -n 'DatabaseBackupDir' /opt/hop/scripts/backup/backup-hop.sh
```

ค่าที่ถูกต้องต้องเห็น:

```text
db_dir="${BACKUP_ROOT}/postgres"
DatabaseBackupDir=${db_dir}
```

จากนั้น deploy/sync `scripts/backup/backup-hop.sh` รุ่นล่าสุดขึ้น `/opt/hop/scripts/backup/backup-hop.sh` แล้วทดสอบ:

```bash
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh --dry-run
sudo BACKUP_ENV_FILE=/etc/hop/backup.env /opt/hop/scripts/backup/backup-hop.sh
```

## 9. วิธีตรวจสอบ Log และไฟล์ Backup

```bash
find /opt/hop/backups -maxdepth 3 -type f -ls
df -h
pg_restore --list /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup | head
tar -tzf /opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz | head
```

โครงสร้าง backup:

```text
backups/
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

## 10. วิธีดูรายการ Backup

```bash
BACKUP_ENV_FILE=/etc/hop/backup.env sudo -E /opt/hop/scripts/backup/restore-hop.sh --list
```

## 11. วิธี Restore ล่าสุด

> Warning: Restore จะเขียนทับข้อมูลปัจจุบัน ควรหยุด application หรือปิด user traffic ก่อน restore

```bash
BACKUP_ENV_FILE=/etc/hop/backup.env sudo -E /opt/hop/scripts/backup/restore-hop.sh
```

ระบบจะเลือก `hopdb_*.backup` ล่าสุด และหา storage archive timestamp เดียวกันโดยอัตโนมัติ

## 12. วิธี Restore แบบไม่ถามยืนยัน

ใช้สำหรับ Disaster Recovery automation เท่านั้น:

```bash
RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE \
BACKUP_ENV_FILE=/etc/hop/backup.env \
sudo -E /opt/hop/scripts/backup/restore-hop.sh --yes
```

## 13. วิธี Restore Backup ที่ระบุเอง

```bash
BACKUP_ENV_FILE=/etc/hop/backup.env \
sudo -E /opt/hop/scripts/backup/restore-hop.sh \
  --dump /opt/hop/backups/postgres/hopdb_20260713_020000.backup \
  --storage /opt/hop/backups/storage/hop_uploads_20260713_020000.tar.gz
```

## 14. วิธีทดสอบ Disaster Recovery

1. เตรียม test server หรือ disposable database
2. คัดลอก backup dump และ storage archive
3. ตั้ง `DB_NAME` เป็นฐานทดสอบ
4. รัน `restore-hop.sh --dump ... --storage ...`
5. ตรวจ login
6. ตรวจคำขอลา
7. ตรวจไฟล์แนบ
8. ตรวจรูป profile
9. ตรวจ PDF template
10. บันทึกหลักฐานใน `docs/qa/RESTORE-TEST-EVIDENCE-TEMPLATE.md`

## 15. วิธีตรวจสอบพื้นที่ Disk

```bash
df -h
du -sh /opt/hop/backups
find /opt/hop/backups -type f -maxdepth 3 -ls
```

## 16. วิธีเปลี่ยน Retention

แก้ `/etc/hop/backup.env`

```env
BACKUP_RETENTION_DAYS=14
```

## 17. วิธีหยุด Application ก่อน Restore

Bare metal ตัวอย่าง:

```bash
sudo systemctl stop hop-api
sudo systemctl reload nginx
```

Docker ตัวอย่าง:

```bash
docker compose -f docker-compose.prod.yml stop backend frontend nginx
```

หลัง restore ให้ start service และทำ smoke test

## 18. วิธีถอน Cron Job

```bash
sudo crontab -l | grep -v 'HOP_BACKUP_CRON' | sudo crontab -
sudo crontab -l
```

## 19. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `DB_NAME is required` | env file ไม่ถูก load | ตั้ง `BACKUP_ENV_FILE=/etc/hop/backup.env` |
| `pg_dump not found` | PostgreSQL client tools ไม่มีใน host | ติดตั้ง `postgresql-client` หรือใช้ `BACKUP_MODE=docker` |
| `Another HOP backup process is already running` | backup ซ้อนกัน | ตรวจ process/lock file |
| Backup file 0 byte | `pg_dump` ล้มเหลว | ดู log และแก้ DB connection |
| `pg_restore --list failed` | dump เสียหรือไม่ใช่ custom format | ใช้ backup รอบอื่น |
| Storage restore skipped | ไม่มี archive timestamp เดียวกัน | ตรวจ `backups/storage` |
| `tee: /var/log/hop/backup.log: No such file or directory` | ไม่มี log directory หรือสิทธิ์เขียน log ไม่พอ | สร้าง `/var/log/hop` และปรับ owner หรือใช้ `LOG_FILE=/opt/hop/backups/logs/backup.log` |
| Permission denied | สิทธิ์ directory/log ไม่พอ | ปรับ owner/permission ของ `/opt/hop/backups`, `/var/log/hop` |

## 20. Security Notes

- ห้าม log password/token
- ห้ามเก็บ `.env` ที่มี secret ใน Git
- `/etc/hop/backup.env` ควรเป็น `600`
- backup files ควรอยู่บน disk ที่เข้าถึงได้เฉพาะ admin
- ทดสอบ restore รายเดือนก่อนพึ่งพา backup ในเหตุการณ์จริง
