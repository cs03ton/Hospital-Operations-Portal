# Leave Holidays

เอกสารนี้อธิบายการจัดการวันหยุดราชการในระบบ Leave Management ของ HOP

## Purpose

วันหยุดราชการใช้สำหรับ:

- แสดงวันหยุดในปฏิทินการลา
- ป้องกันการสร้างคำขอลาในวันหยุด
- ตัดวันหยุดออกจากการคำนวณจำนวนวันลา

## API

รายการวันหยุดแบบเดิมยังรองรับอยู่:

```http
GET /api/leave-holidays?year=2026
```

สำหรับหน้าจัดการวันหยุดราชการ ให้ใช้ pagination:

```http
GET /api/leave-holidays?year=2026&page=1&pageSize=20
GET /api/leave-holidays?year=2027&page=1&pageSize=20&search=สงกรานต์
```

Response แบบ pagination:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 100,
  "totalPages": 5
}
```

## Frontend Filters

หน้า `วันหยุดราชการ` รองรับ:

- ปี
- ค้นหาชื่อวันหยุด
- จำนวนรายการต่อหน้า 10, 20, 50
- เปลี่ยนหน้า

เมื่อเปลี่ยนปีหรือค้นหา ระบบจะกลับไปหน้า 1 อัตโนมัติ

## Permissions

การจัดการวันหยุดราชการต้องใช้ permission:

```text
LeaveAdmin.ManageHolidays
```

หน้าปฏิทินและฟอร์มลาสามารถอ่านวันหยุดตาม permission การดู Leave Request ที่มีอยู่เดิม

## Performance

ฐานข้อมูลมี index บน `holiday_date` แล้ว จึงรองรับการ filter ตามปีได้ดีสำหรับข้อมูลหลายปี

