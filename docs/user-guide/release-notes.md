# 📣 Release Notes

สรุปความสามารถสำคัญของ Hospital Operations Portal (HOP) ตามระยะพัฒนา

## 🚀 v1.0.0 Phase 1

- 🧭 Dashboard Hub
- 👥 User Management
- 🌿 Leave Management
- ✅ Approval Workflow
- 💼 Leave Balance
- 📄 PDF ใบลา
- 📲 LINE Notification
- 🧾 Audit Log

## ✨ v1.5.0 Phase 1.5

- 📊 Executive Dashboard
- 📈 Leave Analytics
- 🩺 Health Center
- 🛠️ Admin Dashboard
- 💾 Backup Center รองรับ path มาตรฐาน `/opt/hop/backups/postgres` และชื่อไฟล์ `hopdb_YYYYMMDD_HHMMSS.backup`
- 📚 Documentation Center
- 📢 Announcement Center สำหรับข่าวสาร/ประกาศประชาสัมพันธ์ภายใน
- 🖼️ Announcement Media รองรับรูปหน้าปก รูปภาพประกอบ preview บนเว็บ และไฟล์แนบ
- 🎯 Announcement Target รองรับทุกคน หน่วยงาน บทบาท บุคคล และ permission พร้อมเลือกหลายรายการ
- 🔔 Announcement Notification Channels เลือกส่ง Notification Bell และ LINE Flex Card รายประกาศ
- 🗑️ Admin สามารถลบประกาศและ media ที่เกี่ยวข้องตามสิทธิ์
- 🔑 Self-service password change
- 📎 Attachment preview สำหรับไฟล์แนบคำขอลา
- 🟠 Returned-for-revision workflow แสดงผลเป็น `ตีกลับรอแก้ไข`
- 📊 Dashboard แยกจำนวนคำขอ `ตีกลับรอแก้ไข` ของผู้ขอออกจากงานรออนุมัติ
- 🧑‍💼 Head Dashboard แยก `คำขอลาของฉันที่รออนุมัติ` และ `คำขอลาของหน่วยงาน`
- 🔎 รายการคำขอลารองรับตัวกรองขอบเขต `คำขอของฉัน` และ `คำขอของหน่วยงาน`
- 🎨 ปรับปรุง UI spacing และ card layout
- 📲 ปรับปรุง LINE Operations Center และ Flex Message Debug

## 🔐 Account Security Update

- เพิ่มเมนู `เปลี่ยนรหัสผ่าน` ใต้ user menu มุมขวาบน
- ผู้ใช้ต้องยืนยันรหัสผ่านปัจจุบันก่อนตั้งรหัสผ่านใหม่
- ระบบแสดง Password Policy และระดับความแข็งแรงของรหัสผ่าน
- หลังเปลี่ยนรหัสผ่านสำเร็จ ระบบออกจากระบบและให้ Login ใหม่
- Admin reset password ยังคงเป็น workflow แยกสำหรับกรณีผู้ใช้ลืมรหัสผ่าน

## 🚐 Fleet Operations Update

- เพิ่ม workflow ขอใช้รถ จัดรถ ตรวจสอบ อนุมัติ และงานขับรถ
- เพิ่มปฏิทินรถ Dashboard รายงาน Feedback การบำรุงรักษา เอกสารรถ และความสามารถของรถ
- เพิ่ม Emergency Request, Delegation และการควบคุม rollout ตามหน่วยงาน
- เพิ่ม LINE Group สำหรับ Fleet พร้อม Event Subscription, Test Send และ Delivery Log
- เพิ่มคู่มือตามบทบาทสำหรับผู้ขอ ผู้จัดรถ ผู้ตรวจสอบ ผู้อำนวยการ พนักงานขับรถ และผู้ดูแล

## 🔧 Repair Management Update

- เพิ่มการแจ้งซ่อมสำหรับงาน IT และช่างทั่วไป พร้อมรูปแนบและคิวงานตามทีม
- รองรับการเริ่มงาน รออะไหล่ บันทึกผลซ่อม ตรวจรับ ส่งกลับ เปิดงานซ้ำ และยกเลิก
- เพิ่ม Dashboard แจ้งซ่อม การตั้งค่าประเภท และช่องทางแจ้งเตือนหมอพร้อมแยกตามทีม

## 🏢 Meeting Room Booking Update

- เพิ่มปฏิทินห้องประชุมแบบเดือน สัปดาห์ และรายการ พร้อมตัวกรองห้อง
- เพิ่มการจองห้องแบบยืนยันทันทีเมื่อช่วงเวลาว่าง และป้องกันเวลาจองซ้ำ
- รองรับรายการจองของฉัน รายละเอียด ไฟล์แนบ Notification และประวัติรายการ
- เพิ่มหน้าผู้ดูแลสำหรับแก้ไขหรือยกเลิกรายการ และจัดการทะเบียนห้อง

## 🧩 แนวทางถัดไป

- เพิ่มภาพหน้าจอให้คู่มือสำคัญเมื่อหน้าจอเข้าสู่เวอร์ชันคงที่
- เพิ่ม workflow help ในหน้าที่ใช้งานบ่อย
- เพิ่ม release note แยกตามเดือนสำหรับ production
