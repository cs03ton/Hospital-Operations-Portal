# User Self Profile

ระบบมีหน้า `ข้อมูลส่วนตัวของฉัน` สำหรับให้ผู้ใช้งานแก้ไขข้อมูลส่วนตัวที่ใช้กับระบบลาและเอกสาร PDF ใบลา

## Route

```text
/profile
```

เข้าถึงจาก user menu มุมขวาบนของ Header

## API

```text
GET /api/me/profile
PUT /api/me/profile
```

Endpoint นี้ใช้ authenticated user จาก access token เท่านั้น ผู้ใช้จึงแก้ไขได้เฉพาะข้อมูลของตนเอง

## Editable Fields

ผู้ใช้แก้ไขเองได้:

- ชื่อ-นามสกุล
- ตำแหน่ง
- เบอร์โทรศัพท์
- ที่อยู่ระหว่างลา / ที่อยู่ติดต่อ
- รูปโปรไฟล์ URL

## Readonly Fields

ผู้ใช้เห็นได้แต่แก้ไขเองไม่ได้:

- Username
- Role
- LINE User ID
- Department
- Approval Rule
- สถานะบัญชี
- Permission

ข้อมูลกลุ่มนี้ต้องแก้ผ่าน User Management โดยผู้ดูแลระบบเท่านั้น

## Validation

- ชื่อ-นามสกุลต้องไม่ว่าง
- เบอร์โทรศัพท์รองรับตัวเลข ช่องว่าง `+`, `-`, `(`, `)` ความยาว 6-30 ตัวอักษร
- ที่อยู่ระหว่างลา optional
- รูปโปรไฟล์ optional

## Audit Event

เมื่อบันทึกข้อมูลสำเร็จ ระบบบันทึก:

```text
UserProfile.Updated
```

Audit detail เก็บเฉพาะชื่อ field ที่เปลี่ยน ไม่บันทึกข้อมูลส่วนตัวแบบละเอียดเกินจำเป็น

## Leave PDF Mapping

PDF ใบลาใช้ข้อมูลจาก profile:

- ชื่อผู้ขอลา
- ตำแหน่ง
- หน่วยงาน
- เบอร์โทรศัพท์
- ที่อยู่ระหว่างลา

ถ้าไม่มีข้อมูลจะแสดง `-`
