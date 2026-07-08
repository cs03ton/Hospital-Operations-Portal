# Restore Test Evidence Template

เอกสารนี้ใช้บันทึกหลักฐานการทดสอบ Restore รายเดือนของ Hospital Operations Portal (HOP)

## ข้อมูลการทดสอบ

| รายการ | ค่า |
|---|---|
| วันที่ทดสอบ | |
| ผู้ทดสอบ | |
| Environment | Test / Staging |
| Backup timestamp | |
| Database dump | |
| Storage archive | |
| Restore target database | |
| Restore target storage | |

## ขั้นตอนและผลการตรวจ

| Step | Expected Result | Actual Result | Status |
|---|---|---|---|
| Restore database สำเร็จ | pg_restore completed without error | | Pass / Fail |
| Restore storage สำเร็จ | storage files extracted successfully | | Pass / Fail |
| Health live | `/health/live` returns success | | Pass / Fail |
| Health ready | `/health/ready` returns success | | Pass / Fail |
| Login | test user can login | | Pass / Fail |
| Users/Roles | users, roles, permissions available | | Pass / Fail |
| Leave Requests | leave requests visible as expected | | Pass / Fail |
| Attachments | attachment download works | | Pass / Fail |
| PDF | leave PDF download works | | Pass / Fail |
| Audit Logs | audit log data restored | | Pass / Fail |

## Evidence

- Restore command:
  ```bash
  # paste sanitized command here
  ```
- Log file path:
  ```text
  /opt/hop/backups/logs/restore_YYYYMMDD_HHMMSS.log
  ```
- Screenshot path:
  ```text
  docs/qa/screenshots/restore-test/YYYY-MM-DD/
  ```

## สรุปผล

- [ ] ผ่านทุกขั้นตอน
- [ ] พบปัญหา ต้องแก้ไขก่อน Production

หมายเหตุ:

> ห้ามแนบค่า secret/token/password ลงในเอกสารหลักฐานนี้

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
