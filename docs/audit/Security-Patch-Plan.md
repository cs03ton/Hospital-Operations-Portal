# Hospital Operations Portal (HOP) - Security Patch Plan

เอกสารนี้เป็นแผนแก้ไขจาก `Security Report.md` เท่านั้น ยังไม่ได้แก้ source code ในรอบ audit

## Patch Status - 2026-07-23

| Item | Status | หลักฐาน |
|---|---|---|
| P0-01 Update vulnerable NuGet packages | Done | `dotnet list backend/Hop.Api/Hop.Api.csproj package --vulnerable --include-transitive` ไม่พบ vulnerable packages |
| P0-02 Harden leave attachment upload | Done | เพิ่ม `FileTypeValidationService`, ตรวจ extension/MIME/magic bytes และเพิ่ม spoofed PDF test |
| P0-03 Harden nginx production config | Done | อัปเดต `deploy/nginx.conf`, `deploy/nginx.baremetal.conf.example`, `docs/NGINX-HARDENING.md` |
| P1-01 Cookie token mode default | Done | frontend default เป็น `cookie`, `.env*.example` เป็น cookie mode |
| P1-02 Fallback authorization policy | Done | เพิ่ม `FallbackPolicy`, mark health/LINE/auth endpoint ที่จำเป็นเป็น anonymous |
| P1-03 Hash refresh tokens at rest | Done | เพิ่ม migration `20260723145513_AddRefreshTokenHashSecurity` |
| P1-04 Access token TTL config | Partial | เพิ่ม `Jwt__AccessTokenMinutes`/`Jwt__ClockSkewMinutes`; `TokenVersion/SecurityStamp` ยังเป็น roadmap |
| P1-05 Forwarded headers | Done | เพิ่ม `UseForwardedHeaders` และ `ForwardedHeaders__KnownProxies__0` |
| P1-06 Profile image hardening | Done | ใช้ shared validator และ storage resolver ปลอดภัยขึ้น |
| P1-07 Import DoS controls | Done | จำกัด holiday import size/rows/zip entry และใช้ XML DTD prohibit |
| P1-08 Container non-root | Done | backend/frontend container runtime เปลี่ยนเป็น non-root |
| P2-01 Exact CSRF exclusion | Done | จำกัด LINE webhook CSRF bypass เป็น POST exact path |
| P2-02 MFA readiness | Done (design) | เพิ่มแนวทางใน `docs/SECURITY.md`; implementation จริงเป็น phase ถัดไป |
| P2-03 Public health fingerprinting | Done | `/api/health` ไม่ส่งชื่อ service แล้ว, detail อยู่ที่ admin health |
| P2-04 Correlation id format | Done | จำกัดเป็น alphanumeric + `-`/`_` |

## P0 - Must Fix Before Production

### P0-01: Update vulnerable NuGet transitive packages

- งานที่ต้องทำ:
  - อัปเดต .NET package patch version จาก `9.0.0` เป็น patch ล่าสุด
  - ตรวจว่า `System.Net.Http 4.3.0` และ `System.Text.RegularExpressions 4.3.0` ไม่ติด vulnerable advisory แล้ว
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Hop.Api.csproj`
  - `backend/Hop.Api.Tests/Hop.Api.Tests.csproj`
- วิธีทดสอบ:
  - `dotnet restore`
  - `dotnet list backend/Hop.Api/Hop.Api.csproj package --vulnerable --include-transitive`
  - `dotnet build backend/Hop.Api/Hop.Api.csproj`
  - `dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj`
- เกณฑ์ผ่าน:
  - ไม่พบ High/Critical advisory
  - build/test ผ่าน

### P0-02: Harden leave attachment upload

- งานที่ต้องทำ:
  - เพิ่ม shared `FileTypeValidationService`
  - ตรวจ extension, browser MIME, magic bytes/file signature
  - ตรวจ image decode สำหรับ jpg/png/webp
  - ตรวจ PDF signature `%PDF-`
  - ตรวจ ClamAV fail-closed ใน production
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Services/LeaveAttachmentStorageService.cs`
  - `backend/Hop.Api/Controllers/LeaveRequestsController.cs`
  - `backend/Hop.Api/Services/AnnouncementMediaStorageService.cs`
  - `backend/Hop.Api/Interfaces/IFileScanningService.cs`
- วิธีทดสอบ:
  - upload valid PDF/JPG/PNG/WEBP สำเร็จ
  - upload `.jpg` ที่ content ไม่ใช่ image ต้อง fail
  - upload `.pdf` ปลอมต้อง fail
  - ปิด ClamAV เมื่อ fail-closed ต้อง upload fail พร้อมข้อความปลอดภัย
- เกณฑ์ผ่าน:
  - malicious/spoofed files ถูก block
  - existing leave attachment flow ยังทำงาน

