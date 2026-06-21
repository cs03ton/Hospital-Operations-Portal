# Phase 1 Realistic Leave Flow QA Report

## Test Summary

ทดสอบ Leave Approval Workflow แบบข้อมูลจำลอง 1 แผนก ตั้งแต่เตรียม user, role permission, leave type, approval chain, สร้างคำขอลา, submit, notification queue, approve 3 ขั้น, negative cases, PDF download และ leave balance

ผลรวม: **Passed with minor frontend route issue**

## Environment

| Item | Value |
|---|---|
| Frontend | `http://127.0.0.1:5173` |
| Backend API | `http://localhost:5000` |
| Health Check | `/healthz` = Healthy |
| Database | Docker PostgreSQL `hop-postgres`, database `hop_db` |
| Test Date | 21/06/2026 |
| Screenshot Folder | `docs/qa/screenshots/phase1-realistic-leave-flow/` |

## Test Data

| Type | Value |
|---|---|
| Department | แผนกเทคโนโลยีสารสนเทศ |
| Leave Type 1 | ลาพักผ่อน, 10 วัน |
| Leave Type 2 | ลาป่วย, 30 วัน |
| Leave Type 3 | ลากิจ, 5 วัน |
| Password | `Test@1234` |

## Test Accounts

| Username | Full Name | Role | Purpose |
|---|---|---|---|
| `staff.it01` | นายทดสอบ ระบบลา | Staff | ผู้ขอลา |
| `head.it01` | นายหัวหน้า ทดสอบ | DepartmentHead | ผู้อนุมัติขั้นที่ 1 |
| `manager.it01` | นางผู้จัดการ ทดสอบ | Admin | ผู้อนุมัติขั้นที่ 2 |
| `director01` | นายผู้อำนวยการ ทดสอบ | Director | ผู้อนุมัติขั้นที่ 3 |
| `staff.other01` | นายผู้ใช้ ไม่เกี่ยวข้อง | Staff | Negative case |

## Approval Chain

| Step | Step Name | Approver | Required Permission |
|---|---|---|---|
| 1 | หัวหน้างาน | `head.it01` | `LeaveManagement.Approve` |
| 2 | หัวหน้ากลุ่มงาน | `manager.it01` | `LeaveManagement.Approve` |
| 3 | ผู้อำนวยการ | `director01` | `LeaveManagement.Approve` |

## Leave Request

| Field | Value |
|---|---|
| Request ID | `019ee976-6987-75a3-8216-2bcb3eb62457` |
| Employee | นายทดสอบ ระบบลา |
| Leave Type | ลาพักผ่อน |
| Start Date | 20/07/2026 |
| End Date | 21/07/2026 |
| Total Days | 2 |
| Reason | ทดสอบระบบลา Phase 1 |
| Final Status | Approved |

## Step Results

| No | Step | Expected | Actual | Status | Screenshot |
|---|---|---|---|---|---|
| 1 | Login page | แสดงหน้า login ภาษาไทย | แสดงถูกต้อง | PASS | `docs/qa/screenshots/phase1-realistic-leave-flow/01-login-page.png` |
| 2 | Submit leave request | current approver = head.it01 | current approver = นายหัวหน้า ทดสอบ | PASS | API evidence |
| 3 | Notification after submit: head.it01 | Badge/API/Dashboard = 1 | 1 / 1 / 1 | PASS | API evidence |
| 4 | Notification after submit: manager.it01 | ยังไม่เห็น badge | 0 / 0 / 0 | PASS | API evidence |
| 5 | Notification after submit: director01 | ยังไม่เห็น badge | 0 / 0 / 0 | PASS | API evidence |
| 6 | Approve step 1 | ย้ายคิวไป manager.it01 | current approver = นางผู้จัดการ ทดสอบ | PASS | API evidence |
| 7 | After step 1: head.it01 | Badge หาย | 0 / 0 / 0 | PASS | `docs/qa/screenshots/phase1-realistic-leave-flow/04-head-dashboard-after-approval.png` |
| 8 | After step 1: manager.it01 | Badge/API/Dashboard = 1 | 1 / 1 / 1 | PASS | API evidence |
| 9 | Approve step 2 | ย้ายคิวไป director01 | current approver = นายผู้อำนวยการ ทดสอบ | PASS | API evidence |
| 10 | After step 2: director01 | Badge/API/Dashboard = 1 | 1 / 1 / 1 | PASS | API evidence |
| 11 | Approve step 3 | คำขอเป็น Approved และไม่มี current approver | Approved, current approver = null | PASS | `docs/qa/screenshots/phase1-realistic-leave-flow/03-staff-leave-detail-approved.png` |
| 12 | Staff tracking | เห็นสถานะอนุมัติแล้วและ timeline | แสดงรายละเอียดคำขอและสถานะสุดท้าย | PASS | `docs/qa/screenshots/phase1-realistic-leave-flow/03-staff-leave-detail-approved.png` |
| 13 | PDF download | ได้ไฟล์ PDF | HTTP 200, `application/pdf` | PASS | API evidence |

## Notification Queue Result

