# Announcement Media Support

Announcement Media Support เพิ่มความสามารถให้ประกาศมีรูปหน้าปก รูปประกอบแบบ Gallery และไฟล์แนบ โดยยังรักษา backward compatibility กับประกาศเดิมที่ใช้ `cover_image_url`

## 1. Architecture

- Backend เก็บ metadata ใน PostgreSQL
- ไฟล์จริงเก็บใน Local Storage ผ่าน `Storage__RootPath`
- Frontend เรียกรูปและไฟล์ผ่าน API endpoint เท่านั้น
- ไม่เปิด public static directory ทั้ง storage root
- ใช้ `IFileScanningService` เดิมก่อนบันทึกไฟล์ถาวร
- ใช้ SkiaSharp สำหรับ decode และ resize รูปภาพ
- Frontend แสดงรูปภาพประกอบแบบ preview dialog บนหน้าเว็บ ไม่เปิดรูปใน tab ใหม่

## 2. Storage Structure

```text
{Storage:RootPath}/announcements/{yyyy}/{MM}/{announcementId}/images/
{Storage:RootPath}/announcements/{yyyy}/{MM}/{announcementId}/attachments/
```

ระบบสร้างชื่อไฟล์จริงด้วย GUID และเก็บชื่อไฟล์ต้นฉบับไว้ในฐานข้อมูล

## 3. Database Schema

ตารางใหม่:

- `announcement_images`

ฟิลด์สำคัญ:

- `announcement_id`
- `original_file_name`
- `stored_file_name`
- `relative_path`
- `large_path`
- `medium_path`
- `thumbnail_path`
- `mime_type`
- `file_size`
- `width`
- `height`
- `display_order`
- `is_cover`
- `created_by_user_id`
- `updated_by_user_id`

ตารางเดิมที่ใช้ต่อ:

- `announcement_files`

เพิ่มฟิลด์:

- `created_by_user_id`

> Note: `announcements.cover_image_url` ยังไม่ถูกลบ ใช้เป็น fallback สำหรับประกาศเก่า

## 4. API Endpoints

User/read endpoints:

```text
GET /api/announcements/{announcementId}/images
GET /api/announcements/images/{imageId}/thumbnail
GET /api/announcements/images/{imageId}/medium
GET /api/announcements/images/{imageId}/large
GET /api/announcements/images/{imageId}/original
GET /api/announcements/files/{fileId}/download
```

Admin endpoints:

```text
POST /api/admin/announcements/{announcementId}/images
GET /api/admin/announcements/{announcementId}/images
PUT /api/admin/announcements/{announcementId}/images/order
DELETE /api/admin/announcements/images/{imageId}
POST /api/admin/announcements/{announcementId}/attachments
DELETE /api/admin/announcements/files/{fileId}
```

## 5. Validation Rules

รูปภาพ:

- รองรับ `.jpg`, `.jpeg`, `.png`, `.webp`
- ขนาดไม่เกิน 10 MB ต่อไฟล์
- ตรวจ extension
- ตรวจ MIME type
- ตรวจ magic bytes / file signature
- decode รูปด้วย SkiaSharp เพื่อยืนยันว่าอ่านได้จริง
- ตรวจ maximum width, height และ pixel count

ไฟล์แนบ:

- รองรับ `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.zip`
- ขนาดไม่เกิน 10 MB ต่อไฟล์
- ใช้ allowlist เท่านั้น

## 6. Security

- ทุกไฟล์ต้องผ่าน `IFileScanningService`
- ไม่ส่ง physical path หรือ relative disk path ไป frontend
- Download/Image endpoint ตรวจสิทธิ์ประกาศทุกครั้ง
- Admin upload/delete ใช้ permission เดิมของ Announcement เช่น `Announcement.Manage`, `Announcement.EditOwn`, `Announcement.EditAll`
- ใช้ `Path.GetFullPath` และตรวจว่า path อยู่ใต้ `Storage:RootPath`
- ป้องกัน path traversal

## 7. Image Variants

