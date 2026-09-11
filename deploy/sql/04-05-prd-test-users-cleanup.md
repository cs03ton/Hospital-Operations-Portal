# เคลียร์บัญชีทดสอบและใบลาของบัญชีทดสอบ

## ขอบเขต

เฉพาะ `staff03`, `nm69003`, `nm69001`, `head01`, `director01`, `admin_support`, `staff02`, `staff01` และใบลาที่บัญชีเหล่านี้เป็นเจ้าของ พร้อมข้อมูลบัญชี/ยอดลาของเป้าหมายและประวัติที่ผูกเฉพาะใบลาเหล่านี้ ลบ `Information Technology` เมื่อไม่มี reference จากข้อมูลที่เก็บไว้

ไม่ลบผู้ใช้อื่น ไม่ปรับยอดสิทธิ์หรือ used/pending ของผู้ใช้อื่น ไม่ตั้งต้นปี 2570 ไม่ reset เลขเอกสาร ไม่แก้ role/permission, Fleet, ประกาศ หรือกฎอนุมัติร่วม

## 1. เตรียม maintenance และ backup

### ขอบเขตเพิ่มเติมที่ยืนยัน

- ลบ notifications, announcement_reads และ announcement_notification_deliveries เฉพาะผู้ใช้เป้าหมาย โดยเก็บตัวประกาศและประวัติผู้รับคนอื่น
- ลบ LINE delivery history ของผู้รับเป้าหมายเฉพาะที่ไม่มีใบลาอ้างอิงหรืออ้างใบลาเป้าหมาย หากอ้างใบลาที่เก็บไว้ยังเป็น blocker
- เลือกกฎอนุมัติของ Information Technology และขั้นตอนของกฎนั้นเป็น candidate เท่านั้น หากมีผู้ใช้อื่น ใบลา หรือ configuration ที่เก็บไว้อ้างถึง จะหยุดทั้ง transaction ไม่ลบกฎร่วม
- ไม่เลือกขั้นอนุมัติของกฎหน่วยงานอื่นเพียงเพราะผู้อนุมัติเป็นบัญชีทดสอบ และไม่ย้ายผู้อนุมัติอัตโนมัติ
- เก็บ admin ที่ย้ายหน่วยงานแล้ว เพราะไม่มี username อยู่ในเป้าหมาย 8 บัญชี
- ตามการยืนยันเพิ่มเติม อนุญาตลบ audit ที่ user_id, entity_id หรือ detail อ้าง UUID/เลขใบลาของข้อมูลที่เลือกไว้ แม้บันทึกนั้นเป็นประวัติที่บัญชีทดสอบแก้ข้อมูลของคนอื่น ตัวข้อมูลธุรกรรมของคนอื่นยังเก็บไว้ แต่ประวัติส่วนนั้นจะหายไป ต้องสำรองและตรวจรายงานก่อน
- ชุดเป้าหมาย audit ถูกตรึงก่อนเลือก audit เพิ่ม ไม่ขยายตาม audit ที่เพิ่งเลือกแบบวนซ้ำ และไม่ลบ audit ทั้งตาราง หากมีข้อมูลอื่นอ้าง audit ที่จะลบยังเป็น blocker
- ผล Preview เพิ่ม issue_count และ affected_rows ทั้งภาพรวมและแยกตาราง/เหตุผล แถวเดียวอาจมีหลาย issues จาก FK และ soft reference

เมื่อเปลี่ยนมาใช้ไฟล์ Cleanup รุ่นใหม่ ต้องเติม UUID ที่ยืนยันไว้ใน template อีกครั้ง ห้ามใช้ไฟล์เก่าที่เติม UUID แล้วแทนรุ่นใหม่ และต้องรัน Preview ใหม่ก่อนทุกครั้ง

ไฟล์นี้ยังไม่ได้รัน PRD ให้ตรวจ database/host ใน DBeaver ก่อนทุกครั้ง และปิดการรับรายการระหว่างดำเนินการ

จาก server ที่ติดตั้ง service ตาม repository:

```bash
sudo systemctl stop hop-api
sudo systemctl is-active hop-api
```

ต้องหยุด API instances และ worker อื่นที่เขียนฐานเดียวกันด้วย งาน background ภายใน hop-api หยุดตาม service ห้ามปล่อย process อีกเครื่องเขียนระหว่าง cleanup

สำรองฐานข้อมูลและ storage ด้วยกระบวนการ backup ที่ตั้งค่าไว้แล้ว Repository มี `scripts/backup/backup-hop.sh` ซึ่งใช้ `/etc/hop/backup.env` โดย default ตรวจ `DB_NAME`, `DB_HOST`, `UPLOADS_PATH`/`STORAGE_PATH` ว่าตรง PRD ก่อนเรียกจาก repository checkout:

