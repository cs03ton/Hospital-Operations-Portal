# Hospital Operations Portal (HOP) - Security Audit Report

วันที่ตรวจ: 23 กรกฎาคม 2026  
ขอบเขต: Backend .NET 9, React frontend, PostgreSQL access, upload/storage, LINE webhook, nginx/deploy/docker, configuration, logging, dependencies  
ข้อจำกัด: รายงานนี้เป็นการตรวจจาก source repository และคำสั่ง audit ที่รันในเครื่องพัฒนา ไม่ใช่ penetration test บน production จริง

## Executive Summary

ระบบ HOP มี foundation ด้าน security ที่ดีขึ้นชัดเจน ได้แก่ JWT validation, permission attributes, CSRF สำหรับ cookie mode, global exception middleware, audit log, LINE webhook signature validation, ClamAV integration ในบาง upload path และ nginx docker config ที่มี security headers หลักครบหลายรายการ

อย่างไรก็ตาม ยังมีช่องว่างที่ควรปิดก่อน production โดยเฉพาะ:

- Leave attachment upload ยังตรวจ extension และ malware scan แต่ยังไม่ validate MIME/signature เท่ากับ announcement media
- Frontend ยัง fallback ไปเก็บ access/refresh token ใน `localStorage` หาก build ไม่กำหนด cookie mode
- NuGet dependency audit พบ transitive vulnerable packages ระดับ High
- Bare-metal nginx example ที่ใกล้กับ production ปัจจุบันยังไม่มี CSP/HSTS/server_tokens/rate limit/TLS guidance ครบเท่า docker nginx config
- Docker image ยังรันด้วย root user
- ไม่มี fallback authorization policy ทำให้ controller/action ใหม่อาจเผลอ public ได้หากลืม `[Authorize]`

## Security Scores

| Area | Score | Reason |
|---|---:|---|
| Authentication | 7.0/10 | JWT + refresh rotation + login rate limiter มีแล้ว แต่ access token 60 นาที revoke ไม่ได้ทันที, ไม่มี MFA, refresh token เก็บ plaintext |
| Authorization | 7.5/10 | ใช้ permission attributes และ leave access service ค่อนข้างดี แต่ไม่มี fallback authorization policy และมี anonymous image endpoint |
| API Security | 7.0/10 | EF Core เป็นหลัก, raw SQL น้อย, แต่ upload validation ไม่สม่ำเสมอและบาง import path มี zip parsing risk |
| Frontend | 7.0/10 | ไม่พบ `dangerouslySetInnerHTML/eval` ใน `frontend/src`, แต่ token fallback เป็น localStorage และ CSP ใน bare-metal ยังไม่ครบ |
| Backend | 7.2/10 | Exception safe, CSRF, audit, webhook signature ดี แต่ upload/JWT/session hardening ยังควรเพิ่ม |
| Infrastructure | 6.5/10 | มี docker-compose/nginx/backup docs แต่ bare-metal hardening และ container non-root ยังไม่ครบ |
| Overall | 7.1/10 | พร้อม pilot ภายใต้การควบคุม แต่ยังมี P0/P1 ที่ควรปิดก่อน production public-facing |

## Critical Findings

ไม่พบช่องโหว่ระดับ Critical ที่ยืนยันได้จาก source repository ในรอบนี้

> หมายเหตุ: หากไฟล์ `.env` ในเครื่องพัฒนาที่มี password/token จริงถูกนำไป deploy, share หรือ commit จะยกระดับเป็น Critical ทันที แม้ปัจจุบัน `.gitignore` จะกัน `.env` แล้ว

## High Findings

### H-01: NuGet transitive dependencies มี advisory ระดับ High

- ไฟล์: [backend/Hop.Api/Hop.Api.csproj](../../backend/Hop.Api/Hop.Api.csproj)
- Evidence:
  - `dotnet list backend/Hop.Api/Hop.Api.csproj package --vulnerable --include-transitive`
  - พบ `System.Net.Http 4.3.0` High: GHSA-7jgj-8wvc-jh57
  - พบ `System.Text.RegularExpressions 4.3.0` High: GHSA-cmhx-cq75-c4mj
