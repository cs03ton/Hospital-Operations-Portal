# Announcement Policy

## Visibility

ประกาศจะถูกแสดงเมื่อเข้าเงื่อนไขทั้งหมด:

1. `status = Published`
2. `publish_at` ว่าง หรือถึงเวลาเผยแพร่แล้ว
3. `expires_at` ว่าง หรือยังไม่หมดอายุ
4. ผู้ใช้อยู่ใน target ของประกาศ

ถ้าประกาศไม่มี target ระบบถือว่าเป็น `Everyone`

## Target Types

| Target Type | วิธีใช้ |
| --- | --- |
| `Everyone` | ทุกผู้ใช้งานที่มี `Announcement.View` |
| `Role` | ระบุชื่อ role เช่น `Staff`, `DepartmentHead`, `Director` |
| `Department` | ระบุ department UUID |
| `User` | ระบุ user UUID |
| `Permission` | ระบุ permission code เช่น `LeaveAdmin.ManageHolidays` |

## Acknowledgement

ถ้า `requires_acknowledgement = true` ผู้ใช้ต้องกด “รับทราบประกาศ” ในหน้ารายละเอียด

ระบบบันทึก:

- `read_at`
- `acknowledged_at`
- audit event `Announcement.Read`
- audit event `Announcement.Acknowledged`

## Admin Rules

- `Draft` ลบได้ผ่าน `Announcement.DeleteDraft`
- `Published` ควร archive/cancel แทนการลบ
- `Archived` ใช้สำหรับเก็บประกาศที่หมดรอบใช้งาน
- `Cancelled` ใช้สำหรับประกาศที่ยกเลิกหรือประกาศผิด

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
