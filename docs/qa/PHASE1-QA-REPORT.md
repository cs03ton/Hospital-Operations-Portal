# Phase 1 QA Report - Login and Leave Management

## Test Summary

- Scope: Login / Logout and Leave Management Phase 1
- Result: 17 passed, 1 failed
- Run stamp: 20260619070151
- Approved request id: 019edeaf-c1c3-7bc1-8efa-143bcecbe958
- Rejected request id: 019edeaf-d426-7999-b5ee-c820c512fb18

## Environment

| Item | Value |
|---|---|
| Frontend | http://127.0.0.1:5173 |
| Backend API | http://localhost:5000 |
| Database | PostgreSQL Docker `hop-postgres` on localhost:55432 |
| Browser automation | Chrome headless via Playwright CDP |
| Screenshot folder | docs/qa/screenshots/phase1/ |

## Test Account

| Username | Role | Purpose |
|---|---|---|
| admin | SuperAdmin | Login, Leave create, submit, approve, reject, PDF, attachment test |

## Test Results

| No | Feature | Step | Expected | Actual | Status | Screenshot |
|---|---|---|---|---|---|---|
| 1 | Login | เปิดหน้าเข้าสู่ระบบ | เห็นฟอร์มภาษาไทยและโลโก้ | แสดงหน้าเข้าสู่ระบบภาษาไทย | PASS | docs/qa/screenshots/phase1/01-login-page.png |
| 2 | Login | กรอก credential ผิด | แสดง validation/error ภาษาไทย | แสดงข้อความเข้าสู่ระบบไม่สำเร็จ | PASS | docs/qa/screenshots/phase1/02-login-invalid-message.png |
| 3 | Login | เข้าสู่ระบบด้วย admin | เข้าสู่ Dashboard ได้ | เข้าสู่ Dashboard สำเร็จ | PASS | docs/qa/screenshots/phase1/03-login-success-dashboard.png |
| 4 | Leave | เปิดรายการคำขอลา | เห็นหน้ารายการคำขอลา | แสดงรายการคำขอลา | PASS | docs/qa/screenshots/phase1/04-leave-list.png |
| 5 | Leave Create | เปิดฟอร์มสร้างคำขอลา | เห็นฟอร์มสร้างคำขอลาภาษาไทย | แสดงฟอร์มสร้างคำขอลา | PASS | docs/qa/screenshots/phase1/05-leave-create-form.png |
| 6 | Leave Create | กดบันทึกโดยข้อมูลไม่ครบ | แสดง validation ภาษาไทย | ฟอร์มยังอยู่และแสดง validation/required state | PASS | docs/qa/screenshots/phase1/06-leave-create-validation.png |
| 7 | Leave Create | สร้างคำขอลาแบบร่าง | เปิดรายละเอียดแบบร่างได้ | สร้าง draft ผ่านและแสดงรายละเอียด | PASS | docs/qa/screenshots/phase1/07-leave-draft-created.png |
| 8 | Leave Attachment | อัปโหลดไฟล์ PDF | แนบไฟล์สำเร็จและเห็นในตาราง | อัปโหลดไฟล์แนบสำเร็จ | PASS | docs/qa/screenshots/phase1/08-leave-attachment-upload-success.png |
| 9 | Leave Attachment | ดาวน์โหลดไฟล์แนบ | ต้องดาวน์โหลดไฟล์ได้ | FAIL: GET /api/leave-attachments/019edeaf-c99f-7e45-91cc-ad92a5928908/download -> 403:  | FAIL | docs/qa/screenshots/phase1/09-leave-attachment-download-failed.png |
| 10 | Leave PDF | ดาวน์โหลดใบลา PDF | ได้ไฟล์ PDF | ดาวน์โหลด PDF ผ่าน endpoint สำเร็จ | PASS | docs/qa/screenshots/phase1/10-leave-pdf-download-success.png |
| 11 | Leave Submit | ส่งคำขออนุมัติ | สถานะเปลี่ยนเป็นรออนุมัติ | ส่งคำขอสำเร็จ | PASS | docs/qa/screenshots/phase1/11-leave-submit-success.png |
| 12 | Leave Approval | อนุมัติคำขอลา | สถานะเป็นอนุมัติแล้ว | อนุมัติสำเร็จ | PASS | docs/qa/screenshots/phase1/12-leave-approved.png |
| 13 | Leave Reject | สร้าง draft สำหรับ reject | เปิดรายละเอียด draft ได้ | สร้าง draft สำหรับ reject สำเร็จ | PASS | docs/qa/screenshots/phase1/13-leave-reject-draft-created.png |
| 14 | Leave Reject | ไม่อนุมัติคำขอลา | สถานะเป็นไม่อนุมัติ | Reject สำเร็จ | PASS | docs/qa/screenshots/phase1/14-leave-rejected.png |
| 15 | Leave Calendar | เปิดปฏิทินการลา | เห็นหน้าปฏิทินการลา | แสดงปฏิทินการลา | PASS | docs/qa/screenshots/phase1/15-leave-calendar.png |
| 16 | Leave Balance | เปิดวันลาคงเหลือ | เห็นรายการวันลาคงเหลือ | แสดงวันลาคงเหลือ | PASS | docs/qa/screenshots/phase1/16-leave-balances.png |
| 17 | Responsive | เปิดรายการลาใน mobile viewport | หน้าไม่ล้นและยังใช้งานได้ | mobile viewport แสดงผลได้ | PASS | docs/qa/screenshots/phase1/17-leave-mobile-responsive.png |
| 18 | Logout | ออกจากระบบ | กลับหน้า login | logout สำเร็จ | PASS | docs/qa/screenshots/phase1/18-logout-success-login.png |

## Downloads

- docs/qa/downloads/leave-request-019edeaf-c1c3-7bc1-8efa-143bcecbe958.pdf

## Bugs Found

- Attachment download failed: GET /api/leave-attachments/019edeaf-c99f-7e45-91cc-ad92a5928908/download -> 403: 

## Risk Before Deploy

- Attachment download returned 403 after successful upload. This blocks leave attachment review/download in Phase 1 and should be fixed before deploy.
- Test used local development admin credentials; production must use real bootstrap admin and disable default admin.

## Recommendation Before Go Live

1. Fix attachment download permission/access logic and rerun attachment download QA.
2. Rerun the same login and leave smoke test with production-like admin/user roles.
3. Verify audit logs after the fixed attachment download path.

## Commands Used

```powershell
docker compose up -d --force-recreate postgres
dotnet build Hospital-Operations-Portal.sln
dotnet run --no-build --no-launch-profile --project backend/Hop.Api/Hop.Api.csproj --urls http://localhost:5000
npm run dev -- --host 127.0.0.1 --port 5173 --strictPort
```
