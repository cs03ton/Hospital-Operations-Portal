# 🛠️ คู่มือผู้ดูแลระบบ

คู่มือนี้สำหรับ Admin และ SuperAdmin ที่ดูแลระบบ Hospital Operations Portal (HOP) ใน Phase 1

> 💡 **บทบาทผู้ดูแล:** ดูแลข้อมูลหลัก สิทธิ์ผู้ใช้ สุขภาพระบบ การสำรองข้อมูล และช่วยเหลือผู้ใช้งาน โดยไม่แก้ workflow การอนุมัติแบบเงียบ ๆ

## 📊 Admin Dashboard

Admin Dashboard เป็นศูนย์ควบคุมผู้ดูแลระบบ ใช้ดูภาพรวมและกดไปหน้าจัดการจริง ไม่ใช่หน้าจัดการข้อมูลแบบตารางซ้ำ

ส่วนสำคัญที่ควรตรวจ:

| Card / Section | ใช้ทำอะไร |
|---|---|
| คำขอลาของฉัน | ตรวจคำขอลาที่เกี่ยวข้องกับบัญชีผู้ดูแล หากมี |
| คำขอยกเลิกใบลา | monitor คำขอยกเลิกใบลาตามสิทธิ์ support/admin |
| ผู้ใช้งาน / หน่วยงาน / บทบาทและสิทธิ์ | ดูตัวเลขรวมและกด `ไปจัดการ` |
| ระบบลา | ตรวจคำขอวันนี้และ balance ที่ควรตรวจสอบ |
| สิ่งที่ควรตรวจสอบ | รายการ warning/to-do ก่อนใช้งานจริง |
| Health Summary | ตรวจ API, Database, Storage, LINE, Backup, Disk |

> ⚠️ **ข้อควรระวัง:** Admin/SuperAdmin ไม่ใช่ผู้อนุมัติปกติและไม่ควรใช้ normal approve แทนผู้อนุมัติ หากต้อง support ต้องใช้ workflow/permission ที่ระบบกำหนดเท่านั้น

## 👥 จัดการผู้ใช้

1. ไปที่ `จัดการระบบผู้ใช้` > `จัดการผู้ใช้`
2. เพิ่ม แก้ไข ปิดใช้งาน หรือลบผู้ใช้ตามสิทธิ์
3. ตรวจ Role, Department, Employment Type, Gender และ Approval Rule

### ✅ Checklist เพิ่มผู้ใช้

- [ ] Username ไม่ซ้ำ
- [ ] ชื่อ-นามสกุลถูกต้อง
- [ ] Role ถูกต้องตามหน้าที่
- [ ] Department ถูกต้อง
- [ ] Approval Rule ถูกต้องสำหรับระบบลา
- [ ] LINE User ID ไม่ซ้ำกับผู้ใช้อื่น

## 🔑 การเปลี่ยนรหัสผ่านและการ Reset Password

ระบบแยกการเปลี่ยนรหัสผ่านออกเป็น 2 กรณี:

| กรณี | ผู้ดำเนินการ | ต้องรู้รหัสผ่านเดิม | ใช้เมื่อ |
|---|---|---:|---|
| Self-service password change | ผู้ใช้เจ้าของบัญชี | ✓ | ผู้ใช้ต้องการเปลี่ยนรหัสผ่านด้วยตนเอง |
| Admin reset password | Admin/SuperAdmin | - | ผู้ใช้ลืมรหัสผ่านหรือเข้าใช้งานไม่ได้ |

### Self-service password change

1. ผู้ใช้เข้าเมนูผู้ใช้งานมุมขวาบน
2. เลือก `เปลี่ยนรหัสผ่าน`
3. ระบบตรวจรหัสผ่านปัจจุบันและ Password Policy
4. เมื่อสำเร็จ ระบบ revoke session เดิมและให้ Login ใหม่

### Admin reset password

1. เปิด `จัดการผู้ใช้`
2. เลือกผู้ใช้ที่ต้องการช่วยเหลือ
3. ตั้งรหัสผ่านใหม่ตามนโยบายโรงพยาบาล
4. แจ้งผู้ใช้ให้ Login และเปลี่ยนรหัสผ่านเองทันที

> ⚠️ **ข้อควรระวัง:** ห้ามขอรหัสผ่านเดิมจากผู้ใช้ และห้ามส่งรหัสผ่านผ่านช่องทางสาธารณะ เช่น กลุ่ม LINE

## 🏢 จัดการหน่วยงาน

1. ไปที่ `จัดการหน่วยงาน`
2. เพิ่มหรือแก้ไขข้อมูลหน่วยงาน
3. ตรวจว่าหน่วยงานมีหัวหน้าหน่วยงานถูกต้อง

> 📌 **คำแนะนำ:** หน่วยงานที่มีคำขอหรือประวัติใช้งานแล้ว ควรพิจารณาปิดใช้งานแทนการลบ

## 🛡️ บทบาทและสิทธิ์

