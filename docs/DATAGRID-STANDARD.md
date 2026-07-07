# Data Grid Standard

เอกสารนี้กำหนดมาตรฐานตารางข้อมูลหลักของ Hospital Operations Portal (HOP)

## เป้าหมาย

ใช้ component กลางสำหรับหน้าจัดการข้อมูลที่มีรายการจำนวนมาก เช่น:

- User Management
- Department Management
- Role & Permission Management
- Holiday
- Leave Balance
- Approval Chain
- LINE Users
- Notification Logs
- Audit Logs

## Component

Frontend ใช้:

`frontend/src/components/common/ManagementDataGrid.tsx`

รองรับ:

- Pagination
- Sorting
- Filtering toolbar
- Search toolbar
- Sticky Header
- Loading State
- Empty State ภาษาไทย
- Responsive table container
- Rows per page: 10, 20, 50, 100

## Delete Dialog

ทุกหน้าที่ลบข้อมูลควรใช้:

`frontend/src/components/common/ConfirmDeleteDialog.tsx`

Dialog ต้องแสดง:

- ชื่อรายการ
- ผลกระทบ
- ข้อมูลอ้างอิงถ้ามี
- ปุ่มยกเลิก
- ปุ่มยืนยัน

## API Standard

Endpoint list ควรรองรับ query parameters:

| Parameter | ความหมาย |
| --- | --- |
| `page` | เลขหน้า เริ่มที่ 1 |
| `pageSize` | จำนวนรายการต่อหน้า |
| `search` | คำค้นหา |
| `sort` | column key สำหรับเรียงลำดับ |
| `direction` | `asc` หรือ `desc` |
| `status` | `all`, `active`, `inactive` |

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 100,
  "totalPages": 5
}
```

## Delete Result Standard

Endpoint delete ควรคืน `DeleteResultResponse`

```json
{
  "action": "SoftDeleted",
  "message": "ไม่สามารถลบได้ เนื่องจากมีข้อมูลอ้างอิง ระบบจึงปิดใช้งานแทน",
  "references": [
    { "label": "Leave Requests", "count": 12 },
    { "label": "Audit Logs", "count": 40 }
  ]
}
```

ค่า `action`:

- `Deleted`
- `SoftDeleted`
- `Blocked`

## UX Rules

- Search หรือ filter แล้วต้องกลับไปหน้า 1
- Sort แล้วต้องกลับไปหน้า 1
- Empty state ต้องเป็นภาษาไทย
- Action icon ต้องมี tooltip ภาษาไทย
- Delete ต้องมี confirm dialog เสมอ
- ห้าม hard delete ถ้ามีข้อมูลอ้างอิง

## Backend Rules

- Backend ต้อง enforce permission และ delete protection จริง
- Frontend guard ใช้เพื่อ UX เท่านั้น
- ข้อมูลที่มี audit/history ต้องใช้ soft delete หรือ block
- Delete operation ต้องบันทึก audit log

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
