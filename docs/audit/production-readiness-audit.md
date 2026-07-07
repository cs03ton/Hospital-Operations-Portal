# Production Readiness Audit: Hospital Operations Portal (HOP)

วันที่ตรวจสอบ: 6 กรกฎาคม 2026  
ปรับปรุงล่าสุด: 6 กรกฎาคม 2026 จากการ scan repository ปัจจุบัน  
ขอบเขต: ตรวจสอบสถานะจริงจาก repository และ working tree ปัจจุบันเท่านั้น  
ข้อจำกัด: ไม่ได้แก้ source code, config, database schema หรือรัน deploy จริง มีการสร้างรายงานนี้เป็นไฟล์เดียว

## Executive Summary

HOP มีความพร้อมด้าน Production Readiness อยู่ในระดับ **Partial / Conditional Ready** สำหรับ Phase 1 Pilot กล่าวคือมีโครงสร้างหลักครบหลายส่วนแล้ว เช่น production Docker Compose, Nginx reverse proxy, backup/restore scripts, health dashboard, safe error handling, granular permission, audit log, manual และ test checklist

อย่างไรก็ตามยังมีความเสี่ยงก่อน production จริง โดยเฉพาะ:

- **P0:** Secret management ยังต้องปิดงานให้เรียบร้อยก่อน deploy จริง เนื่องจากมี local `.env` / `.env.development` ที่มี secret ในเครื่อง, มี dev password `Admin@1234` อยู่ใน source/docs, และ `.env.production.example` ยังเป็นไฟล์ untracked ใน working tree
- **P1:** Docker/deploy พร้อมใช้เชิงโครงสร้าง แต่ยังมี fallback `localhost` ใน frontend/runtime defaults ที่ต้องควบคุมด้วย env production และควรเพิ่ม TLS checklist ให้ชัดเจน
- **P1:** Testing มี backend tests และ QA report แล้ว แต่รายงานล่าสุดยังระบุว่า manual browser E2E, LINE real delivery และ PDF visual check ต้อง rerun บน pilot database จริง
- **P1:** Backup/restore มี script และเอกสาร แต่ยังไม่พบหลักฐาน cron/schedule จริงหรือผล monthly restore test จริง

สรุป Go/No-Go: **ยังไม่ควร Production Go-Live แบบเต็ม** จนกว่าจะปิด P0 และ rerun P1 validation บน environment pilot จริง แต่สามารถใช้เป็นฐานสำหรับ Pilot Readiness ได้หากดำเนิน checklist ที่ระบุไว้ครบ

## ตารางสถานะภาพรวม

