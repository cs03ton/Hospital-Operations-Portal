# Profile Image Upload

ระบบข้อมูลส่วนตัวของ HOP ใช้การอัปโหลดรูปโปรไฟล์แทนการกรอก URL เอง

## Storage

ไฟล์ถูกเก็บที่:

```text
storage/profile-images/{userId}/avatar.{ext}
```

เมื่ออัปโหลดรูปใหม่ ระบบจะแทนที่ไฟล์ `avatar.*` เดิมในโฟลเดอร์ของผู้ใช้งานคนนั้น

## File Rules

- รองรับ `jpg`, `jpeg`, `png`, `webp`
- จำกัดขนาดไม่เกิน `2 MB`
- ตรวจ content type ก่อนบันทึก
- ไม่ใช้ชื่อไฟล์จากผู้ใช้งานโดยตรง

## API

```http
GET    /api/me/profile
POST   /api/me/profile/image
DELETE /api/me/profile/image
GET    /api/users/{id}/profile-image
```

`POST /api/me/profile/image` ใช้ `multipart/form-data` field ชื่อ `file`

## LINE Binding

หน้า `ข้อมูลส่วนตัวของฉัน` มีส่วน `การเชื่อมต่อ LINE` สำหรับให้ผู้ใช้ผูก LINE OA กับบัญชี HOP ของตัวเอง

API ที่ใช้:

```http
GET  /api/me/profile/line
POST /api/me/profile/line/pairing-code
POST /api/me/profile/line/unbind
POST /api/me/profile/line/test-send
```

รายละเอียด flow อยู่ที่ [LINE-USER-BINDING.md](LINE-USER-BINDING.md)

## Cache Busting

`profileImageUrl` ที่ส่งกลับ frontend จะมี query string `?v={ticks}` จาก `profile_image_updated_at` เพื่อให้ Header และหน้า profile refresh รูปใหม่ทันที

## Audit Events

- `UserProfile.ImageUploaded`
- `UserProfile.ImageDeleted`

Audit log เก็บเฉพาะ metadata เช่น file name, content type และ file size ไม่เก็บ binary

## LINE Flex Message

LINE Flex จะใช้รูปโปรไฟล์เฉพาะเมื่อ `Line__PublicFileBaseUrl` / `PUBLIC_FILE_BASE_URL` หรือ `Line__PublicAppUrl` / `PUBLIC_APP_URL` เป็น URL ที่ LINE เข้าถึงได้จริง

ห้ามใช้ `localhost`, `127.0.0.1`, `10.x.x.x`, `172.16.x.x`, หรือ `192.168.x.x` สำหรับ image URL ใน LINE

ถ้าไม่มี public URL ระบบจะ fallback เป็น initials avatar เพื่อไม่ให้ Flex Message พัง
