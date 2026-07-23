# Announcement Notification Channels

เอกสารนี้อธิบายการเลือกช่องทางแจ้งเตือนต่อประกาศใน Hospital Operations Portal (HOP)

## แนวคิด

ประกาศทุกฉบับยังแสดงในศูนย์ประกาศและ Dashboard ตามกลุ่มเป้าหมายเสมอ ส่วนช่องทางแจ้งเตือนเป็นตัวเลือกเพิ่มเติมต่อประกาศหนึ่งรายการ

| ตัวเลือก | ความหมาย |
| --- | --- |
| ไม่ส่งแจ้งเตือน | แสดงเฉพาะใน feed/dashboard |
| Notification Bell | สร้าง notification ในระบบให้ผู้รับ |
| LINE | สร้าง LINE delivery queue ให้ผู้รับที่เชื่อมต่อ LINE แล้ว |
| ทั้งสองช่องทาง | สร้างทั้ง notification ในระบบและ LINE queue |

ค่าเริ่มต้น:

- `notify_in_app = true`
- `notify_via_line = false`

## Publish Flow

1. ผู้ดูแลสร้างหรือแก้ไขประกาศ
2. เลือกกลุ่มเป้าหมาย เช่น ทุกคน หน่วยงาน บทบาท ผู้ใช้ หรือสิทธิ์
3. เลือกช่องทางแจ้งเตือน
4. ก่อนเผยแพร่ ระบบ preview จำนวนผู้รับ
5. เมื่อ publish สำเร็จ ระบบ dispatch ตามช่องทางที่เลือก
6. หากเลือก LINE ระบบส่งเป็น Flex Card พร้อมหัวข้อ สรุป และปุ่ม `ดูรายละเอียด`
7. ถ้า LINE ส่งไม่สำเร็จ ประกาศยังเผยแพร่ตามปกติ และ LINE retry worker จะดำเนินการต่อ

## Backend Source of Truth

Backend enforce สิทธิ์และช่องทางด้วย field ใน `announcements`:

- `notify_in_app`
- `notify_via_line`
- `notification_sent_at`
- `line_notification_queued_at`
- `notification_dispatch_status`
- `notification_dispatch_error`
- `notification_config_version`

ตาราง `announcement_notification_deliveries` ใช้เก็บ ledger การส่งรายผู้รับ พร้อม `idempotency_key` เพื่อป้องกันการส่งซ้ำ

## API

```text
POST /api/admin/announcements/{id}/notification-preview
POST /api/admin/announcements/{id}/publish
```

Preview response แสดง:

- จำนวนผู้รับทั้งหมด
- จำนวนผู้รับ Notification Bell
- จำนวนผู้รับ LINE ที่เชื่อมต่อแล้ว
- จำนวนผู้รับที่ยังไม่เชื่อมต่อ LINE
- warning ที่ควรตรวจสอบก่อน publish

## Permissions

| Permission | ใช้ทำอะไร |
| --- | --- |
| `Announcement.Notification.Configure` | กำหนดช่องทางแจ้งเตือน |
| `Announcement.Notification.Preview` | ดู preview ผู้รับก่อนส่ง |
| `Announcement.Notification.SendInApp` | ส่ง Notification Bell |
| `Announcement.Notification.SendLine` | ส่ง LINE |
| `Announcement.Notification.ViewDelivery` | ดูสถานะ delivery |
| `Announcement.Notification.RetryFailed` | retry รายการที่ล้มเหลวในอนาคต |

## Audit Events

- `Announcement.NotificationPreviewed`
- `Announcement.NotificationDispatched`
- `Announcement.NotificationSkipped`
- `Announcement.NotificationDispatchFailed`
- `Announcement.ScheduledPublished`

## ข้อควรระวัง

> **Warning:** LINE เป็น best-effort channel หาก LINE API ล่มหรือผู้ใช้ยังไม่เชื่อมต่อ LINE ระบบต้องไม่ rollback การเผยแพร่ประกาศ

> **Note:** ผู้ใช้ที่ไม่มี `LineUserId` จะยังเห็นประกาศตามสิทธิ์ในระบบ แต่จะไม่ได้รับ LINE

## User Guide

ผู้ใช้งานและผู้ดูแลสามารถอ่านวิธีใช้งานประกาศได้ที่ Documentation Center:

- `/docs/announcement-guide`
- source file: `docs/user-guide/announcement.md`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