| Area | Status | Priority | Evidence | Gap | Recommended Next Action |
|---|---|---:|---|---|---|
| Secret Management | Risk | P0 | `.gitignore`, `.env.example`, `.env.production.example`, `backend/Hop.Api/appsettings.json`, `backend/Hop.Api/Program.cs`, `frontend/src/pages/LoginPage.tsx` | มี local `.env`/`.env.development` ที่มี secret ในเครื่อง, `.env.production.example` ยัง untracked, source มี dev password `Admin@1234`, frontend มี fallback localhost | track sanitized `.env.production.example`, ลบ/ย้าย dev credential ออกจาก source UI, rotate secret ที่เคยใช้ทดสอบจริง, ใช้ secret manager หรือ protected env บน server |
| Docker / Deploy | Partial | P1 | `docker-compose.prod.yml`, `deploy/nginx.conf`, `deploy/*.sh`, `deploy/backend.Dockerfile`, `deploy/frontend.Dockerfile`, `docs/DEPLOYMENT.md` | โครงครบ แต่ยังต้องยืนยัน `.env.production`, TLS, domain, public URL, และ frontend fallback | dry-run `docker compose config`, deploy staging, เพิ่ม TLS/HTTPS runbook และยืนยัน `VITE_API_BASE_URL` production |
| Backup / Restore | Partial | P1 | `scripts/backup/backup-hop.sh`, `scripts/backup/restore-hop.sh`, `docs/BACKUP-RESTORE.md` | มี script และ runbook แต่ยังไม่พบหลักฐาน cron จริงหรือ monthly restore test จริง; checklist บางจุดอ้าง script เก่า | ตั้ง cron/systemd timer, เก็บผล restore test รายเดือน, sync checklist ให้ตรงกับ script ใหม่ |
| Health Check | Done / Partial | P1 | `backend/Hop.Api/Program.cs`, `backend/Hop.Api/Controllers/AdminHealthController.cs`, `frontend/src/pages/AdminHealthPage.tsx`, `docs/HEALTH-DASHBOARD.md` | มี `/health`, `/healthz`, `/api/admin/health`; backup health ยังตรวจจาก folder/file ล่าสุด ไม่ใช่ job status จริง | เพิ่ม backup job status file และ alert hook ภายหลัง |
| Error Handling | Done | P1 | `backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs`, `frontend/src/components/common/ErrorBoundary.tsx`, `frontend/src/api/httpClient.ts`, `docs/ERROR-HANDLING.md` | มี safe Thai error + referenceId แล้ว; ยังควรทดสอบ production mode จริง | เพิ่ม smoke test production exception และ log search by referenceId |
| Permission & Role | Partial | P0/P1 | `backend/Hop.Api/Authorization/LeavePermissions.cs`, `backend/Hop.Api/Services/LeaveRequestAccessService.cs`, `backend/Hop.Api/Controllers/LeaveRequestsController.cs`, `frontend/src/routes/AppRoutes.tsx`, `docs/PERMISSION-MATRIX.md` | Granular permission ดีขึ้นมาก แต่ไม่พบ role ชื่อ `hr` ตรง ๆ; ใช้ `Admin`/`LeaveAdmin` แทน | ระบุ HR role mapping อย่างเป็นทางการ หรือ seed role `HR` หาก policy ต้องการชื่อ role แยก |
| Testing | Partial | P1 | `backend/Hop.Api.Tests/*`, `frontend/e2e/*`, `docs/qa/PHASE1-PILOT-TEST-REPORT.md`, `docs/TESTING.md` | Backend tests มี แต่ manual E2E/LINE real/PDF visual ยังต้อง rerun ตามรายงาน | รัน full test suite บน staging/pilot DB และอัปเดต QA report พร้อม screenshots |
| Manual / Docs | Done / Partial | P2 | `docs/manuals/phase1/*`, `docs/manuals/assets/screenshots/*`, `docs/DEPLOYMENT.md`, `docs/PHASE1-PILOT-CHECKLIST.md` | คู่มือและ screenshot มีแล้ว แต่ควร refresh หลัง UI/dashboard ล่าสุด | ทำ doc QA pass รอบสุดท้ายก่อนอบรมผู้ใช้ |
| Logging / Audit | Done / Partial | P1 | `backend/Hop.Api/Services/AuditLogService.cs`, `backend/Hop.Api/Controllers/AuditLogsController.cs`, `backend/Hop.Api/Middleware/PermissionDeniedAuditMiddleware.cs`, `docs/AUDIT-EVENTS.md`, `docs/AUDIT-RETENTION.md` | Audit/export/retention มีแล้ว แต่ควรยืนยัน event coverage ทั้งหมดจาก workflow จริง | เพิ่ม audit coverage checklist และ retention schedule/owner |

## 1. Secret Management

### สถานะ: Risk

### Evidence

- `.gitignore` กันไฟล์ env และ secret:
  - `.env`
  - `.env.*`
  - `*.secret`
  - ยกเว้น `.env.example` และ `.env.production.example`
- พบไฟล์ local ใน working tree:
  - `.env`
  - `.env.development`
  - `.env.example`
  - `.env.production.example`
- `git ls-files` แสดงว่า track เฉพาะ `.env.example`; `.env.production.example` ยังไม่ถูก track ใน repository ณ ตอน audit
- `backend/Hop.Api/appsettings.json` และ `backend/Hop.Api/appsettings.Development.json` ไม่พบ token/secret จริงในค่าที่ตรวจ พบเป็นค่าว่างสำหรับ LINE secret/token และ connection string
- `backend/Hop.Api/Program.cs` enforce:
  - `ConnectionStrings__DefaultConnection` required
  - `Jwt__Key` ต้องมีอย่างน้อย 32 ตัวอักษร
