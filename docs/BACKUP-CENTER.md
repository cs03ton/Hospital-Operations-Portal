# Backup Center

Backup Center คือศูนย์กลางสำหรับให้ SuperAdmin และผู้ได้รับสิทธิ์เฉพาะ ตรวจสอบประวัติการสำรองข้อมูล ตรวจสอบความสมบูรณ์ของไฟล์ ดูประวัติ restore และจัดการ retention policy ของ HOP

> **สำคัญ:** Backup Center ไม่แสดง secret, token, password หรือ connection string และไม่ควรใช้แทนขั้นตอน restore production ในช่วง maintenance window

## Route และสิทธิ์

| ส่วน | ค่า |
|---|---|
| Frontend | `/admin/backup` |
| เมนู | `จัดการระบบ` > `Backup Center` |
| ผู้ใช้งานหลัก | SuperAdmin |
| ผู้ใช้งานอื่น | เฉพาะผู้ได้รับ permission โดยตรง |
| Backend API | `/api/admin/backups` |

## Permission

| Permission | ใช้สำหรับ | ผู้ที่ควรได้สิทธิ์ |
|---|---|---|
| `System.Backup.View` | เปิดหน้า Backup Center และดู history | SuperAdmin / IT Support ที่ได้รับมอบหมาย |
| `System.Backup.Run` | ตรวจสอบไฟล์ backup และบันทึกผล verification | SuperAdmin |
| `System.Backup.Restore` | ทำ restore preview และบันทึก restore run | SuperAdmin เท่านั้น |
| `System.Backup.ManageRetention` | preview/apply retention policy | SuperAdmin เท่านั้น |

Staff, DepartmentHead, Director และผู้ใช้ทั่วไปไม่ควรเห็นเมนูนี้ และหากเข้าผ่าน URL ตรงต้องถูกปฏิเสธสิทธิ์

## โครงสร้างข้อมูล

ระบบมีตารางหลัก:

| Table | ใช้สำหรับ |
|---|---|
| `backup_runs` | เก็บ metadata ของไฟล์ backup เช่น path, size, checksum, status, verifiedAt |
| `restore_runs` | เก็บประวัติ restore preview/restore request พร้อมเหตุผลและผู้ดำเนินการ |

ไฟล์ backup จริงยังอยู่ที่ filesystem ของ server ไม่ได้เก็บ binary ใน database

## โครงสร้างไฟล์ Backup มาตรฐาน

Production bare-metal ใช้ path หลัก:

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

## Tabs ในหน้า Backup Center

| Tab | รายละเอียด |
|---|---|
| Overview | สรุป backup ล่าสุด, failed ล่าสุด, verified ล่าสุด, restore test ล่าสุด, ขนาดรวม และ policy |
| Backup History | ค้นหา/filter รายการ backup, ตรวจสอบสถานะ, verify, เลือก backup เพื่อ restore preview |
| Restore | แสดง backup ที่เลือก, preview ความพร้อม, กรอกเหตุผลและ confirmation ก่อนบันทึก restore |
| Restore History | ดูประวัติ restore request/restore test |
| Retention | preview รายการที่จะ keep/delete และ apply retention เมื่อได้รับอนุมัติ |
| Settings | แสดง path และ policy ที่ระบบอ่านจาก environment |

## API

| Method | Endpoint | Permission | รายละเอียด |
|---|---|---|---|
| GET | `/api/admin/backups/overview` | `System.Backup.View` | สรุปสถานะ backup |
| GET | `/api/admin/backups` | `System.Backup.View` | รายการ backup แบบ paging/filter |
| GET | `/api/admin/backups/{id}` | `System.Backup.View` | รายละเอียด backup |
| POST | `/api/admin/backups/{id}/verify` | `System.Backup.Run` | ตรวจ archive และบันทึก checksum |
| POST | `/api/admin/backups/{id}/restore-preview` | `System.Backup.Restore` | ตรวจความพร้อมก่อน restore |
| POST | `/api/admin/backups/{id}/restore` | `System.Backup.Restore` | บันทึก restore request พร้อมเหตุผล |
| GET | `/api/admin/backups/restore-runs` | `System.Backup.Restore` | ประวัติ restore |
| POST | `/api/admin/backups/retention/preview` | `System.Backup.ManageRetention` | preview policy |
| POST | `/api/admin/backups/retention/apply` | `System.Backup.ManageRetention` | ลบไฟล์ที่เข้าเงื่อนไข retention |