1. เปิด `บทบาทและสิทธิ์`
2. ตรวจ permission ตามบทบาท
3. หลีกเลี่ยงการให้สิทธิ์กว้างเกินจำเป็น

> ⚠️ **หลักความปลอดภัย:** ให้สิทธิ์เท่าที่จำเป็นต่อหน้าที่ ไม่ควรให้ `ViewAll` หรือ `Override` โดยไม่จำเป็น

## 🌿 ประเภทลาและนโยบายวันลา

1. ตรวจ `ประเภทการลา`
2. ตรวจ requires balance, carry over, fiscal year และ attachment policy
3. ตรวจ `กฎการอนุมัติวันลา`

## 💼 ยอดวันลาคงเหลือและยกยอดวันลา

1. เปิด `จัดการวันลาคงเหลือ`
2. ตรวจสิทธิ์ประจำปี ใช้ไปแล้ว รออนุมัติ และคงเหลือ
3. ใช้ rollover เฉพาะเมื่อผ่านการตรวจสอบข้อมูลปีงบประมาณแล้ว

> ✅ **ก่อน rollover:** สำรองฐานข้อมูล ตรวจปีงบประมาณ และตรวจ preview ให้ครบก่อน confirm

## 📲 LINE Operations Center

1. เปิด `จัดการระบบ` > `ตั้งค่า LINE`
2. ตรวจสถานะ LINE Enabled, Access Token, Channel Secret และ Test User
3. ส่งข้อความทดสอบเมื่อจำเป็น
4. ห้ามบันทึก token หรือ secret ลงในเอกสารหรือหน้าจอที่แชร์สาธารณะ

## 🩺 Health Center

1. เปิด `จัดการระบบ` > `Health Center`
2. ตรวจ API, Database, Storage, LINE, Disk, Memory, CPU และ Backup
3. หากสถานะเป็น Warning หรือ Unhealthy ให้ตรวจ log และแจ้ง IT

## 💾 Backup/Restore

1. ตรวจ `จัดการระบบ` > `Backup Center`
2. เปิด tab `Overview` เพื่อตรวจเวลาสำรองข้อมูลล่าสุด
3. เปิด tab `Backup History` เพื่อตรวจรายการ backup แบบ paging/filter
4. ชื่อไฟล์ database ต้องอยู่ในรูปแบบ `hopdb_YYYYMMDD_HHMMSS.backup`
5. path มาตรฐานของ database backup คือ `/opt/hop/backups/postgres`
6. ตรวจว่า storage backup อยู่ที่ `/opt/hop/backups/storage`
7. กด verify backup สำคัญเพื่อบันทึก checksum และสถานะ
8. ใช้ tab `Restore` เพื่อ preview และบันทึกเหตุผลก่อน restore
9. ใช้ tab `Restore History` เพื่อตรวจประวัติ restore test
10. ใช้ tab `Retention` เพื่อ preview/apply policy การลบ backup เก่า
11. ทดสอบ restore รายเดือนตาม checklist

### ✅ Checklist Backup Center

- [ ] สถานะ Backup ไม่เป็น `Unhealthy`
- [ ] พบไฟล์ล่าสุดใน `/opt/hop/backups/postgres`
- [ ] ไฟล์ database มีขนาดมากกว่า 0 byte
- [ ] พบไฟล์ storage `hop_uploads_YYYYMMDD_HHMMSS.tar.gz`
- [ ] log backup รอบล่าสุดไม่มี error
- [ ] มีหลักฐาน restore test ล่าสุด
- [ ] Retention preview ไม่มีไฟล์สำคัญอยู่ในรายการลบ
- [ ] Restore request มีเหตุผล ผู้ดำเนินการ และเวลา
- [ ] ไม่มี secret/token/password แสดงในหน้า Backup Center หรือ log

### 🔐 สิทธิ์ Backup Center

| Permission | ใช้สำหรับ |
|---|---|
| `System.Backup.View` | ดู Backup Center |
| `System.Backup.Run` | Verify backup |
| `System.Backup.Restore` | Restore preview / restore history |
| `System.Backup.ManageRetention` | จัดการ retention |

> ⚠️ **ข้อควรระวัง:** ควรให้สิทธิ์ restore และ retention เฉพาะ SuperAdmin หรือผู้ดูแลระบบที่ได้รับมอบหมายเป็นลายลักษณ์อักษรเท่านั้น

## 🚀 Deploy Checklist

- [ ] ตรวจ `.env` และ secret management
- [ ] ตรวจ database migration
- [ ] สำรองข้อมูลก่อน deploy
- [ ] บันทึกชื่อไฟล์ backup เช่น `hopdb_20260709_142201.backup`
- [ ] ทดสอบ login, dashboard, leave workflow, PDF และ LINE
- [ ] ตรวจ Health Center หลัง deploy

> 🧭 **แนวทางปฏิบัติ:** ทุกครั้งที่ deploy ควรมี backup, rollback plan และ smoke test หลัง deploy