- `docker-compose.prod.yml` รับค่า secret ผ่าน environment variables เช่น:
  - `POSTGRES_PASSWORD`
  - `Jwt__Key`
  - `Line__ChannelSecret`
  - `Line__AccessToken`
- `deploy/04-crosscheck.sh` มี secret leakage check สำหรับ frontend dist
- `frontend/src/pages/LoginPage.tsx` ยังมี dev password string `Admin@1234`
- `frontend/src/api/httpClient.ts` และ `frontend/src/api/securityApi.ts` มี fallback เป็น `https://localhost:5000`
- `.env.example` และ docs หลายไฟล์มีค่า example เช่น `Admin@1234`, `change-this-*`, localhost ซึ่งเป็นตัวอย่าง แต่ต้องไม่ใช้จริงใน production

### Gap

- Local `.env` / `.env.development` มีค่า secret จริงในเครื่อง แม้ถูก ignore แต่เป็น operational risk บน workstation
- `.env.production.example` ยัง untracked ทำให้ production setup อาจไม่ reproducible หากยังไม่ commit template ที่ sanitized
- dev password `Admin@1234` อยู่ใน source และ docs หลายจุด เป็น risk ด้าน social/operational หากผู้ใช้เข้าใจผิดว่ายังใช้ได้ใน production
- fallback `localhost` ใน frontend หาก env production ไม่ถูก inject จะทำให้หน้าบ้านชี้ผิด endpoint

### Recommended Next Action

- **P0:** Commit เฉพาะ `.env.production.example` ที่ sanitized แล้ว และห้าม commit `.env.production`
- **P0:** ลบหรือเปลี่ยน `Admin@1234` ใน frontend source ให้เป็น dev-only fixture ที่ไม่แสดงใน production build
- **P0:** Rotate LINE/JWT/DB secret ที่เคยใส่ใน local หากเคยใช้กับของจริง
- **P1:** ใช้ server-side secret management เช่น protected `.env.production` permission 600, Docker secrets, หรือ secret manager ตาม infra ที่ใช้จริง
- **P1:** บังคับ production build fail ถ้า `VITE_API_BASE_URL` หรือ same-origin config ไม่ถูกต้อง

## 2. Docker / Deploy

### สถานะ: Partial

### Evidence

- `docker-compose.prod.yml` มี services:
  - `postgres`
  - `backend`
  - `clamav`
  - `frontend`
  - `nginx`
- `docker-compose.prod.yml` มี volumes:
  - `hop_prod_postgres_data`
  - `hop_prod_storage`
  - `hop_prod_clamav_data`
- `deploy/nginx.conf` proxy:
  - `/api/` ไป backend
  - `/healthz` และ `/health` ไป backend
  - `/` ไป frontend พร้อม SPA fallback ผ่าน `@spa_fallback`
- มี deploy scripts:
  - `deploy/00-check-env.sh`
  - `deploy/01-deploy-db.sh`
  - `deploy/02-deploy-backend.sh`
  - `deploy/03-deploy-frontend.sh`
  - `deploy/04-crosscheck.sh`
  - `deploy/deploy-all.sh`
  - `deploy/rollback.sh`
- มี production docs:
  - `docs/DEPLOYMENT.md`
  - `docs/DEPLOYMENT-CHECKLIST.md`
  - `docs/ROLLBACK.md`
  - `docs/ENVIRONMENT.md`

### Gap

- ยังมี fallback localhost ใน:
  - `deploy/frontend.Dockerfile`
  - `frontend/src/api/httpClient.ts`
  - `frontend/src/api/securityApi.ts`
- `deploy/nginx.conf` เป็น HTTP port 80 ยังไม่รวม TLS/HTTPS termination ในไฟล์นี้
- ต้องยืนยันว่า `.env.production` จริงมี `PUBLIC_APP_URL`, `Storage__PublicBaseUrl`, `VITE_API_BASE_URL` หรือ same-origin config ถูกต้อง

### Recommended Next Action

- **P1:** รัน `docker compose --env-file .env.production -f docker-compose.prod.yml config` ใน staging
- **P1:** เพิ่ม HTTPS/TLS deployment note หรือ nginx TLS variant สำหรับ production
- **P1:** เพิ่ม crosscheck ว่า frontend dist ไม่มี `localhost:5000` ก่อน deploy
- **P2:** เพิ่ม blue/green หรือ rollback validation หลัง deploy