ระบบสร้าง variants:

- `original`
- `large`
- `medium`
- `thumbnail`

ค่า default:

- large: ด้านยาวไม่เกิน 1600 px
- medium: ด้านยาวไม่เกิน 800 px
- thumbnail: 400 x 225 px แบบ crop สำหรับ Card

## 8. Configuration

ตัวอย่าง environment:

```text
Storage__RootPath=/opt/hop/storage
Storage__Announcements__MaxImageSizeBytes=10485760
Storage__Announcements__MaxAttachmentSizeBytes=10485760
Storage__Announcements__AllowedImageExtensions__0=.jpg
Storage__Announcements__AllowedImageExtensions__1=.jpeg
Storage__Announcements__AllowedImageExtensions__2=.png
Storage__Announcements__AllowedImageExtensions__3=.webp
```

## 9. Deployment

1. Deploy backend build ใหม่
2. Deploy frontend build ใหม่
3. Run EF migration `AddAnnouncementMediaSupport`
4. ตรวจว่า service user มีสิทธิ์อ่าน/เขียน `Storage__RootPath`
5. ตรวจ FileScan/ClamAV ตาม production policy

ตัวอย่างสิทธิ์ Linux:

```bash
sudo mkdir -p /opt/hop/storage/announcements
sudo chown -R hop:hop /opt/hop/storage
sudo chmod -R 750 /opt/hop/storage
```

## 10. Backup / Restore

Database backup อย่างเดียวไม่เพียงพอ ต้อง backup storage root ด้วย

สิ่งที่ต้อง backup:

- PostgreSQL database
- `{Storage:RootPath}/announcements`
- attachments และ media อื่นใน storage root

ลำดับ restore ที่แนะนำ:

1. Restore database
2. Restore storage files
3. ตรวจ permission ของ storage
4. เปิดหน้า Announcement Detail เพื่อทดสอบ cover/gallery/download

## 11. Cleanup / Orphan Handling

- หาก validation หรือ virus scan ไม่ผ่าน จะไม่บันทึกไฟล์ถาวร
- หาก upload ไฟล์สำเร็จแต่ DB save ล้มเหลว ระบบพยายามลบไฟล์ที่สร้างแล้ว
- หากลบ image/file จากระบบ จะลบไฟล์จริงก่อนลบ metadata

## 12. Troubleshooting

| อาการ | สาเหตุที่เป็นไปได้ | วิธีตรวจ |
|---|---|---|
| Upload ไม่ได้ | Storage permission ไม่ถูกต้อง | ตรวจ owner/mode ของ `Storage__RootPath` |
| ไฟล์ถูก reject | MIME/signature ไม่ตรง | ลองอัปโหลดไฟล์จริงจากโปรแกรมที่เชื่อถือได้ |
| รูปไม่แสดง | media endpoint ไม่มีสิทธิ์อ่านประกาศ | ตรวจ target/permission ของประกาศ |
| คลิกรูปแล้วไม่ preview | Browser cache หรือ image endpoint ตอบ error | refresh หน้าเว็บและตรวจ Network ของ image variant |
| ClamAV error | FileScan fail-closed | ตรวจ service/socket ของ ClamAV |

## 13. User Guide Integration

คู่มือสำหรับผู้ใช้และผู้ดูแลประกาศถูกผูกใน Documentation Center แล้ว:

- slug: `announcement-guide`
- source: `docs/user-guide/announcement.md`
- route: `/docs/announcement-guide`

## 14. Rollback

Rollback migration:

```bash
dotnet ef database update <migration-before-AddAnnouncementMediaSupport>
```

ก่อน rollback production ต้อง backup database และ storage ก่อนทุกครั้ง

## 15. Phase Next

ยังไม่รวม Rich Content Editor ในเฟสนี้ สิ่งที่เตรียมไว้สำหรับเฟสถัดไป:

- image endpoint แยก variant แล้ว
- storage structure รองรับ media หลายประเภท
- DTO แยก cover/gallery/attachments ชัดเจน

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
