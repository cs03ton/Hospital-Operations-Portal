# Department Management

เอกสารนี้อธิบายมาตรฐานการจัดการหน่วยงานใน Hospital Operations Portal (HOP) Phase 1

## Department Grid

หน้า `จัดการหน่วยงาน` ใช้ตารางมาตรฐานสำหรับค้นหา เรียงลำดับ และแบ่งหน้า

### Columns

| Column | รายละเอียด |
| --- | --- |
| Department Code | รหัสอ้างอิงจาก `department id` แบบย่อ |
| Department Name | ชื่อหน่วยงาน |
| Manager | เตรียมรองรับข้อมูลหัวหน้าหน่วยงานในอนาคต |
| Users Count | จำนวนผู้ใช้ที่อยู่ในหน่วยงาน |
| Status | ใช้งาน / ปิดใช้งาน |
| Created | วันที่สร้าง |
| Action | แก้ไข / ลบ |

### Search / Filter / Sort

- Search: ชื่อหน่วยงาน, รายละเอียด
- Filter: status
- Sort: code, name, users count, created date
- Pagination: 10, 20, 50, 100 รายการต่อหน้า

## Delete Policy

ระบบห้ามลบหน่วยงานที่มีข้อมูลอ้างอิง เพื่อป้องกันข้อมูลผู้ใช้ ประวัติลา และ audit log เสียหาย

### ห้ามลบเมื่อพบข้อมูลต่อไปนี้

- User ที่อยู่ในหน่วยงาน
- Approval Chain ที่ผูกกับหน่วยงาน
- Approval Escalation Rule ที่ผูกกับหน่วยงาน
- Leave Request ของผู้ใช้ในหน่วยงาน
- Audit Log ที่อ้างอิงหน่วยงาน

ถ้าพบข้อมูลอ้างอิง API จะคืนสถานะ `409 Conflict` พร้อม `DeleteResultResponse`

ข้อความที่แสดง:

> ไม่สามารถลบหน่วยงานได้ เนื่องจากมีข้อมูลอ้างอิงในระบบ

## API

`GET /api/departments`

รองรับ backward-compatible list และรองรับ paging เมื่อส่ง `page`

Query:

- `page`
- `pageSize`
- `search`
- `sort`
- `direction`
- `status`

`DELETE /api/departments/{id}`

ลบได้เฉพาะหน่วยงานที่ไม่มีข้อมูลอ้างอิง

## Audit

- `Department.Create`
- `Department.Edit`
- `Department.Delete`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
