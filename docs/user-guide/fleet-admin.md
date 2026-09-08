# ⚙️ คู่มือผู้ดูแลระบบรถ

คู่มือนี้สำหรับ Admin และ SuperAdmin ที่ดูแล Fleet Module

> 💡 **ภาพรวมเร็ว:** ดูแลข้อมูลรถและคนขับ → กำหนดสิทธิ์ → ตรวจข้อยกเว้น → ดูแล LINE Groups → ตรวจรายงานและ Audit

## 🗂️ Master Data และสิทธิ์

- จัดการประเภทรถ รถ Driver Profile และสถานะพร้อมใช้งาน
- ใช้ User เดิมเป็นแหล่งข้อมูลบุคลากร ห้ามสร้างบุคลากรซ้ำใน Fleet
- กำหนด permission ตามหน้าที่จริงผ่าน Role Management
- เลิกใช้ข้อมูลด้วยสถานะ inactive เพื่อรักษาประวัติ ห้ามลบ Assignment/Trip ที่ใช้งานจริง

## 🔧 การบำรุงรักษาและข้อยกเว้น

ติดตามการบำรุงรักษา เอกสารรถ เลขไมล์ และรถไม่พร้อมใช้งาน การ Override เลขไมล์หรือยุติทริปต้องมีเหตุผลและ Audit

## 🔔 LINE Groups

1. เปิด `จองรถ > LINE Groups`
2. ตรวจชื่อกลุ่ม endpoint และสถานะการเชื่อมต่อ
3. ยืนยันกลุ่มให้เป็น Active และเปิดเฉพาะ Fleet Event ที่ต้องการ
4. ทดสอบส่งและตรวจ Delivery Log

ค่าเชื่อมต่อจริงต้องเก็บผ่านหน้าตั้งค่าหรือระบบ configuration ที่ปลอดภัย ห้ามเขียน credential ลง source code หรือคู่มือ

สถานะที่ควรตรวจ:

- 🟡 `Pending` — ยังไม่ยืนยันปลายทาง
- 🟢 `Active` — พร้อมสร้าง delivery
- 🔄 `Retry` — ส่งไม่สำเร็จชั่วคราว ระบบจะลองใหม่
- 🔴 `Failed` / `Attention required` — ต้องตรวจ endpoint, credential หรือ policy
- ⚫ `Disabled` — ระบบไม่ส่งจนกว่าจะเปิดใหม่

> 🔐 **ความปลอดภัย:** ห้ามใส่ credential จริงในคู่มือหรือ source code และไม่ควรแสดง Destination ID แบบเต็มบนหน้าจอ

## 📊 Dashboard, รายงาน และ Feedback

Dashboard แสดงภาพรวม คิว งานเกินกำหนด และสถานะ Feedback รายงาน Feedback สำหรับบริหารต้องไม่เปิดเผยผู้ประเมิน ไม่แสดงคะแนนให้คนขับ และไม่ทำ Leaderboard

## 🩺 Checklist เมื่อระบบผิดปกติ

1. ตรวจ Database และ Backend health
2. ตรวจ Fleet feature flag และ permission
3. ตรวจ Outbox/Delivery Log โดยใช้ Correlation ID
4. ตรวจ destination และ event subscription
5. Retry เฉพาะ transient failure และต้องไม่ทำให้ Fleet transaction rollback
6. ห้ามแก้ข้อมูล production โดยตรงหากไม่มีแผน rollback และ audit
