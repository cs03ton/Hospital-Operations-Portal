# Diagnostics Center

Diagnostics Center คือศูนย์ตรวจสอบปัญหาเชิงระบบสำหรับ HOP Phase 1.5 เพื่อให้ Admin/SuperAdmin ตรวจสถานะ service สำคัญ รัน test เฉพาะจุด อ่าน log แบบ redacted และสร้าง Support Bundle สำหรับส่งต่อทีม IT ได้อย่างปลอดภัย

## Route และ API

| ส่วน | ค่า |
|---|---|
| Frontend | `/admin/diagnostics` |
| Backend API | `/api/admin/diagnostics/*` |
| Permission View | `System.Diagnostics.View` |
| Permission Run | `System.Diagnostics.Run` |
| Permission Export | `System.Diagnostics.Export` |
| Role fallback | `Admin`, `SuperAdmin` |

## หน้าที่หลัก

| Tab | ใช้ทำอะไร |
|---|---|
| ภาพรวม | ดูสถานะ API, Database, Storage, Upload, PDF, LINE, Backup และ worker แบบย่อ |
| Tests | รัน diagnostic test ราย service เช่น database, storage, LINE text/flex, backup |
| Logs | อ่าน log จาก path ที่อนุญาต โดยระบบ mask secret/token/password ก่อนแสดง |
| Recent Errors | ดู error/audit ล่าสุดพร้อม Reference ID |
| Support Bundle | สร้างไฟล์ zip สำหรับส่งทีม IT วิเคราะห์ incident |
| History | ดูประวัติ diagnostic run และ support bundle |

## Security / Redaction

ระบบต้องไม่แสดงหรือ export ข้อมูลลับต่อไปนี้:

- JWT / Bearer token
- LINE Channel Secret / Access Token
- Password / Connection string password
- Cookie / CSRF token
- LINE User ID เต็ม
- เบอร์โทรศัพท์ / อีเมล / เลขบัตรประชาชน

ข้อมูล log และไฟล์ใน Support Bundle จะผ่าน `DiagnosticsRedactionService` ก่อนแสดงหรือบันทึกลง bundle

## Diagnostic Tests

| Test | รายละเอียด |
|---|---|
| `database` | ตรวจ `SELECT 1` และ latency |
| `storage` | ตรวจ storage root และการเขียนไฟล์ |
| `upload` | ตรวจ upload path ที่ระบบใช้งาน |
| `pdf` | ตรวจ template/font path เบื้องต้น |
| `line-text` | ส่งข้อความทดสอบไปยัง LINE Test User ID ถ้าตั้งค่าไว้ |
| `line-flex` | ส่ง Flex Message minimal เพื่อแยกปัญหา payload |
| `backup` | ตรวจ summary จาก Backup Center |
| `notification-worker` | ตรวจ pending/failed/retry ของ LINE delivery queue |

> **Note:** LINE test ใช้ค่าจาก backend config/env เท่านั้น และห้ามแสดง token บน frontend

## Support Bundle

Support Bundle เป็นไฟล์ `.zip` อายุ 24 ชั่วโมง โดยค่าเริ่มต้นเก็บไว้ใน:

```text
StorageRoot/diagnostics/support-bundles
```

หรือกำหนดเองได้ด้วย:

```text
Diagnostics__BundlePath=/opt/hop/support-bundles
Diagnostics__LogRoot=/opt/hop/logs
```

ข้อมูลที่รวมได้:

- Health summary
- Environment แบบ sanitized
- Migration history
- Recent errors
- Log ที่ถูก redaction
- Deployment snapshot แบบปลอดภัย

## Audit Events

| Event | Trigger |
|---|---|
| `Diagnostics.Viewed` | เปิด summary |
| `Diagnostics.TestStarted` | เริ่มรัน test |
| `Diagnostics.TestCompleted` | test สำเร็จหรือจบด้วย warning/unhealthy |
| `Diagnostics.TestFailed` | test ล้มเหลวจาก exception |
| `Diagnostics.SupportBundleGenerated` | สร้าง bundle |
| `Diagnostics.SupportBundleDownloaded` | ดาวน์โหลด bundle |

## Acceptance Checklist

- [ ] Admin/SuperAdmin เปิด `/admin/diagnostics` ได้
- [ ] Staff เปิดไม่ได้
- [ ] API response ไม่มี secret/token/password/connection string
- [ ] รัน test database/storage/upload/pdf ได้
- [ ] LINE test fail แล้ว workflow หน้าเว็บไม่ล้ม
- [ ] Logs แสดงข้อมูลที่ถูก mask แล้ว
- [ ] สร้าง Support Bundle ต้องกรอกเหตุผล
- [ ] ดาวน์โหลด Support Bundle ได้ด้วย permission `System.Diagnostics.Export`
- [ ] ตาราง `diagnostic_runs` และ `support_bundles` ถูกสร้างจาก migration

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