## 3. Backup / Restore

### สถานะ: Partial

### Evidence

- `scripts/backup/backup-hop.sh`
  - รองรับ `BACKUP_MODE=host|docker`
  - ใช้ `pg_dump --format=custom --no-owner`
  - backup storage เป็น `tar.gz`
  - มี retention ด้วย `BACKUP_RETENTION_DAYS`
  - เขียน log ลง backup logs
- `scripts/backup/restore-hop.sh`
  - รองรับ `BACKUP_MODE=host|docker`
  - ใช้ `pg_restore --clean --if-exists --no-owner`
  - มี safety confirmation `RESTORE_CONFIRM=I_UNDERSTAND_THIS_WILL_OVERWRITE_HOP`
  - restore storage archive ได้
- `docs/BACKUP-RESTORE.md`
  - อธิบาย env, backup, restore, cron, monthly restore test checklist

### Gap

- ยังไม่พบหลักฐานว่า cron/systemd timer ถูกตั้งจริงใน repo
- Health dashboard ตรวจ backup จาก folder/file ล่าสุด ยังไม่ใช่สถานะ job สำเร็จล่าสุดแบบ structured
- `docs/PHASE1-PILOT-CHECKLIST.md` ยังมีรายการอ้าง `scripts/backup-postgres.ps1` ซึ่งไม่ตรงกับ script ปัจจุบัน `scripts/backup/backup-hop.sh`

### Recommended Next Action

- **P1:** ตั้ง cron จริงบน pilot server และเก็บ log path มาตรฐาน
- **P1:** ทำ monthly restore test และบันทึกผลใน docs/qa หรือ runbook
- **P1:** อัปเดต checklist ให้ตรงกับ script ปัจจุบัน
- **P2:** เพิ่ม backup status marker เช่น `last_success.json` เพื่อให้ health dashboard ตรวจได้แม่นขึ้น

## 4. Health Check

### สถานะ: Done / Partial

### Evidence

- `backend/Hop.Api/Program.cs`
  - map `/health`
  - map `/healthz`
- `backend/Hop.Api/Controllers/AdminHealthController.cs`
  - `GET /api/admin/health`
  - `[RequirePermission("SystemSettings.View")]`
  - ตรวจ Database, Storage, LINE, Disk, Backup, Version, Environment, Server Time
- `backend/Hop.Api/DTOs/HealthDtos.cs`
  - มี DTO สำหรับ database/storage/line/disk/backup
- `frontend/src/pages/AdminHealthPage.tsx`
  - หน้า admin health dashboard
- `frontend/src/routes/AppRoutes.tsx`
  - route `/admin/health`
- `docs/HEALTH-DASHBOARD.md`
  - เอกสารใช้งาน health dashboard

### Gap

- Backup health ยังเป็นการ scan folder ล่าสุด ไม่ใช่ผลสำเร็จของ job แบบ structured
- ยังไม่พบ health check ของ background queue/notification retry worker แบบละเอียด
- ยังไม่ได้ยืนยันผลบน production/staging runtime จริงใน audit รอบนี้

### Recommended Next Action

- **P1:** เพิ่ม structured backup status file
- **P2:** เพิ่ม queue/worker health component
- **P2:** เพิ่ม endpoint smoke test ใน deployment crosscheck

## 5. Error Handling

### สถานะ: Done

### Evidence

- `backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs`
  - จับ unhandled exception
  - ใช้ `context.TraceIdentifier` เป็น `referenceId`
  - production response ใช้ข้อความปลอดภัยภาษาไทย
  - development แสดงรายละเอียดมากขึ้นตาม environment
- `backend/Hop.Api/Program.cs`
  - `app.UseMiddleware<GlobalExceptionMiddleware>()`
- `frontend/src/components/common/ErrorBoundary.tsx`
  - แสดง “เกิดข้อผิดพลาด”
  - แสดง `Reference ID`
  - มีปุ่มกลับหน้าหลักและโหลดใหม่
- `frontend/src/api/httpClient.ts`
  - แสดง toast พร้อม Reference ID หาก backend ส่งมา