### P0-03: Harden bare-metal nginx production config

- งานที่ต้องทำ:
  - เพิ่ม `server_tokens off`
  - เพิ่ม CSP เทียบเท่า docker nginx config
  - เพิ่ม HSTS เฉพาะ HTTPS
  - เพิ่ม TLS 1.2/1.3 example
  - เพิ่ม `limit_req` สำหรับ login/webhook/upload ตามความเหมาะสม
- ไฟล์ที่เกี่ยวข้อง:
  - `deploy/nginx.baremetal.conf.example`
  - `docs/NGINX-HARDENING.md`
  - `docs/DEPLOYMENT.md`
- วิธีทดสอบ:
  - `sudo nginx -t`
  - curl response headers
  - login/upload/LINE webhook ยังทำงาน
- เกณฑ์ผ่าน:
  - headers ครบ
  - ไม่มี redirect/challenge ทำให้ webhook fail
  - nginx reload ได้

## P1 - Should Fix Before Full Production

### P1-01: Make cookie token mode production-safe by default

- งานที่ต้องทำ:
  - เปลี่ยน frontend default token storage เป็น `cookie`
  - production build fail หาก token storage mode ไม่ใช่ `cookie`
  - ตรวจ `.env.production.example` ให้มี `VITE_AUTH_TOKEN_STORAGE_MODE=cookie`
- ไฟล์ที่เกี่ยวข้อง:
  - `frontend/src/api/httpClient.ts`
  - `.env.production.example`
  - `docs/ENVIRONMENT.md`
- วิธีทดสอบ:
  - production build ด้วย cookie mode
  - login/refresh/logout ทำงาน
  - localStorage ไม่มี access/refresh token
- เกณฑ์ผ่าน:
  - access/refresh token ไม่ถูกเก็บใน localStorage ใน production

### P1-02: Add fallback authorization policy

- งานที่ต้องทำ:
  - ตั้ง `FallbackPolicy` ให้ require authenticated user
  - ใส่ `[AllowAnonymous]` เฉพาะ endpoint public จริง เช่น login, refresh, csrf, LINE webhook, health live เฉพาะที่จำเป็น
  - เพิ่ม regression tests สำหรับ endpoint protected
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Program.cs`
  - `backend/Hop.Api/Controllers/AuthController.cs`
  - `backend/Hop.Api/Controllers/LineWebhookController.cs`
  - `backend/Hop.Api/Controllers/HealthController.cs`
- วิธีทดสอบ:
  - unauthenticated protected endpoint ต้อง 401/403
  - `/api/line/webhook` valid signature ยัง 200
  - `/api/auth/login` ยัง login ได้
- เกณฑ์ผ่าน:
  - ไม่มี endpoint ใหม่ public โดยไม่ตั้งใจ

### P1-03: Hash refresh tokens at rest

- งานที่ต้องทำ:
  - เพิ่ม column `token_hash`
  - migrate existing sessions โดย force logout หรือ transition safely
  - เก็บ token prefix/masked value เฉพาะ audit ถ้าต้องใช้
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Models/RefreshToken.cs`
  - `backend/Hop.Api/Controllers/AuthController.cs`
  - EF migration
- วิธีทดสอบ:
  - login/refresh rotation/logout ผ่าน
  - DB ไม่มี refresh token plaintext
  - reuse detection ยังทำงาน
- เกณฑ์ผ่าน:
  - DB leak ไม่สามารถนำ token ไปใช้ได้ตรง ๆ

### P1-04: Shorten access token and add token version readiness

- งานที่ต้องทำ:
  - เปลี่ยน access token TTL เป็น config เช่น `Jwt:AccessTokenMinutes=15`
  - เพิ่ม user `TokenVersion` หรือ `SecurityStamp` ใน phase ถัดไป
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Services/JwtTokenService.cs`
  - `backend/Hop.Api/appsettings.json`
- วิธีทดสอบ:
  - token หมดอายุตาม config
  - refresh ทำงานก่อนหมด session
- เกณฑ์ผ่าน:
  - stolen access token window ลดลง

### P1-05: Add Forwarded Headers middleware for nginx

- งานที่ต้องทำ:
  - เพิ่ม `UseForwardedHeaders`
  - จำกัด `KnownProxies` หรือ `KnownNetworks`
  - ตรวจ secure cookie/scheme/audit IP หลัง reverse proxy
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Program.cs`
  - `deploy/nginx.baremetal.conf.example`
  - `docs/DEPLOYMENT.md`
- วิธีทดสอบ:
  - audit log IP ตรง client
  - secure cookie ทำงานหลัง nginx
  - login rate limiter ไม่เห็นทุกคนเป็น `127.0.0.1`
