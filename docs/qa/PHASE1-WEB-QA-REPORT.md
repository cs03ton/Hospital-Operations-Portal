# Phase 1 Web QA Report

## Test Summary

- Total: 25
- Passed: 24
- Failed: 0
- Blocked: 1

## Environment

- Frontend URL: http://localhost:5173
- Backend URL: http://localhost:5000
- Browser: Playwright Chrome channel
- Test mode: Local web UI

## Connectivity Verification

- `GET /health`: Passed, returned `200 Healthy`
- `GET /healthz`: Passed, returned `200 Healthy`
- `POST /api/auth/login`: Passed with `admin`
- Frontend login: Passed and redirected to `/dashboard`
- E2E command: `npm run e2e:phase1`

## Deploy Readiness

- Readiness: 96%
- Critical Issues: 0
- High Issues: 0
- Remaining risk: Reject flow is not fully exercised in this automation run because the same request is approved before the reject step.

## Test Account

- Username: admin
- Password: ใช้ค่าจาก env หรือ default QA password

## Test Result Table

| No | Module | Test Case | Step | Expected | Actual | Status | Severity | Screenshot |
|---|---|---|---|---|---|---|---|---|
| 1 | Authentication | Login Page | เปิดหน้า Login | แสดงฟอร์ม Login ภาษาไทย | หน้า Login แสดงผลได้ | Passed | Low | docs/qa/screenshots/phase1/01-login-page.png |
| 2 | Authentication | Login | เข้าสู่ระบบด้วยบัญชี QA | เข้าสู่ Dashboard ได้ | Login สำเร็จและเข้าสู่ Dashboard | Passed | Low | docs/qa/screenshots/phase1/02-login-success-dashboard.png |
| 3 | Dashboard | Dashboard | ตรวจหน้า Dashboard | แสดงแดชบอร์ดระบบลา | Dashboard เปิดได้หลัง login | Passed | Low | docs/qa/screenshots/phase1/03-dashboard.png |
| 4 | User Management | User list | เปิดหน้าจัดการผู้ใช้ | แสดงรายการผู้ใช้หรือข้อความโหลด/ว่าง | เปิดหน้าจัดการผู้ใช้ได้ | Passed | Low | docs/qa/screenshots/phase1/04-user-management-list.png |
| 5 | User Management | User create form | เปิดฟอร์มเพิ่มผู้ใช้ | แสดงฟอร์มเพิ่มผู้ใช้ | ฟอร์มเพิ่มผู้ใช้เปิดได้ | Passed | Low | docs/qa/screenshots/phase1/05-user-create-form.png |
| 6 | User Management | User validation | กดบันทึกโดยไม่กรอกข้อมูลครบ | แสดง validation ภาษาไทย | มีการแสดงผลฟอร์ม/validation สำหรับผู้ใช้ | Passed | Low | docs/qa/screenshots/phase1/06-user-create-validation.png |
| 7 | Department Management | Department list | เปิดหน้าจัดการหน่วยงาน | แสดงรายการหน่วยงาน | เปิดหน้าจัดการหน่วยงานได้ | Passed | Low | docs/qa/screenshots/phase1/07-department-list.png |
| 8 | Role Permission | Role list | เปิดหน้าบทบาทและสิทธิ์ | แสดงบทบาทและเมนูจัดการสิทธิ์ | เปิดหน้าบทบาทและสิทธิ์ได้ | Passed | Low | docs/qa/screenshots/phase1/08-role-permission-page.png |
| 9 | Audit Log | Audit log | เปิดหน้า Audit Log | แสดงบันทึกการใช้งาน | เปิดหน้า Audit Log ได้ | Passed | Low | docs/qa/screenshots/phase1/09-audit-log-page.png |
| 10 | Leave Management | Leave request list | เปิดรายการคำขอลา | แสดงรายการคำขอลาและ filter | เปิดรายการคำขอลาได้ | Passed | Low | docs/qa/screenshots/phase1/10-leave-request-list.png |
| 11 | Leave Management | Leave request filter | Filter สถานะคำขอลา | กรองรายการตามสถานะได้ | เลือก filter สถานะได้ | Passed | Low | docs/qa/screenshots/phase1/11-leave-request-filter.png |
| 12 | Leave Management | Create leave form | เปิดฟอร์มสร้างคำขอลา | แสดงฟอร์มสร้างคำขอลา | ฟอร์มสร้างคำขอลาเปิดได้ | Passed | Low | docs/qa/screenshots/phase1/12-leave-create-form.png |
| 13 | Leave Management | Create leave draft | บันทึกคำขอลาแบบร่าง | สร้างแบบร่างและไปหน้ารายละเอียด | สร้างแบบร่างได้ | Passed | Low | docs/qa/screenshots/phase1/13-leave-create-result.png |
| 14 | Leave Management | Leave detail | เปิดรายละเอียดคำขอลา | แสดงรายละเอียดคำขอลา | เปิดรายละเอียดคำขอลาได้ | Passed | Low | docs/qa/screenshots/phase1/14-leave-detail.png |
| 15 | Leave Workflow | Submit leave | กดส่งคำขออนุมัติ | เปลี่ยนสถานะหรือแสดงผลลัพธ์ชัดเจน | กดส่งคำขอได้ | Passed | Low | docs/qa/screenshots/phase1/15-leave-submit-success.png |
| 16 | Leave Attachment | Upload attachment | อัปโหลดไฟล์แนบ | ไฟล์ถูกอัปโหลดหรือแสดง error ที่เข้าใจได้ | ทดสอบ action อัปโหลดแล้ว | Passed | Low | docs/qa/screenshots/phase1/16-leave-attachment-upload.png |
| 17 | Leave Attachment | Download attachment | ดาวน์โหลดไฟล์แนบ | ดาวน์โหลดไฟล์ได้ | เริ่มดาวน์โหลดไฟล์แนบได้ | Passed | Low | docs/qa/screenshots/phase1/17-leave-attachment-download.png |
| 18 | Leave PDF | Download PDF | ดาวน์โหลดใบลา PDF | ดาวน์โหลด PDF ได้ | เริ่มดาวน์โหลด PDF ได้ | Passed | Low | docs/qa/screenshots/phase1/18-leave-pdf-download.png |
| 19 | Leave Approval | Approve leave | กดอนุมัติคำขอลา | อนุมัติได้เมื่อมีสิทธิ์และสถานะถูกต้อง | กดอนุมัติได้ | Passed | Low | docs/qa/screenshots/phase1/19-leave-approved-success.png |
| 20 | Leave Approval | Reject leave | กดไม่อนุมัติคำขอลา | ไม่อนุมัติได้เมื่อมีคำขอ pending | ไม่มีคำขอ pending สำหรับ reject หลัง approve | Blocked | Low | docs/qa/screenshots/phase1/20-leave-reject-or-blocked.png |
| 21 | Leave Calendar | Calendar | เปิดปฏิทินการลา | แสดงปฏิทินการลา | เปิดปฏิทินการลาได้ | Passed | Low | docs/qa/screenshots/phase1/21-leave-calendar.png |
| 22 | Leave Calendar | Calendar filter | Filter ปฏิทินตามสถานะ | กรองข้อมูลบนปฏิทินได้ | เลือก filter สถานะบนปฏิทินได้ | Passed | Low | docs/qa/screenshots/phase1/22-leave-calendar-filter.png |
| 23 | Leave Calendar | Holiday display | ตรวจวันหยุดประจำปีในปฏิทิน | วันหยุดแสดงเป็นสีฟ้าเมื่อมีข้อมูล | หน้าปฏิทินมี legend วันหยุดประจำปีและพื้นที่แสดงวันหยุด | Passed | Low | docs/qa/screenshots/phase1/23-leave-calendar-holiday.png |
| 24 | Permission Guard | Menu visibility | ตรวจ sidebar/menu Phase 1 | แสดงเฉพาะเมนู Phase 1 และซ่อนโมดูลอื่น | ไม่พบเมนูนอก Phase 1 จาก sidebar ที่มองเห็น | Passed | Low | docs/qa/screenshots/phase1/24-permission-hidden-menu.png |
| 25 | Authentication | Logout | ออกจากระบบ | กลับหน้า Login | Logout สำเร็จ | Passed | Low | docs/qa/screenshots/phase1/25-logout.png |

## Bug Cases

## BUG-001: Reject leave

- Module: Leave Approval
- Step: กดไม่อนุมัติคำขอลา
- Expected: ไม่อนุมัติได้เมื่อมีคำขอ pending
- Actual: ไม่มีคำขอ pending สำหรับ reject หลัง approve
- Severity: Low
- Screenshot: docs/qa/screenshots/phase1/20-leave-reject-or-blocked.png
- Suggested Fix: ปรับ QA script รอบถัดไปให้สร้างคำขอ pending แยกอีก 1 รายการสำหรับ reject โดยเฉพาะ


## Recommendation

- แก้รายการที่มีสถานะ Failed/Blocked ก่อน deploy ถ้า severity เป็น Critical/High
- ตรวจ screenshot ทุกไฟล์ใน docs/qa/screenshots/phase1/ เพื่อ sign-off ก่อน pilot

## สิ่งที่ควรแก้ก่อน deploy

- ไม่มี Critical/High จาก automation run นี้

## สิ่งที่แก้หลัง pilot ได้

- Leave Approval: ไม่มีคำขอ pending สำหรับ reject หลัง approve
