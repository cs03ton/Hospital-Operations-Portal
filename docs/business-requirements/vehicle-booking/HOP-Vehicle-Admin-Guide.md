# Fleet Admin Guide (Milestone 1)

Fleet Admin จัดการประเภทรถ รถ และ Driver Profile ด้วย permission `FleetVehicle.Manage` และ `FleetDriver.Manage` เท่านั้น

- ห้ามสร้าง user/department ซ้ำ; เลือกจากข้อมูล active ที่มีอยู่
- การเลิกใช้ master data ให้ตั้ง `IsActive=false`; ห้ามลบประวัติจริง
- เลขใบขับขี่เป็นข้อมูลจำกัดสิทธิ์และไม่ควรส่งออกโดยไม่จำเป็น
- ก่อน import ต้อง preview validation และยืนยันก่อน commit (จะเพิ่มใน milestone import)
- permission seed ไม่ assign role อัตโนมัติ ผู้ดูแลต้องกำหนดผ่าน Role Management
# Dashboard รถสำหรับผู้ดูแล

ผู้ดูแลที่มี Fleet permissions ครบจะเห็น section รวมทุก capability เพียงครั้งเดียว พร้อมภาพรวมรายเดือน งานเกินกำหนด และ Outbox failure เมนู รายงาน และตั้งค่า ยังคงถูกป้องกันด้วย permission เฉพาะ ห้ามใช้ badge หรือ Dashboard แทนข้อมูลหลักในฐานข้อมูล