```bash
sudo bash scripts/backup/backup-hop.sh
```

ยืนยันว่า backup ฐานและไฟล์สำเร็จ และมีวิธีกู้คืนที่ทดสอบแล้ว ห้ามใช้ storage path default โดยไม่ตรวจว่าเป็นตำแหน่งจริง

## 2. รัน Preview

เปิด `04-prd-test-users-preview.sql` ใน DBeaver และ Execute SQL Script ทั้งไฟล์ ตั้ง Stop on error, ไม่ commit ทีละ statement และไม่มี transaction งานอื่นค้างอยู่

Preview ไม่เขียนข้อมูลถาวร ใช้ temporary tables ใน transaction แล้ว `ROLLBACK` ตอนท้าย มีการล็อกตารางชั่วคราวเพื่อให้ข้อมูลที่ตรวจสอดคล้องกัน จึงควรรันใน maintenance window แม้เป็น preview

เก็บผลลัพธ์ต่อไปนี้ไว้ในพื้นที่ที่ผู้ดูแลเท่านั้นเข้าถึง:

- Username และ actual_id ของ 8 บัญชี ตรวจเทียบกับบัญชีที่ต้องการลบ
- Department ID ของ Information Technology
- จำนวน rows_to_delete แยกตามตาราง/เหตุผล
- Blocker table: ตาราง คอลัมน์/FK และ row_id ที่อ้างข้อมูลเป้าหมาย
- File manifest: stored_path และ shared_with_retained_row ส่งออก CSV ก่อน cleanup

หาก blocker มีแถว ห้ามข้ามการตรวจเพื่อบังคับลบ โดยเฉพาะผู้อนุมัติของใบลาคนอื่น, approval chain, Fleet, ประกาศ และผู้ใช้อื่นใน Information Technology ต้องตรวจและตัดสินใจเป็นรายกรณีก่อน งานรอบนี้ไม่ได้อนุญาตให้ย้าย reference หรือแก้ข้อมูลของคนอื่น

## 3. ยืนยัน UUID และ Cleanup

เปิดสำเนา `05-prd-test-users-cleanup.sql` แก้เฉพาะ UUID ในสองส่วนต้นไฟล์:

```sql
-- ตัวอย่างรูปแบบเท่านั้น ใช้ actual_id จาก preview จริง:
('staff03', 'UUID-จาก-preview'::uuid)
-- ทำให้ครบทั้ง 8 บัญชีใน cleanup_targets

INSERT INTO cleanup_department VALUES('UUID-หน่วยงาน-จาก-preview'::uuid);
```

ไฟล์ต้นฉบับเป็น NULL ทั้งหมดเพื่อไม่ให้ลบได้ทันทีโดยยังไม่ตรวจ identity หากบัญชีขาดตั้งแต่ preview แรก ต้องสืบ UUID จากรายการเป้าหมายที่ผู้ดูแลยืนยัน ห้ามใส่ UUID สุ่มเพื่อข้าม check สำหรับการรันซ้ำหลังสำเร็จให้คง UUID เดิมไว้ การไม่มีบัญชีแล้วถือเป็น no-op แต่ถ้า username ถูกสร้างใหม่ด้วย UUID อื่นจะหยุด

Execute SQL Script ทั้งไฟล์ โดยเลือก Stop on error และปิด per-statement commit Script ตรวจ blocker ใหม่ใน transaction เดียวกับการลบ ไม่ใช้ผล preview เก่าเป็นสิทธิ์ลบโดยตรง

- ไม่ใช้ TRUNCATE CASCADE และไม่ปิด foreign key/trigger
- ลบเฉพาะแถวที่ผ่าน ownership allowlist ตามลำดับ FK ที่ตรวจจาก catalog จริง
- ตรวจ retained rows ของทุก public table เทียบค่าทุกคอลัมน์ก่อน COMMIT เพื่อจับ trigger/cascade ที่เปลี่ยนข้อมูลอื่น
- ถ้าเกิด error: รัน `ROLLBACK;` ใน connection เดิมก่อนแก้ไขแล้วเริ่มใหม่ ห้ามรันต่อจากบรรทัดที่ error
- หากมี trigger ป้องกัน audit deletion จะ error และ rollback; script ไม่ปลดกลไกนั้น

