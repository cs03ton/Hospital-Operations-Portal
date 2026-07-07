# HOP Phase 1 Production Checklist

เป้าหมาย: ปิดงาน Production Readiness ของ Hospital Operations Portal (HOP) Phase 1 ก่อนขึ้น Production ภายในวันที่ 1 ตุลาคม 2026  
แหล่งอ้างอิงหลัก: `docs/audit/production-readiness-audit.md`  
ปรับปรุงล่าสุด: 7 กรกฎาคม 2026 หลังแก้ P0 Secret Management, HR Role Mapping, frontend dist crosscheck, checklist sync และ audit coverage docs
ขอบเขตเอกสารนี้: Checklist สำหรับแก้ไข/ตรวจรับเท่านั้น ไม่มีการแก้ source code ในรอบจัดทำเอกสารนี้

## วิธีใช้ Checklist

1. ไล่ปิดงานตามลำดับ P0 → P1 → P2
2. ทุก item ต้องมีผู้รับผิดชอบและวันครบกำหนดก่อนเริ่มแก้ไขจริง
3. เมื่อแก้ไขเสร็จ ให้บันทึกผลทดสอบและหลักฐาน เช่น command output, screenshot, QA report, หรือ deploy log
4. งาน P0 ต้องผ่านทั้งหมดก่อนเริ่ม pilot
5. งาน P1 ต้องผ่านทั้งหมดก่อน production go-live

## สถานะที่ใช้

| Status | ความหมาย |
|---|---|
| `[ ]` | ยังไม่เริ่ม / ยังไม่ผ่าน |
| `[~]` | กำลังดำเนินการ / รอทดสอบ |
| `[x]` | ผ่านแล้ว พร้อมแนบหลักฐาน |

## P0 Must Fix Before Pilot

### P0-01: จัดการ `.env.production.example` ให้เป็น template production ที่ sanitized และถูก track

- [~] สถานะ: ทำ template sanitized แล้ว / รอ track ใน git

**งานที่ต้องทำ**

1. ตรวจ `.env.production.example` ให้ไม่มี secret/token/password จริง
2. ใช้ placeholder ที่ชัดเจน เช่น `change-this-*`, blank value หรือ `<strong-secret>`
3. ยืนยันว่า `.env.production.example` ถูก track ใน git
4. ยืนยันว่า `.env.production` จริงยังถูก ignore และไม่ถูก commit

**ไฟล์ที่เกี่ยวข้อง**

- `.env.production.example`
- `.gitignore`
- `docs/ENVIRONMENT.md`
- `docs/DEPLOYMENT.md`
- `docs/audit/production-readiness-audit.md`

**วิธีทดสอบ**

```powershell
git ls-files .env.production.example
git check-ignore .env.production
Select-String -Path .env.production.example -Pattern "real-token|real-secret|Bearer|password-from-production"
```

**เกณฑ์ผ่าน**

- `git ls-files .env.production.example` แสดงไฟล์
- `.env.production` ถูก ignore
- ไม่มีค่า secret จริงใน `.env.production.example`
- มีเฉพาะ placeholder หรือค่าว่างที่ปลอดภัย

**เกณฑ์ไม่ผ่าน**

- `.env.production.example` ยัง untracked
- `.env.production` ไม่ถูก ignore
- พบ secret/token/password จริงใน example file

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- สร้าง/ปรับ `.env.production.example` ให้ไม่มี secret/token/password จริงแล้ว
- `.gitignore` กัน `.env.production` และ `appsettings.Production.json` แล้ว
- ผลตรวจ `git check-ignore .env .env.development .env.production backend/Hop.Api/appsettings.Production.json` ผ่าน
- สถานะยังเป็น `[~]` เพราะ `git status` ยังแสดง `.env.production.example` เป็น untracked ต้อง `git add` ก่อนจึงผ่านเงื่อนไข “ถูก track ใน git”

### P0-02: ลบหรือจำกัด dev credential `Admin@1234` ไม่ให้เป็น production risk

- [x] สถานะ: ผ่านแล้ว

**งานที่ต้องทำ**

1. ตรวจทุกจุดที่มี `Admin@1234`
2. ลบจาก source UI หรือทำให้เป็น development-only อย่างชัดเจน
3. เอกสารที่ยังต้องมีตัวอย่างรหัสผ่านต้องระบุว่าใช้เฉพาะ development/test
4. ยืนยันว่า production seed ห้ามใช้ default password นี้

**ไฟล์ที่เกี่ยวข้อง**

- `frontend/src/pages/LoginPage.tsx`
- `backend/Hop.Api/Data/DevelopmentDataSeeder.cs`
- `.env.example`
- `docs/TESTING.md`
- `docs/MIGRATION-RUNBOOK.md`
- `docs/audit/production-readiness-audit.md`

**วิธีทดสอบ**

```powershell
rg -n "Admin@1234" frontend backend docs .env.example .env.production.example
```

**เกณฑ์ผ่าน**

- ไม่พบ `Admin@1234` ใน production-facing source UI
- หากยังพบใน docs ต้องมีข้อความกำกับว่า development/test only
- `DevelopmentDataSeeder` ป้องกัน production default password แล้ว

**เกณฑ์ไม่ผ่าน**