## Restore Safety

Restore flow บนหน้าเว็บถูกออกแบบให้เป็นขั้นตอนควบคุมความเสี่ยง:

1. เลือก backup จาก Backup History
2. กด preview เพื่อดู warning/error
3. กรอกเหตุผล
4. พิมพ์ confirmation text
5. ระบบบันทึก `restore_runs` และ audit log
6. การ restore production จริงต้องทำตาม runbook ในช่วง maintenance window

> **Warning:** ห้าม restore production ระหว่างมีผู้ใช้งานอยู่ และต้องมี pre-restore backup ก่อนเสมอ

## Retention Policy

ค่าเริ่มต้นที่แนะนำ:

| Policy | ค่า |
|---|---:|
| Daily | 14 วัน |
| Weekly | 8 สัปดาห์ |
| Monthly | 12 เดือน |
| Keep verified backups | เปิด |
| Keep failed backups | 7 วัน |

Retention จะไม่ลบ:

- backup ล่าสุดของแต่ละประเภท
- backup ที่ verified แล้ว หาก policy กำหนดให้เก็บ
- backup ที่เคยถูกใช้ใน restore history
- backup ที่ยังอยู่สถานะ Running

อ่านรายละเอียดเพิ่มเติมที่ [RETENTION-POLICY.md](RETENTION-POLICY.md)

## Environment ที่เกี่ยวข้อง

ตัวอย่าง `/etc/hop/backup.env`:

```env
BACKUP_MODE=host
BACKUP_ROOT=/opt/hop/backups
BACKUP_RETENTION_DAYS=30

DB_HOST=localhost
DB_PORT=5432
DB_NAME=hop_db
DB_USER=hop_user

UPLOADS_PATH=/opt/hop/uploads
LOG_FILE=/var/log/hop/backup.log
LOCK_FILE=/var/lock/hop-backup.lock
```

> **Warning:** ห้ามบันทึก `DB_PASSWORD`, JWT secret หรือ LINE token ลงใน repository หรือเอกสารคู่มือ

## Checklist สำหรับผู้ดูแลระบบ

- [ ] เห็นเมนู Backup Center เฉพาะ SuperAdmin หรือผู้ได้รับสิทธิ์
- [ ] Backup ล่าสุดอยู่ใน `/opt/hop/backups/postgres`
- [ ] ชื่อไฟล์ database เป็น `hopdb_YYYYMMDD_HHMMSS.backup`
- [ ] Storage backup อยู่ใน `/opt/hop/backups/storage`
- [ ] Backup log รอบล่าสุดไม่มี error
- [ ] กด verify backup แล้วสถานะเป็น Verified
- [ ] Restore preview แสดง warning/error ชัดเจน
- [ ] Restore history มีผู้ดำเนินการ เหตุผล และเวลา
- [ ] Retention preview ถูกตรวจสอบก่อน apply
- [ ] ไม่มี secret/token/password แสดงในหน้าเว็บหรือ log

## Troubleshooting

| อาการ | สาเหตุที่พบบ่อย | วิธีแก้ |
|---|---|---|
| ไม่พบ backup | `BACKUP_ROOT` ไม่ตรง | ตั้ง `BACKUP_ROOT=/opt/hop/backups` |
| verify database fail | ไม่มี `pg_restore` หรือไฟล์เสีย | ติดตั้ง PostgreSQL client หรือเลือกไฟล์ใหม่ |
| verify storage fail | ไม่มี `tar` หรือ archive เสีย | ตรวจ storage archive และ log |
| retention ไม่ลบไฟล์ | ไฟล์ถูก protect โดย policy | ตรวจเหตุผลใน retention preview |
| Staff เข้าแล้ว 403 | ถูกต้องตาม policy | ไม่ต้องแก้สิทธิ์ |

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