- `docs/ERROR-HANDLING.md`
  - ระบุ production ไม่แสดง stack trace

### Gap

- ต้องทดสอบจริงใน `ASPNETCORE_ENVIRONMENT=Production`
- ยังไม่เห็น correlation id ที่ propagate ข้าม reverse proxy เป็น header มาตรฐาน เช่น `X-Correlation-ID`

### Recommended Next Action

- **P1:** เพิ่ม production smoke test สำหรับ 500 response
- **P2:** เพิ่ม correlation id middleware/header ถ้าต้องการ trace ข้าม Nginx/API/log aggregator

## 6. Permission & Role

### สถานะ: Partial

### Evidence

- Roles ที่พบใน seed/code/docs:
  - `SuperAdmin`
  - `Admin`
  - `LeaveAdmin`
  - `Director`
  - `DepartmentHead`
  - `Staff`
- `backend/Hop.Api/Authorization/LeavePermissions.cs`
  - มี granular leave permissions เช่น `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveRequest.ViewDepartment`, `LeaveRequest.ViewAll`, `LeaveApproval.ApproveCurrentStep`, `LeaveApproval.Override`, `LeaveSupport.ViewAll`
- `backend/Hop.Api/Services/LeaveRequestAccessService.cs`
  - `canViewAll` มาจาก `LeaveRequest.ViewAll` หรือ `LeaveSupport.ViewAll`
  - Director ไม่ได้ ViewAll จาก role name อัตโนมัติ
  - DepartmentHead เห็น own + staff department ตาม policy
- `backend/Hop.Api/Controllers/LeaveRequestsController.cs`
  - list/detail/pdf/attachments ใช้ permission + `CanAccessLeaveRequest`
  - approve/reject ใช้ `LeaveApproval.ApproveCurrentStep`
  - self approval ถูก block และ audit `SelfApprovalBlocked`
  - override endpoints ต้องใช้ `LeaveApproval.Override`
  - override ต้องมี reason, บันทึก `ApprovalOverrideLog`, IP, User-Agent และ audit `LeaveApproval.OverrideApproved/Rejected`
- `frontend/src/routes/AppRoutes.tsx`
  - มี `PermissionGuard`
  - route `/leave/pending-approvals`, `/admin/leave-support`, `/admin/health` ผูก permission
- `frontend/src/config/menuConfig.ts`
  - ซ่อน/แสดงเมนูตาม permission และ role
- `docs/PERMISSION-MATRIX.md`
  - ระบุ role/permission matrix
- `docs/LEAVE-REQUEST-VISIBILITY.md`
  - ระบุ BUG-001 fixed: Director no longer has implicit ViewAll
- พบ authorization attributes ใน controller ประมาณ 129 จุดจากการ scan

### Gap

- Requirement ระบุ role `hr` แต่ repo ปัจจุบันไม่พบ role ชื่อ `HR` ตรง ๆ; มี `LeaveAdmin`/`Admin` ที่ทำหน้าที่ใกล้เคียง
- Dashboard module config ยังมีบางจุดที่ใช้ role เช่น `SuperAdmin` เพื่อแสดง dashboard card ซึ่งควรตรวจว่าไม่ขัดกับ policy “ไม่ hardcode role กระจายหลายไฟล์”
- Notification controller มี role-based categories สำหรับ Staff/DepartmentHead/Director/Admin/SuperAdmin ควรทบทวนต่อว่าไม่ทำให้ visibility ขยายเกิน policy

### Recommended Next Action

- **P0:** ตัดสินใจว่าจะใช้ role `HR` จริงหรือ map HR เป็น `LeaveAdmin` อย่างเป็นทางการ แล้วอัปเดต docs/seed/UI ให้ตรงกัน
- **P1:** เพิ่ม regression tests สำหรับ dashboard module visibility และ notification role categories
- **P1:** ตรวจ endpoint ทุกตัวที่เกี่ยวกับ calendar/report ว่าใช้ `LeaveRequestAccessService` หรือ policy equivalent

## 7. Testing

### สถานะ: Partial

### Evidence

- Backend test project:
  - `backend/Hop.Api.Tests/Hop.Api.Tests.csproj`
