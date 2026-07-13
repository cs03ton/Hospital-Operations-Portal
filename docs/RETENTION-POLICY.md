# Backup Retention Policy

เอกสารนี้อธิบายนโยบายการเก็บรักษาไฟล์สำรองข้อมูลของ HOP เพื่อควบคุมพื้นที่ดิสก์และยังคงมีหลักฐาน backup/restore ที่เพียงพอสำหรับ Phase 1 Production

## เป้าหมาย

1. เก็บ backup ล่าสุดเพียงพอสำหรับ rollback
2. ไม่ลบ backup ที่ผ่านการ verify หรือเคยใช้ restore
3. ลดความเสี่ยง disk เต็มจากไฟล์ backup สะสม
4. มีหลักฐานให้ตรวจสอบย้อนหลัง

## Scope

Retention ครอบคลุมไฟล์:

- PostgreSQL dump: `/opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup`
- Storage archive: `/opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz`
- Metadata ใน table `backup_runs`

## Policy แนะนำ

| ประเภท | ระยะเวลาเก็บ | หมายเหตุ |
|---|---:|---|
| Daily backup | 14-30 วัน | ใช้ rollback เหตุการณ์ล่าสุด |
| Weekly backup | 8 สัปดาห์ | ใช้ตรวจย้อนหลังระยะกลาง |
| Monthly backup | 12 เดือน | ควรย้ายไป off-server storage |
| Failed backup | 7 วัน | เก็บเพื่อวิเคราะห์ปัญหา |
| Verified backup | เก็บไว้จนกว่าจะมีรายการ verified ใหม่ที่ปลอดภัยกว่า | ไม่ควรลบทันที |
| Restored backup | เก็บไว้เป็นหลักฐาน | ไม่ลบอัตโนมัติ |

## Retention Guardrails

ระบบ Backup Center จะป้องกันไม่ให้ลบไฟล์ต่อไปนี้:

1. Backup ล่าสุดของแต่ละประเภท
2. Backup ที่สถานะ `Running`
3. Backup ที่ผ่านการ verify หากเปิด `KeepVerified`
4. Backup ที่เคยถูกอ้างอิงใน `restore_runs`

## ขั้นตอนใช้งานใน Backup Center

1. เปิด `จัดการระบบ` > `Backup Center`
2. ไปที่ tab `Retention`
3. กด `Preview Retention`
4. ตรวจรายการ `Keep` และ `Delete`
5. ตรวจเหตุผลของแต่ละรายการ
6. ถ้าต้องการลบ ให้กรอกเหตุผล
7. พิมพ์ confirmation text ตามที่ระบบกำหนด
8. กด `Apply Retention`

> **Warning:** Retention apply เป็นการลบไฟล์จริงจาก filesystem ควรทำเฉพาะเมื่อ preview ถูกตรวจสอบแล้ว

## Environment

ค่าพื้นฐานจาก script:

```env
BACKUP_ROOT=/opt/hop/backups
BACKUP_RETENTION_DAYS=30
```

สำหรับ policy รายละเอียด เช่น daily/weekly/monthly ให้ใช้ค่าที่แสดงใน Backup Center และเอกสาร deployment ของหน่วยงาน

## Audit และหลักฐาน

Retention action ต้องบันทึก:

- ผู้ดำเนินการ
- เหตุผล
- จำนวนไฟล์ที่ลบ
- ขนาดพื้นที่ที่คืนได้
- timestamp

## Checklist

- [ ] มี backup ล่าสุดก่อน apply retention
- [ ] preview ไม่มีไฟล์สำคัญอยู่ในกลุ่ม Delete
- [ ] verified/restored backup ถูก keep
- [ ] disk usage ลดลงหลัง apply
- [ ] audit log ถูกบันทึก

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
