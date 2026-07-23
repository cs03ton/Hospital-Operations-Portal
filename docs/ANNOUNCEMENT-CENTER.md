# Announcement Center

Announcement Center คือศูนย์ข่าวสารและประกาศภายใน Hospital Operations Portal (HOP) สำหรับสื่อสารข่าวสำคัญ ประกาศระบบ และประกาศที่ต้องให้เจ้าหน้าที่รับทราบ

## Scope Phase 1.5

- หน้า feed ผู้ใช้งาน: `/announcements`
- หน้ารายละเอียดประกาศ: `/announcements/{id}`
- หน้าจัดการประกาศ: `/admin/announcements`
- รองรับสถานะ `Draft`, `Scheduled`, `Published`, `Expired`, `Archived`, `Cancelled`
- รองรับระดับความสำคัญ `Normal`, `Important`, `Critical`
- รองรับการแสดงผลแบบ featured, popup, banner ผ่าน flags
- รองรับ target แบบ `Everyone`, `Role`, `Department`, `User`, `Permission`
- Frontend รองรับการเลือก target value หลายรายการตาม target type เช่น หลายหน่วยงาน หลายบทบาท หรือหลายผู้ใช้
- รองรับ read tracking และ acknowledgement
- Backend enforce visibility จริงก่อนคืนข้อมูล
- รองรับ Cover Image, Gallery Images และไฟล์แนบหลายไฟล์ผ่าน Announcement Media Support
- รองรับ image preview บนหน้าเว็บโดยไม่ต้องเปิดหน้าใหม่
- รองรับการเลือกช่องทางแจ้งเตือนต่อประกาศ: ไม่ส่งแจ้งเตือน, Notification Bell, LINE หรือทั้งสองช่องทาง
- LINE notification ใช้ Flex Card พร้อมปุ่ม `ดูรายละเอียด`
- การส่ง LINE เป็น best-effort ผ่าน queue/retry และไม่ทำให้การเผยแพร่ประกาศ rollback ถ้า LINE ส่งไม่สำเร็จ
- ผู้ดูแลสามารถลบประกาศและ media ที่เกี่ยวข้องได้ตาม permission

## API

User APIs:

```text
GET /api/announcements/feed
GET /api/announcements/featured
GET /api/announcements/popup
GET /api/announcements/{id}
POST /api/announcements/{id}/acknowledge
GET /api/announcements/{id}/images
GET /api/announcements/images/{imageId}/{variant}
GET /api/announcements/files/{fileId}/download
```

Admin APIs:

```text
GET /api/admin/announcements
GET /api/admin/announcements/{id}
GET /api/admin/announcements/categories
POST /api/admin/announcements
PUT /api/admin/announcements/{id}
POST /api/admin/announcements/{id}/publish
POST /api/admin/announcements/{id}/notification-preview
POST /api/admin/announcements/{id}/unpublish
POST /api/admin/announcements/{id}/archive
POST /api/admin/announcements/{id}/cancel
POST /api/admin/announcements/{id}/duplicate
DELETE /api/admin/announcements/{id}
POST /api/admin/announcements/{id}/images
GET /api/admin/announcements/{id}/images
PUT /api/admin/announcements/{id}/images/order
DELETE /api/admin/announcements/images/{imageId}
POST /api/admin/announcements/{id}/attachments
DELETE /api/admin/announcements/files/{fileId}
```

## Database

ตารางหลัก:

- `announcements`
- `announcement_categories`
- `announcement_targets`
- `announcement_files`
- `announcement_images`
- `announcement_reads`
- `announcement_notification_deliveries`

Fields สำคัญใน `announcements`:

- `notify_in_app`: เปิด/ปิด Notification Bell สำหรับประกาศนั้น
- `notify_via_line`: เปิด/ปิด LINE notification สำหรับประกาศนั้น
- `notification_sent_at`: เวลาที่สร้าง Notification Bell สำเร็จ
- `line_notification_queued_at`: เวลาที่สร้าง LINE queue สำเร็จ
- `notification_dispatch_status`: สถานะ dispatch เช่น `Pending`, `Sent`, `Queued`, `Skipped`, `Failed`
- `notification_dispatch_error`: error แบบ sanitized สำหรับ admin ตรวจสอบ
- `notification_config_version`: version สำหรับ idempotency ป้องกันส่งซ้ำ

Migration:

```text
20260720030142_AddAnnouncementCenter
20260721144138_AddAnnouncementMediaSupport
20260722030007_AddAnnouncementNotificationChannels
```

## Notification Channels

ผู้ดูแลประกาศเลือกช่องทางแจ้งเตือนได้ในหน้าเพิ่ม/แก้ไขประกาศ:

| ช่องทาง | ผลลัพธ์ |
| --- | --- |
| ไม่เลือกช่องทาง | ประกาศยังแสดงใน feed/dashboard แต่ไม่สร้าง notification เพิ่ม |
| Notification Bell | สร้างรายการใน `notifications` ให้ผู้รับที่อยู่ใน target |
| LINE | สร้าง queue ใน `line_delivery_logs` ให้ผู้รับที่เชื่อมต่อ LINE แล้ว |
| ทั้งสองช่องทาง | สร้างทั้ง Notification Bell และ LINE queue |

ก่อนเผยแพร่ ระบบมี preview จำนวนผู้รับ เช่น จำนวนผู้รับทั้งหมด จำนวนผู้รับที่เชื่อมต่อ LINE และ warning สำหรับผู้ใช้ที่ยังไม่เชื่อมต่อ LINE

## Audit Events

- `Announcement.NotificationPreviewed`
- `Announcement.NotificationDispatched`
- `Announcement.NotificationSkipped`
- `Announcement.NotificationDispatchFailed`
- `Announcement.ScheduledPublished`
- `Announcement.Deleted`

## Remaining Gap

- Rich Content Editor เช่น Tiptap และ inline image ในเนื้อหาประกาศยังอยู่ใน roadmap ถัดไป
- Analytics เชิงลึก เช่น รายชื่อผู้ยังไม่รับทราบ ยังอยู่ใน roadmap ถัดไป

## User Guide

คู่มือผู้ใช้งานในระบบ Documentation Center:

- slug: `announcement-guide`
- source: `docs/user-guide/announcement.md`

รายละเอียด media อ่านเพิ่มเติมที่ `docs/ANNOUNCEMENT-MEDIA.md`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
