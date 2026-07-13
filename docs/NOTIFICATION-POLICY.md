# Notification Policy

## Priority

| Priority | Use Case |
|---|---|
| Critical | Security, database, backup, or workflow blocker |
| High | Action needed soon, failed delivery, SLA risk |
| Normal | Standard work item |
| Information | Informational update |
| Success | Completed action |

## Notification Type

| Type | Meaning |
|---|---|
| ActionRequired | User should act or resolve an issue |
| Information | User only needs to know |

Action required notifications are shown before information notifications.

## Categories

| Category | Thai Label |
|---|---|
| Leave | ระบบลา |
| User | ระบบผู้ใช้ |
| Notification | ระบบแจ้งเตือน |
| Backup | ระบบสำรองข้อมูล |
| System | ระบบ |

## Expiration

Recommended defaults:

- ส่งคำขอลาสำเร็จ: 7 วัน
- อนุมัติแล้ว: 30 วัน
- ไม่อนุมัติ: 30 วัน
- ยกเลิกแล้ว: 30 วัน

Action required notifications disappear when:

- the underlying action is completed
- the user marks a persisted notification as read
- the notification is archived
- the notification expires

## Audit Events

Important notification lifecycle events:

- `Notification.Created`
- `Notification.Read`
- `Notification.Archived`
- `Notification.Expired`

The current API writes audit events for read/archive actions. Created/expired events should be written by future notification producer and expiration worker implementations.

## Leave Revision Notifications

| Event | ผู้รับ | ประเภท | รายละเอียด |
|---|---|---|---|
| `LeaveReturnedForRevisionToRequester` | ผู้ขอ | Information/High | แจ้งเหตุผลที่คำขอถูกตีกลับรอแก้ไข |
| `LeaveResubmittedToApprover` | ผู้อนุมัติ step เดิม | ActionRequired/High | แจ้งว่าผู้ขอส่งคำขอที่แก้ไขแล้ว |
| `LeaveRevisionCancelled` | ผู้ขอ | Information | ยืนยันการยกเลิกคำขอที่ถูกตีกลับรอแก้ไข |

เมื่อคำขอถูกตีกลับรอแก้ไข ระบบต้อง clear notification ประเภท action required ของผู้อนุมัติเดิมออกจากคิวงานรออนุมัติ และเมื่อผู้ขอส่งใหม่ ระบบจะแจ้งกลับไปยังผู้อนุมัติ step เดิม