- Test files สำคัญ:
  - `AuthAndPermissionTests.cs`
  - `LeaveSecurityHardeningTests.cs`
  - `Phase1CriticalLeaveWorkflowTests.cs`
  - `LeaveValidationTests.cs`
  - `LeaveBalanceRolloverTests.cs`
  - `LeaveAttachmentAccessTests.cs`
  - `LeavePdfTests.cs`
  - `LineRetryTests.cs`
  - `AdminHealthAndErrorTests.cs`
  - `FileUploadValidationTests.cs`
  - `PostgresApiE2ETests.cs`
- Frontend E2E:
  - `frontend/e2e/phase1-web-qa.spec.ts`
  - `frontend/e2e/screenshots/hop-screenshot.spec.ts`
- Docs:
  - `docs/TESTING.md`
  - `docs/qa/PHASE1-PILOT-TEST-REPORT.md`
  - `docs/PHASE1-PILOT-CHECKLIST.md`
- `docs/qa/PHASE1-PILOT-TEST-REPORT.md` ระบุ:
  - Automated result: 86 passed, 0 failed
  - Frontend build result: Passed
  - Conditional GO หลัง manual browser E2E rerun
  - ครอบคลุม login, create leave, submit, approve, reject, cancel, permission, balance, LINE, PDF

### Gap

- Audit รอบนี้ไม่ได้รัน `dotnet test` หรือ `npm run build`; ตรวจจาก repository และรายงานที่มีอยู่เท่านั้น
- รายงาน QA ระบุ manual browser E2E, LINE real delivery และ PDF visual rendering ยังต้องทำซ้ำบน pilot database จริง
- `frontend/e2e/phase1-web-qa.spec.ts` ยังพบ URL fallback บางจุดเป็น localhost ซึ่งเหมาะกับ local แต่ต้องตั้ง env สำหรับ pilot

### Recommended Next Action

- **P1:** รัน full suite บน staging/pilot:
  - `dotnet test`
  - `npm run build`
  - `npm run e2e:phase1` หรือ Playwright command ที่ใช้จริง
- **P1:** อัปเดต `docs/qa/PHASE1-PILOT-TEST-REPORT.md` ด้วยผลล่าสุด, screenshot paths และ bug list
- **P2:** เพิ่ม CI gate สำหรับ production readiness tests

## 8. Manual / Docs

### สถานะ: Done / Partial

### Evidence

- คู่มือ Phase 1:
  - `docs/manuals/phase1/README.md`
  - `docs/manuals/phase1/01-Getting-Started.md`
  - `docs/manuals/phase1/02-Dashboard-User-Guide.md`
  - `docs/manuals/phase1/03-Leave-User-Guide.md`
  - `docs/manuals/phase1/04-Approval-User-Guide.md`
  - `docs/manuals/phase1/05-HR-User-Guide.md`
  - `docs/manuals/phase1/06-Admin-User-Guide.md`
  - `docs/manuals/phase1/07-Executive-User-Guide.md`
  - `docs/manuals/phase1/08-FAQ.md`
  - `docs/manuals/phase1/09-Troubleshooting.md`
  - `docs/manuals/phase1/10-Glossary.md`
  - `docs/manuals/phase1/11-Quick-Start-One-Page.md`
- Screenshot docs/assets:
  - `docs/manuals/assets/screenshots/*`
  - มีภาพจริงหลาย role เช่น login, dashboard, leave, approval, HR, superadmin
- Deployment docs:
  - `docs/DEPLOYMENT.md`
  - `docs/DEPLOYMENT-CHECKLIST.md`
  - `docs/BACKUP-RESTORE.md`
  - `docs/ENVIRONMENT.md`
  - `docs/PRODUCTION-CHECKLIST.md`
- Presentation docs:
  - `docs/presentation/source/*`
  - `docs/presentation/hop-executive/*`

### Gap

- ต้อง refresh screenshot/manual หลัง Dashboard Hub และ UI ล่าสุด
- คู่มือ HR มี แต่ role ใน code ใช้ `LeaveAdmin`/`Admin` ไม่พบ role `HR` ตรง ๆ ต้องทำ wording ให้ตรงกับ implementation
- เอกสารบางจุดยังมี localhost/dev password เป็นตัวอย่าง ต้องกำกับว่าเป็น development-only ให้ชัด