- หน้า login production ยังแสดง/ฝังค่า `Admin@1234`
- เอกสาร production แนะนำให้ใช้รหัสผ่าน default
- seeder ยังสร้าง admin production ด้วยรหัสผ่าน default ได้

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- ลบ default username/password ออกจาก `frontend/src/pages/LoginPage.tsx`
- ลบ fallback `Admin@1234` ออกจาก `backend/Hop.Api/Data/DevelopmentDataSeeder.cs`
- บังคับให้ bootstrap admin ใช้ `Seed__AdminPassword` หรือ `SEED_ADMIN_PASSWORD`
- ปรับ `frontend/e2e/phase1-web-qa.spec.ts` ให้ใช้ `PHASE1_QA_USERNAME` / `PHASE1_QA_PASSWORD` เท่านั้น และ skip test ถ้าไม่ได้ตั้งค่า
- ปรับ backend tests ให้ใช้ runtime-generated test secrets แทน hardcoded token/password ตัวอย่าง
- ผล scan:

```powershell
rg -n "Admin@1234|Nm@12345|secret-from-json|token-from-json|secret-token-value|secret-channel-value|test-token|change-this-postgres-password|change-this-strong-postgres-password|change-this-jwt-secret|change-this-line" frontend backend tests .env.example .env.production.example -g "!*bin*" -g "!*obj*" -g "!frontend/dist/**" -g "!tests/screenshots/config/screenshot-users.json"
```

ไม่พบรายการใน source/config ที่เกี่ยวข้อง

### P0-03: Rotate secret ที่เคยใช้ทดสอบจริง

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. เปลี่ยน JWT signing key production
2. เปลี่ยน PostgreSQL password production
3. เปลี่ยน LINE Channel Access Token / Channel Secret หากเคยใส่ในเครื่อง local
4. บันทึกผู้ดำเนินการ วันที่ และช่องทางเก็บ secret ใหม่
5. ห้ามบันทึก secret จริงลง repository หรือเอกสารทั่วไป

**ไฟล์ที่เกี่ยวข้อง**

- `.env`
- `.env.development`
- `.env.production`
- `docker-compose.prod.yml`
- `docs/ENVIRONMENT.md`
- `docs/DEPLOYMENT-CHECKLIST.md`

**วิธีทดสอบ**

```powershell
rg -n "Jwt__Key|POSTGRES_PASSWORD|Line__AccessToken|Line__ChannelSecret" .env.production.example docker-compose.prod.yml docs/ENVIRONMENT.md
```

ทดสอบบน server:

```bash
docker compose --env-file .env.production -f docker-compose.prod.yml config
docker compose --env-file .env.production -f docker-compose.prod.yml up -d
```

**เกณฑ์ผ่าน**

- production app start ได้ด้วย secret ใหม่
- login ได้
- LINE settings แสดง masked configured status
- ไม่มี secret จริงใน git

**เกณฑ์ไม่ผ่าน**

- ระบบยังใช้ secret เดิมที่เคยใส่ใน local
- secret ถูกพิมพ์ใน log หรือ commit
- backend start ไม่ได้เพราะ `Jwt__Key` หรือ connection string ไม่ครบ

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- ยังไม่ได้ rotate secret จริง เพราะต้องดำเนินการบน production/LINE/PostgreSQL environment จริง
- Repository พร้อมรับ secret ผ่าน environment variables แล้ว
- ห้ามปิดข้อนี้จนกว่าจะมีหลักฐานว่าเปลี่ยน JWT key, DB password, LINE token/secret และระบบ production start/login/test LINE ได้ด้วยค่าใหม่

### P0-04: ยืนยัน production environment variables สำคัญ

- [~] สถานะ: template/source พร้อมแล้ว / รอยืนยันบน server

**งานที่ต้องทำ**

1. สร้าง `.env.production` บน server จาก `.env.production.example`
2. ตั้งค่า env สำคัญครบ:
   - `Jwt__Key`
   - `ConnectionStrings__DefaultConnection` หรือ DB env ที่ compose ใช้ประกอบ connection string
   - `POSTGRES_PASSWORD`
   - `PUBLIC_APP_URL`
   - `Storage__PublicBaseUrl`
   - `VITE_API_BASE_URL` หรือยืนยัน same-origin strategy
   - `Line__Enabled`
   - `Line__PublicAppUrl`
   - `Line__PublicFileBaseUrl`
3. ตรวจว่า frontend ไม่ชี้กลับ localhost

**ไฟล์ที่เกี่ยวข้อง**

- `.env.production.example`
- `docker-compose.prod.yml`
- `deploy/00-check-env.sh`
- `deploy/04-crosscheck.sh`
- `frontend/src/api/httpClient.ts`
- `frontend/src/api/securityApi.ts`
- `docs/ENVIRONMENT.md`

**วิธีทดสอบ**

```bash
deploy/00-check-env.sh
docker compose --env-file .env.production -f docker-compose.prod.yml config
```

หลัง build frontend:

```bash
grep -R -I "localhost:5000" frontend/dist || true
grep -R -I "127.0.0.1" frontend/dist || true
```

**เกณฑ์ผ่าน**

- deploy env check ผ่าน
- compose config ผ่าน
- frontend dist ไม่มี API endpoint เป็น localhost/127.0.0.1
- เปิดระบบผ่าน production URL แล้วเรียก `/api/*` ได้

**เกณฑ์ไม่ผ่าน**

