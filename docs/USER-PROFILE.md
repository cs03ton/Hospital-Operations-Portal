# User Self Profile

ระบบมีหน้า `ข้อมูลส่วนตัวของฉัน` สำหรับให้ผู้ใช้งานแก้ไขข้อมูลส่วนตัวที่ใช้กับระบบลาและเอกสาร PDF ใบลา

## LINE Connection

หน้า `ข้อมูลส่วนตัวของฉัน` มี card `การเชื่อมต่อ LINE`

ถ้ายังไม่ได้เชื่อมต่อ:

- แสดงสถานะ `ยังไม่ได้เชื่อมต่อ`
- ปุ่ม `เชื่อมต่อ LINE`
- เมื่อกด ระบบแสดง QR Code, short code, ปุ่มคัดลอกรหัส และปุ่มเปิด LINE OA

ถ้าเชื่อมต่อแล้ว:

- แสดง Display Name
- แสดง LINE picture ถ้ามี
- แสดงวันที่เชื่อมต่อ
- ปุ่ม `ส่งข้อความทดสอบถึงฉัน`
- ปุ่ม `เชื่อมต่อ LINE บัญชีอื่น`
- ปุ่ม `ยกเลิกการเชื่อมต่อ LINE`

ดูรายละเอียดที่ [LINE-CONNECT-LINK.md](LINE-CONNECT-LINK.md)

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
- อีเมล
- เบอร์โทรศัพท์
- ที่อยู่ระหว่างลา / ที่อยู่ติดต่อ
- รูปโปรไฟล์ผ่านการอัปโหลดไฟล์

## Hidden System Fields

ข้อมูลระบบต่อไปนี้ไม่แสดงในหน้า profile ผู้ใช้งานทั่วไป และแก้ไขได้ผ่าน User Management โดยผู้ดูแลระบบเท่านั้น:

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
- อีเมล optional และต้องอยู่ในรูปแบบอีเมลที่ถูกต้อง
- เบอร์โทรศัพท์รองรับตัวเลข ช่องว่าง `+`, `-`, `(`, `)` ความยาว 6-30 ตัวอักษร
- ที่อยู่ระหว่างลา optional
- รูปโปรไฟล์ optional รองรับ JPG, PNG, WEBP ขนาดไม่เกิน 2 MB

## Profile Image

ระบบไม่ให้ผู้ใช้กรอก URL รูปเองแล้ว รูปจะถูกเก็บใน:

```text
storage/profile-images/{userId}/avatar.{ext}
```

API ที่เกี่ยวข้อง:

```text
POST /api/me/profile/image
DELETE /api/me/profile/image
GET /api/users/{id}/profile-image
```

เมื่ออัปโหลดหรือลบรูป ระบบ refresh `profileImageUrl` ผ่าน cache busting query string เพื่อให้ Header และหน้า profile เปลี่ยนรูปทันที

## Audit Event

เมื่อบันทึกข้อมูลสำเร็จ ระบบบันทึก:

```text
UserProfile.Updated
UserProfile.ImageUploaded
UserProfile.ImageDeleted
```

Audit detail เก็บเฉพาะชื่อ field ที่เปลี่ยน ไม่บันทึกข้อมูลส่วนตัวแบบละเอียดเกินจำเป็น

## Leave PDF Mapping

PDF ใบลาใช้ข้อมูลจาก profile:

- ชื่อผู้ขอลา
- ตำแหน่ง
- หน่วยงาน
- อีเมล
- เบอร์โทรศัพท์
- ที่อยู่ระหว่างลา

ถ้าไม่มีข้อมูลจะแสดง `-`