### Recommended Next Action

- **P2:** ทำ manual QA pass รอบสุดท้ายหลัง freeze UI
- **P2:** เพิ่ม release version/date ใน manual
- **P2:** แยก “Development example” ออกจาก “Production instruction” ให้ชัดใน docs

## 9. Logging / Audit

### สถานะ: Done / Partial

### Evidence

- `backend/Hop.Api/Services/AuditLogService.cs`
  - บันทึก action, resource, result, userId, IP
- `backend/Hop.Api/Middleware/PermissionDeniedAuditMiddleware.cs`
  - บันทึก permission denied
- `backend/Hop.Api/Controllers/AuditLogsController.cs`
  - list/filter audit log
  - export CSV
  - export Excel
  - export PDF
  - retention run endpoint
- `backend/Hop.Api/Services/AuditRetentionService.cs`
  - ใช้ `AuditLog:RetentionDays` default 365
- Leave flow evidence:
  - `LeaveRequest.PdfGenerated`
  - `LeaveAttachment.Upload`
  - `LeaveRequest.Approved`
  - `LeaveRequest.Rejected`
  - `SelfApprovalBlocked`
  - `LeaveApproval.OverrideApproved`
  - `LeaveApproval.OverrideRejected`
- Auth evidence:
  - `Auth.LoginSuccess`
  - `Auth.LoginFailed`
  - `Auth.LoginLocked`
- LINE evidence:
  - `Line.TestMessageSent`
  - `Line.TestMessageFailed`
  - `Line.TestFlexSent`
  - `Line.TestFlexFailed`
- Docs:
  - `docs/AUDIT-EVENTS.md`
  - `docs/AUDIT-RETENTION.md`

### Gap

- ต้องยืนยันจาก end-to-end run ว่าทุก event ตาม requirement ถูกยิงจริงครบทุก flow
- Retention มี endpoint/manual run แต่ยังไม่พบ schedule job
- ควรตรวจเพิ่มเติมว่า attachment download และ PDF download แยก event ตาม policy หรือใช้ `PdfGenerated` เป็น event หลัก

### Recommended Next Action

- **P1:** เพิ่ม audit event checklist ใน Phase 1 E2E report
- **P1:** ตั้ง retention schedule หรือ operational SOP
- **P2:** เพิ่ม dashboard/alert สำหรับ failed audit write หากต้องการ compliance สูงขึ้น

## Priority Plan

### P0: ต้องแก้ก่อน production deploy

1. ปิด Secret Management:
   - track `.env.production.example` ที่ sanitized
   - ลบ dev password ออกจาก source UI หรือจำกัดเป็น dev-only
   - rotate secret ที่ใช้จริงใน local/test หากมีโอกาสรั่ว
2. ตัดสินใจ role HR:
   - เพิ่ม role `HR` หรือประกาศ mapping เป็น `LeaveAdmin`/`Admin` ให้ชัดใน seed/docs/UI
3. ยืนยัน production env:
   - `Jwt__Key`
   - `ConnectionStrings__DefaultConnection`
   - `POSTGRES_PASSWORD`
   - `PUBLIC_APP_URL`
   - `Storage__PublicBaseUrl`
   - `VITE_API_BASE_URL` หรือ same-origin strategy

### P1: ควรทำก่อน pilot

1. รัน full test suite และ manual E2E บน pilot DB จริง
2. ทดสอบ LINE real delivery ด้วย LINE OA/channel จริง
3. เปิด PDF ภาษาไทยจาก browser จริงและตรวจ layout
4. ตั้ง backup cron/systemd timer และทำ restore test
5. เพิ่ม staging/deploy crosscheck ว่า frontend dist ไม่มี localhost/secret markers
6. อัปเดต checklist ที่อ้าง script backup เก่า

### P2: ทำหลัง pilot หรือก่อน scale-up

1. เพิ่ม structured backup status และ alert
2. เพิ่ม correlation id header ข้าม Nginx/API
3. เพิ่ม CI gate สำหรับ production readiness
4. refresh screenshots/manual หลัง UI freeze
5. เพิ่ม queue/background worker health ถ้ามีการเปิดใช้ worker จริง