- ความเสี่ยง: dependency เก่าระดับ transitive อาจมีช่องโหว่ที่ถูก exploit ผ่าน HTTP/regex processing ในบาง runtime path
- วิธีโจมตี: attacker ส่ง input ที่กระตุ้น vulnerable code path ของ package transitive
- ผลกระทบ: DoS, information disclosure หรือ behavior ที่ advisory ระบุ ขึ้นกับ runtime path
- วิธีแก้:
  - อัปเดต top-level packages .NET 9 จาก `9.0.0` เป็น patch ล่าสุดที่รองรับ
  - รัน `dotnet list package --vulnerable --include-transitive` ซ้ำจนไม่พบ High/Critical
- ตัวอย่างแนวทาง:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.x" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.x" />
```

### H-02: Leave attachment upload ยังไม่ validate MIME/signature ของไฟล์

- ไฟล์:
  - [backend/Hop.Api/Services/LeaveAttachmentStorageService.cs](../../backend/Hop.Api/Services/LeaveAttachmentStorageService.cs)
  - [backend/Hop.Api/Controllers/LeaveRequestsController.cs](../../backend/Hop.Api/Controllers/LeaveRequestsController.cs)
- Evidence:
  - `LeaveAttachmentStorageService.cs:21-35` ตรวจ size และ extension
  - `LeaveAttachmentStorageService.cs:60` บันทึก `ContentType = file.ContentType`
  - `LeaveRequestsController.cs:774` เรียก `fileScanningService.ScanAsync`
  - ไม่พบ magic-byte/decode validation เหมือน announcement media
- ความเสี่ยง: attacker อัปโหลดไฟล์ที่ปลอม extension หรือ MIME ได้ เช่น polyglot/malicious PDF/image
- วิธีโจมตี: อัปโหลดไฟล์ `.jpg` หรือ `.pdf` ที่ content จริงไม่ตรงชนิด แล้วให้ผู้ใช้อื่น preview/download
- ผลกระทบ: malware distribution, phishing payload, PDF exploit, content-type confusion
- วิธีแก้:
  - นำ validation pattern จาก `AnnouncementMediaStorageService` มาใช้กับ leave attachments
  - ตรวจ extension + MIME + magic bytes + decode image
  - บังคับ ClamAV fail-closed ใน production
- ตัวอย่างแนวทาง:

```csharp
var mimeType = await fileTypeValidator.ValidateAsync(file, allowedExtensions, cancellationToken);
if (!allowedMimeTypes.Contains(mimeType))
{
    throw new InvalidOperationException("File content type is not allowed.");
}
```

### H-03: Frontend fallback เก็บ token ใน localStorage หาก build config ไม่ถูกตั้ง

- ไฟล์:
  - [frontend/src/api/httpClient.ts](../../frontend/src/api/httpClient.ts)
  - [frontend/src/context/AuthContext.tsx](../../frontend/src/context/AuthContext.tsx)
- Evidence:
  - `httpClient.ts:7` default `VITE_AUTH_TOKEN_STORAGE_MODE ?? "localStorage"`
  - `AuthContext.tsx:25-31`, `108-111` อ่าน/เขียน access token, refresh token, user ลง `localStorage` เมื่อไม่ใช่ cookie mode
- ความเสี่ยง: หากเกิด XSS หรือ browser extension compromise จะขโมย access/refresh token ได้
- วิธีโจมตี: inject JavaScript ผ่าน dependency/DOM injection/third-party script แล้วอ่าน `localStorage`
- ผลกระทบ: session hijacking จนกว่า token หมดอายุหรือถูก revoke
- วิธีแก้:
  - Production default ควรเป็น cookie mode
  - build fail หาก `VITE_AUTH_TOKEN_STORAGE_MODE` ไม่ใช่ `cookie` ใน production
  - refresh token ควรอยู่ใน HttpOnly Secure cookie เท่านั้น
- ตัวอย่างแนวทาง:

```ts
const tokenStorageMode = (import.meta.env.VITE_AUTH_TOKEN_STORAGE_MODE ?? "cookie").toLowerCase();
if (import.meta.env.PROD && tokenStorageMode !== "cookie") {
  throw new Error("Production must use cookie token storage mode.");
}
```

### H-04: Bare-metal nginx hardening ยังไม่ครบเท่า docker nginx config

- ไฟล์:
  - [deploy/nginx.baremetal.conf.example](../../deploy/nginx.baremetal.conf.example)
  - [deploy/nginx.conf](../../deploy/nginx.conf)
- Evidence:
  - Docker nginx มี CSP/HSTS ที่ `deploy/nginx.conf:40-45`
  - Bare-metal nginx มีเฉพาะ `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` ที่ `deploy/nginx.baremetal.conf.example:12-15`
  - ไม่พบ `server_tokens off`, `limit_req`, TLS 1.2/1.3 config ใน bare-metal example
- ความเสี่ยง: production bare-metal อาจไม่มี CSP/HSTS/rate limit ทำให้ XSS impact สูงขึ้น และขาด HTTP hardening
- วิธีโจมตี: ใช้ reflected/stored XSS ที่หลุดมาแล้ว browser ไม่มี CSP ช่วยลดผลกระทบ, brute force/DoS ไม่มี rate limit ที่ edge
- ผลกระทบ: session/token theft risk สูงขึ้น, clickjacking/sniffing บางส่วนถูกลดแล้วแต่ยังไม่ครบ
- วิธีแก้:
  - ย้าย header policy จาก docker nginx ไปยัง bare-metal config
  - เพิ่ม TLS server block, HSTS เฉพาะ HTTPS, rate limit เฉพาะ auth/upload/API
  - ตั้ง `server_tokens off`
- ตัวอย่างแนวทาง:

```nginx
server_tokens off;
add_header Content-Security-Policy "default-src 'self'; object-src 'none'; frame-ancestors 'none';" always;
limit_req_zone $binary_remote_addr zone=api_login:10m rate=5r/m;
```

## Medium Findings

### M-01: ไม่มี fallback authorization policy

- ไฟล์: [backend/Hop.Api/Program.cs](../../backend/Hop.Api/Program.cs)
- Evidence:
  - `Program.cs:176-179` ใช้ authentication/authorization middleware
  - ไม่พบ `FallbackPolicy`
  - Controller ส่วนใหญ่มี `[Authorize]` แต่ต้องพึ่ง developer ว่าจะไม่ลืม
- ความเสี่ยง: controller/action ใหม่อาจ public โดยไม่ตั้งใจ
- วิธีโจมตี: เรียก endpoint ใหม่ที่ลืมใส่ `[Authorize]`
- ผลกระทบ: data exposure หรือ unauthorized action
- วิธีแก้:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

แล้ว mark เฉพาะ endpoint ที่ตั้งใจ public ด้วย `[AllowAnonymous]`

### M-02: Health endpoint แบบ public แม้ข้อมูลไม่ sensitive มาก

- ไฟล์: [backend/Hop.Api/Controllers/HealthController.cs](../../backend/Hop.Api/Controllers/HealthController.cs), [backend/Hop.Api/Program.cs](../../backend/Hop.Api/Program.cs)
- Evidence:
  - `HealthController.cs` ไม่มี `[Authorize]`
  - `Program.cs:182-185` map `/health`, `/healthz`, `/health/live`, `/health/ready`
- ความเสี่ยง: attacker ใช้ fingerprint service/status/uptime ได้
- วิธีโจมตี: scan endpoint health เพื่อหา service, timing, readiness
- ผลกระทบ: reconnaissance ง่ายขึ้น
- วิธีแก้:
  - คง `/health/live` แบบ minimal public ได้
  - จำกัด `/health/ready` และ detail health ผ่าน network ACL หรือ admin auth

### M-03: Access token อายุ 60 นาทีและ logout ไม่ revoke access token

- ไฟล์: [backend/Hop.Api/Services/JwtTokenService.cs](../../backend/Hop.Api/Services/JwtTokenService.cs), [backend/Hop.Api/Controllers/AuthController.cs](../../backend/Hop.Api/Controllers/AuthController.cs)
- Evidence:
  - `JwtTokenService.cs:43` access token expires 60 minutes
  - `AuthController.cs:146-165` logout revoke refresh token/cookie แต่ access token ที่ออกไปแล้วยังใช้ได้จนหมดอายุ
- ความเสี่ยง: token ที่ถูกขโมยยังใช้ต่อได้ระหว่าง window
- วิธีโจมตี: ขโมย bearer token จาก localStorage/log/browser และเรียก API ก่อนหมดอายุ
- ผลกระทบ: unauthorized access ชั่วคราว
- วิธีแก้:
  - ลด access token เป็น 10-15 นาที
  - เพิ่ม token version/security stamp หรือ denylist สำหรับ logout/password change ใน phase ถัดไป

### M-04: Refresh token เก็บ plaintext ใน database

- ไฟล์: [backend/Hop.Api/Models/RefreshToken.cs](../../backend/Hop.Api/Models/RefreshToken.cs), [backend/Hop.Api/Controllers/AuthController.cs](../../backend/Hop.Api/Controllers/AuthController.cs)
- Evidence:
  - `AuthController.cs:54-58`, `124-129` บันทึก refresh token value ลง DB ตรง ๆ
  - `RefreshToken.cs` model มี `Token` เป็น string
- ความเสี่ยง: หาก DB dump หลุด attacker ใช้ refresh token ได้ทันที
- วิธีโจมตี: ขโมย DB backup หรือ read-only DB credential
- ผลกระทบ: account takeover ผ่าน refresh token
- วิธีแก้:
  - เก็บ hash ของ refresh token แทน plaintext
  - compare ด้วย hash และเก็บ token prefix สำหรับ audit เท่านั้น

```csharp
var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
```

### M-05: Profile image upload เชื่อ `ContentType` จาก browser เป็นหลัก

- ไฟล์: [backend/Hop.Api/Controllers/MeProfileController.cs](../../backend/Hop.Api/Controllers/MeProfileController.cs)
- Evidence:
  - `MeProfileController.cs:140-145` ตรวจ `file.ContentType` แล้ว map extension
  - ไม่พบ magic-byte/decode validation หรือ malware scan ใน upload path นี้
- ความเสี่ยง: content-type spoofing
- วิธีโจมตี: ส่งไฟล์ payload ที่ header เป็น `image/png` แต่ content จริงไม่ใช่ image
- ผลกระทบ: malicious file storage/served as image, content confusion
- วิธีแก้: ใช้ shared image validator เดียวกับ announcement media

### M-06: Anonymous profile image endpoint อาจเปิดเผยข้อมูลภาพส่วนตัวหากเดา/รู้ UUID

- ไฟล์: [backend/Hop.Api/Controllers/UsersController.cs](../../backend/Hop.Api/Controllers/UsersController.cs)
- Evidence:
  - `UsersController.cs:23` `[AllowAnonymous]` สำหรับ `GET /api/users/{id}/profile-image`
  - `UsersController.cs:40-42` ส่ง cache public
- ความเสี่ยง: profile image เข้าถึงได้โดยไม่ login หากรู้ user ID
- วิธีโจมตี: ใช้ UUID จาก API/LINE/flex/URL เพื่อดึงรูป
- ผลกระทบ: privacy exposure ระดับรูปบุคลากร
- วิธีแก้:
  - ใช้ signed public URL อายุสั้นสำหรับ LINE
  - หรือแยก anonymous image proxy เฉพาะรูปที่ user consent/public flag

### M-07: CSRF webhook exclusion ใช้ prefix match ไม่ใช่ exact path

- ไฟล์: [backend/Hop.Api/Middleware/CsrfProtectionMiddleware.cs](../../backend/Hop.Api/Middleware/CsrfProtectionMiddleware.cs)
- Evidence:
  - `CsrfProtectionMiddleware.cs:74-75` ใช้ `StartsWithSegments("/api/line/webhook")`
- ความเสี่ยง: path ใต้ `/api/line/webhook/*` จะถูกยกเว้น CSRF ด้วย หากมี endpoint เพิ่มภายหลัง
- วิธีโจมตี: ใช้ endpoint unsafe method ที่ path prefix เดียวกันในอนาคต
- ผลกระทบ: CSRF bypass เฉพาะ path prefix
- วิธีแก้: ตรวจ exact path + method

```csharp
var isLineWebhook = HttpMethods.IsPost(context.Request.Method)
    && context.Request.Path.Equals("/api/line/webhook", StringComparison.OrdinalIgnoreCase);
```

### M-08: ไม่มี Forwarded Headers middleware ทำให้ IP/audit/rate limit หลัง nginx อาจคลาดเคลื่อน

- ไฟล์: [backend/Hop.Api/Program.cs](../../backend/Hop.Api/Program.cs), [deploy/nginx.baremetal.conf.example](../../deploy/nginx.baremetal.conf.example)
- Evidence:
  - nginx ส่ง `X-Forwarded-For` และ `X-Forwarded-Proto`
  - `Program.cs` ไม่พบ `UseForwardedHeaders`
- ความเสี่ยง: audit IP เป็น proxy IP, login rate limit อาจบิดเบือน, HTTPS scheme detection ไม่แน่นอน
- วิธีโจมตี: ลดคุณภาพ audit/rate-limit forensic หรือทำให้ lockout behavior ไม่ตรง
- ผลกระทบ: security monitoring และ incident response อ่อนลง
- วิธีแก้: ตั้ง `ForwardedHeadersOptions` จำกัด known proxy เป็น `127.0.0.1`/nginx IP

### M-09: Backup verification เรียก external process แม้ใช้ ArgumentList แล้ว ยังต้องจำกัด path/permission ต่อ

- ไฟล์: [backend/Hop.Api/Services/BackupCenterService.cs](../../backend/Hop.Api/Services/BackupCenterService.cs)
- Evidence:
  - `BackupCenterService.cs:612-626` เรียก `pg_restore`/`tar` ด้วย `ProcessStartInfo`
  - ใช้ `ArgumentList` และ `UseShellExecute=false` ซึ่งลด command injection ได้ดี
- ความเสี่ยง: หาก backup file path จาก DB ถูก tamper หรือสิทธิ์ admin ถูก abuse อาจใช้ binary/path ที่ไม่คาดคิด
- วิธีโจมตี: tamper DB backup path หรือ PATH environment
- ผลกระทบ: file read/verification abuse หรือ process execution surface
- วิธีแก้:
  - ใช้ absolute binary path จาก config allowlist
  - ตรวจ backup file path ต้องอยู่ใต้ `Backup:RootPath`
  - run service user ที่สิทธิต่ำ

### M-10: Holiday XLSX import มี zip/xml parsing surface

- ไฟล์: [backend/Hop.Api/Controllers/LeaveHolidaysController.cs](../../backend/Hop.Api/Controllers/LeaveHolidaysController.cs)
- Evidence:
  - `LeaveHolidaysController.cs:244` เปิด `ZipArchive`
  - `LeaveHolidaysController.cs:253`, `373` ใช้ `XDocument.Load`
- ความเสี่ยง: zip bomb/large XML หากไม่มี strict size/entry limits
- วิธีโจมตี: upload XLSX ที่มี zip entry ใหญ่หรือ XML ซับซ้อน
- ผลกระทบ: memory/CPU DoS
- วิธีแก้:
  - จำกัด upload size
  - จำกัดจำนวน entry, uncompressed size, row count
  - ใช้ `XmlReaderSettings { DtdProcessing = Prohibit }`

## Low Findings

### L-01: Local `.env` มี default/dev password แม้ถูก ignore แล้ว

- ไฟล์: `.env`, [.gitignore](../../.gitignore)
- Evidence:
  - `.gitignore` กัน `.env` และ `.env.*`
  - local `.env` มี `POSTGRES_PASSWORD=...`, `SEED_ADMIN_PASSWORD=Admin@1234`, `Jwt__Key=change-this...`
  - `git ls-files` พบ tracked เฉพาะ `.env.example`
- ความเสี่ยง: เผลอ copy/share/deploy dev secret
- วิธีแก้:
  - rotate ค่า production จริงทั้งหมด
  - ใช้ `/etc/hop/hop-api.env` หรือ secret manager สำหรับ production
  - เก็บ local `.env` ให้ออกนอก workspace หากจำเป็น

### L-02: README ยังระบุ dev credential ตัวอย่าง

- ไฟล์: [README.md](../../README.md)
- Evidence:
  - `README.md:178` มี `Password: Admin@1234`
- ความเสี่ยง: ผู้ดูแลอาจเข้าใจผิดนำไปใช้ production
- วิธีแก้: ทำให้ชัดว่าเป็น local-only และชี้ไป secret env เท่านั้น หรือย้ายไป docs dev setup ที่ไม่ production-facing

### L-03: Announcement attachment อนุญาต `application/octet-stream` สำหรับบางไฟล์

- ไฟล์: [backend/Hop.Api/Services/AnnouncementMediaStorageService.cs](../../backend/Hop.Api/Services/AnnouncementMediaStorageService.cs)
- Evidence:
  - `AnnouncementMediaStorageService.cs:295-303` validate content type จาก allowlist
- ความเสี่ยง: browser/OS บางตัวส่ง octet-stream ทำให้ตรวจ MIME อ่อนลง
- วิธีแก้: เพิ่ม magic/file signature validation สำหรับ Office/ZIP/PDF

### L-04: Correlation ID อนุญาต `/` ใน header

- ไฟล์: [backend/Hop.Api/Middleware/CorrelationIdMiddleware.cs](../../backend/Hop.Api/Middleware/CorrelationIdMiddleware.cs)
- Evidence:
  - `CorrelationIdMiddleware.cs:31-37` allow letters/digits และ `- _ . : /`
- ความเสี่ยง: ไม่ใช่ CRLF injection แต่ทำให้ log/search format messy ได้
- วิธีแก้: จำกัดเป็น RFC-safe เช่น `[A-Za-z0-9._-]`

## Informational Findings / Positive Controls

### I-01: LINE webhook security pattern ถูกต้องในภาพรวม

- ไฟล์: [backend/Hop.Api/Controllers/LineWebhookController.cs](../../backend/Hop.Api/Controllers/LineWebhookController.cs)
- Evidence:
  - endpoint เป็น anonymous เฉพาะ webhook
  - ตรวจ `X-Line-Signature` ด้วย raw body + HMAC-SHA256
  - invalid/missing signature ตอบ 401
- สรุป: ถูกต้องที่ webhook ไม่ใช้ JWT แต่ยังใช้ signature

### I-02: Global exception middleware ปิด stack trace ใน production

- ไฟล์: [backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs](../../backend/Hop.Api/Middleware/GlobalExceptionMiddleware.cs)
- Evidence:
  - production ส่ง `SafeErrorResponse` พร้อม referenceId
  - development เท่านั้นที่ส่ง detail

### I-03: SQL injection risk ต่ำจาก code ที่ตรวจ

- Evidence:
  - พบ `ExecuteSqlRawAsync("SELECT 1")` ใน health/diagnostics เท่านั้น
  - business query ส่วนใหญ่ใช้ EF Core LINQ

### I-04: Frontend ไม่พบ unsafe HTML API ใน `frontend/src`

- Evidence:
  - `rg` ไม่พบ `dangerouslySetInnerHTML`, `innerHTML`, `eval(`, `new Function`, `Function(`

### I-05: npm production dependency audit ผ่าน

- Evidence:
  - `npm audit --omit=dev --audit-level=moderate` ใน `frontend` ได้ `found 0 vulnerabilities`

## OWASP Top 10 Mapping

| OWASP | Status | Relevant Findings |
|---|---|---|
| A01 Broken Access Control | Partial | M-01, M-02, M-06 |
| A02 Cryptographic Failures | Partial | M-03, M-04, L-01 |
| A03 Injection | Low observed risk | I-03, M-09 |
| A04 Insecure Design | Partial | M-01, M-08 |
| A05 Security Misconfiguration | Partial | H-04, M-02, M-08 |
| A06 Vulnerable Components | Risk | H-01 |
| A07 Identification and Authentication Failures | Partial | M-03, M-04, no MFA |
| A08 Software and Data Integrity Failures | Partial | H-01, upload validation findings |
| A09 Security Logging and Monitoring Failures | Partial | M-08, audit retention should be tested |
| A10 SSRF | No direct issue found | LINE calls use configured LINE API path; keep config locked down |

## Security Readiness Summary

| Area | Status | Priority |
|---|---|---|
| JWT validation | Done | P2 hardening for shorter TTL/token version |
| Refresh rotation | Partial | P1 hash refresh tokens |
| Permission-based API | Partial | P1 fallback policy |
| LINE webhook | Done | P2 exact CSRF exclude path |
| Upload security | Partial | P0/P1 unify validators |
| Exception handling | Done | P2 verify production environment |
| Secret management | Partial | P1 rotate real secrets, keep local env safe |
| Dependencies | Risk | P0/P1 update vulnerable transitive packages |
| Nginx hardening | Partial | P0/P1 for bare-metal production |
| Docker hardening | Partial | P1 non-root containers |

## Recommended Next Actions

1. P0: Update vulnerable NuGet dependency graph and rerun vulnerability audit
2. P0: Harden leave attachment/profile image upload with shared validator and ClamAV policy
3. P0: Align bare-metal nginx hardening with docker nginx, especially CSP/HSTS/server_tokens/rate limit/TLS
4. P1: Make production frontend cookie-token mode fail-safe
5. P1: Add fallback authorization policy and explicit `[AllowAnonymous]` only where intended
6. P1: Hash refresh tokens in database
7. P1: Add forwarded headers configuration for nginx reverse proxy
8. P2: Add MFA readiness and stronger password history/expiration policy if required by hospital policy