ไฟล์มี COMMIT เมื่อทุก check ผ่าน ผล `deleted_rows` เป็นจำนวนที่ลบจริง การรันซ้ำด้วย UUID เดิมต้องไม่มีรายการเพิ่มให้ลบ

## 4. ไฟล์แนบและเปิดระบบ

SQL ไม่แตะ filesystem เก็บไฟล์ไว้จนยืนยันผล DB เสร็จ จาก manifest ให้ตรวจว่า path อยู่ใต้ storage จริง ไม่ใช่ symlink/path traversal และไม่ถูกใช้โดย retained row ก่อนย้ายไป quarantine ตามกระบวนการของผู้ดูแล ห้ามลบโฟลเดอร์ uploads/storage ทั้งหมด หรือรัน stored_path เป็น shell command

```bash
sudo systemctl start hop-api
sudo systemctl status hop-api --no-pager
sudo journalctl -u hop-api --since "10 minutes ago" --no-pager
```

หาก service ส่ง log ไปไฟล์ ให้ตรวจ `/opt/hop/logs/hop-api.log` และ `/opt/hop/logs/hop-api.err.log` ตาม systemd template ด้วย ตรวจบัญชีผู้ดูแลที่เก็บไว้ login ได้ ใบลา/ยอดสิทธิ์ของผู้ใช้อื่นและ Fleet ยังเหมือนเดิม

## ข้อจำกัดและการทดสอบ

SQL อ่าน public tables ทั้งหมดเป็น temporary snapshot เพื่อค้นหา FK และ UUID/เลขคำขอที่ฝังในข้อความ/JSON รวมทั้งตรวจ retained data จึงต้องมีสิทธิ์อ่านครบและพื้นที่ temp เพียงพอ ตั้ง timeout 180 วินาที; หากเกินให้ประเมินขนาดข้อมูลก่อนปรับค่า

บล็อก schema แบบ partition/foreign/RLS และ incoming FK ข้าม schema ที่ยังไม่รองรับ Soft-reference matching เป็นแบบ conservative อาจรายงานข้อความอ้าง UUID โดยไม่ได้เป็น FK จริง ต้องตรวจด้วยคน ไม่อนุมานว่าเป็นสิทธิ์ลบ และไม่ได้ยืนยัน reference ในระบบภายนอก, ข้อมูลเข้ารหัส หรือการอ้างด้วยชื่ออย่างเดียว

ทดสอบด้วย PostgreSQL 16 ฐานจำลองแยก ใช้ synthetic RBAC/Leave/Fleet tables พร้อม FK ไม่ใช่สำเนา PRD:

- Preview ไม่เปลี่ยนข้อมูล, cleanup สำเร็จและรันซ้ำ
- ไม่กรอก UUID และ username/UUID ไม่ตรง
- ใบลาคนอื่นอ้างผู้อนุมัติเป้าหมาย, Fleet อ้างเป้าหมาย, soft reference
- ผู้ใช้ที่เก็บไว้ยังอยู่ในหน่วยงานเป้าหมาย, schema ไม่ตรง
- Trigger เปลี่ยน retained row ต้อง rollback พร้อมทุกบัญชียังอยู่ครบ

ชุดทดสอบเพิ่มเติมครอบคลุมประวัติประกาศ/แจ้งเตือนส่วนตัว, admin ที่ย้ายหน่วยงาน, กฎร่วมที่ถูกอ้างจากผู้ใช้/ใบลา/configuration, ขั้นอนุมัติของหน่วยงานอื่น, LINE log ของใบลาคนอื่น และ audit ตามขอบเขตที่อนุมัติ รวม 19 กรณี โดยทดสอบ user_id/entity_id/detail, เก็บ audit ที่ไม่เกี่ยวข้อง และหยุดเมื่อมีข้อมูลอื่นอ้าง audit ที่จะลบ

สถานะตรวจรอบขยายขอบเขต audit (2026-09-11): ยืนยัน logic เลือกแถว/ตรวจ blocker ใน Preview กับ Cleanup ตรงกัน และรัน PostgreSQL regression tests ผ่านครบ 19 กรณี โดยสร้างฐานจำลองแยกต่อกรณีและลบฐานจำลองเมื่อจบ ไม่ได้ทดสอบหรือลบข้อมูล PRD ต้องรัน Preview ใหม่บน PRD และตรวจ blockers ก่อน Cleanup เสมอ

เรียก regression tests ได้ด้วย `pwsh -File tests/sql/test-users-cleanup.ps1` ใช้ Docker local และสร้าง/ลบเฉพาะฐานชื่อสุ่ม `hop_cleanup_test_*` ของแต่ละกรณี