## Files Inspected

### Secret / Environment

- `.gitignore`
- `.env`
- `.env.development`
- `.env.example`
- `.env.production.example`
- `backend/Hop.Api/appsettings.json`
- `backend/Hop.Api/appsettings.Development.json`
- `backend/Hop.Api/Program.cs`
- `frontend/src/api/httpClient.ts`
- `frontend/src/api/securityApi.ts`
- `frontend/src/pages/LoginPage.tsx`

### Docker / Deploy

- `docker-compose.yml`
- `docker-compose.prod.yml`
- `deploy/nginx.conf`
- `deploy/backend.Dockerfile`
- `deploy/frontend.Dockerfile`
- `deploy/00-check-env.sh`
- `deploy/01-deploy-db.sh`
- `deploy/02-deploy-backend.sh`
- `deploy/03-deploy-frontend.sh`
- `deploy/04-crosscheck.sh`
- `deploy/deploy-all.sh`
- `deploy/rollback.sh`

### Backup / Restore

- `scripts/backup/backup-hop.sh`
- `scripts/backup/restore-hop.sh`
- `docs/BACKUP-RESTORE.md`

### Health / Error

- `backend/Hop.Api/Controllers/AdminHealthController.cs`
- `backend/Hop.Api/DTOs/HealthDtos.cs`
- `backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs`
- `frontend/src/pages/AdminHealthPage.tsx`
- `frontend/src/components/common/ErrorBoundary.tsx`
- `frontend/src/api/httpClient.ts`
- `docs/HEALTH-DASHBOARD.md`
- `docs/ERROR-HANDLING.md`

### Permission / Role / Leave Security

- `backend/Hop.Api/Authorization/LeavePermissions.cs`
- `backend/Hop.Api/Authorization/RequirePermissionAttribute.cs`
- `backend/Hop.Api/Services/LeaveRequestAccessService.cs`
- `backend/Hop.Api/Controllers/LeaveRequestsController.cs`
- `backend/Hop.Api/Data/DevelopmentDataSeeder.cs`
- `frontend/src/routes/AppRoutes.tsx`
- `frontend/src/config/menuConfig.ts`
- `frontend/src/config/dashboardModules.ts`
- `docs/PERMISSION-MATRIX.md`
- `docs/LEAVE-REQUEST-VISIBILITY.md`
- `docs/security/PERMISSION-MODEL.md`

### Testing / QA

- `backend/Hop.Api.Tests/*`
- `frontend/e2e/*`
- `tests/screenshots/*`
- `docs/TESTING.md`
- `docs/qa/PHASE1-PILOT-TEST-REPORT.md`
- `docs/PHASE1-PILOT-CHECKLIST.md`

### Manual / Docs

- `docs/manuals/phase1/*`
- `docs/manuals/assets/screenshots/*`
- `docs/DEPLOYMENT.md`
- `docs/DEPLOYMENT-CHECKLIST.md`
- `docs/PRODUCTION-CHECKLIST.md`

### Logging / Audit

- `backend/Hop.Api/Services/AuditLogService.cs`
- `backend/Hop.Api/Controllers/AuditLogsController.cs`
- `backend/Hop.Api/Middleware/PermissionDeniedAuditMiddleware.cs`
- `backend/Hop.Api/Services/AuditRetentionService.cs`
- `docs/AUDIT-EVENTS.md`
- `docs/AUDIT-RETENTION.md`

## Final Assessment

| Category | Readiness |
|---|---:|
| Secret Management | 60% |
| Docker / Deploy | 80% |
| Backup / Restore | 75% |
| Health Check | 85% |
| Error Handling | 85% |
| Permission / Role | 80% |
| Testing | 70% |
| Manual / Docs | 85% |
| Logging / Audit | 80% |

Overall Readiness: **ประมาณ 78%**

คำแนะนำโดยรวม: HOP มี foundation ที่ดีและใกล้พร้อม Pilot มาก แต่ควรปิด P0 เรื่อง secret/role mapping ก่อน จากนั้น rerun P1 validation บน pilot environment จริงเพื่อยืนยันว่า workflow, LINE, PDF, backup และ permission ทำงานครบตาม production policy.