- env สำคัญขาด
- frontend เรียก `https://localhost:5000`
- LINE public URL ยังเป็น localhost หรือ private LAN สำหรับ production

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- `.env.example` และ `.env.production.example` ระบุ key สำคัญเป็นค่าว่าง/placeholder แล้ว ได้แก่ `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `POSTGRES_PASSWORD`, `Seed__AdminPassword`, `Seed__StandardItPassword`, `Line__ChannelSecret`, `Line__AccessToken`
- เพิ่ม QA env keys ใน `.env.example`: `PHASE1_QA_USERNAME`, `PHASE1_QA_PASSWORD`
- ผลตรวจ `Select-String -Path backend/Hop.Api/appsettings*.json -Pattern "Password|Secret|AccessToken|ChannelAccessToken|Jwt|ConnectionStrings"` พบเฉพาะ key/ค่าว่าง ไม่พบ secret จริง
- สถานะยังเป็น `[~]` เพราะต้องยืนยัน `.env.production` จริงบน server และ run `docker compose --env-file .env.production -f docker-compose.prod.yml config`

### P0-05: ตัดสินใจและล็อก Role Mapping สำหรับ HR

- [x] สถานะ: ผ่านแล้ว

**งานที่ต้องทำ**

1. ตัดสินใจว่า Phase 1 จะใช้ role `HR` จริง หรือใช้ `LeaveAdmin` / `Admin` เป็น HR operator
2. อัปเดตเอกสาร permission matrix ให้ตรงกับ implementation
3. หากต้องใช้ role `HR` จริง ให้สร้าง seed/migration/test ในรอบ implementation ภายหลัง
4. ตรวจเมนู HR/Leave Admin ว่าสอดคล้องกับ role ที่เลือก

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Data/DevelopmentDataSeeder.cs`
- `docs/PERMISSION-MATRIX.md`
- `docs/security/PERMISSION-MODEL.md`
- `docs/manuals/phase1/05-HR-User-Guide.md`
- `frontend/src/config/menuConfig.ts`
- `frontend/src/routes/AppRoutes.tsx`

**วิธีทดสอบ**

```powershell
rg -n "HR|LeaveAdmin|Admin|ManageBalances|ManageHolidays|ManageTypes" backend frontend docs/PERMISSION-MATRIX.md docs/manuals/phase1
```

Manual:

1. Login ด้วยบัญชี HR/LeaveAdmin/Admin ที่กำหนด
2. ตรวจเมนูวันหยุด, ยอดวันลา, ประเภทลา, รายงาน
3. Login ด้วย Staff/Head/Director แล้วต้องไม่เห็นเมนู admin เกินสิทธิ์

**เกณฑ์ผ่าน**

- เอกสารและระบบใช้ชื่อ role/permission ตรงกัน
- ผู้ใช้ HR/LeaveAdmin ทำงาน HR ได้ครบตาม Phase 1
- Staff/Head/Director ไม่เห็นเมนู admin ที่ไม่ควรเห็น

**เกณฑ์ไม่ผ่าน**

- เอกสารพูดถึง `HR` แต่ระบบไม่มี role หรือ mapping ชัดเจน
- ผู้ดูแลวันลาไม่มีสิทธิ์ทำงาน HR
- ผู้ใช้ทั่วไปเห็นเมนูจัดการระบบ

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- ตัดสินใจ Phase 1 อย่างเป็นทางการว่า **ไม่ใช้ role `HR` แยกใน runtime ปัจจุบัน**
- ใช้ `LeaveAdmin` เป็น HR operator สำหรับงานระบบลา และใช้ `Admin` เฉพาะกรณีผู้ดูแลระบบที่ได้รับสิทธิ์ support เพิ่มเติม
- อัปเดตเอกสารให้ตรงกัน:
  - `docs/PERMISSION-MATRIX.md`
  - `docs/security/PERMISSION-MODEL.md`
  - `docs/manuals/phase1/05-HR-User-Guide.md`
- Evidence จาก code:
  - `backend/Hop.Api/Data/DevelopmentDataSeeder.cs` seed role `LeaveAdmin`
  - `frontend/src/config/menuConfig.ts` ใช้ permission กลุ่ม `LeaveAdmin.*` สำหรับเมนูประเภทลา, กฎอนุมัติ, วันลาคงเหลือ, วันหยุดราชการ
  - `frontend/src/routes/AppRoutes.tsx` guard route HR/Leave Admin ด้วย `LeaveAdmin.Manage*`

## P1 Should Fix Before Production

### P1-01: รัน full backend/frontend build และ test suite บน staging หรือ pilot environment

- [~] สถานะ: automated local gate ผ่านแล้ว / รอ manual E2E บน pilot

**งานที่ต้องทำ**