| State | User | Notification Approval Count | `/api/approvals/my-pending` | Dashboard Pending |
|---|---|---:|---:|---:|
| หลัง Submit | head.it01 | 1 | 1 | 1 |
| หลัง Submit | manager.it01 | 0 | 0 | 0 |
| หลัง Submit | director01 | 0 | 0 | 0 |
| หลัง Step 1 Approved | head.it01 | 0 | 0 | 0 |
| หลัง Step 1 Approved | manager.it01 | 1 | 1 | 1 |
| หลัง Step 2 Approved | manager.it01 | 0 | 0 | 0 |
| หลัง Step 2 Approved | director01 | 1 | 1 | 1 |
| หลัง Step 3 Approved | director01 | 0 | 0 | 0 |

## Leave Balance

| Leave Type | Before | After | Result |
|---|---:|---:|---|
| ลาพักผ่อน - Entitled | 10 | 10 | PASS |
| ลาพักผ่อน - Used | 0 | 2 | PASS |
| ลาพักผ่อน - Pending | 0 | 0 | PASS |
| ลาพักผ่อน - Remaining | 10 | 8 | PASS |

## Negative Cases

| No | Case | Expected | Actual | Status |
|---|---|---|---|---|
| 1 | `manager.it01` approve ก่อนถึงคิว | 403 Forbidden | 403 | PASS |
| 2 | `director01` approve ก่อนถึงคิว | 403 Forbidden | 403 | PASS |
| 3 | `staff.it01` approve คำขอตัวเอง | 403 Forbidden | 403 | PASS |
| 4 | `staff.other01` ดู list แล้วไม่เห็นคำขอนี้ | ไม่พบ request | 0 matching request | PASS |
| 5 | `staff.other01` เปิด detail คำขอนี้ | 403 Forbidden | 403 | PASS |

## Screenshots

| No | Description | Path |
|---|---|---|
| 1 | Login page | `docs/qa/screenshots/phase1-realistic-leave-flow/01-login-page.png` |
| 2 | Staff dashboard | `docs/qa/screenshots/phase1-realistic-leave-flow/02-staff-dashboard.png` |
| 3 | Staff leave detail approved | `docs/qa/screenshots/phase1-realistic-leave-flow/03-staff-leave-detail-approved.png` |
| 4 | Head dashboard after approval | `docs/qa/screenshots/phase1-realistic-leave-flow/04-head-dashboard-after-approval.png` |
| 5 | Manager dashboard after approval | `docs/qa/screenshots/phase1-realistic-leave-flow/05-manager-dashboard-after-approval.png` |
| 6 | Director dashboard after approval | `docs/qa/screenshots/phase1-realistic-leave-flow/06-director-dashboard-after-approval.png` |
| 7 | Pending approvals route issue | `docs/qa/screenshots/phase1-realistic-leave-flow/07-pending-approvals-route-missing.png` |

## Bugs / Blockers

## BUG-001: `/leave/pending-approvals` route does not exist

- Module: Leave Approval / Frontend Routing
- Step: เปิด `/leave/pending-approvals`
- Expected: เห็นหน้ารายการคำขอที่รอฉันอนุมัติ
- Actual: Route ถูกตีความเป็น `/leave/:id` และพยายามโหลด `pending-approvals` เป็น leave request id
- Severity: Medium
- Screenshot: `docs/qa/screenshots/phase1-realistic-leave-flow/07-pending-approvals-route-missing.png`
- Suggested Fix: เพิ่ม route/page สำหรับ `/leave/pending-approvals` หรือแก้ปุ่ม "ดูทั้งหมด" ให้ไปหน้าที่มีอยู่จริงพร้อม filter ที่ถูกต้อง

## BUG-002: Vite dev server does not serve BrowserRouter deep links directly

- Module: Frontend Dev/Deployment
- Step: เปิด `http://127.0.0.1:5173/login` หรือ `/dashboard` โดยตรง
- Expected: Serve `index.html` แล้ว React Router ทำงาน
- Actual: Dev server ตอบ 404 สำหรับ deep link แต่ `/index.html` ใช้งานได้และ redirect ภายในแอปได้
- Severity: Low for API workflow, Medium for QA/deployment verification
- Suggested Fix: ตรวจ Vite dev server fallback หรือใช้ production Nginx SPA fallback ให้ชัดเจนก่อน deploy

## Evidence Files

- `docs/qa/realistic-flow-setup.json`
- `docs/qa/realistic-flow-results.json`
- `docs/qa/realistic-flow-screenshots.json`

## Recommendation Before Deploy

1. เพิ่มหรือแก้หน้า `/leave/pending-approvals` ให้ใช้งานได้จริง เพราะ endpoint `/api/approvals/my-pending` ผ่านแล้ว แต่ frontend route ยังขาด
2. ตรวจ SPA fallback สำหรับ production Nginx และ local QA ให้ deep link เปิดได้โดยตรง
3. เก็บ test data ชุดนี้ไว้เฉพาะ local/dev database เท่านั้น ห้ามนำไป production
4. Rerun UI screenshot flow หลังแก้ pending approvals route