- เกณฑ์ผ่าน:
  - proxy headers ถูกเชื่อถือเฉพาะ proxy ที่กำหนด

### P1-06: Harden profile image upload and public delivery

- งานที่ต้องทำ:
  - ใช้ shared image validator
  - เพิ่ม image decode/magic-byte
  - พิจารณา public/signed URL สำหรับ LINE แทน anonymous endpoint ถาวร
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Controllers/MeProfileController.cs`
  - `backend/Hop.Api/Controllers/UsersController.cs`
- วิธีทดสอบ:
  - image valid upload ได้
  - spoofed content type fail
  - LINE flex ยังแสดง avatar เมื่อ public URL พร้อม
- เกณฑ์ผ่าน:
  - ไม่มี content-type spoofing และ privacy behavior ถูกกำหนดชัด

### P1-07: Add upload/import DoS controls

- งานที่ต้องทำ:
  - จำกัด holiday XLSX import size
  - จำกัด zip entries/uncompressed size/row count
  - ใช้ `XmlReaderSettings` ที่ prohibit DTD
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Controllers/LeaveHolidaysController.cs`
- วิธีทดสอบ:
  - normal import ผ่าน
  - zip bomb/oversized workbook fail อย่างปลอดภัย
- เกณฑ์ผ่าน:
  - import ไม่ทำให้ memory/CPU spike เกินควบคุม

### P1-08: Container non-root hardening

- งานที่ต้องทำ:
  - เพิ่ม non-root user ใน backend Dockerfile
  - ตรวจ frontend/nginx container runtime user หรือใช้ nginx unprivileged image
  - ปรับ volume permission
- ไฟล์ที่เกี่ยวข้อง:
  - `deploy/backend.Dockerfile`
  - `deploy/frontend.Dockerfile`
  - `docker-compose.prod.yml`
- วิธีทดสอบ:
  - container start ได้
  - write storage/log ได้เฉพาะ path ที่กำหนด
  - `docker exec id` ไม่ใช่ root
- เกณฑ์ผ่าน:
  - app ไม่รัน root ใน container

## P2 - Nice to Have / Defense in Depth

### P2-01: Exact-match CSRF exclusion for LINE webhook

- งานที่ต้องทำ:
  - เปลี่ยน `StartsWithSegments("/api/line/webhook")` เป็น exact path + POST method
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Middleware/CsrfProtectionMiddleware.cs`
- วิธีทดสอบ:
  - `/api/line/webhook` valid signature 200
  - `/api/line/webhook/anything` ไม่ถูกยกเว้นอัตโนมัติ
- เกณฑ์ผ่าน:
  - exemption แคบที่สุด

### P2-02: Add MFA readiness

- งานที่ต้องทำ:
  - ออกแบบ field และ UI สำหรับ MFA flag ในอนาคต
  - รองรับ TOTP หรือ LINE second factor policy
- ไฟล์ที่เกี่ยวข้อง:
  - `docs/SECURITY.md`
  - auth roadmap
- วิธีทดสอบ:
  - design review
- เกณฑ์ผ่าน:
  - พร้อมต่อยอดโดยไม่กระทบ phase 1

### P2-03: Reduce public health fingerprinting

- งานที่ต้องทำ:
  - แยก public live health แบบ minimal
  - protect detail/admin health ด้วย permission
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Controllers/HealthController.cs`
  - `backend/Hop.Api/Controllers/AdminHealthController.cs`
- วิธีทดสอบ:
  - public live ยังใช้กับ load balancer ได้
  - detail health ต้องใช้ admin permission
- เกณฑ์ผ่าน:
  - public endpoint ไม่เผย service detail เกินจำเป็น

### P2-04: Tighten correlation id format

- งานที่ต้องทำ:
  - จำกัด correlation id เป็น alphanumeric + `-_.`
  - เพิ่ม test สำหรับ CRLF/invalid chars
- ไฟล์ที่เกี่ยวข้อง:
  - `backend/Hop.Api/Middleware/CorrelationIdMiddleware.cs`
- วิธีทดสอบ:
  - valid id ถูกส่งต่อ
  - invalid id ถูกแทนด้วย server trace id
- เกณฑ์ผ่าน:
  - log/search format stable

## Verification Gate After Patches

ให้ใช้ gate นี้หลัง patch:

```bash
dotnet restore
dotnet build backend/Hop.Api/Hop.Api.csproj
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
dotnet list backend/Hop.Api/Hop.Api.csproj package --vulnerable --include-transitive

cd frontend
npm audit --omit=dev --audit-level=moderate
npm run build
```

Manual security smoke:

- Login/logout/refresh token
- CSRF cookie mode save form
- LINE webhook Verify
- Leave attachment valid/invalid uploads
- Announcement image/file uploads
- Admin health protected
- Permission denied routes
- nginx response headers