1. รัน backend tests
2. รัน frontend build
3. รัน frontend E2E หรือ manual E2E ตาม checklist
4. บันทึกผลใน QA report ล่าสุด

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api.Tests/*`
- `frontend/e2e/*`
- `docs/TESTING.md`
- `docs/qa/PHASE1-PILOT-TEST-REPORT.md`
- `docs/PHASE1-PILOT-CHECKLIST.md`

**วิธีทดสอบ**

```powershell
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
npm run build --prefix frontend
```

ถ้ามี script e2e:

```powershell
npm run e2e:phase1 --prefix frontend
```

**เกณฑ์ผ่าน**

- `dotnet test` ผ่าน
- `npm run build` ผ่าน
- E2E/manual checklist ผ่านครบ critical flows
- QA report มีวันที่, environment, ผู้ทดสอบ, ผลผ่าน/ไม่ผ่าน

**เกณฑ์ไม่ผ่าน**

- test fail โดยไม่มี waiver
- build fail
- ไม่มีผล manual E2E บน pilot environment

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- รัน backend tests:

```powershell
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
```

ผลลัพธ์: Passed 116/116

- รัน frontend production build แบบ same-origin:

```powershell
$env:VITE_API_URL=''; $env:VITE_API_BASE_URL=''; npm run build
```

ผลลัพธ์: Passed

- รัน frontend dist scan:

```powershell
rg -n "localhost:5000|https://localhost:5000|http://localhost:5000|127\.0\.0\.1|Line__ChannelSecret|Line__ChannelAccessToken|Line__AccessToken|Jwt__Key|POSTGRES_PASSWORD|change-this-jwt-secret|change-this-strong-postgres-password|Admin@1234|Nm@12345|VITE_API_URL =|VITE_API_BASE_URL =" frontend/dist
```

ผลลัพธ์: ไม่พบรายการ

- ยังไม่ปิดเป็น `[x]` เพราะ manual E2E บน pilot/staging environment ยังไม่ได้รัน

### P1-02: ทดสอบ critical leave workflow บน pilot database จริง

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. Staff login
2. Staff create leave request
3. Submit
4. Head เห็น notification/pending approval
5. Head approve
6. Director เห็น notification/pending approval
7. Director approve final
8. ตรวจ status, current approver, leave balance, PDF
9. ทำ reject flow และ cancel flow แยกกัน

**ไฟล์ที่เกี่ยวข้อง**

- `docs/TESTING.md`
- `docs/qa/PHASE1-PILOT-TEST-REPORT.md`
- `backend/Hop.Api.Tests/Phase1CriticalLeaveWorkflowTests.cs`
- `frontend/e2e/phase1-web-qa.spec.ts`

**วิธีทดสอบ**

Manual test ตามบัญชี pilot:

1. Login เป็น Staff
2. สร้าง/ส่งคำขอลา
3. Login เป็น Head
4. อนุมัติ
5. Login เป็น Director
6. อนุมัติ
7. ตรวจ Dashboard, Notification Bell, `/leave/pending-approvals`, PDF, Leave Balance

**เกณฑ์ผ่าน**

- Badge แสดงเฉพาะผู้อนุมัติปัจจุบัน
- หลัง approve step 1 งานย้ายไป Director
- หลัง final approve `CurrentApproverId = null`
- used balance ถูกหัก
- reject/cancel ไม่หัก used balance และ pending notification หาย

**เกณฑ์ไม่ผ่าน**

- ผู้ไม่เกี่ยวข้องเห็นงานรออนุมัติ
- Director/Head approve ก่อนถึงคิวได้
- reject/cancel แล้วยังมี pending badge
- balance หักผิด

### P1-03: ทดสอบ LINE real delivery และ Flex Message

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. ตั้งค่า LINE OA production/pilot channel
2. Bind LINE User ID กับผู้ใช้ pilot
3. ส่ง plain text test
4. ส่ง minimal flex test
5. ส่ง full leave approval flex
6. ตรวจ line delivery logs
7. ยืนยันว่า token/secret ไม่ถูกส่ง frontend หรือ log

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Controllers/AdminLineController.cs`
- `backend/Hop.Api/Controllers/LineWebhookController.cs`
- `frontend/src/pages/LineSettingsPage.tsx`
- `frontend/src/pages/LineUsersPage.tsx`
- `docs/LINE-MESSAGING.md`
- `docs/LINE-FLEX-MESSAGE.md`
- `docs/LINE-USER-BINDING.md`

**วิธีทดสอบ**

1. เปิด Admin > ตั้งค่า LINE
2. ตรวจ status card
3. กด Send Plain Text Test
4. กด Send Minimal Flex Test
5. กด Send Full Leave Flex Test
6. ตรวจ `line_delivery_logs`

**เกณฑ์ผ่าน**

- Plain text ส่งสำเร็จ
- Minimal flex ส่งสำเร็จ
- Full leave flex ส่งสำเร็จ หรือถ้า fail ต้องมี LINE response body ที่ชัดเจน
- Frontend แสดงเฉพาะ masked status
- ไม่มี token/secret ใน browser response/log

**เกณฑ์ไม่ผ่าน**

- HTTP 400/401 จาก LINE โดยไม่มี diagnostic เพียงพอ
- ส่ง token/secret กลับ frontend
- LINE user binding ผิด user หรือ duplicate ผิด policy

### P1-04: ตรวจ PDF ใบลา ภาษาไทย และ layout จริง

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. Download PDF จากคำขอลาจริง
2. ตรวจฟอนต์ TH SarabunPSK / Thai rendering
3. ตรวจข้อมูลผู้ขอ, วันลา, ครึ่งวัน, balance, approval timeline, ความเห็นหัวหน้า/ผอ.
4. ตรวจ permission download PDF

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Services/*Pdf*`
- `backend/Hop.Api/Controllers/LeaveRequestsController.cs`
- `docs/LEAVE-PDF.md`
- `docs/LEAVE-PDF-THAI-FONT.md`
- `docs/LEAVE-FORM-TEMPLATE.md`
- `backend/Hop.Api.Tests/LeavePdfTests.cs`

**วิธีทดสอบ**

1. สร้างคำขอลา full day
2. สร้างคำขอลาครึ่งวัน
3. อนุมัติครบ flow
4. กด “ดาวน์โหลดแบบฟอร์มใบลา”
5. เปิด PDF ด้วย PDF reader จริง

**เกณฑ์ผ่าน**

- ภาษาไทยไม่แยกตัวอักษร
- Layout ไม่ล้นหน้า A4
- วันที่เป็นรูปแบบไทยที่ระบบกำหนด
- ความเห็นผู้อนุมัติแสดงครบ
- ผู้ไม่มีสิทธิ์ download ไม่ได้

**เกณฑ์ไม่ผ่าน**

- ภาษาไทยเพี้ยนหรือหาย
- ข้อมูล approval timeline ไม่ครบ
- PDF download ได้จาก user ที่ไม่มีสิทธิ์

### P1-05: ตั้ง backup schedule และทำ restore test จริง

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. ตั้ง cron หรือ systemd timer สำหรับ backup
2. Backup PostgreSQL
3. Backup storage
4. Restore เข้า test database
5. Restore storage เข้า test path/volume
6. ตรวจ login, leave requests, attachments, profile images, PDF templates
7. บันทึกผล restore test

**ไฟล์ที่เกี่ยวข้อง**

- `scripts/backup/backup-hop.sh`
- `scripts/backup/restore-hop.sh`
- `docs/BACKUP-RESTORE.md`
- `docs/PHASE1-PILOT-CHECKLIST.md`
- `docker-compose.prod.yml`

**วิธีทดสอบ**

Backup:

```bash
BACKUP_MODE=docker DB_NAME=hop_db DB_USER=hop_app DB_PASSWORD="<secret>" BACKUP_ROOT=/opt/hop/backups scripts/backup/backup-hop.sh
```

Restore:

```bash
RESTORE_CONFIRM=I_UNDERSTAND_THIS_WILL_OVERWRITE_HOP DB_DUMP_PATH=/path/to/dump STORAGE_ARCHIVE_PATH=/path/to/storage.tar.gz scripts/backup/restore-hop.sh
```

**เกณฑ์ผ่าน**

- backup สำเร็จและมี dump/storage archive
- restore test สำเร็จใน test environment
- login และข้อมูลหลักตรวจได้
- มี log และวันเวลาของ backup/restore

**เกณฑ์ไม่ผ่าน**

- backup script fail
- restore fail
- ไม่มีหลักฐาน restore test
- backup เก็บ secret plain text ใน repo/log

### P1-06: ตรวจ Admin Health Dashboard บน staging/pilot

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. Login เป็น Admin/SuperAdmin
2. เปิด `/admin/health`
3. ตรวจ API, Database, Storage, LINE, Disk, Backup
4. Login เป็น Staff แล้วต้องเข้าไม่ได้

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Controllers/AdminHealthController.cs`
- `backend/Hop.Api/DTOs/HealthDtos.cs`
- `frontend/src/pages/AdminHealthPage.tsx`
- `frontend/src/routes/AppRoutes.tsx`
- `docs/HEALTH-DASHBOARD.md`

**วิธีทดสอบ**

```bash
curl -f https://<domain>/healthz
curl -f https://<domain>/health
```

Manual:

1. Admin เปิด `/admin/health`
2. Staff เปิด `/admin/health`

**เกณฑ์ผ่าน**

- `/health` และ `/healthz` ตอบสำเร็จ
- Admin เห็น health dashboard
- Staff ถูก block
- response ไม่แสดง secret/connection string/token

**เกณฑ์ไม่ผ่าน**

- health endpoint fail
- Staff เข้า health dashboard ได้
- response มี secret หรือ internal stack trace

### P1-07: ทดสอบ production safe error handling

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. ทดสอบ backend 500 response ใน production mode
2. ตรวจ response มี `referenceId`
3. ตรวจไม่แสดง stack trace
4. ตรวจ frontend ErrorBoundary ภาษาไทย
5. ตรวจ log หา referenceId ได้

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs`
- `backend/Hop.Api/Program.cs`
- `frontend/src/components/common/ErrorBoundary.tsx`
- `frontend/src/api/httpClient.ts`
- `docs/ERROR-HANDLING.md`

**วิธีทดสอบ**

Manual หรือ temporary staging-only endpoint/test ที่ทำให้เกิด exception โดยไม่กระทบ production data

**เกณฑ์ผ่าน**

- User เห็นข้อความภาษาไทยปลอดภัย
- มี Reference ID
- Production response ไม่มี stack trace
- Backend log มี referenceId เดียวกัน

**เกณฑ์ไม่ผ่าน**

- Stack trace แสดงบน frontend/response
- ไม่มี referenceId
- log หา referenceId ไม่เจอ

### P1-08: ตรวจ Audit Event Coverage และ Retention

- [~] สถานะ: coverage/docs พร้อมแล้ว / รอทดสอบ workflow จริงบน pilot

**งานที่ต้องทำ**

1. ทดสอบ flow create/submit/approve/reject/cancel/pdf/upload/download/login/permission denied
2. ตรวจ audit log สำหรับแต่ละ event
3. ทดสอบ export CSV/Excel/PDF
4. ทดสอบ retention run บน test data
5. กำหนดผู้รับผิดชอบ retention schedule

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Services/AuditLogService.cs`
- `backend/Hop.Api/Controllers/AuditLogsController.cs`
- `backend/Hop.Api/Middleware/PermissionDeniedAuditMiddleware.cs`
- `backend/Hop.Api/Services/AuditRetentionService.cs`
- `docs/AUDIT-EVENTS.md`
- `docs/AUDIT-RETENTION.md`
- `frontend/src/pages/AuditLogPage.tsx`
- `frontend/src/pages/AuditLogExportPage.tsx`

**วิธีทดสอบ**

1. ทำ action จริงตาม flow
2. เปิดหน้า Audit Log
3. Filter ตาม action/resource/user/date
4. Export เป็น CSV/Excel/PDF
5. Run retention ใน test environment

**เกณฑ์ผ่าน**

- Event สำคัญถูกบันทึกครบ
- Export เปิดได้
- Permission denied ถูก audit
- Retention run ลบเฉพาะข้อมูลเกินอายุ

**เกณฑ์ไม่ผ่าน**

- Action สำคัญไม่มี audit log
- Export fail
- Retention ลบข้อมูลผิดช่วงเวลา

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- ตรวจพบ service/controller/middleware สำหรับ audit ครบ:
  - `backend/Hop.Api/Services/AuditLogService.cs`
  - `backend/Hop.Api/Controllers/AuditLogsController.cs`
  - `backend/Hop.Api/Middleware/PermissionDeniedAuditMiddleware.cs`
  - `backend/Hop.Api/Services/AuditRetentionService.cs`
- ยืนยันจาก code ว่า Audit Log รองรับ list/filter/export CSV/Excel/PDF และ retention run endpoint
- อัปเดตเอกสาร:
  - `docs/AUDIT-EVENTS.md` เพิ่ม core events: login, permission denied, leave create/submit/approve/reject/cancel, PDF, attachment, profile, export, retention
  - `docs/AUDIT-RETENTION.md` ปรับเป็น Phase 1 capability และระบุ retention policy/endpoint ปัจจุบัน
- ยังไม่ปิดเป็น `[x]` เพราะต้องทำ manual workflow จริงบน pilot เพื่อยืนยัน event ในฐานข้อมูลจริงครบทุก action

### P1-09: ตรวจ deploy crosscheck ว่า frontend dist ไม่มี secret หรือ localhost endpoint

- [~] สถานะ: local production dist scan ผ่านแล้ว / รอ live deploy crosscheck

**งานที่ต้องทำ**

1. Build frontend แบบ production
2. Scan `frontend/dist`
3. ตรวจ secret markers
4. ตรวจ localhost/private endpoint
5. เพิ่มผล scan ลง deploy report

**ไฟล์ที่เกี่ยวข้อง**

- `deploy/04-crosscheck.sh`
- `frontend/src/api/httpClient.ts`
- `frontend/src/api/securityApi.ts`
- `deploy/frontend.Dockerfile`
- `docker-compose.prod.yml`

**วิธีทดสอบ**

```bash
npm run build --prefix frontend
deploy/04-crosscheck.sh
grep -R -I "localhost:5000" frontend/dist || true
grep -R -I "Line__ChannelSecret\|Jwt__Key\|POSTGRES_PASSWORD" frontend/dist || true
```

**เกณฑ์ผ่าน**

- ไม่มี secret marker ใน dist
- ไม่มี localhost API endpoint ใน production dist
- crosscheck script ผ่าน

**เกณฑ์ไม่ผ่าน**

- พบ secret marker
- พบ `localhost:5000` ใน dist production
- crosscheck fail โดยไม่มี waiver

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- แก้ frontend API fallback จาก `https://localhost:5000` เป็น same-origin ใน:
  - `frontend/src/api/httpClient.ts`
  - `frontend/src/api/securityApi.ts`
- ลบ debug console logging ของ Vite env ออกจาก `frontend/src/api/authApi.ts`
- แก้ production frontend Docker default:
  - `deploy/frontend.Dockerfile` เปลี่ยน `ARG VITE_API_BASE_URL=` เป็นค่าว่างสำหรับ same-origin
  - `frontend/.env.example` เปลี่ยน `VITE_API_URL` และ `VITE_API_BASE_URL` เป็นค่าว่าง
- รัน build แบบ production same-origin:

```powershell
$env:VITE_API_URL=''; $env:VITE_API_BASE_URL=''; npm run build
```

- ผล build: ผ่าน
- ผล scan:

```powershell
rg -n "localhost:5000|https://localhost:5000|http://localhost:5000|127\.0\.0\.1|Line__ChannelSecret|Line__ChannelAccessToken|Line__AccessToken|Jwt__Key|POSTGRES_PASSWORD|change-this-jwt-secret|change-this-strong-postgres-password|Admin@1234|Nm@12345|VITE_API_URL =|VITE_API_BASE_URL =" frontend/dist
```

ไม่พบรายการใน `frontend/dist`
- ตรวจ syntax script:

```bash
bash -n deploy/04-crosscheck.sh
```

ผ่าน
- ยังไม่ปิดเป็น `[x]` เพราะยังไม่ได้รัน `deploy/04-crosscheck.sh` กับ `.env.production` และ containers จริงบน staging/pilot

### P1-10: อัปเดต Phase 1 checklist ให้ตรงกับ script และ runtime ปัจจุบัน

- [x] สถานะ: ผ่านแล้ว

**งานที่ต้องทำ**

1. แก้ checklist ที่ยังอ้าง script เก่า เช่น `scripts/backup-postgres.ps1`
2. ระบุ script ปัจจุบัน `scripts/backup/backup-hop.sh` และ `scripts/backup/restore-hop.sh`
3. เพิ่มขั้นตอนตรวจ `.env.production.example`
4. เพิ่มขั้นตอนตรวจ P0/P1 ในเอกสารนี้

**ไฟล์ที่เกี่ยวข้อง**

- `docs/PHASE1-PILOT-CHECKLIST.md`
- `docs/BACKUP-RESTORE.md`
- `docs/DEPLOYMENT-CHECKLIST.md`
- `docs/audit/phase1-production-checklist.md`

**วิธีทดสอบ**

```powershell
rg -n "backup-postgres|restore-postgres|backup-hop|restore-hop|env.production" docs/PHASE1-PILOT-CHECKLIST.md docs/BACKUP-RESTORE.md docs/DEPLOYMENT-CHECKLIST.md
```

**เกณฑ์ผ่าน**

- เอกสารอ้าง script ปัจจุบันถูกต้อง
- ไม่มีคำสั่งเก่าที่ทำให้ผู้ปฏิบัติงานสับสน
- checklist มีขั้นตอนก่อน production ครบ

**เกณฑ์ไม่ผ่าน**

- ยังอ้าง script เก่าที่ไม่มีอยู่จริง
- ไม่มีขั้นตอน restore test

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- อัปเดต `docs/PHASE1-PILOT-CHECKLIST.md` ให้ใช้ production backup/restore script ปัจจุบัน:
  - `scripts/backup/backup-hop.sh`
  - `scripts/backup/restore-hop.sh`
- อัปเดต `docs/BACKUP-RESTORE.md` ให้ production runbook ใช้เฉพาะ Linux/Ubuntu script ปัจจุบัน และตัด reference ของ PowerShell PostgreSQL-only helper เดิมออกจาก production guidance
- ตรวจเอกสารด้วย:

```powershell
rg -n "backup-postgres|restore-postgres|backup-hop|restore-hop|env.production" docs/PHASE1-PILOT-CHECKLIST.md docs/BACKUP-RESTORE.md docs/DEPLOYMENT-CHECKLIST.md
```

ผลลัพธ์เหลือเฉพาะ script production ปัจจุบันและ `.env.production` checklist

## P2 Nice To Have

### P2-01: เพิ่ม structured backup status สำหรับ Health Dashboard

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. ให้ backup script เขียน status file เช่น `last_success.json`
2. Admin Health Dashboard อ่านข้อมูลนี้แทนการ scan folder อย่างเดียว
3. แสดง last success, duration, backup size, error ล่าสุด

**ไฟล์ที่เกี่ยวข้อง**

- `scripts/backup/backup-hop.sh`
- `backend/Hop.Api/Controllers/AdminHealthController.cs`
- `frontend/src/pages/AdminHealthPage.tsx`
- `docs/HEALTH-DASHBOARD.md`

**วิธีทดสอบ**

1. รัน backup สำเร็จ
2. ตรวจ status file
3. เปิด `/admin/health`

**เกณฑ์ผ่าน**

- Health Dashboard แสดง backup status จาก structured file
- ถ้า backup fail มี message ปลอดภัย

**เกณฑ์ไม่ผ่าน**

- Dashboard ยังเดาจาก file timestamp อย่างเดียว

### P2-02: เพิ่ม Correlation ID / Request ID Header ข้าม Nginx และ Backend

- [x] สถานะ: ผ่านแล้ว

**งานที่ต้องทำ**

1. กำหนด header เช่น `X-Correlation-ID`
2. Nginx ส่งต่อ header
3. Backend ใช้ correlation ID ใน log และ error response
4. Frontend แสดง Reference ID เดียวกัน

**ไฟล์ที่เกี่ยวข้อง**

- `deploy/nginx.conf`
- `backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs`
- `backend/Hop.Api/Program.cs`
- `frontend/src/api/httpClient.ts`
- `docs/ERROR-HANDLING.md`

**วิธีทดสอบ**

```bash
curl -H "X-Correlation-ID: test-hop-001" https://<domain>/api/<test-endpoint>
```

**เกณฑ์ผ่าน**

- Backend log มี `test-hop-001`
- Error response หรือ Reference ID สัมพันธ์กับ request เดียวกัน

**เกณฑ์ไม่ผ่าน**

- Header หายระหว่าง Nginx กับ backend

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- เพิ่ม `backend/Hop.Api/Middleware/CorrelationIdMiddleware.cs`
- เพิ่ม middleware ใน `backend/Hop.Api/Program.cs` ก่อน `GlobalExceptionMiddleware`
- ปรับ `deploy/nginx.conf` ให้ส่งต่อ `X-Correlation-ID` ไป backend
- `GlobalExceptionMiddleware` ใช้ `HttpContext.TraceIdentifier` เป็น `referenceId` อยู่แล้ว ทำให้ค่า correlation id กลายเป็น Reference ID เดียวกัน
- เพิ่ม test ใน `backend/Hop.Api.Tests/AdminHealthAndErrorTests.cs`
- อัปเดต `docs/ERROR-HANDLING.md` และ `docs/MONITORING.md`

### P2-03: เพิ่ม CI Gate สำหรับ Production Readiness

- [~] สถานะ: workflow เพิ่มแล้ว / รอ run บน PR จริง

**งานที่ต้องทำ**

1. เพิ่ม pipeline สำหรับ build/test
2. เพิ่ม secret scan
3. เพิ่ม Docker compose config validation
4. เพิ่ม frontend dist scan
5. เพิ่ม test report artifact

**ไฟล์ที่เกี่ยวข้อง**

- `.github/workflows/*` หรือ CI config ที่ใช้จริง
- `deploy/04-crosscheck.sh`
- `docs/CI-CD.md`
- `docs/TESTING.md`

**วิธีทดสอบ**

1. เปิด PR
2. ตรวจ CI checks
3. ตรวจ artifact/log

**เกณฑ์ผ่าน**

- PR ที่ build/test fail merge ไม่ได้
- secret marker ทำให้ CI fail
- test report ถูกเก็บเป็น artifact

**เกณฑ์ไม่ผ่าน**

- ยังต้องตรวจ readiness ด้วย manual ทั้งหมด

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- อัปเดต `.github/workflows/ci.yml`
- เพิ่ม frontend dist readiness scan หลัง `npm run build`
- เพิ่ม job `production-readiness` สำหรับ:
  - secret/default credential marker scan
  - syntax check ของ deploy/backup scripts
  - `docker compose -f docker-compose.prod.yml config` ด้วย CI-only dummy env
  - scan compose output ไม่ให้มี dev marker
- อัปเดต `docs/CI-CD.md`
- สถานะยังเป็น `[~]` เพราะต้องรอ GitHub Actions run จริงบน PR/push เพื่อยืนยันใน CI environment

### P2-04: Refresh คู่มือและ Screenshot หลัง UI Freeze

- [ ] สถานะ: ยังไม่เริ่ม

**งานที่ต้องทำ**

1. Capture screenshot ทุก role หลัง UI freeze
2. อัปเดต manual link/image mapping
3. ตรวจคำว่า HR/LeaveAdmin/Admin ให้ตรงกับ role mapping
4. ใส่ version/date ของคู่มือ

**ไฟล์ที่เกี่ยวข้อง**

- `docs/manuals/phase1/*`
- `docs/manuals/assets/screenshots/*`
- `tests/screenshots/*`
- `docs/manuals/assets/screenshots/Manual-Screenshot-Mapping.md`

**วิธีทดสอบ**

```powershell
npm run docs:screenshot
```

หรือใช้ manual capture ตาม `docs/manuals/assets/screenshots/Capture-Guide.md`

**เกณฑ์ผ่าน**

- Screenshot ตรงกับ UI ล่าสุด
- ทุก manual link เปิดได้
- คู่มือระบุ version/date

**เกณฑ์ไม่ผ่าน**

- คู่มือแสดง UI เก่า
- รูปหายหรือไม่ตรง role

### P2-05: เพิ่ม Health Check สำหรับ Queue / Background Worker

- [x] สถานะ: ผ่านแล้ว

**งานที่ต้องทำ**

1. ระบุ background jobs ที่ใช้จริง เช่น LINE retry, notification queue, backup job
2. เพิ่ม health component หรือ dashboard card
3. แสดง queue length, pending retry, last success/failure

**ไฟล์ที่เกี่ยวข้อง**

- `backend/Hop.Api/Controllers/AdminHealthController.cs`
- `backend/Hop.Api/Services/*Line*`
- `backend/Hop.Api/Services/*Notification*`
- `frontend/src/pages/AdminHealthPage.tsx`
- `docs/MONITORING.md`

**วิธีทดสอบ**

1. สร้าง failed LINE delivery ใน test environment
2. เปิด `/admin/health`
3. ตรวจ queue/retry status

**เกณฑ์ผ่าน**

- Admin เห็นสถานะ worker/queue
- ไม่มี secret ใน response

**เกณฑ์ไม่ผ่าน**

- Queue fail แต่ health ยังแสดง Healthy โดยไม่มีรายละเอียด

**อัปเดตล่าสุด 7 กรกฎาคม 2026**

- เพิ่ม `QueueHealthResponse` ใน `backend/Hop.Api/DTOs/HealthDtos.cs`
- เพิ่ม queue/worker health ใน `backend/Hop.Api/Controllers/AdminHealthController.cs`
- แสดงข้อมูล:
  - `lineRetryEnabled`
  - `approvalEscalationEnabled`
  - `pendingLineDeliveries`
  - `failedLineDeliveries`
  - `pendingRetries`
  - `lastLineSuccessAt`
  - `lastLineFailureAt`
- เพิ่ม card “Queue / Worker Status” ใน `frontend/src/pages/AdminHealthPage.tsx`
- อัปเดต `frontend/src/api/adminApi.ts`
- เพิ่ม backend test ใน `backend/Hop.Api.Tests/AdminHealthAndErrorTests.cs`
- อัปเดต `docs/HEALTH-DASHBOARD.md` และ `docs/MONITORING.md`

## Milestone แนะนำก่อน 1 ตุลาคม 2026

| ช่วงเวลา | งานหลัก | ผลลัพธ์ที่ต้องมี |
|---|---|---|
| ภายใน 2 สัปดาห์แรก | ปิด P0 ทั้งหมด | Secret/role/env พร้อม pilot |
| ภายในสิ้นเดือนถัดไป | ปิด P1-01 ถึง P1-05 | Test, LINE, PDF, backup ผ่านบน pilot |
| ก่อน go-live 2 สัปดาห์ | ปิด P1-06 ถึง P1-10 | Health, error, audit, deploy checklist ผ่าน |
| หลัง go-live หรือก่อน scale-up | P2 | Monitoring/CI/manual polish |

## Go / No-Go Gate

### Pilot Gate

- [ ] P0-01 ผ่าน
- [ ] P0-02 ผ่าน
- [ ] P0-03 ผ่าน
- [ ] P0-04 ผ่าน
- [ ] P0-05 ผ่าน

ผลสรุป Pilot Gate: `GO / NO-GO`  
ผู้อนุมัติ: `________________`  
วันที่: `________________`

### Production Gate

- [ ] P1-01 ผ่าน
- [ ] P1-02 ผ่าน
- [ ] P1-03 ผ่าน
- [ ] P1-04 ผ่าน
- [ ] P1-05 ผ่าน
- [ ] P1-06 ผ่าน
- [ ] P1-07 ผ่าน
- [ ] P1-08 ผ่าน
- [ ] P1-09 ผ่าน
- [ ] P1-10 ผ่าน

ผลสรุป Production Gate: `GO / NO-GO`  
ผู้อนุมัติ: `________________`  
วันที่: `________________`

## Evidence Log

ใช้พื้นที่นี้บันทึกหลักฐานหลังปิดงาน:

| Date | Item | Evidence Path / Command Output | Result | Owner |
|---|---|---|---|---|
|  |  |  |  |  |
